/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dec0de.Bll.AnswerLoader;

namespace Dec0de.UI.PostProcess
{
    /*
     * Given the Viterbi results, inspect the fields attempting to indentify
     * some that seem particularly bad, and some that seem particularly good.
     * 
     * This class does remove some suspect entries, but it's purpose right now
     * is as a placeholder for eventually doing some post-viterbi analysis of
     * the data.
     * 
     * NOTE:
     * A lot of the info collected here is not used and may not be relevant.
     * 
     * TODO: Clean this up.
     */



    public class PostProcessor
    {
        /// <summary>
        /// List of image blocks located in memory.
        /// </summary>
        public readonly List<ImageBlock> imageBlocks; 
        /// <summary>
        /// List of call log records inferred from record level run of Viterbi
        /// </summary>
        public List<ProcessedCallLog> callLogFields;
        /// <summary>
        /// List of address book records inferred from record level run of Viterbi
        /// </summary>
        public List<ProcessedAddressBook> addressBookFields;
        /// <summary>
        /// List of SMS records inferred from record level run of Viterbis
        /// </summary>
        public List<ProcessedSms> smsFields;
        private List<MetaField> metaCallLogs;
        private List<MetaField> metaAddressBook;
        private List<MetaField> metaSms;
        private Dictionary<string, int> adrBookNumbersFull = new Dictionary<string, int>();
        private Dictionary<string, int> adrBookNumbers7 = new Dictionary<string, int>();
        private Dictionary<string, int> callLogNumbersFull = new Dictionary<string, int>();
        private Dictionary<string, int> callLogNumbers7 = new Dictionary<string, int>();
        private Dictionary<string, List<ProcessedCallLog>> callLogSevenDigits =
            new Dictionary<string, List<ProcessedCallLog>>();
        private Dictionary<string, List<ProcessedAddressBook>> adrBookSevenDigits =
            new Dictionary<string, List<ProcessedAddressBook>>();
        private Dictionary<string, List<ProcessedSms>> smsSevenDigits =
            new Dictionary<string, List<ProcessedSms>>();
        private Dictionary<string, int> sms1NumbersFull = new Dictionary<string, int>();
        private Dictionary<string, int> sms1Numbers7 = new Dictionary<string, int>();
        private Dictionary<string, int> sms2NumbersFull = new Dictionary<string, int>();
        private Dictionary<string, int> sms2Numbers7 = new Dictionary<string, int>();
        private Dictionary<string, List<ProcessedCallLog>> callLogNames =
            new Dictionary<string, List<ProcessedCallLog>>();
        private Dictionary<string, List<ProcessedAddressBook>> adrBookNames =
            new Dictionary<string, List<ProcessedAddressBook>>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="callLogs">List of call logs inferred by the record level run of Viterbi</param>
        /// <param name="addressBookEntries">List of address book entries inferred by the record level run of Viterbi</param>
        /// <param name="sms">List of SMSs inferred by the record level run of Viterbi</param>
        public PostProcessor(List<MetaField> callLogs, List<MetaField> addressBookEntries,
            List<MetaField> sms, List<ImageBlock> imageBlocks)
        {
            this.metaCallLogs = callLogs;
            this.metaAddressBook = addressBookEntries;
            this.metaSms = sms;
            this.imageBlocks = imageBlocks;
            DateTime local = DateTime.Now;
            DateTime utc = local.ToUniversalTime();
            DateTime max = (local > utc) ? local : utc;
            DateTime lowerDate = new DateTime(1991, 1, 1);
            DateTime upperDate = max.AddDays(1);
            this.callLogFields = GetCallLogs(callLogs, true, lowerDate, upperDate);
            this.addressBookFields = GetAdrBookEntries(addressBookEntries);
            this.smsFields = GetSmsEntries(sms, true, lowerDate, upperDate);
        }
        
        /// <summary>
        /// Takes the list of call log metafields and generates a list of ProcessedCallLog
        /// objects. Eliminates those whose timestamps are clearly out of range.
        /// </summary>
        /// <param name="metaList">List of call log records to be filtered.</param>
        /// <param name="pruneTimestamp">Whether or not to use the timestamp of a call log record as a parameter for filtering.</param>
        /// <param name="lower">Lower limit for timestamps.</param>
        /// <param name="upper">Upper limit for timestamps.</param>
        /// <returns>A list of processed call log records.</returns>
        private List<ProcessedCallLog> GetCallLogs(List<MetaField> metaList, bool pruneTimestamp, DateTime lower, DateTime upper)
        {
            List<ProcessedCallLog> pList = new List<ProcessedCallLog>();
            foreach (MetaCallLog mf in metaList) {
                if (!mf.TimeStamp.HasValue) continue;
                if (pruneTimestamp && ((mf.TimeStamp.Value < lower) || (mf.TimeStamp.Value > upper))) {
                    continue;
                }
                pList.Add(new ProcessedCallLog(mf));
            }
            // Can we get rid of any duplicate call logs?
            pList = EliminateDupCallLogs(pList);
            // Save numbers and names for later cross referencing.
            foreach (ProcessedCallLog pcl  in pList) {
                MetaCallLog mf = (MetaCallLog)pcl.MetaData;
                AddNumber(callLogNumbersFull, mf.Number);
                AddNumber(callLogNumbers7, mf.SevenDigit);
                AddRecordToDict(callLogNames, mf.Name, pcl);
                AddRecordToDict(callLogSevenDigits, mf.SevenDigit, pcl);
            }
            return pList;
        }

