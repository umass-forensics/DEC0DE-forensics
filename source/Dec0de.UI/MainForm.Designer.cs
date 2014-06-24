namespace Dec0de.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMain = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDecode = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonCancel = new System.Windows.Forms.ToolStripButton();
            this.toolStripFilters = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonResults = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonQuit = new System.Windows.Forms.ToolStripButton();
            this.labelStatus1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.pictureBox5 = new System.Windows.Forms.PictureBox();
            this.pictureBox6 = new System.Windows.Forms.PictureBox();
            this.pictureBox7 = new System.Windows.Forms.PictureBox();
            this.pictureBox8 = new System.Windows.Forms.PictureBox();
            this.pictureBox9 = new System.Windows.Forms.PictureBox();
            this.labelStatus2 = new System.Windows.Forms.Label();
            this.labelStatus3 = new System.Windows.Forms.Label();
            this.labelStatus4 = new System.Windows.Forms.Label();
            this.labelStatus5 = new System.Windows.Forms.Label();
            this.labelStatus6 = new System.Windows.Forms.Label();
            this.labelStatus7 = new System.Windows.Forms.Label();
            this.labelStatus8 = new System.Windows.Forms.Label();
            this.labelStatus9 = new System.Windows.Forms.Label();
            this.pictureBoxUMass = new System.Windows.Forms.PictureBox();
            this.userStateMachinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.toolStripMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox9)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUMass)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(458, 24);
            this.menuStrip1.TabIndex = 14;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.databaseToolStripMenuItem,
            this.userStateMachinesToolStripMenuItem,
            this.aboutToolStripMenuItem,
            this.toolStripMenuItem1,
            this.quitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // databaseToolStripMenuItem
            // 
            this.databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            this.databaseToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.databaseToolStripMenuItem.Text = "Database...";
            this.databaseToolStripMenuItem.Click += new System.EventHandler(this.databaseToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(186, 6);
            // 
            // quitToolStripMenuItem
            // 
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.quitToolStripMenuItem.Text = "Quit";
            this.quitToolStripMenuItem.Click += new System.EventHandler(this.quitToolStripMenuItem_Click);
            // 
            // toolStripMain
            // 
            this.toolStripMain.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripMain.CanOverflow = false;
            this.toolStripMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStripMain.ImageScalingSize = new System.Drawing.Size(64, 64);
            this.toolStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonOpen,
            this.toolStripButtonDecode,
            this.toolStripButtonCancel,
            this.toolStripFilters,
            this.toolStripButtonResults,
            this.toolStripButtonQuit});
            this.toolStripMain.Location = new System.Drawing.Point(0, 24);
            this.toolStripMain.Name = "toolStripMain";
            this.toolStripMain.ShowItemToolTips = false;
            this.toolStripMain.Size = new System.Drawing.Size(458, 86);
            this.toolStripMain.TabIndex = 15;
            this.toolStripMain.Text = "toolStrip1";
            // 
            // toolStripButtonOpen
            // 
            this.toolStripButtonOpen.Image = global::Dec0de.UI.Properties.Resources.getmemory;
            this.toolStripButtonOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOpen.Name = "toolStripButtonOpen";
            this.toolStripButtonOpen.Padding = new System.Windows.Forms.Padding(8, 0, 4, 0);
            this.toolStripButtonOpen.Size = new System.Drawing.Size(80, 83);
            this.toolStripButtonOpen.Text = "Open";
            this.toolStripButtonOpen.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.toolStripButtonOpen.Click += new System.EventHandler(this.toolStripButtonOpen_Click);
            // 
            // toolStripButtonDecode
            // 
            this.toolStripButtonDecode.Image = global::Dec0de.UI.Properties.Resources.decode;
            this.toolStripButtonDecode.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDecode.Name = "toolStripButtonDecode";
            this.toolStripButtonDecode.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.toolStripButtonDecode.Size = new System.Drawing.Size(76, 83);
            this.toolStripButtonDecode.Text = "Decode";
            this.toolStripButtonDecode.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.toolStripButtonDecode.Click += new System.EventHandler(this.toolStripButtonSearch_Click);
            // 
            // toolStripButtonCancel
            // 
            this.toolStripButtonCancel.Image = global::Dec0de.UI.Properties.Resources.stop;
            this.toolStripButtonCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCancel.Name = "toolStripButtonCancel";
            this.toolStripButtonCancel.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.toolStripButtonCancel.Size = new System.Drawing.Size(76, 83);
            this.toolStripButtonCancel.Text = "Cancel";
            this.toolStripButtonCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.toolStripButtonCancel.Click += new System.EventHandler(this.toolStripButtonCancel_Click);
            // 
            // toolStripFilters
            // 
            this.toolStripFilters.Image = global::Dec0de.UI.Properties.Resources.dcfilter;
            this.toolStripFilters.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripFilters.Name = "toolStripFilters";
            this.toolStripFilters.Size = new System.Drawing.Size(68, 83);
            this.toolStripFilters.Text = "Filters";
            this.toolStripFilters.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.toolStripFilters.Click += new System.EventHandler(this.toolStripFilters_Click);
            // 
            // toolStripButtonResults
            // 
            this.toolStripButtonResults.Image = global::Dec0de.UI.Properties.Resources.results;
            this.toolStripButtonResults.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonResults.Name = "toolStripButtonResults";
            this.toolStripButtonResults.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.toolStripButtonResults.Size = new System.Drawing.Size(76, 83);
            this.toolStripButtonResults.Text = "Results";
            this.toolStripButtonResults.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.toolStripButtonResults.Click += new System.EventHandler(this.toolStripButtonResults_Click);
            // 
            // toolStripButtonQuit
            // 
            this.toolStripButtonQuit.Image = global::Dec0de.UI.Properties.Resources.go;
            this.toolStripButtonQuit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonQuit.Name = "toolStripButtonQuit";
            this.toolStripButtonQuit.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.toolStripButtonQuit.Size = new System.Drawing.Size(72, 83);
            this.toolStripButtonQuit.Text = "Quit";
            this.toolStripButtonQuit.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.toolStripButtonQuit.Click += new System.EventHandler(this.toolStripButtonQuit_Click);
            // 
            // labelStatus1
            // 
            this.labelStatus1.AutoSize = true;
            this.labelStatus1.Location = new System.Drawing.Point(47, 126);
            this.labelStatus1.Name = "labelStatus1";
            this.labelStatus1.Size = new System.Drawing.Size(124, 13);
            this.labelStatus1.TabIndex = 16;
            this.labelStatus1.Text = "Calculate file SHA1 hash";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Dec0de.UI.Properties.Resources.check;
            this.pictureBox1.Location = new System.Drawing.Point(25, 123);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(16, 16);
            this.pictureBox1.TabIndex = 17;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::Dec0de.UI.Properties.Resources.check;
            this.pictureBox2.Location = new System.Drawing.Point(25, 145);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(16, 16);
            this.pictureBox2.TabIndex = 18;
            this.pictureBox2.TabStop = false;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.Location = new System.Drawing.Point(25, 167);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(16, 16);
            this.pictureBox3.TabIndex = 19;
            this.pictureBox3.TabStop = false;
            // 
            // pictureBox4
            // 
            this.pictureBox4.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox4.Image")));
            this.pictureBox4.Location = new System.Drawing.Point(25, 189);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(16, 16);
            this.pictureBox4.TabIndex = 20;
            this.pictureBox4.TabStop = false;
            // 
            // pictureBox5
            // 
            this.pictureBox5.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox5.Image")));
            this.pictureBox5.Location = new System.Drawing.Point(25, 211);
            this.pictureBox5.Name = "pictureBox5";
            this.pictureBox5.Size = new System.Drawing.Size(16, 16);
            this.pictureBox5.TabIndex = 21;
            this.pictureBox5.TabStop = false;
            // 
            // pictureBox6
            // 
            this.pictureBox6.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox6.Image")));
            this.pictureBox6.Location = new System.Drawing.Point(25, 233);
            this.pictureBox6.Name = "pictureBox6";
            this.pictureBox6.Size = new System.Drawing.Size(16, 16);
            this.pictureBox6.TabIndex = 22;
            this.pictureBox6.TabStop = false;
            // 
            // pictureBox7
            // 
            this.pictureBox7.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox7.Image")));
            this.pictureBox7.Location = new System.Drawing.Point(25, 255);
            this.pictureBox7.Name = "pictureBox7";
            this.pictureBox7.Size = new System.Drawing.Size(16, 16);
            this.pictureBox7.TabIndex = 23;
            this.pictureBox7.TabStop = false;
            // 
            // pictureBox8
            // 
            this.pictureBox8.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox8.Image")));
            this.pictureBox8.Location = new System.Drawing.Point(25, 277);
            this.pictureBox8.Name = "pictureBox8";
            this.pictureBox8.Size = new System.Drawing.Size(16, 16);
            this.pictureBox8.TabIndex = 24;
            this.pictureBox8.TabStop = false;
            // 
            // pictureBox9
            // 
            this.pictureBox9.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox9.Image")));
            this.pictureBox9.Location = new System.Drawing.Point(25, 299);
            this.pictureBox9.Name = "pictureBox9";
            this.pictureBox9.Size = new System.Drawing.Size(16, 16);
            this.pictureBox9.TabIndex = 25;
            this.pictureBox9.TabStop = false;
            // 
            // labelStatus2
            // 
            this.labelStatus2.AutoSize = true;
            this.labelStatus2.Location = new System.Drawing.Point(47, 148);
            this.labelStatus2.Name = "labelStatus2";
            this.labelStatus2.Size = new System.Drawing.Size(122, 13);
            this.labelStatus2.TabIndex = 26;
            this.labelStatus2.Text = "Locate graphical images";
            // 
            // labelStatus3
            // 
            this.labelStatus3.AutoSize = true;
            this.labelStatus3.Location = new System.Drawing.Point(47, 170);
            this.labelStatus3.Name = "labelStatus3";
            this.labelStatus3.Size = new System.Drawing.Size(117, 13);
            this.labelStatus3.TabIndex = 27;
            this.labelStatus3.Text = "Generate block hashes";
            // 
            // labelStatus4
            // 
            this.labelStatus4.AutoSize = true;
            this.labelStatus4.Location = new System.Drawing.Point(47, 192);
            this.labelStatus4.Name = "labelStatus4";
            this.labelStatus4.Size = new System.Drawing.Size(134, 13);
            this.labelStatus4.TabIndex = 28;
            this.labelStatus4.Text = "Perform block hash filtering";
            // 
            // labelStatus5
            // 
            this.labelStatus5.AutoSize = true;
            this.labelStatus5.Location = new System.Drawing.Point(47, 214);
            this.labelStatus5.Name = "labelStatus5";
            this.labelStatus5.Size = new System.Drawing.Size(105, 13);
            this.labelStatus5.TabIndex = 29;
            this.labelStatus5.Text = "Filter image locations";
            // 
            // labelStatus6
            // 
            this.labelStatus6.AutoSize = true;
            this.labelStatus6.Location = new System.Drawing.Point(47, 236);
            this.labelStatus6.Name = "labelStatus6";
            this.labelStatus6.Size = new System.Drawing.Size(175, 13);
            this.labelStatus6.TabIndex = 30;
            this.labelStatus6.Text = "Run Viterbi algorithm to locate fields";
            // 
            // labelStatus7
            // 
            this.labelStatus7.AutoSize = true;
            this.labelStatus7.Location = new System.Drawing.Point(47, 258);
            this.labelStatus7.Name = "labelStatus7";
            this.labelStatus7.Size = new System.Drawing.Size(186, 13);
            this.labelStatus7.TabIndex = 31;
            this.labelStatus7.Text = "Run Viterbi algorithm to locate records";
            // 
            // labelStatus8
            // 
            this.labelStatus8.AutoSize = true;
            this.labelStatus8.Location = new System.Drawing.Point(47, 280);
            this.labelStatus8.Name = "labelStatus8";
            this.labelStatus8.Size = new System.Drawing.Size(120, 13);
            this.labelStatus8.TabIndex = 32;
            this.labelStatus8.Text = "Perform post processing";
            // 
            // labelStatus9
            // 
            this.labelStatus9.AutoSize = true;
            this.labelStatus9.Location = new System.Drawing.Point(47, 302);
            this.labelStatus9.Name = "labelStatus9";
            this.labelStatus9.Size = new System.Drawing.Size(34, 13);
            this.labelStatus9.TabIndex = 33;
            this.labelStatus9.Text = "Finish";
            // 
            // pictureBoxUMass
            // 
            this.pictureBoxUMass.Image = global::Dec0de.UI.Properties.Resources.Combination1_blk_maroon_310x101;
            this.pictureBoxUMass.Location = new System.Drawing.Point(74, 152);
            this.pictureBoxUMass.Name = "pictureBoxUMass";
            this.pictureBoxUMass.Size = new System.Drawing.Size(310, 101);
            this.pictureBoxUMass.TabIndex = 34;
            this.pictureBoxUMass.TabStop = false;
            // 
            // userStateMachinesToolStripMenuItem
            // 
            this.userStateMachinesToolStripMenuItem.Name = "userStateMachinesToolStripMenuItem";
            this.userStateMachinesToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.userStateMachinesToolStripMenuItem.Text = "User State Machines...";
            this.userStateMachinesToolStripMenuItem.Click += new System.EventHandler(this.userStateMachinesToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(458, 344);
            this.Controls.Add(this.labelStatus9);
            this.Controls.Add(this.labelStatus8);
            this.Controls.Add(this.labelStatus7);
            this.Controls.Add(this.labelStatus6);
            this.Controls.Add(this.labelStatus5);
            this.Controls.Add(this.labelStatus4);
            this.Controls.Add(this.labelStatus3);
            this.Controls.Add(this.labelStatus2);
            this.Controls.Add(this.pictureBox9);
            this.Controls.Add(this.pictureBox8);
            this.Controls.Add(this.pictureBox7);
            this.Controls.Add(this.pictureBox6);
            this.Controls.Add(this.pictureBox5);
            this.Controls.Add(this.pictureBox4);
            this.Controls.Add(this.pictureBox3);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.labelStatus1);
            this.Controls.Add(this.toolStripMain);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.pictureBoxUMass);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "UMass DEC0DE";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStripMain.ResumeLayout(false);
            this.toolStripMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox9)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUMass)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem quitToolStripMenuItem;
        private System.Windows.Forms.ToolStrip toolStripMain;
        private System.Windows.Forms.ToolStripButton toolStripButtonOpen;
        private System.Windows.Forms.ToolStripButton toolStripButtonDecode;
        private System.Windows.Forms.ToolStripButton toolStripButtonCancel;
        private System.Windows.Forms.ToolStripButton toolStripButtonResults;
        private System.Windows.Forms.Label labelStatus1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.PictureBox pictureBox4;
        private System.Windows.Forms.PictureBox pictureBox5;
        private System.Windows.Forms.PictureBox pictureBox6;
        private System.Windows.Forms.PictureBox pictureBox7;
        private System.Windows.Forms.PictureBox pictureBox8;
        private System.Windows.Forms.PictureBox pictureBox9;
        private System.Windows.Forms.Label labelStatus2;
        private System.Windows.Forms.Label labelStatus3;
        private System.Windows.Forms.Label labelStatus4;
        private System.Windows.Forms.Label labelStatus5;
        private System.Windows.Forms.Label labelStatus6;
        private System.Windows.Forms.Label labelStatus7;
        private System.Windows.Forms.Label labelStatus8;
        private System.Windows.Forms.Label labelStatus9;
        private System.Windows.Forms.PictureBox pictureBoxUMass;
        private System.Windows.Forms.ToolStripButton toolStripButtonQuit;
        private System.Windows.Forms.ToolStripMenuItem databaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripButton toolStripFilters;
        private System.Windows.Forms.ToolStripMenuItem userStateMachinesToolStripMenuItem;
    }
}

