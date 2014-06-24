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
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Dec0de.UI
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            labelVersion.Text = "Version: " + VersionDecode.VersionString;
            linkLabelURL.Text = "forensics.umass.edu";
            try {
                DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;
                labelBuild.Text = "Built: " + buildDate.ToString("yyyy-MM-dd HH:mm");
            } catch {
                labelBuild.Text = "Built: <unknown>";
            }
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            try {
                linkLabelURL.Links.Add(0, linkLabelURL.Text.Length, "http://forensics.umass.edu");
            } catch {
            }
        }

        private void linkLabelURL_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData.ToString());
        }
    }
}