        /// <summary>
        /// Takes the list of address book metafields and generates a list of
        /// ProcessedAddressBook objects.
        /// </summary>
        /// <param name="metaList">List of address book records to be filtered.</param>
        /// <returns>A list of processed address book records.</returns>
        private List<ProcessedAddressBook> GetAdrBookEntries(List<MetaField> metaList)
        {
            List<ProcessedAddressBook> pList = new List<ProcessedAddressBook>();
            foreach (MetaAddressBookEntry mf in metaList) {
                ProcessedAddressBook pab = new ProcessedAddressBook(mf);
                // Save numbers and names for later cross referencing.
                AddNumber(adrBookNumbersFull, mf.Number);
                AddNumber(adrBookNumbers7, mf.SevenDigit);
                AddRecordToDict(adrBookNames, mf.Name, pab);
                AddRecordToDict(adrBookSevenDigits, mf.SevenDigit, pab);
                pList.Add(pab);
            }
            return pList;
        }

        /// <summary>
        /// Takes the list of SMS metafields and generates a list of ProcessedSms
        /// objects. Eliminates those whose timestamps seem to clearly be out of range.
        /// </summary>
        /// <param name="metaList">List of SMS records to be filtered.</param>
        /// <param name="pruneTimestamp">Whether or not to use the timestamp of an SMS as a parameter for filtering.</param>
        /// <param name="lower">Lower limit for timestamps.</param>
        /// <param name="upper">Upper limit for timestamps.</param>
        /// <returns>A list of processed SMS records.</returns>
        private List<ProcessedSms> GetSmsEntries(List<MetaField> metaList, bool pruneTimestamp, DateTime lower, DateTime upper)
        {
            List<ProcessedSms> pList = new List<ProcessedSms>();
            foreach (MetaSms mf in metaList) {
                // TODO: Can we assume a timestamp?
                if (!mf.TimeStamp.HasValue) continue;
                if (pruneTimestamp && ((mf.TimeStamp.Value < lower) || (mf.TimeStamp.Value > upper))) {
                    continue;
                }
                ProcessedSms psms = new ProcessedSms(mf);
                // Save numbers for later cross referencing.
                AddNumber(sms1NumbersFull, mf.Number);
                AddNumber(sms1Numbers7, mf.SevenDigit);
                AddNumber(sms2NumbersFull, mf.Number2);
                AddNumber(sms2Numbers7, mf.SevenDigit2);
                AddRecordToDict(smsSevenDigits, mf.SevenDigit, psms);
                if (mf.SevenDigit != mf.SevenDigit2) {
                    AddRecordToDict(smsSevenDigits, mf.SevenDigit2, psms);
                } else {
                    try {
                        if (mf.Number != mf.Number2) {
                            AddRecordToDict(smsSevenDigits, mf.SevenDigit2, psms);
                        }
                    } catch {
                    }
                }
                pList.Add(psms);
            }
            return pList;
        }


