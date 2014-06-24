/**
 * Copyright (C) 2013 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Dec0de.UI.DecodeFilters
{
    /*
     * This is a form for defining filters to be applied to records prior to
     * displaying the results.
     *
     * Currently we only support some simple timestamp filters, and the elimination
     * of address book fields that contain certain characters.
     */

    public partial class DefineFiltersForm : Form
    {
        public XDocument XdocFilter = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="xDoc">The confiuration XML file as an Xdocument.</param>
        public DefineFiltersForm(XDocument xDoc)
        {
            InitializeComponent();
            DisableAll();
            if (LoadFromXdoc(xDoc)) {
                XdocFilter = xDoc;
            }
        }

        /// <summary>
        /// The default state is all filters are disabled.
        /// </summary>
        private void DisableAll()
        {
            checkBoxCallLogAge.Checked = false;
            textBoxCallLogAge.Enabled = false;
            checkBoxSmsAge.Checked = false;
            textBoxSmsAge.Enabled = false;
            checkBoxAdrBookChars.Checked = false;
            textBoxAdrBookChars.Enabled = false;
        }

        /// <summary>
        /// Reads the configuration XDocument and updates the form's fields.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        private bool LoadFromXdoc(XDocument xDoc)
        {
            if (xDoc == null) {
                return false;
            }
            // Find required elements.
            XElement xFilters = null;
            try {
                xFilters = xDoc.Element("decodefilters").Element("filters");
            } catch {
            }
            if (xFilters == null) {
                return false;
            }
            // Get the "simple" built-in filters. (Currently all that we support).
            try {
                XElement xSimple = xFilters.Element("simple");
                if (xSimple == null) {
                    return true;
                }
                int days = 0;
                bool enabled = false;
                if (Filters.GetTimestampValue(xSimple.Element("calllogtime"), out days, out enabled)) {
                    textBoxCallLogAge.Text = days.ToString();
                    if (enabled) {
                        checkBoxCallLogAge.Checked = true;
                        textBoxCallLogAge.Enabled = true;
                    }
                }
                if (Filters.GetTimestampValue(xSimple.Element("smstime"), out days, out enabled)) {
                    textBoxSmsAge.Text = days.ToString();
                    if (enabled) {
                        checkBoxSmsAge.Checked = true;
                        textBoxSmsAge.Enabled = true;
                    }
                }
                string str = null;
                if (Filters.GetCharsValue(xSimple.Element("adrbookchars"), out str, out enabled)) {
                    textBoxAdrBookChars.Text = str;
                    if (enabled) {
                        checkBoxAdrBookChars.Checked = true;
                        textBoxAdrBookChars.Enabled = true;
                    }
                }
            } catch {
            }
            return true;
        }

        private void checkBoxCallLogAge_CheckedChanged(object sender, EventArgs e)
        {
            textBoxCallLogAge.Enabled = checkBoxCallLogAge.Checked;
        }

        private void checkBoxSmsAge_CheckedChanged(object sender, EventArgs e)
        {
            textBoxSmsAge.Enabled = checkBoxSmsAge.Checked;
        }

        private void checkBoxAdrBookChars_CheckedChanged(object sender, EventArgs e)
        {
            textBoxAdrBookChars.Enabled = checkBoxAdrBookChars.Checked;
        }

        /// <summary>
        /// User clicked the OK button. Save configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, EventArgs e)
        {
            // Create a new XDocument to represent the configuration.
            XDocument xDoc = new XDocument();
            XElement xRoot = new XElement("decodefilters");
            XElement xFilters = new XElement("filters");
            XElement xSimple = new XElement("simple");
            xDoc.Add(xRoot);
            xRoot.Add(xFilters);
            xFilters.Add(xSimple);
            xSimple.Add(DefineTimeElement("calllogtime", textBoxCallLogAge, checkBoxCallLogAge));
            xSimple.Add(DefineTimeElement("smstime", textBoxSmsAge, checkBoxSmsAge));
            xSimple.Add(DefineCharsElement("adrbookchars", textBoxAdrBookChars, checkBoxAdrBookChars));
            XdocFilter = xDoc;
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Called to create an elemenet that defines a date range filter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="textBox"></param>
        /// <param name="checkBox"></param>
        /// <returns></returns>
        private XElement DefineTimeElement(string name, TextBox textBox, CheckBox checkBox)
        {
            bool enabled = checkBox.Checked;
            int days = 0;
            if (!int.TryParse(textBox.Text, out days)) {
                enabled = false;
            }
            XElement xEl = new XElement(name);
            xEl.Add(new XAttribute("days", days));
            xEl.Add(new XAttribute("enabled", enabled));
            return xEl;
        }

        /// <summary>
        /// Called to create an element that defines a string of characters.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="textBox"></param>
        /// <param name="checkBox"></param>
        /// <returns></returns>
        private XElement DefineCharsElement(string name, TextBox textBox, CheckBox checkBox)
        {
            bool enabled = checkBox.Checked;
            string str = textBox.Text.Trim();
            if (str.Length == 0) {
                enabled = false;
            }
            XElement xEl = new XElement(name);
            xEl.Add(new XAttribute("chars", str));
            xEl.Add(new XAttribute("enabled", enabled));
            return xEl;
        }
    }
}
