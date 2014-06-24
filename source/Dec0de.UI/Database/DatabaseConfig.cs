/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
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

namespace Dec0de.UI.Database
{
    public partial class DatabaseConfig : Form
    {
        public string DatabaseFolder = null;

        private const string dbConfigName = "databaseconf.xml";

        public DatabaseConfig()
        {
            InitializeComponent();
            buttonOK.Enabled = false;
            textBoxDir.Text = "";
        }

        /// <summary>
        /// Called when the form is loaded. Attempts to read in the configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DatabaseConfig_Load(object sender, EventArgs e)
        {
            string dir = LoadConfiguration();
            if (dir != null) {
                textBoxDir.Text = dir;
            }
        }

        /// <summary>
        /// Called when the database directory text box changes. If not empty
        /// then we can enable the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxDir_TextChanged(object sender, EventArgs e)
        {
            buttonOK.Enabled = !String.IsNullOrWhiteSpace(textBoxDir.Text);
        }

        /// <summary>
        /// Called when the user clicks the OK button. Allow the dialog to close
        /// only if the configured directory exists.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, EventArgs e)
        {
            string path = textBoxDir.Text.Trim();
            if (!Directory.Exists(path)) {
                MessageBox.Show("The specified directory does not exist or is not a directory", "Directory",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            // Save the configuration.
            DatabaseFolder = path;
            SaveConfiguration(path);
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Save the database configuration as an XML file.
        /// </summary>
        /// <param name="path"></param>
        private void SaveConfiguration(string path)
        {
            try {
                XDocument xDoc = new XDocument(
                    new XElement("DecodeDatabase",
                        new XElement("configuration",
                            new XElement("path", path))));
                xDoc.Save(Path.Combine(MainForm.Program.AppDataDirectory, dbConfigName));
            } catch (Exception ex) {
                MessageBox.Show("Failed to save the configuration file: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Displays a diagram to browse for the directory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDirBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Browse for Directory";
            if (!String.IsNullOrWhiteSpace(textBoxDir.Text) && (Directory.Exists(textBoxDir.Text.Trim()))) {
                fbd.SelectedPath = textBoxDir.Text.Trim();
            }
            if (fbd.ShowDialog() == DialogResult.OK) {
                textBoxDir.Text = fbd.SelectedPath;
            }
        }

        /// <summary>
        /// Reads the database folder from the configuration file. 
        /// </summary>
        /// <returns>Returns null if not configured, or the folder does not exist.</returns>
        public static string ReadDatabaseFolder()
        {
            string path = LoadConfiguration();
            if (path != null) {
                if (Directory.Exists(path)) {
                    return path;
                }
            }
            return null;
        }

        /// <summary>
        /// Loads the database configuration file.
        /// </summary>
        /// <returns>Returns the directory path, or null on failure.</returns>
        private static string LoadConfiguration()
        {
            try {
                XDocument xDoc = XDocument.Load(Path.Combine(MainForm.Program.AppDataDirectory, dbConfigName));
                XElement xEl = xDoc.Element("DecodeDatabase").Element("configuration").Element("path");
                if (String.IsNullOrWhiteSpace(xEl.Value)) {
                    return null;
                } else {
                    return xEl.Value;
                }
            } catch {
                return null;
            }
        }

    }
}