        /// <summary>
        /// Attempts to eliminate duplicate call logs by timestamp and number.
        /// It chooses to keep the least ambiguous call log.
        /// </summary>
        private List<ProcessedCallLog> EliminateDupCallLogs(List<ProcessedCallLog> callLogs)
        {
            HashSet<int> remove = new HashSet<int>();
            HashSet<string> completed = new HashSet<string>();
            foreach (ProcessedCallLog pcl in callLogs) {
                try {
                    if (!pcl.MetaData.TimeStamp.HasValue) continue;
                    if (remove.Contains(pcl.Id)) continue;
                    string hash = String.Format("{0}:::{1}",
                                                ((DateTime) pcl.MetaData.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss"),
                                                pcl.MetaData.Number);
                    if (completed.Contains(hash)) continue;
                    List<ProcessedCallLog> dups = new List<ProcessedCallLog>();
                    dups.Add(pcl);
                    ProcessedCallLog alpha = pcl;
                    int alphaRank = RankCallLogType(pcl.MetaData.Type);
                    foreach (ProcessedCallLog pcl2 in callLogs) {
                        if (!pcl2.MetaData.TimeStamp.HasValue) continue;
                        if (pcl.Id == pcl2.Id) continue;
                        string hash2 = String.Format("{0}:::{1}",
                                                     ((DateTime) pcl2.MetaData.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss"),
                                                     pcl2.MetaData.Number);
                        if (hash != hash2) continue;
                        dups.Add(pcl2);
                        int r = RankCallLogType(pcl2.MetaData.Type);
                        if (r < alphaRank) {
                            alpha = pcl2;
                            alphaRank = r;
                        }
                    }
                    completed.Add(hash);
                    if (dups.Count <= 1) continue;
                    for (int n = 1; n < dups.Count; n++) {
                        if (RankCallLogType(dups[n].MetaData.Type) > alphaRank) {
                            remove.Add(dups[n].Id);
                        }
                    }
                } catch {
                }
            }
            if (remove.Count == 0) {
                return callLogs;
            }
            List<ProcessedCallLog> newCallLogs = new List<ProcessedCallLog>();
            foreach (ProcessedCallLog pcl in callLogs) {
                if (remove.Contains(pcl.Id)) continue;
                newCallLogs.Add(pcl);
            }
            return newCallLogs;
        }

        /// <summary>
        /// Gives a simple 0 (good) or 1 (ambiguous) rank to the call log type.
        /// </summary>
        /// <param name="typ">The category of call log record, like received, dialed or missed.</param>
        /// <returns>Whether or not the call log record seems ambiguous.</returns>    
        private int RankCallLogType(string typ)
        {
            if (String.IsNullOrEmpty(typ)) {
                return 1;
            }
            if ((typ == MetaField.DEFAULT_STRING) || (typ == "???")) {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// This is where the inspection of fields begin.
        /// 
        /// Note: We use redundant, serial loops in the routines below.
        /// This could certainly be optimized.
        /// 
        /// Note: Much of this is currently not used.
        /// </summary>
        public void Process()
        {
            //EliminateByDate();
            IdentifySuspectPhoneNumbersAndNameBadness(false);
            CalculateProximity();
            CalculateTimeDistances();
            CrosscheckCallAndAdressNames();
            CrossCheckNumbers();
            GetSmsNumberInfo();
            RemoveDuplicates();
            //EliminateDupAddresses();
        }

        /// <summary>
        /// Adds a number to a dictionary. Maintains a count of the times the
        /// number has been seen.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="number"></param>
        private void AddNumber(Dictionary<string, int> dict, string number)
        {
            if ((number == null) || (number == MetaField.DEFAULT_STRING)) {
                return;
            }
            if (!dict.ContainsKey(number)) {
                dict.Add(number, 1);
            }
            else {
                dict[number] = dict[number] + 1;
            }
        }

        /// <summary>
        /// Decrements the number count. Removes it from the dictionary when
        /// the count gets to zero.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="number"></param>
        private void RemNumber(Dictionary<string, int> dict, string number)
        {
            if ((number == null) || (number == MetaField.DEFAULT_STRING)) {
                return;
            }
            if (!dict.ContainsKey(number)) {
                return;
            }
            int n = dict[number] - 1;
            if (n == 0) {
                dict.Remove(number);
            } else {
                dict[number] = n;
            }
        }

        /// <summary>
        /// Add call log to a dictionary using a key, e.g., number or name. Use a list
        /// for key collisions.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="pcl"></param>
        private void AddRecordToDict(Dictionary<string, List<ProcessedCallLog>> dict, string key, ProcessedCallLog pcl)
        {
            if ((key == null) || (key == MetaField.DEFAULT_STRING)) {
                return;
            }
            if (!dict.ContainsKey(key)) {
                List<ProcessedCallLog> list = new List<ProcessedCallLog>();
                list.Add(pcl);
                dict.Add(key, list);
            } else {
                dict[key].Add(pcl);
            }
        }

        /// <summary>
        /// Add an address book to a dictionary using a key, e.g., number or name. Use a list
        /// for key collisions.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="pab"></param>
        private void AddRecordToDict(Dictionary<string, List<ProcessedAddressBook>> dict, string key, ProcessedAddressBook pab)
        {
            if (!dict.ContainsKey(key)) {
                List<ProcessedAddressBook> list = new List<ProcessedAddressBook>();
                list.Add(pab);
                dict.Add(key, list);
            } else {
                dict[key].Add(pab);
            }
        }

        /// <summary>
        /// Add an SMS entry to a dictionary using a key, e.g., number. Use a list
        /// for key collisions.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="psms"></param>
        private void AddRecordToDict(Dictionary<string, List<ProcessedSms>> dict, string key, ProcessedSms psms)
        {
            if (!dict.ContainsKey(key)) {
                List<ProcessedSms> list = new List<ProcessedSms>();
                list.Add(psms);
                dict.Add(key, list);
            } else {
                dict[key].Add(psms);
            }
        }


        /// <summary>
        /// Remove fields if their dates are out of range.
        /// </summary>
        /// <param name="minDate"></param>
        /// <param name="maxDate"></param>
        private void EliminateByDate(DateTime minDate, DateTime maxDate)
        {
            // Start will call logs.
            int n = 0;
            while (n < callLogFields.Count) {
                ProcessedCallLog pcl = callLogFields[n];
                if ((pcl.MetaData.TimeStamp > maxDate) || (pcl.MetaData.TimeStamp < minDate)) {
                    RemNumber(callLogNumbersFull, pcl.MetaData.Number);
                    RemNumber(callLogNumbers7, pcl.MetaData.SevenDigit);
                    callLogFields.RemoveAt(n);
                }
                else {
                    n++;
                }
            }
            // Now do the SMS entries.
            n = 0;
            while (n < smsFields.Count) {
                ProcessedSms sms = smsFields[n];
                if ((sms.MetaData.TimeStamp > maxDate) || (sms.MetaData.TimeStamp < minDate)) {
                    RemNumber(sms1NumbersFull, sms.MetaData.Number);
                    RemNumber(sms1Numbers7, sms.MetaData.SevenDigit);
                    RemNumber(sms2NumbersFull, sms.MetaData.Number2);
                    RemNumber(sms2Numbers7, sms.MetaData.SevenDigit2);
                    smsFields.RemoveAt(n);
                } else {
                    n++;
                }
            }
        }

        /// <summary>
        /// Overload for eliminating dates. Uses current time + 1 day and January 1, 1991.
        /// </summary>
        private void EliminateByDate()
        {
            DateTime local = DateTime.Now;
            DateTime utc = local.ToUniversalTime();
            DateTime max = (local > utc) ? local : utc;
            EliminateByDate(new DateTime(1991, 1, 1), max.AddDays(1));
        }

        /// <summary>
        /// Flags entries with phone numbers that don't meet certain criteria. Also
        /// calculates the "badness" of a name.
        /// 
        /// Optionally, can remove suspect phone nunbers.
        /// </summary>
        private void IdentifySuspectPhoneNumbersAndNameBadness(bool removeSuspectNums)
        {
            List<ProcessedCallLog> newCallLogFields = new List<ProcessedCallLog>();
            foreach (ProcessedCallLog pcl in callLogFields) {
                pcl.SuspectPhoneNumber = FieldUtils.SuspectPhoneNumber(pcl.MetaData.Number, false);
                if (pcl.SuspectPhoneNumber && removeSuspectNums) continue;
                pcl.NameBadness = FieldUtils.ScoreNameBadness(pcl.MetaData.Name);
                newCallLogFields.Add(pcl);
            }
            callLogFields = newCallLogFields;
            List<ProcessedAddressBook> newAddressBookFileds = new List<ProcessedAddressBook>();
            foreach (ProcessedAddressBook pab in addressBookFields) {
                pab.SuspectPhoneNumber = FieldUtils.SuspectPhoneNumber(pab.MetaData.Number, false);
                if (pab.SuspectPhoneNumber && removeSuspectNums) continue;
                pab.NameBadness = FieldUtils.ScoreNameBadness(pab.MetaData.Name);
                newAddressBookFileds.Add(pab);
            }
            addressBookFields = newAddressBookFileds;
            List<ProcessedSms> newSmsFields = new List<ProcessedSms>();
            foreach (ProcessedSms psms in this.smsFields) {
                psms.SuspectPhoneNumber = FieldUtils.SuspectPhoneNumber(psms.MetaData.Number, true, true);
                if (psms.SuspectPhoneNumber && removeSuspectNums) continue;
                if (!String.IsNullOrEmpty(psms.MetaData.Number2) && !MetaField.DEFAULT_STRING.Equals(psms.MetaData.Number2)) {
                    psms.SuspectPhoneNumber2 = FieldUtils.SuspectPhoneNumber(psms.MetaData.Number2, true, true);
                    if (psms.SuspectPhoneNumber2 && removeSuspectNums) continue;
                }
                newSmsFields.Add(psms);
            }
            smsFields = newSmsFields;
        }

        /// <summary>
        /// Calculate the proximity of fields to other same-type fields. This is based
        /// on the offset of the field within the memory file.
        /// </summary>
        private void CalculateProximity()
        {
            List<long> offsets = new List<long>();
            foreach (ProcessedCallLog pcl in this.callLogFields) {
                if (pcl.SuspectPhoneNumber || (pcl.NameBadness >= 2)) {
                    // Don't use entries with suspect numbers or names in the calculation.
                    continue;
                }
                offsets.Add(pcl.MetaData.ProximityOffset);
            }
            offsets.Sort();
            foreach (ProcessedCallLog pcl in this.callLogFields) {
                FindProximityDistances(pcl.MetaData.ProximityOffset, offsets, pcl, ProcessedInformation.DISTANCE_RANGE_CALLLOG);
            }
            offsets = new List<long>();
            foreach (ProcessedAddressBook pab in this.addressBookFields) {
                if (pab.SuspectPhoneNumber || (pab.NameBadness >= 2)) {
                    // Don't use entries with suspect numbers or names in the calculation.
                    continue;
                }
                offsets.Add(pab.MetaData.ProximityOffset);
            }
            offsets.Sort();
            foreach (ProcessedAddressBook pab in this.addressBookFields) {
                FindProximityDistances(pab.MetaData.ProximityOffset, offsets, pab, ProcessedInformation.DISTANCE_RANGE_ADR);
            }
            offsets = new List<long>();
            foreach (ProcessedSms sms in this.smsFields) {
                if (sms.SuspectPhoneNumber) {
                    // Don't use entries with suspect numbers in the calculation.
                    // We currently only look at the first number, not the second.
                    continue;
                }
                offsets.Add(sms.MetaData.ProximityOffset);
            }
            offsets.Sort();
            foreach (ProcessedSms sms in this.smsFields) {
                FindProximityDistances(sms.MetaData.ProximityOffset, offsets, sms, ProcessedInformation.DISTANCE_RANGE_SMS);
            }
        }

        /// <summary>
        /// Calculate the nearest other entry of the same type in memory. Count
        /// how many entries of the same type are within a defined range.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="offsets"></param>
        /// <param name="pi"></param>
        /// <param name="delta"></param>
        private void FindProximityDistances(long offset, List<long> offsets, ProcessedInformation pi, int delta)
        {
            pi.MinMemoryDistance = long.MaxValue;
            pi.MemProximityCount = 0;
            if (offsets.Count == 0) {
                // Everything was suspect ...
                return;
            }
            for (int n = 0; n < offsets.Count; n++) {
                if (offsets[n] == offset) {
                    // Skip self.
                    continue;
                }
                long diff;
                if (offsets[n] < offset) {
                    diff = offset - offsets[n];
                } else {
                    diff = offsets[n] - offset;
                }
                if (diff < pi.MinMemoryDistance) pi.MinMemoryDistance = diff;
                if (diff <= delta) {
                    pi.MemProximityCount++;
                } else if (offsets[n] > offset) {
                    return;
                }
            }
        }

        /// <summary>
        /// Determines the distance in physical memory (as defined by file offset)
        /// of timestamp fields.
        /// </summary>
        private void CalculateTimeDistances()
        {
            List<DateTime> timestamps = new List<DateTime>();
            foreach (ProcessedCallLog pcl in this.callLogFields) {
                timestamps.Add(pcl.MetaData.TimeStamp.Value);
            }
            timestamps.Sort();
            foreach (ProcessedCallLog pcl in this.callLogFields) {
                FindTimestamptDistances(pcl.MetaData.TimeStamp.Value, timestamps, pcl);
            }
            timestamps = new List<DateTime>();
            foreach (ProcessedSms sms in this.smsFields) {
                timestamps.Add(sms.MetaData.TimeStamp.Value);
            }
            timestamps.Sort();
            foreach (ProcessedSms sms in this.smsFields) {
                FindTimestamptDistances(sms.MetaData.TimeStamp.Value, timestamps, sms);
            }
        }

        private void FindTimestamptDistances(DateTime timestamp, List<DateTime> timestamps, ProcessedInformation pi)
        {
            for (int n = 0; n < timestamps.Count; n++) {
                if (timestamps[n] == timestamp) {
                    double diff = int.MaxValue;
                    if (n > 0) {
                        TimeSpan ts = timestamp - timestamps[n - 1];
                        diff = ts.TotalDays;
                    }
                    if (n < (timestamps.Count - 1)) {
                        TimeSpan ts = timestamps[n + 1] - timestamp;
                        double d = ts.TotalDays;
                        if (d < diff) diff = d;
                    }
                    pi.MinDayDistance = diff;
                    int proximityCount = 0;
                    for (int i = n - 1; i > 0; i--) {
                        if (timestamps[i].AddDays(ProcessedInformation.DISTANCE_DAYS) >= timestamp) {
                            proximityCount++;
                        } else {
                            break;
                        }
                    }
                    for (int i = n + 1; i < timestamps.Count; i++) {
                        if (timestamps[i] <= timestamp.AddDays(ProcessedInformation.DISTANCE_DAYS)) {
                            proximityCount++;
                        } else {
                            break;
                        }
                    }
                    pi.DayProximityCount = proximityCount;
                    break;
                }
            }
        }

        private void CrosscheckCallAndAdressNames()
        {
            foreach (ProcessedCallLog pcl in this.callLogFields) {
                if ((pcl.MetaData.Name == null) || (pcl.MetaData.Name == MetaField.DEFAULT_STRING)) {
                    continue;
                }
                List<ProcessedAddressBook> list;
                if (!adrBookNames.TryGetValue(pcl.MetaData.Name, out list)) {
                    continue;
                }
                foreach (ProcessedAddressBook pab in list) {
                    if (pab.InCallLog && pcl.InAddressBook) continue;
                    if (CompareNumbers(pcl.MetaData, pab.MetaData)) {
                        pcl.InAddressBook = true;
                        pab.InCallLog = true;
                        pcl.NumberInAdrBook = true;
                        pab.NumberInCallLog = true;
                    }
                }
            }
            foreach (ProcessedAddressBook pab in this.addressBookFields) {
                if ((pab.MetaData.Name == null) || (pab.MetaData.Name == MetaField.DEFAULT_STRING)) {
                    continue;
                }
                List<ProcessedCallLog> list;
                if (!callLogNames.TryGetValue(pab.MetaData.Name, out list)) {
                    continue;
                }
                foreach (ProcessedCallLog pcl in list) {
                    if (pcl.InAddressBook && pab.InCallLog) continue;
                    if (CompareNumbers(pcl.MetaData, pab.MetaData)) {
                        pcl.InAddressBook = true;
                        pab.InCallLog = true;
                        pcl.NumberInAdrBook = true;
                        pab.NumberInCallLog = true;
                    }
                }
            }

        }

        private bool CompareNumbers(MetaCallLog mcl, MetaAddressBookEntry mabe)
        {
            if ((mcl.Number != null) && (mabe.Number != null) &&
                (mcl.Number != MetaField.DEFAULT_STRING) && (mcl.Number == mabe.Number)) {
                return true;
            }
            if ((mcl.Number != null) && (mabe.Number != null)) {
                string acode1 = Dec0de.Bll.Utilities.GetAreaCode(mcl.Number);
                string acode2 = Dec0de.Bll.Utilities.GetAreaCode(mabe.Number);
                if ((acode1 != null) && (acode2 != null) && (acode1 != acode2)) {
                    return false;
                }
            }
            if ((mcl.SevenDigit != null) && (mabe.SevenDigit != null) &&
                (mcl.SevenDigit != MetaField.DEFAULT_STRING) && (mcl.SevenDigit == mabe.SevenDigit)) {
                return true;
            }
            return false;
        }

        private bool CompareNumbers(MetaCallLog mcl, MetaSms msms)
        {
            if (CompareNumbers1(mcl, msms)) {
                return true;
            }
            return CompareNumbers2(mcl, msms);
        }

        private bool CompareNumbers(MetaAddressBookEntry mabe, MetaSms msms)
        {
            if (CompareNumbers1(mabe, msms)) {
                return true;
            }
            return CompareNumbers2(mabe, msms);
        }

        private bool CompareNumbers1(MetaCallLog mcl, MetaSms msms)
        {
            if ((mcl.Number != null) && (msms.Number != null) &&
                (mcl.Number != MetaField.DEFAULT_STRING) && (mcl.Number == msms.Number)) {
                return true;
            }
            if ((mcl.Number != null) && (msms.Number != null)) {
                string acode1 = Dec0de.Bll.Utilities.GetAreaCode(mcl.Number);
                string acode2 = Dec0de.Bll.Utilities.GetAreaCode(msms.Number);
                if ((acode1 != null) && (acode2 != null) && (acode1 != acode2)) {
                    return false;
                }
            }
            if ((mcl.SevenDigit != null) && (msms.SevenDigit != null) &&
                (mcl.SevenDigit != MetaField.DEFAULT_STRING) && (mcl.SevenDigit == msms.SevenDigit)) {
                return true;
            }
            return false;
        }

        private bool CompareNumbers2(MetaCallLog mcl, MetaSms msms)
        {
            if ((mcl.Number != null) && (msms.Number2 != null) &&
                (mcl.Number != MetaField.DEFAULT_STRING) && (mcl.Number == msms.Number2)) {
                return true;
            }
            if ((mcl.Number != null) && (msms.Number2 != null)) {
                string acode1 = Dec0de.Bll.Utilities.GetAreaCode(mcl.Number);
                string acode2 = Dec0de.Bll.Utilities.GetAreaCode(msms.Number2);
                if ((acode1 != null) && (acode2 != null) && (acode1 != acode2)) {
                    return false;
                }
            }
            if ((mcl.SevenDigit != null) && (msms.SevenDigit2 != null) &&
                (mcl.SevenDigit != MetaField.DEFAULT_STRING) && (mcl.SevenDigit == msms.SevenDigit2)) {
                return true;
            }
            return false;
        }

        private bool CompareNumbers1(MetaAddressBookEntry mabe, MetaSms msms)
        {
            if ((mabe.Number != null) && (msms.Number != null) &&
                (mabe.Number != MetaField.DEFAULT_STRING) && (mabe.Number == msms.Number)) {
                return true;
            }
            if ((mabe.Number != null) && (msms.Number != null)) {
                string acode1 = Dec0de.Bll.Utilities.GetAreaCode(mabe.Number);
                string acode2 = Dec0de.Bll.Utilities.GetAreaCode(msms.Number);
                if ((acode1 != null) && (acode2 != null) && (acode1 != acode2)) {
                    return false;
                }
            }
            if ((mabe.SevenDigit != null) && (msms.SevenDigit != null) &&
                (mabe.SevenDigit != MetaField.DEFAULT_STRING) && (mabe.SevenDigit == msms.SevenDigit)) {
                return true;
            }
            return false;
        }

        private bool CompareNumbers2(MetaAddressBookEntry mabe, MetaSms msms)
        {
            if ((mabe.Number != null) && (msms.Number2 != null) &&
                (mabe.Number != MetaField.DEFAULT_STRING) && (mabe.Number == msms.Number2)) {
                return true;
            }
            if ((mabe.Number != null) && (msms.Number2 != null)) {
                string acode1 = Dec0de.Bll.Utilities.GetAreaCode(mabe.Number);
                string acode2 = Dec0de.Bll.Utilities.GetAreaCode(msms.Number2);
                if ((acode1 != null) && (acode2 != null) && (acode1 != acode2)) {
                    return false;
                }
            }
            if ((mabe.SevenDigit != null) && (msms.SevenDigit2 != null) &&
                (mabe.SevenDigit != MetaField.DEFAULT_STRING) && (mabe.SevenDigit == msms.SevenDigit2)) {
                return true;
            }
            return false;
        }

        private void GetSmsNumberInfo()
        {
            foreach (ProcessedSms psms in this.smsFields) {
                MetaSms sms = psms.MetaData;
                psms.Number1Frequency =  GetNumberCount(sms.Number, sms1NumbersFull) + GetNumberCount(sms.Number, sms2NumbersFull);
                psms.Number2Frequency = GetNumberCount(sms.Number2, sms1NumbersFull) + GetNumberCount(sms.Number2, sms2NumbersFull);
                psms.SevenDigits1Frequency = GetNumberCount(sms.SevenDigit, sms1Numbers7) + GetNumberCount(sms.SevenDigit, sms2Numbers7);
                psms.SevenDigits2Frequency = GetNumberCount(sms.SevenDigit2, sms1Numbers7) + GetNumberCount(sms.SevenDigit2, sms2Numbers7);
            }
        }

        /// <summary>
        /// Increments a count if the number was previously seen.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        private int GetNumberCount(string number, Dictionary<string, int> dict)
        {
            if ((number == null) || (number == MetaField.DEFAULT_STRING)) {
                return 0;
            }
            int count;
            if (dict.TryGetValue(number, out count)) {
                return count;
            }
            return 0;
        }

        /// <summary>
        /// Attempts to determine if a phone number has been seen in other types
        /// of records.
        /// </summary>
        private void CrossCheckNumbers()
        {
            foreach (ProcessedCallLog pcl in this.callLogFields) {
                if (!pcl.NumberInAdrBook) {
                    List<ProcessedAddressBook> list;
                    if (this.adrBookSevenDigits.TryGetValue(pcl.MetaData.SevenDigit, out list)) {
                        foreach (ProcessedAddressBook pab in list) {
                            if (CompareNumbers(pcl.MetaData, pab.MetaData)) {
                                pcl.NumberInAdrBook = true;
                                pab.NumberInCallLog = true;
                                break;
                            }
                        }
                    }
                }
                if (!pcl.NumberInSms) {
                    List<ProcessedSms> list;
                    if (this.smsSevenDigits.TryGetValue(pcl.MetaData.SevenDigit, out list)) {
                        foreach (ProcessedSms psms in list) {
                            if (CompareNumbers(pcl.MetaData, psms.MetaData)) {
                                pcl.NumberInSms = true;
                                psms.NumberInCallLog = true;
                                break;
                            }
                        }
                    }
                }
            }
            foreach (ProcessedAddressBook pab in this.addressBookFields) {
                if (!pab.NumberInCallLog) {
                    List<ProcessedCallLog> list;
                    if (this.callLogSevenDigits.TryGetValue(pab.MetaData.SevenDigit, out list)) {
                        foreach (ProcessedCallLog pcl in list) {
                            if (CompareNumbers(pcl.MetaData, pab.MetaData)) {
                                pab.NumberInCallLog = true;
                                pcl.NumberInAdrBook = true;
                                break;
                            }
                        }
                    }
                }
                if (!pab.NumberInSms) {
                    List<ProcessedSms> list;
                    if (this.smsSevenDigits.TryGetValue(pab.MetaData.SevenDigit, out list)) {
                        foreach (ProcessedSms psms in list) {
                            if (CompareNumbers(pab.MetaData, psms.MetaData)) {
                                pab.NumberInSms = true;
                                psms.NumberInAdrBook = true;
                                break;
                            }
                        }
                    }
                }
            }
            foreach (ProcessedSms psms in this.smsFields) {
                if (!psms.NumberInCallLog) {
                    List<ProcessedCallLog> list;
                    if (this.callLogSevenDigits.TryGetValue(psms.MetaData.SevenDigit, out list)) {
                        foreach (ProcessedCallLog pcl in list) {
                            if (CompareNumbers1(pcl.MetaData, psms.MetaData)) {
                                psms.NumberInCallLog = true;
                                pcl.NumberInSms = true;
                                break;
                            }
                        }
                    }
                }
                if (!psms.NumberInCallLog) {
                    List<ProcessedCallLog> list;
                    if (this.callLogSevenDigits.TryGetValue(psms.MetaData.SevenDigit2, out list)) {
                        foreach (ProcessedCallLog pcl in list) {
                            if (CompareNumbers2(pcl.MetaData, psms.MetaData)) {
                                psms.NumberInCallLog = true;
                                pcl.NumberInSms = true;
                                break;
                            }
                        }
                    }
                }
                if (!psms.NumberInAdrBook) {
                    List<ProcessedAddressBook> list;
                    if (this.adrBookSevenDigits.TryGetValue(psms.MetaData.SevenDigit, out list)) {
                        foreach (ProcessedAddressBook pab in list) {
                            if (CompareNumbers1(pab.MetaData, psms.MetaData)) {
                                psms.NumberInAdrBook = true;
                                pab.NumberInSms = true;
                                break;
                            }
                        }
                    }
                }
                if (!psms.NumberInAdrBook) {
                    List<ProcessedAddressBook> list;
                    if (this.adrBookSevenDigits.TryGetValue(psms.MetaData.SevenDigit2, out list)) {
                        foreach (ProcessedAddressBook pab in list) {
                            if (CompareNumbers2(pab.MetaData, psms.MetaData)) {
                                psms.NumberInAdrBook = true;
                                pab.NumberInSms = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initiates the removal of duplicate entries.
        /// </summary>
        private void RemoveDuplicates()
        {
            RemoveDuplicateCallLogs();
            RemoveDuplicateAddresses();
            RemoveDuplicateSms();
        }

        /// <summary>
        /// Removes duplicate call log entries.
        /// </summary>
        private void RemoveDuplicateCallLogs()
        {
            List<ProcessedCallLog> newCallLogFields = new List<ProcessedCallLog>();
            HashSet<string> exists = new HashSet<string>();
            foreach (ProcessedCallLog pcl in callLogFields) {
                string key = pcl.KeyString();
                if (exists.Contains(key)) continue;
                exists.Add(key);
                newCallLogFields.Add(pcl);
            }
            callLogFields = newCallLogFields;
        }

        /// <summary>
        /// Removes duplicate address book entries.
        /// </summary>
        private void RemoveDuplicateAddresses()
        {
            List<ProcessedAddressBook> newAddressBookFields = new List<ProcessedAddressBook>();
            HashSet<string> exists = new HashSet<string>();
            foreach (ProcessedAddressBook pab in addressBookFields) {
                string key = pab.KeyString();
                if (exists.Contains(key)) continue;
                exists.Add(key);
                newAddressBookFields.Add(pab);
            }
            addressBookFields = newAddressBookFields;
        }

        /// <summary>
        /// Removes duplicate SMS entries.
        /// </summary>
        private void RemoveDuplicateSms()
        {
            List<ProcessedSms> newSmsFields = new List<ProcessedSms>();
            HashSet<string> exists = new HashSet<string>();
            foreach (ProcessedSms psms in smsFields) {
                string key = psms.KeyString();
                if (exists.Contains(key)) continue;
                exists.Add(key);
                newSmsFields.Add(psms);
            }
            smsFields = newSmsFields;
        }

    }
}
