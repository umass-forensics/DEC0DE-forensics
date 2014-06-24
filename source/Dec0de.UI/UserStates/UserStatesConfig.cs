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
using System.IO;

namespace Dec0de.UI.UserStates
{
    public partial class UserStatesConfig : Form
    {
        public string XmlPath = "";
        public bool OptEnabled = false;

        private const string configFileName = "stateconf.xml";

        public UserStatesConfig()
        {
            InitializeComponent();
        }

        private void UserStatesConfig_Load(object sender, EventArgs e)
        {
            string path;
            checkBoxEnable.Checked = LoadConfiguration(out path);
            if (!String.IsNullOrWhiteSpace(path)) {
                textBoxXML.Text = path.Trim();
            }
        }

        /// <summary>
        /// Browse for the state machine XML.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK) {
                return;
            }
            textBoxXML.Text = dlg.FileName;
        }

        /// <summary>
        /// Loads the configuration file.
        /// </summary>
        private static bool LoadConfiguration(out string path)
        {
            bool enabled = false;
            path = "";
            try {
                XDocument xDoc = XDocument.Load(Path.Combine(MainForm.Program.AppDataDirectory, configFileName));
                XElement xEl = xDoc.Element("DecodeUserStates").Element("configuration").Element("path");
                if (String.IsNullOrWhiteSpace(xEl.Value)) {
                    enabled = false;
                } else {
                    path = xEl.Value;
                    xEl = xDoc.Element("DecodeUserStates").Element("configuration").Element("enabled");
                    enabled = bool.Parse(xEl.Value);
                }
            } catch {
                enabled = false;
            }
            return enabled;
        }

        /// <summary>
        /// Save the configuration as an XML file.
        /// </summary>
        /// <param name="path">Path of the state machine XML (not the configuration XML).</param>
        /// <param name="enabled">Whether or not the user state machine option is enabled.</param>
        private void SaveConfiguration(string path, bool enabled)
        {
            try {
                XElement xPath = new XElement("path", path);
                XElement xEnabled = new XElement("enabled", enabled);
                XElement xConfig = new XElement("configuration");
                xConfig.Add(xPath);
                xConfig.Add(xEnabled);
                XDocument xDoc = new XDocument(new XElement("DecodeUserStates", xConfig));
                xDoc.Save(Path.Combine(MainForm.Program.AppDataDirectory, configFileName));
            } catch (Exception ex) {
                MessageBox.Show("Failed to save the configuration file: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// User clicked OK. Update the configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, EventArgs e)
        {
            bool enabled = checkBoxEnable.Checked;
            string path = textBoxXML.Text.Trim();
            if (enabled) {
                if (!File.Exists(path)) {
                    MessageBox.Show("The specified XML file does not exist", "XML File",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
            }
            // Save the configuration.
            XmlPath = path;
            OptEnabled = enabled;
            SaveConfiguration(path, enabled);
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Reads the database folder from the configuration file. 
        /// </summary>
        /// <returns>Returns null if not configured, or the folder does not exist.</returns>
        public static bool ReadUserStateConfig(out string path)
        {
            bool enabled = LoadConfiguration(out path);
            if (enabled) {
                if (!String.IsNullOrWhiteSpace(path)) {
                    try {
                        if (!File.Exists(path)) {
                            enabled = false;
                        }
                    } catch {
                        enabled = false;
                    }
                } else {
                    enabled = false;
                }
            }
            return enabled;
        }

    }
}
