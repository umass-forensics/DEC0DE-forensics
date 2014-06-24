/**
 * Copyright (C) 2013 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Dec0de.UI.DecodeFilters
{
    /*
     * Class for access to the configured results filters.
     * 
     * TODO:
     * Currently we only support a default configuration file. We would
     * want to allow the user to define multiple sets of filters if we
     * decide to offer a more complex set of user-defined filters (e.g.,
     * regular expressions).
     */

    static class ResultFilters
    {
        private const string DEFAULT_FILTER_FILE = "filters.xml";

        private static XDocument activeFilterXdoc = null;
        private static Filters filters = null;
        private static object lockObj = new Object();

        /// <summary>
        /// Loads the defaults set of filters.
        /// </summary>
        public static void LoadDefaultFilters()
        {
            try {
                string path = Path.Combine(MainForm.Program.AppDataDirectory, DEFAULT_FILTER_FILE);
                XDocument xDoc = XDocument.Load(path);
                if (xDoc.Element("decodefilters") != null) {
                    UpdateFilters(xDoc);
                }
            } catch {
            }
        }

        /// <summary>
        ///  Updates the filters that are in effect.
        /// </summary>
        /// <param name="xDoc"></param>
        private static void UpdateFilters(XDocument xDoc)
        {
            lock (lockObj) {
                activeFilterXdoc = xDoc;
                filters = new Filters(xDoc);
            }
        }

        /// <summary>
        /// Returns the active filters class.
        /// </summary>
        /// <returns></returns>
        public static Filters GetActiveFilters()
        {
            // Kind of silly to use a lock ...
            lock (lockObj) {
                return filters;
            }
        }

        /// <summary>
        /// Displays the dialog for configuring filters.
        /// </summary>
        public static void ShowDialog()
        {
            DefineFiltersForm form = new DefineFiltersForm(activeFilterXdoc);
            if (form.ShowDialog() == DialogResult.Cancel) {
                return;
            }
            UpdateFilters(form.XdocFilter);
            try {
                string path = Path.Combine(MainForm.Program.AppDataDirectory, DEFAULT_FILTER_FILE);
                form.XdocFilter.Save(path);
            } catch {
            }
        }

        
    }

}
