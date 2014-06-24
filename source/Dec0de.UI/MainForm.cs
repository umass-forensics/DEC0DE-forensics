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
using Dec0de.UI.Database;
using Dec0de.UI.DecodeFilters;
using Dec0de.UI.DecodeResults;
using Dec0de.UI.PostProcess;
using Dec0de.UI.UserStates;
using PhoneDecoder.DecodeResults;

namespace Dec0de.UI
{
    public partial class MainForm : Form
    {
        public const string DecodeProductName = "UMass DEC0DE";
        public string AppDataDirectory = null;

        public bool UserStatesEnabled = false;
        public string UserStatesPath = "";

        public static MainForm Program;
        private WorkerThread workerThread = null;

        public static volatile bool Terminating = false;

        private delegate void EndWorkCallback(
            bool success, PostProcessor postProcess, string filePath, PhoneInfo phoneInfo);

        private PostProcessor postProcess = null;
        private string filePath = null;
        private PhoneInfo phoneInfo = null;

        private delegate void OutputCallback(string msg);
        private delegate void StepUpdateCallback(int step);

        private bool stepsInitialzed = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            Program = this;
            InitializeComponent();
            HideChecksAbdSteps();
            EnableDisableFields();
        }

        /// <summary>
        /// Called when the program is loaded. Performs additional initialization.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Make certain we have an application data directory.
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDataDirectory = Path.Combine(appDataFolder, DecodeProductName);
            if (!Directory.Exists(AppDataDirectory)) {
                try {
                    Directory.CreateDirectory(AppDataDirectory);
                } catch (Exception ex) {
                    MessageBox.Show(String.Format("Unable to create application data directory: {0}", ex.Message),
                                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    Application.Exit();
                }
            }
            // Make certain we have a directory for the database.
            DatabaseCreator.DatabaseFolder = DatabaseConfig.ReadDatabaseFolder();
            if (DatabaseCreator.DatabaseFolder == null) {
                DatabaseCreator.DatabaseFolder = ShowDatabaseConfiguration();
            }
            if (DatabaseCreator.DatabaseFolder == null) {
                Application.Exit();
            }
            // Make certain that we have a database. If not, create it.
            if (!DatabaseCreator.Exists()) {
                try {
                    DatabaseCreator.CreateAndInitialize();
                } catch (Exception ex) {
                    string err = String.Format("Unable to coninue: failed to create and initialize the database. {0}",
                                               ex.Message);
                    MessageBox.Show(err, "Fatal Database Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    Application.Exit();
                }
            } else {
                DatabaseCreator.PopulateConstantsMigrate();
            }
            // Load the default filters.
            DecodeFilters.ResultFilters.LoadDefaultFilters();
            // Load whether user-defined states are enabled.
            UserStatesEnabled = UserStatesConfig.ReadUserStateConfig(out UserStatesPath);
        }

        /// <summary>
        /// Invokes the database configuration dialog.
        /// </summary>
        private string ShowDatabaseConfiguration()
        {
            DatabaseConfig dbDlg = new DatabaseConfig();
            DialogResult rslt = dbDlg.ShowDialog();
            if (rslt != DialogResult.OK) {
                return null;
            }
            return dbDlg.DatabaseFolder;
        }

        /// <summary>
        /// Hides all of the fields used to show decoding progress.
        /// </summary>
        private void HideChecksAbdSteps()
        {
            pictureBox1.Hide();
            labelStatus1.Hide();
            pictureBox2.Hide();
            labelStatus2.Hide();
            pictureBox3.Hide();
            labelStatus3.Hide();
            pictureBox4.Hide();
            labelStatus4.Hide();
            pictureBox5.Hide();
            labelStatus5.Hide();
            pictureBox6.Hide();
            labelStatus6.Hide();
            pictureBox7.Hide();
            labelStatus7.Hide();
            pictureBox8.Hide();
            labelStatus8.Hide();
            pictureBox9.Hide();
            labelStatus9.Hide();
            pictureBoxUMass.Show();
        }

        /// <summary>
        /// Initializes all of the fields used to show decoding progress.
        /// </summary>
        private void InitializeSteps()
        {
            pictureBoxUMass.Hide();
            pictureBox1.Hide();
            labelStatus1.Font = new Font(labelStatus1.Font, FontStyle.Regular);
            labelStatus1.Show();
            pictureBox2.Hide();
            labelStatus2.Font = new Font(labelStatus2.Font, FontStyle.Regular);
            labelStatus2.Show();
            pictureBox3.Hide();
            labelStatus3.Font = new Font(labelStatus3.Font, FontStyle.Regular);
            labelStatus3.Show();
            pictureBox4.Hide();
            labelStatus4.Font = new Font(labelStatus4.Font, FontStyle.Regular);
            labelStatus4.Show();
            pictureBox5.Hide();
            labelStatus5.Font = new Font(labelStatus5.Font, FontStyle.Regular);
            labelStatus5.Show();
            pictureBox6.Hide();
            labelStatus6.Font = new Font(labelStatus6.Font, FontStyle.Regular);
            labelStatus6.Show();
            pictureBox7.Hide();
            labelStatus7.Font = new Font(labelStatus7.Font, FontStyle.Regular);
            labelStatus7.Show();
            pictureBox8.Hide();
            labelStatus8.Font = new Font(labelStatus8.Font, FontStyle.Regular);
            labelStatus8.Show();
            pictureBox9.Hide();
            labelStatus9.Text = "Finish";
            labelStatus9.Font = new Font(labelStatus9.Font, FontStyle.Regular);
            labelStatus9.Show();
            stepsInitialzed = true;
        }

        /// <summary>
        /// Given a decoding step, indicate that it has started by changing the
        /// font to bold.
        /// </summary>
        /// <param name="step"></param>
        public void SetStepStarted(int step)
        {
            if (MainForm.Program.InvokeRequired) {
                StepUpdateCallback cb = new StepUpdateCallback(SetStepStarted);
                this.Invoke(cb, new object[] { step });
            } else {
                switch (step) {
                    case 1:
                        labelStatus1.Font = new Font(labelStatus1.Font, FontStyle.Bold);
                        break;
                    case 2:
                        labelStatus2.Font = new Font(labelStatus2.Font, FontStyle.Bold);
                        break;
                    case 3:
                        labelStatus3.Font = new Font(labelStatus3.Font, FontStyle.Bold);
                        break;
                    case 4:
                        labelStatus4.Font = new Font(labelStatus4.Font, FontStyle.Bold);
                        break;
                    case 5:
                        labelStatus5.Font = new Font(labelStatus5.Font, FontStyle.Bold);
                        break;
                    case 6:
                        labelStatus6.Font = new Font(labelStatus6.Font, FontStyle.Bold);
                        break;
                    case 7:
                        labelStatus7.Font = new Font(labelStatus7.Font, FontStyle.Bold);
                        break;
                    case 8:
                        labelStatus8.Font = new Font(labelStatus8.Font, FontStyle.Bold);
                        break;
                    case 9:
                        labelStatus9.Font = new Font(labelStatus9.Font, FontStyle.Bold);
                        break;
                }
                stepsInitialzed = false;
            }
        }

        /// <summary>
        /// Indicate that a decoding step has completed by placing a check next
        /// to the step.
        /// </summary>
        /// <param name="step"></param>
        public void SetStepCompleted(int step)
        {
            if (MainForm.Program.InvokeRequired) {
                StepUpdateCallback cb = new StepUpdateCallback(SetStepCompleted);
                this.Invoke(cb, new object[] { step });
            } else {
                switch (step) {
                    case 1:
                        pictureBox1.Show();
                        break;
                    case 2:
                        pictureBox2.Show();
                        break;
                    case 3:
                        pictureBox3.Show();
                        break;
                    case 4:
                        pictureBox4.Show();
                        break;
                    case 5:
                        pictureBox5.Show();
                        break;
                    case 6:
                        pictureBox6.Show();
                        break;
                    case 7:
                        pictureBox7.Show();
                        break;
                    case 8:
                        pictureBox8.Show();
                        break;
                    case 9:
                        pictureBox9.Show();
                        break;
                }
                stepsInitialzed = false;
            }
        }

        /// <summary>
        /// Mark the current decoding step completed and the next step started.
        /// </summary>
        /// <param name="finishedStep"></param>
        public void SetNextStep(int finishedStep)
        {
            if (MainForm.Program.InvokeRequired) {
                StepUpdateCallback cb = new StepUpdateCallback(SetNextStep);
                this.Invoke(cb, new object[] { finishedStep });
            } else {
                SetStepCompleted(finishedStep);
                SetStepStarted(finishedStep + 1);
            }
        }

        /// <summary>
        /// Called to initiate decoding. This is done in a separate thread.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="manufacturer"></param>
        /// <param name="model"></param>
        /// <param name="note"></param>
        /// <param name="noStore"></param>
        private void StartWork(string path, string manufacturer, string model, string note, bool noStore)
        {
            this.postProcess = null;
            this.filePath = null;
            this.phoneInfo = null;
            this.workerThread = new WorkerThread(path, manufacturer, model, note, noStore);
            EnableDisableFields();
            InitializeSteps();
            this.workerThread.Start();
        }

        /// <summary>
        /// Invoked by the decoding thread to indicate that it has completed.
        /// </summary>
        /// <param name="success">To indicate if the decoding process was successful.</param>
        /// <param name="postProcess">The results after post porcessing the results from record level Viterbi inference.</param>
        /// <param name="filePath">The path to the phone's memory file.</param>
        /// <param name="phoneInfo">Stores the manufacturer, model etc. information about the phone.</param>
        public void EndWork(bool success, PostProcessor postProcess, string filePath, PhoneInfo phoneInfo)
        {
            if (Terminating) return;
            if (MainForm.Program.InvokeRequired) {
                EndWorkCallback cb = new EndWorkCallback(EndWork);
                this.Invoke(cb, new object[] {success, postProcess, filePath, phoneInfo});
            } else {
                this.workerThread = null;
                if (success) {
                    this.postProcess = postProcess;
                    this.filePath = filePath;
                    this.phoneInfo = phoneInfo;
                    labelStatus9.Text = String.Format("Calls={0}, Addresses={1}, SMS={2}, Images={3}",
                                                      postProcess.callLogFields.Count,
                                                      postProcess.addressBookFields.Count,
                                                      postProcess.smsFields.Count, postProcess.imageBlocks.Count);
                } else {
                    this.postProcess = null;
                    HideChecksAbdSteps();
                }
                EnableDisableFields();
            }
        }

        /// <summary>
        /// Enable or disable buttons based on actions that the user can take.
        /// </summary>
        private void EnableDisableFields()
        {
            toolStripButtonOpen.Enabled = (workerThread == null);
            toolStripButtonDecode.Enabled = ((filePath != null) && (phoneInfo != null));
            toolStripButtonResults.Enabled = (postProcess != null);
            toolStripButtonCancel.Enabled = (workerThread != null);
        }

        /// <summary>
        /// Invoked when this form is closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Terminating = true;
            try {
                if (this.workerThread != null) {
                    this.workerThread.WorkThread.Abort();
                }
            } catch {
            }
        }

