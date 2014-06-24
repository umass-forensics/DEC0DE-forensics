/**
 * Copyright (C) 2013 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Dec0de.UI.PostProcess;

namespace Dec0de.UI.DecodeFilters
{
    /*
     * Class for filtering records prior to showing them in the results.
     */

    public class Filters
    {
        /*
         * The Filter class represents an individual filter.
         */

        private class Filter
        {
            // When testing a filter it can return 1 of 3 results:
            //   REJECT: discard the record
            //   ACCEPT: accept the record
            //   NOTAPPLICABLE: no action; move on to the next filter.
            public enum FilterResult
            {
                REJECT,
                ACCEPT,
                NOTAPPLICABLE
            };

            public enum FieldType
            {
                TIMESTAMP,
                PHONENUM,
                NAME,
                SMSTEXT,
                CLTYPE
            };

            // The types of filters that we support.
            public enum FilterType
            {
                AGE,            // timestamp not older than a value
                HASCHAR         // string contains a character
            };

            private bool reject;
            private FieldType fieldType;    // What field(s) does this filter look at?
            private FilterType filterType;
            private DateTime earliestTime;
            private char[] chars = null;

            /*
             * Constructor per type of filter.
             */

            /// <summary>
            /// Simple age filter constructor.
            /// </summary>
            /// <param name="days"></param>
            public Filter(int days)
            {
                this.filterType = FilterType.AGE;
                this.reject = true;
                this.fieldType = FieldType.TIMESTAMP;
                this.earliestTime = DateTime.UtcNow.AddDays(-days);
            }

            /// <summary>
            /// Simple character in text filter constructor.
            /// </summary>
            /// <param name="chars"></param>
            public Filter(string chars)
            {
                this.filterType = FilterType.HASCHAR;
                this.reject = true;
                this.fieldType = FieldType.NAME;
                this.chars = chars.ToCharArray();
            }

            /// <summary>
            /// For a call log: does this filter affect the record?
            /// </summary>
            /// <param name="pcl"></param>
            /// <returns></returns>
            public FilterResult IsFiltered(ProcessedCallLog pcl)
            {
                switch (fieldType) {
                    case FieldType.TIMESTAMP:
                        if (filterType == FilterType.AGE) {
                            return AgeCheck(pcl.MetaData.TimeStamp);
                        }
                        break;
                    case FieldType.PHONENUM:
                        break;
                    case FieldType.NAME:
                        break;
                    case FieldType.CLTYPE:
                        break;
                    default:
                        break;
                }
                return FilterResult.NOTAPPLICABLE;
            }

            /// <summary>
            /// For an address book entry: does this filter affect it?
            /// </summary>
            /// <param name="pab"></param>
            /// <returns></returns>
            public FilterResult IsFiltered(ProcessedAddressBook pab)
            {
                switch (fieldType) {
                    case FieldType.PHONENUM:
                        break;
                    case FieldType.NAME:
                        if (filterType == FilterType.HASCHAR) {
                            return CharsCheck(pab.MetaData.Name);
                        }
                        break;
                    default:
                        break;
                }
                return FilterResult.NOTAPPLICABLE;
            }

            /// <summary>
            /// For an SMS record: does this filter affect it?
            /// </summary>
            /// <param name="psms"></param>
            /// <returns></returns>
            public FilterResult IsFiltered(ProcessedSms psms)
            {
                switch (fieldType) {
                    case FieldType.TIMESTAMP:
                        if (filterType == FilterType.AGE) {
                            return AgeCheck(psms.MetaData.TimeStamp);
                        }
                        break;
                    case FieldType.PHONENUM:
                        break;
                    case FieldType.SMSTEXT:
                        break;
                    default:
                        break;
                }
                return FilterResult.NOTAPPLICABLE;
            }

            /// <summary>
            /// Called when checking a timestamp age.
            /// </summary>
            /// <param name="timestamp"></param>
            /// <returns></returns>
            private FilterResult AgeCheck(DateTime? timestamp)
            {
                if (timestamp == null) {
                    return FilterResult.NOTAPPLICABLE;
                }
                if (timestamp < earliestTime) {
                    return FilterResult.REJECT;
                }
                return FilterResult.NOTAPPLICABLE;
            }

            /// <summary>
            /// Called when checking to see if a character exists in a string.
            /// </summary>
            /// <param name="field"></param>
            /// <returns></returns>
            private FilterResult CharsCheck(string field)
            {
                if (field == null) {
                    return FilterResult.NOTAPPLICABLE;
                }
                if (field.IndexOfAny(chars) >= 0) {
                    return FilterResult.REJECT;
                }
                return FilterResult.NOTAPPLICABLE;
            }

        }

        // We keep a list of filters per record type. The lists are intended
        // to be ordered.
        private List<Filter> clFilters = new List<Filter>();
        private List<Filter> abFilters = new List<Filter>();
        private List<Filter> smsFilters = new List<Filter>();

        public Filters(XDocument xDoc)
        {
            CreateFromXdoc(xDoc);
        }

        /// <summary>
        /// Given an XDocument of a filter configuration file, generate the set of
        /// filters.
        /// </summary>
        /// <param name="xDoc"></param>
        private void CreateFromXdoc(XDocument xDoc)
        {
            if (xDoc == null) {
                return;
            }
            XElement xFilters = null;
            try {
                xFilters = xDoc.Element("decodefilters").Element("filters");
            } catch {
            }
            if (xFilters == null) {
                return;
            }
            // Currently we only support simple filters (that are defined at most
            // once).
            try {
                XElement xSimple = xFilters.Element("simple");
                if (xSimple == null) {
                    return;
                }
                int days = 0;
                bool enabled = false;
                if (GetTimestampValue(xSimple.Element("calllogtime"), out days, out enabled)) {
                    if (enabled) {
                        clFilters.Add(new Filter(days));
                    }
                }
                if (GetTimestampValue(xSimple.Element("smstime"), out days, out enabled)) {
                    if (enabled) {
                        smsFilters.Add(new Filter(days));
                    }
                }
                string str = null;
                if (GetCharsValue(xSimple.Element("adrbookchars"), out str, out enabled)) {
                    if (enabled) {
                        abFilters.Add(new Filter(str));
                    }
                }
            } catch {
            }
        }

        /// <summary>
        /// Read a timestamp from an element.
        /// </summary>
        /// <param name="xEl"></param>
        /// <param name="days"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public static bool GetTimestampValue(XElement xEl, out int days, out bool enabled)
        {
            days = 0;
            enabled = false;
            try {
                if (xEl == null) {
                    return false;
                }
                days = Math.Abs(int.Parse(xEl.Attribute("days").Value));
                enabled = bool.Parse(xEl.Attribute("enabled").Value);
                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Read a string of characters from an element.
        /// </summary>
        /// <param name="xEl"></param>
        /// <param name="str"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public static bool GetCharsValue(XElement xEl, out string str, out bool enabled)
        {
            str = null;
            enabled = false;
            try {
                if (xEl == null) {
                    return false;
                }
                str = xEl.Attribute("chars").Value;
                enabled = bool.Parse(xEl.Attribute("enabled").Value);
                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Cycle through the list of call log filters to determine if the
        /// supplied call log should be filtered.
        /// </summary>
        /// <param name="pcl"></param>
        /// <returns>True if it should be filtered, false if it should be kept.</returns>
        public bool IsFiltered(ProcessedCallLog pcl)
        {
            foreach (Filter filter in clFilters) {
                switch (filter.IsFiltered(pcl)) {
                    case Filter.FilterResult.NOTAPPLICABLE:
                        break;
                    case Filter.FilterResult.ACCEPT:
                        return false;
                    case Filter.FilterResult.REJECT:
                        return true;
                }
            }
            // Default is to keep the record.
            return false;
        }

        /// <summary>
        /// Cycle through the list of address book filters to determine if the
        /// supplied entry should be filtered. 
        /// </summary>
        /// <param name="pab"></param>
        /// <returns>True if it should be filtered, false if it should be kept.</returns>
        public bool IsFiltered(ProcessedAddressBook pab)
        {
            foreach (Filter filter in abFilters) {
                switch (filter.IsFiltered(pab)) {
                    case Filter.FilterResult.NOTAPPLICABLE:
                        break;
                    case Filter.FilterResult.ACCEPT:
                        return false;
                    case Filter.FilterResult.REJECT:
                        return true;
                }
            }
            // Default is to keep the record.
            return false;
        }

        /// <summary>
        /// Cycle through the list of SMS filters to determine if the supplied
        /// SMS record should be filtered.  
        /// </summary>
        /// <param name="psms"></param>
        /// <returns>True if it should be filtered, false if it should be kept.</returns>
        public bool IsFiltered(ProcessedSms psms)
        {
            foreach (Filter filter in smsFilters) {
                switch (filter.IsFiltered(psms)) {
                    case Filter.FilterResult.NOTAPPLICABLE:
                        break;
                    case Filter.FilterResult.ACCEPT:
                        return false;
                    case Filter.FilterResult.REJECT:
                        return true;
                }
            }
            // Default is to keep the record.
            return false;
        }
    }
}
