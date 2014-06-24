namespace Dec0de.UI.DecodeFilters
{
    partial class DefineFiltersForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DefineFiltersForm));
            this.tabControlFilters = new System.Windows.Forms.TabControl();
            this.tabPageSimple = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxCallLogAge = new System.Windows.Forms.TextBox();
            this.checkBoxCallLogAge = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBoxSmsAge = new System.Windows.Forms.CheckBox();
            this.textBoxSmsAge = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxAdrBookChars = new System.Windows.Forms.TextBox();
            this.checkBoxAdrBookChars = new System.Windows.Forms.CheckBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.tabControlFilters.SuspendLayout();
            this.tabPageSimple.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlFilters
            // 
            this.tabControlFilters.Controls.Add(this.tabPageSimple);
            this.tabControlFilters.Location = new System.Drawing.Point(13, 13);
            this.tabControlFilters.Name = "tabControlFilters";
            this.tabControlFilters.SelectedIndex = 0;
            this.tabControlFilters.Size = new System.Drawing.Size(531, 335);
            this.tabControlFilters.TabIndex = 0;
            // 
            // tabPageSimple
            // 
            this.tabPageSimple.BackColor = System.Drawing.SystemColors.Control;
            this.tabPageSimple.Controls.Add(this.groupBox2);
            this.tabPageSimple.Controls.Add(this.groupBox4);
            this.tabPageSimple.Controls.Add(this.groupBox1);
            this.tabPageSimple.Location = new System.Drawing.Point(4, 22);
            this.tabPageSimple.Name = "tabPageSimple";
            this.tabPageSimple.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSimple.Size = new System.Drawing.Size(523, 309);
            this.tabPageSimple.TabIndex = 0;
            this.tabPageSimple.Text = "Simple Filters";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxCallLogAge);
            this.groupBox1.Controls.Add(this.textBoxCallLogAge);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(7, 7);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(510, 61);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Call Logs:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Age (days):";
            // 
            // textBoxCallLogAge
            // 
            this.textBoxCallLogAge.Location = new System.Drawing.Point(73, 21);
            this.textBoxCallLogAge.Name = "textBoxCallLogAge";
            this.textBoxCallLogAge.Size = new System.Drawing.Size(77, 20);
            this.textBoxCallLogAge.TabIndex = 1;
            // 
            // checkBoxCallLogAge
            // 
            this.checkBoxCallLogAge.AutoSize = true;
            this.checkBoxCallLogAge.Location = new System.Drawing.Point(167, 24);
            this.checkBoxCallLogAge.Name = "checkBoxCallLogAge";
            this.checkBoxCallLogAge.Size = new System.Drawing.Size(59, 17);
            this.checkBoxCallLogAge.TabIndex = 2;
            this.checkBoxCallLogAge.Text = "Enable";
            this.checkBoxCallLogAge.UseVisualStyleBackColor = true;
            this.checkBoxCallLogAge.CheckedChanged += new System.EventHandler(this.checkBoxCallLogAge_CheckedChanged);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkBoxSmsAge);
            this.groupBox4.Controls.Add(this.textBoxSmsAge);
            this.groupBox4.Controls.Add(this.label2);
            this.groupBox4.Location = new System.Drawing.Point(7, 83);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(510, 61);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "SMS:";
            // 
            // checkBoxSmsAge
            // 
            this.checkBoxSmsAge.AutoSize = true;
            this.checkBoxSmsAge.Location = new System.Drawing.Point(167, 24);
            this.checkBoxSmsAge.Name = "checkBoxSmsAge";
            this.checkBoxSmsAge.Size = new System.Drawing.Size(59, 17);
            this.checkBoxSmsAge.TabIndex = 2;
            this.checkBoxSmsAge.Text = "Enable";
            this.checkBoxSmsAge.UseVisualStyleBackColor = true;
            this.checkBoxSmsAge.CheckedChanged += new System.EventHandler(this.checkBoxSmsAge_CheckedChanged);
            // 
            // textBoxSmsAge
            // 
            this.textBoxSmsAge.Location = new System.Drawing.Point(73, 21);
            this.textBoxSmsAge.Name = "textBoxSmsAge";
            this.textBoxSmsAge.Size = new System.Drawing.Size(77, 20);
            this.textBoxSmsAge.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Age (days):";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkBoxAdrBookChars);
            this.groupBox2.Controls.Add(this.textBoxAdrBookChars);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Location = new System.Drawing.Point(7, 157);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(510, 61);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Address Book:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(93, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Ignore characters:";
            // 
            // textBoxAdrBookChars
            // 
            this.textBoxAdrBookChars.Location = new System.Drawing.Point(107, 25);
            this.textBoxAdrBookChars.Name = "textBoxAdrBookChars";
            this.textBoxAdrBookChars.Size = new System.Drawing.Size(298, 20);
            this.textBoxAdrBookChars.TabIndex = 1;
            // 
            // checkBoxAdrBookChars
            // 
            this.checkBoxAdrBookChars.AutoSize = true;
            this.checkBoxAdrBookChars.Location = new System.Drawing.Point(428, 28);
            this.checkBoxAdrBookChars.Name = "checkBoxAdrBookChars";
            this.checkBoxAdrBookChars.Size = new System.Drawing.Size(59, 17);
            this.checkBoxAdrBookChars.TabIndex = 3;
            this.checkBoxAdrBookChars.Text = "Enable";
            this.checkBoxAdrBookChars.UseVisualStyleBackColor = true;
            this.checkBoxAdrBookChars.CheckedChanged += new System.EventHandler(this.checkBoxAdrBookChars_CheckedChanged);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(469, 366);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(388, 366);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // DefineFiltersForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(556, 403);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.tabControlFilters);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DefineFiltersForm";
            this.Text = "Define Filters";
            this.tabControlFilters.ResumeLayout(false);
            this.tabPageSimple.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlFilters;
        private System.Windows.Forms.TabPage tabPageSimple;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkBoxCallLogAge;
        private System.Windows.Forms.TextBox textBoxCallLogAge;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox checkBoxSmsAge;
        private System.Windows.Forms.TextBox textBoxSmsAge;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxAdrBookChars;
        private System.Windows.Forms.TextBox textBoxAdrBookChars;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
    }
}