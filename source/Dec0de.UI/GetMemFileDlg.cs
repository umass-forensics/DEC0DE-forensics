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
using System.IO;

namespace Dec0de.UI
{
    public partial class GetMemFileDlg : Form
    {
        public string FilePath;
        public string Manufacturer;
        public string Model;
        public string Note;
        public bool DoNotStoreHashes;

        public GetMemFileDlg()
        {
            InitializeComponent();
            textBoxManufacturer.Enabled = false;
            buttonOK.Enabled = false;
            AddPhoneManufacturers();
        }

        private void AddPhoneManufacturers()
        {
            string[] manufacturers = { "<Not Specified>", "LG", "Motorola", "Nokia", "Samsung", "Sony Ericsson", "<Other>", "<Unknown>" };
            comboBoxManufacturer.Items.AddRange(manufacturers);
            comboBoxManufacturer.SelectedIndex = 0;
        }

        private void buttonBrowseInput_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = "Memory files (*.bin,*.xry)|*.bin;*.xry|All files (*.*)|*.*";
            if (dlg.ShowDialog() != DialogResult.OK) {
                return;
            }
            textBoxInputFile.Text = dlg.FileName;
        }

        private void comboBoxManufacturer_SelectedIndexChanged(object sender, EventArgs e)
        {
            try {
                if (comboBoxManufacturer.SelectedIndex >= 0) {
                    if ((string)comboBoxManufacturer.Items[comboBoxManufacturer.SelectedIndex] == "<Other>") {
                        textBoxManufacturer.Enabled = true;
                        return;
                    }
                }
            } catch {
            }
            textBoxManufacturer.Enabled = false;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.None;
            if (String.IsNullOrWhiteSpace(textBoxInputFile.Text)) {
                MessageBox.Show("You must specify a memory file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            FilePath = textBoxInputFile.Text.Trim();
            if (!File.Exists(FilePath)) {
                MessageBox.Show("Memory file does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (comboBoxManufacturer.SelectedIndex < 0) {
                MessageBox.Show("You must specify the phone's make", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            } else if ((string)comboBoxManufacturer.Items[comboBoxManufacturer.SelectedIndex] == "<Other>") {
                Manufacturer = textBoxManufacturer.Text.Trim().ToLower();
                if (Manufacturer.Length == 0) {
                    MessageBox.Show("You must manually enter the phone's make when selecting \"<Other>\"",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            } else {
                Manufacturer = ((string)comboBoxManufacturer.Items[comboBoxManufacturer.SelectedIndex]).ToLower();
            }
            Model = textBoxModel.Text.Trim();
            Note = textBoxNotes.Text;
            DoNotStoreHashes = checkBoxNoStore.Checked;
            DialogResult = DialogResult.OK;
        }

        private void textBoxInputFile_TextChanged(object sender, EventArgs e)
        {
            buttonOK.Enabled = (textBoxInputFile.Text.Trim().Length > 0);
        }

    }
}