        /// <summary>
        /// Called when the user wants to exit the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!AskQuit()) return;
            Application.Exit();
        }

        /// <summary>
        /// Called when the user wants to open a memory file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            GetMemFileDlg dlg = new GetMemFileDlg();
            if (dlg.ShowDialog() != DialogResult.OK) {
                return;
            }
            this.filePath = dlg.FilePath;
            this.phoneInfo = new PhoneInfo(dlg.Manufacturer, dlg.Model, dlg.Note, dlg.DoNotStoreHashes);
            this.postProcess = null;
            HideChecksAbdSteps();
            EnableDisableFields();
        }

        /// <summary>
        /// Called when the user clicks on the button to begin decoding.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonSearch_Click(object sender, EventArgs e)
        {
            if ((filePath != null) && (phoneInfo != null)) {
                StartWork(filePath, phoneInfo.Manufacturer, phoneInfo.Model, phoneInfo.Note, phoneInfo.DoNotStore);
            }
        }

        /// <summary>
        /// Called when the user clicks on the button to see the decoding results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonResults_Click(object sender, EventArgs e)
        {
            if (this.postProcess == null) {
                return;
            }
            (new DecodeResultsForm(this.Handle, this.postProcess, this.filePath, this.phoneInfo,
                ResultFilters.GetActiveFilters())).Show();
        }

        /// <summary>
        /// Asks the user if he or she really wants to quit.
        /// </summary>
        /// <returns></returns>
        private bool AskQuit()
        {
            if (MessageBox.Show("Do you really want to quit?", "Quit?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Called when the quit button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonQuit_Click(object sender, EventArgs e)
        {
            if (!AskQuit()) return;
            Application.Exit();
        }

        /// <summary>
        /// Called to cancel an in-progress decoding.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonCancel_Click(object sender, EventArgs e)
        {
            try {
                if (workerThread != null) {
                    toolStripButtonCancel.Enabled = false;
                    workerThread.Cancel();
                }
            } catch {
            }
        }

        /// <summary>
        /// Called to view the about dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new AboutForm()).ShowDialog();
        }


        /// <summary>
        /// Called when the user requests the database configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void databaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string newFolder = ShowDatabaseConfiguration();
            if ((newFolder != null) && (!DatabaseCreator.DatabaseFolder.Equals(newFolder))) {
                MessageBox.Show("Changes to the database folder will not take effect until the program is restarted",
                                "Database Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Called when the user clicks the button to define filters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripFilters_Click(object sender, EventArgs e)
        {
            toolStripFilters.Enabled = false;
            DecodeFilters.ResultFilters.ShowDialog();
            toolStripFilters.Enabled = true;
        }

        /// <summary>
        /// Called when the user selects the menu item to configure user-defined
        /// states.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void userStateMachinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserStatesConfig dlg = new UserStatesConfig();
            if (dlg.ShowDialog() != DialogResult.OK) {
                return;
            }
            UserStatesEnabled = dlg.OptEnabled;
            UserStatesPath = dlg.XmlPath;
        }

    }
}
