/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using Dec0de.Bll.AnswerLoader;
using Dec0de.UI;
using Dec0de.UI.DecodeResults;
using Dec0de.UI.PostProcess;
using Dec0de.UI.DecodeFilters;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;

namespace PhoneDecoder.DecodeResults
{
    public partial class DecodeResultsForm : Form
    {
        private PostProcessor processedData;
        private FileStream stream;
        private PhoneInfo phoneInfo;
        private List<CallLogListViewItem> callLogItems = null;
        private List<AddressBookListViewItem> adrBookItems = null;
        private List<SmsListViewItem> smsItems = null;
        private List<ImageListViewItem> imageItems = null;

        private int callLogSortColumn = -1;
        private int adrBookSortColumn = -1;
        private int smsSortColumn = -1;
        private int imagesSortColumn = -1;

        private enum TabSelections : int
        {
            CALLLOG = 0,
            ADRBOOK = 1,
            SMS = 2,
            IMAGES = 3
        }

        private readonly System.Drawing.Color SimilarNumberColor = System.Drawing.Color.PaleGoldenrod;
        private readonly System.Drawing.Color EntryInAdrBookColor = System.Drawing.Color.PaleGreen;
        private readonly System.Drawing.Color EntryInSmsColor = System.Drawing.Color.PaleTurquoise;
        private readonly System.Drawing.Color EntryInCallLogColor = System.Drawing.Color.PaleVioletRed;
        private readonly System.Drawing.Color SearchColor = System.Drawing.Color.Silver;

        private TabSelections currentTab = TabSelections.CALLLOG;

        public DecodeResultsForm(IntPtr handle, PostProcessor postProcess, string filePath, PhoneInfo phoneInfo,
            Dec0de.UI.DecodeFilters.Filters filters)
        {
            try {
                this.stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            } catch {
                stream = null;
            }
            this.processedData = postProcess;
            this.phoneInfo = phoneInfo;
            InitializeComponent();
            toolStripStatusLabelSummary.Text = "";
            DcUtils.SetWaitCursor(handle);
            CreateListviewItems(postProcess.callLogFields, postProcess.addressBookFields, postProcess.smsFields,
                                postProcess.imageBlocks, filters);
            InitDropdowns();
            radioClHideChecked.Checked = true;
            radioClRemoveChecked.Checked = true;
            radioClHighlightCriteria.Checked = true;
            radioAbHideChecked.Checked = true;
            radioAbRemoveChecked.Checked = true;
            radioAbHighlightCriteria.Checked = true;
            radioSmsHideChecked.Checked = true;
            radioSmsRemoveChecked.Checked = true;
            radioSmsHighlightCriteria.Checked = true;
            toolStripButtonImage.Enabled = false;
            PopulateViews();
            DcUtils.ResetWaitCursor();
        }

        private void InitDropdowns()
        {
            string[] clActions =
                {
                    "Similar number", "Numbers in address book", "Names in address book",
                    "Numbers with names in address book", "Numbers in SMS"
                };
            comboBoxClHighlight.Items.AddRange(clActions);
            comboBoxClHighlight.SelectedIndex = 0;
            string[] abActions =
                {
                    "Similar number", "Numbers in call log", "Names in call log",
                    "Numbers with names in call log", "Numbers in SMS"
                };
            comboBoxAbHighlight.Items.AddRange(abActions);
            comboBoxAbHighlight.SelectedIndex = 0;
            string[] smsActions =
                {
                    "Similar number 1", "Similar number 2", "Numbers in call log",
                    "Numbers in address book"
                };
            comboBoxSmsHighlight.Items.AddRange(smsActions);
            comboBoxSmsHighlight.SelectedIndex = 0;
        }

        private void CreateListviewItems(List<ProcessedCallLog> callLogs, List<ProcessedAddressBook> addressBook,
                                         List<ProcessedSms> sms, List<ImageBlock> imageBlocks,
                                        Dec0de.UI.DecodeFilters.Filters filters)
        {            
            callLogItems = new List<CallLogListViewItem>();
            foreach (ProcessedCallLog pcl in callLogs) 
            {
                if (filters != null && filters.IsFiltered(pcl)) 
                    continue;

                CallLogListViewItem item = new CallLogListViewItem(pcl);
                MetaCallLog mcl = pcl.MetaData;
                item.Name = pcl.Id.ToString();
                item.SubItems.Add(FormatPhoneNum(mcl.Number));
                item.SubItems.Add(FormatName(mcl.Name));
                item.SubItems.Add(FormatCallLogType(mcl.Type));
                item.SubItems.Add(FormatTimestamp(mcl.TimeStamp));
                callLogItems.Add(item);
            }

            adrBookItems = new List<AddressBookListViewItem>();
            foreach (ProcessedAddressBook pab in addressBook) {
                if ((filters != null) && filters.IsFiltered(pab)) continue;
                AddressBookListViewItem item = new AddressBookListViewItem(pab);
                MetaAddressBookEntry mabe = pab.MetaData;
                item.Name = pab.Id.ToString();
                item.SubItems.Add(FormatName(mabe.Name));
                item.SubItems.Add(FormatPhoneNum(mabe.Number));
                adrBookItems.Add(item);
            }

            smsItems = new List<SmsListViewItem>();
            foreach (ProcessedSms psms in sms) {
                if ((filters != null) && filters.IsFiltered(psms)) continue;
                SmsListViewItem item = new SmsListViewItem(psms);
                MetaSms msms = psms.MetaData;
                item.Name = psms.Id.ToString();
                item.SubItems.Add(FormatPhoneNum(msms.Number));
                item.SubItems.Add(FormatPhoneNum(msms.Number2));
                item.SubItems.Add(FormatMessage(msms.Message));
                item.SubItems.Add(FormatTimestamp(msms.TimeStamp));
                smsItems.Add(item);
            }

            imageItems = new List<ImageListViewItem>();
            foreach (ImageBlock ib in imageBlocks) {
                ImageListViewItem item = new ImageListViewItem(ib);
                string name = String.Format("Image {0:D4}", ib.Num);
                item.Name = name;
                item.SubItems.Add(name);
                item.SubItems.Add(ib.GetImageType());
                item.SubItems.Add(ib.Length.ToString());
                item.SubItems.Add(ib.Offset.ToString());
                imageItems.Add(item);
            }
        }

        private void PopulateViews()
        {
            PopulateCallLogView();
            PopulateAdrBookView();
            PopulateSmsView();
            PopulateImagesView();
        }

        private void UpdateSummaryText(TabSelections tab)
        {
            if (tab != currentTab) {
                return;
            }
            int shown = 0;
            int hidden = 0;
            switch (tab) {
                case TabSelections.CALLLOG:
                    shown = listViewCallLogs.Items.Count;
                    hidden = callLogItems.Count - shown;
                    break;
                case TabSelections.ADRBOOK:
                    shown = listViewAdrBook.Items.Count;
                    hidden = adrBookItems.Count - shown;
                    break;
                case TabSelections.SMS:
                    shown = listViewSMS.Items.Count;
                    hidden = smsItems.Count - shown;
                    break;
                case TabSelections.IMAGES:
                    shown = listViewImages.Items.Count;
                    hidden = imageItems.Count - shown;
                    break;
            }
            toolStripStatusLabelSummary.Text = String.Format("Shown = {0}, Hidden = {1}", shown, hidden);
        }

        private void PopulateCallLogView(bool unhideAll, bool showUnchecked, bool showChecked)
        {
            listViewCallLogs.BeginUpdate();
            listViewCallLogs.Items.Clear();
            try {
                foreach (CallLogListViewItem item in callLogItems) {
                    if (unhideAll) {
                        item.Hidden = false;
                    } else {
                        if (item.Hidden) continue;
                        if (item.Checked && !showChecked) {
                            item.Hidden = true;
                            continue;
                        } else if (!item.Checked && !showUnchecked) {
                            item.Hidden = true;
                            continue;
                        }
                    }
                    listViewCallLogs.Items.Add(item);
                }
            } finally {
                listViewCallLogs.EndUpdate();
            }
            UpdateSummaryText(TabSelections.CALLLOG);
        }

        private void PopulateCallLogView()
        {
            PopulateCallLogView(false, true, true);
        }

        private void PopulateAdrBookView(bool unhideAll, bool showUnchecked, bool showChecked)
        {
            listViewAdrBook.BeginUpdate();
            listViewAdrBook.Items.Clear();
            try {
                foreach (AddressBookListViewItem item in adrBookItems) {
                    if (unhideAll) {
                        item.Hidden = false;
                    } else {
                        if (item.Hidden) continue;
                        if (item.Checked && !showChecked) {
                            item.Hidden = true;
                            continue;
                        } else if (!item.Checked && !showUnchecked) {
                            item.Hidden = true;
                            continue;
                        }
                    }
                    listViewAdrBook.Items.Add(item);
                }
            } finally {
                listViewAdrBook.EndUpdate();
            }
            UpdateSummaryText(TabSelections.ADRBOOK);
        }

        private void PopulateAdrBookView()
        {
            PopulateAdrBookView(false, true, true);
        }

        private void PopulateSmsView(bool unhideAll, bool showUnchecked, bool showChecked)
        {
            listViewSMS.BeginUpdate();
            listViewSMS.Items.Clear();
            try {
                foreach (SmsListViewItem item in smsItems) {
                    if (unhideAll) {
                        item.Hidden = false;
                    } else {
                        if (item.Hidden) continue;
                        if (item.Checked && !showChecked) {
                            item.Hidden = true;
                            continue;
                        } else if (!item.Checked && !showUnchecked) {
                            item.Hidden = true;
                            continue;
                        }
                    }
                    listViewSMS.Items.Add(item);
                }
            } finally {
                listViewSMS.EndUpdate();
            }
            UpdateSummaryText(TabSelections.SMS);
        }

        private void PopulateSmsView()
        {
            PopulateSmsView(false, true, true);
        }

        private void PopulateImagesView(bool unhideAll, bool showUnchecked, bool showChecked)
        {
            listViewImages.BeginUpdate();
            listViewImages.Items.Clear();
            try {
                foreach (ImageListViewItem item in imageItems) {
                    if (unhideAll) {
                        item.Hidden = false;
                    } else {
                        if (item.Hidden) continue;
                        if (item.Checked && !showChecked) {
                            item.Hidden = true;
                            continue;
                        } else if (!item.Checked && !showUnchecked) {
                            item.Hidden = true;
                            continue;
                        }
                    }
                    listViewImages.Items.Add(item);
                }
            } finally {
                listViewImages.EndUpdate();
            }
            UpdateSummaryText(TabSelections.IMAGES);
        }

        private void PopulateImagesView()
        {
            PopulateImagesView(false, true, true);
        }

        private string FormatPhoneNum(string phoneNum)
        {
            return FieldUtils.FormatPhoneNumber(phoneNum);
        }

        private string FormatCallLogType(string type)
        {
            if (String.IsNullOrEmpty(type)) {
                return "";
            }
            if (type == Dec0de.Bll.AnswerLoader.MetaField.DEFAULT_STRING) {
                return "";
            }
            return type;
        }

        private string FormatName(string name)
        {
            if (String.IsNullOrEmpty(name)) {
                return "";
            }
            if (name == Dec0de.Bll.AnswerLoader.MetaField.DEFAULT_STRING) {
                return "";
            }
            return name;
        }

        private string FormatTimestamp(DateTime? datetime)
        {
            try {
                if (datetime == null) {
                    return "";
                }
                // new DateTime() should never be returned.
                DateTime dt = datetime ?? new DateTime(1900, 1, 1);
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            } catch {
                return "?";
            }
        }

        private string FormatMessage(string msg)
        {
            if (String.IsNullOrEmpty(msg)) {
                return "";
            }
            if (msg == Dec0de.Bll.AnswerLoader.MetaField.DEFAULT_STRING) {
                return "";
            }
            return msg;
        }

        private bool ThumbnailCallback()
        {
            return false;
        }

        private void ShowThumbnail(Bitmap bmp)
        {
            try {
                int maxHeight = pictureBoxImg.Height;
                int maxWidth = pictureBoxImg.Width;
                int height;
                int width;
                // Adjust the height to fit within the box and then
                // scale the width.
                if (maxHeight > bmp.Height) {
                    height = bmp.Height;
                    width = bmp.Width;
                } else {
                    height = maxHeight;
                    double scale = ((double) height)/((double) bmp.Height);
                    width = (int) (((double) bmp.Width)*scale);
                }
                // If the width is too large, then we need to put the
                // width within the box and scale the height.
                if (width > maxWidth) {
                    width = maxWidth;
                    double scale = ((double) width)/((double) bmp.Width);
                    height = (int) (((double) bmp.Height)*scale);
                }
                pictureBoxImg.Image = bmp.GetThumbnailImage(width, height, ThumbnailCallback, IntPtr.Zero);
            } catch (Exception ex) {
                throw ex;
            }
        }

        private void listViewImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool showNone = true;
            try {
                if ((stream == null) || (listViewImages.SelectedItems.Count == 0)) {
                    toolStripButtonImage.Enabled = false;
                    return;
                }
                toolStripButtonImage.Enabled = true;
                ShowThumbnail(new Bitmap(GetSelectedBitmap()));
                showNone = false;
            } catch (Exception) {
                showNone = true;
            } finally {
                if (showNone) {
                    pictureBoxImg.Image = null;
                }
            }
        }

        private void toolStripButtonImage_Click(object sender, EventArgs e)
        {
            Stream outStream = null;
            Stream inStream = null;
            try {
                ImageListViewItem item = (ImageListViewItem) listViewImages.SelectedItems[0];
                if (item == null) {
                    return;
                }
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = String.Format("Image_{0:D4}", item.ImageBlock.Num);
                dlg.Filter =
                    "JPEG files (*.jpg)|*.jpg|PNG files (*.png)|*.png|GIF files (*.gif)|*.gif|PNG files (*.ong)|*.png|All files (*.*)|*.*";
                switch (item.ImageBlock.ImgType) {
                    case ImageBlock.ImageType.JPG:
                        dlg.FilterIndex = 1;
                        break;
                    case ImageBlock.ImageType.PNG:
                        dlg.FilterIndex = 2;
                        break;
                    case ImageBlock.ImageType.GIF:
                        dlg.FilterIndex = 3;
                        break;
                    case ImageBlock.ImageType.BMP:
                        dlg.FilterIndex = 4;
                        break;
                    default:
                        dlg.FilterIndex = 5;
                        break;
                }
                dlg.RestoreDirectory = true;
                dlg.Title = "Save Image";
                if (dlg.ShowDialog() != DialogResult.OK) {
                    return;
                }
                outStream = dlg.OpenFile();
                if (outStream == null) {
                    throw new Exception("Unable to create image file");
                }
                inStream = GetSelectedBitmapStream(item);
                if (inStream == null) {
                    throw new Exception("Unable to access image data");
                }
                inStream.CopyTo(outStream);
            } catch (Exception ex) {
                MessageBox.Show("Unable to save image: " + ex.Message, "Save Image", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            } finally {
                if (outStream != null) {
                    outStream.Close();
                }
                if (inStream != null) {
                    inStream.Close();
                }
            }
        }

        private Bitmap GetSelectedBitmap()
        {
            Stream memFile = GetSelectedBitmapStream();
            if (memFile == null) {
                return null;
            }
            try {
                return new Bitmap(memFile);
            } catch {
                return null;
            } finally {
                memFile.Close();
            }
        }

        private Stream GetSelectedBitmapStream(ImageListViewItem item)
        {
            try {
                byte[] data = new byte[item.ImageBlock.Length];
                stream.Position = item.ImageBlock.Offset;
                if (stream.Read(data, 0, item.ImageBlock.Length) != item.ImageBlock.Length) {
                    return null;
                }
                return new MemoryStream(data);
            } catch {
                return null;
            }
        }

        private Stream GetSelectedBitmapStream()
        {
            try {
                ImageListViewItem item = (ImageListViewItem) listViewImages.SelectedItems[0];
                if (item == null) {
                    return null;
                }
                return GetSelectedBitmapStream(item);
            } catch {
                return null;
            }
        }

        private void toolStripButtonExportCSV_Click(object sender, EventArgs e)
        {
            int indx = tabResults.SelectedIndex;
            if ((indx < 0) || (indx > 2)) return;
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
            dlg.Title = "Save Spreadsheet";
            if (dlg.ShowDialog() != DialogResult.OK) {
                return;
            }
            StreamWriter stream = null;
            try {
                stream = new StreamWriter(dlg.OpenFile());
                if (indx == 0) {
                    ExportCsvCallLogs(stream);
                } else if (indx == 1) {
                    ExportCsvAdrBook(stream);
                } else {
                    ExportCsvSms(stream);
                }
            } catch (Exception ex) {
                MessageBox.Show("Failed to create CSV file: " + ex.Message, "Error", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            } finally {
                if (stream != null) {
                    stream.Close();
                }
            }
        }

        private void ExportCsvCallLogs(StreamWriter stream)
        {
            stream.WriteLine("\"Number\",\"Name\",\"Type\",\"Timestamp\"");
            foreach (CallLogListViewItem item in listViewCallLogs.Items) {
                stream.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                                 item.SubItems[1].Text, EscapeQuotes(item.SubItems[2].Text),
                                 EscapeQuotes(item.SubItems[3].Text), item.SubItems[4].Text);
            }
        }

        private void ExportCsvAdrBook(StreamWriter stream)
        {
            stream.WriteLine("\"Name\",\"Number\"");
            foreach (AddressBookListViewItem item in listViewAdrBook.Items) {
                stream.WriteLine("\"{0}\",\"{1}\"",
                                 EscapeQuotes(item.SubItems[1].Text), item.SubItems[2].Text);
            }
        }

        private void ExportCsvSms(StreamWriter stream)
        {
            stream.WriteLine("\"Number 1\",\"Number 2\",\"Text\",\"Timestamp\"");
            foreach (SmsListViewItem item in listViewSMS.Items) {
                stream.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                                 item.SubItems[1].Text, item.SubItems[2].Text,
                                 EscapeQuotes(item.SubItems[3].Text), item.SubItems[4].Text);
            }
        }

        private string EscapeQuotes(string str)
        {
            if (str.IndexOf('"') < 0) {
                return str;
            }
            StringBuilder quoted = new StringBuilder(str.Length*2);
            foreach (char c in str) {
                if (c == '"') {
                    quoted.Append("\"\"");
                } else {
                    quoted.Append(c);
                }
            }
            return quoted.ToString();
        }


        #region ExportSpreadsheet
            private
            void toolStripButtonExcel_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "XLSX files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
            dlg.Title = "Save Spreadsheet";
            if (dlg.ShowDialog() != DialogResult.OK) {
                return;
            }
            SpreadsheetDocument spreadSheet = null;
            try {
                // Create the spreadsheet.
                spreadSheet = SpreadsheetDocument.Create(dlg.FileName, SpreadsheetDocumentType.Workbook);
                WorkbookPart workbookpart = spreadSheet.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();
                Sheets sheets = spreadSheet.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

                // First worksheet is the phone information.
                WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                Worksheet worksheet = new Worksheet();
                worksheetPart.Worksheet = worksheet;
                UInt32Value sheetId = 1;
                Sheet sheet = new Sheet()
                                  {
                                      Id = spreadSheet.WorkbookPart.GetIdOfPart(worksheetPart),
                                      SheetId = sheetId,
                                      Name = "Phone Information"
                                  };
                sheets.Append(sheet);
                SheetData sheetData = new SheetData();
                AddRow(sheetData, "Saved", DateTime.Now.ToString("f"));
                AddRow(sheetData, "Manufacturer", phoneInfo.Manufacturer);
                AddRow(sheetData, "Model", phoneInfo.Model);
                AddRow(sheetData, "Note", phoneInfo.Note);
                worksheet.Append(sheetData);

                // Next worrksheet is the call logs.
                worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                worksheet = new Worksheet();
                worksheetPart.Worksheet = worksheet;
                sheet = new Sheet()
                            {
                                Id = spreadSheet.WorkbookPart.GetIdOfPart(worksheetPart),
                                SheetId = ++sheetId,
                                Name = "Call Logs"
                            };
                sheets.Append(sheet);
                sheetData = new SheetData();
                AddRow(sheetData, "Number", "Name", "Type", "Timestamp");
                foreach (CallLogListViewItem item in callLogItems) {
                    AddRow(sheetData, item.SubItems[1].Text, item.SubItems[2].Text, item.SubItems[3].Text,
                           item.SubItems[4].Text);
                }
                worksheet.Append(sheetData);

                // Now add the address book worksheet.
                worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                worksheet = new Worksheet();
                worksheetPart.Worksheet = worksheet;
                sheet = new Sheet()
                            {
                                Id = spreadSheet.WorkbookPart.GetIdOfPart(worksheetPart),
                                SheetId = ++sheetId,
                                Name = "Address Book"
                            };
                sheets.Append(sheet);
                sheetData = new SheetData();
                AddRow(sheetData, "Name", "Number");
                foreach (AddressBookListViewItem item in adrBookItems) {
                    AddRow(sheetData, item.SubItems[1].Text, item.SubItems[2].Text);
                }
                worksheet.Append(sheetData);

                // Add text messages.
                worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                worksheet = new Worksheet();
                worksheetPart.Worksheet = worksheet;
                sheet = new Sheet()
                            {
                                Id = spreadSheet.WorkbookPart.GetIdOfPart(worksheetPart),
                                SheetId = ++sheetId,
                                Name = "SMS"
                            };
                sheets.Append(sheet);
                sheetData = new SheetData();
                AddRow(sheetData, "Number 1", "Number 2", "Text", "Timestamp");
                foreach (SmsListViewItem item in smsItems) {
                    AddRow(sheetData, item.SubItems[1].Text, item.SubItems[2].Text, item.SubItems[3].Text,
                           item.SubItems[4].Text);
                }
                worksheet.Append(sheetData);

                // Add images.
                worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                worksheet = new Worksheet();
                worksheetPart.Worksheet = worksheet;
                sheet = new Sheet()
                {
                    Id = spreadSheet.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = ++sheetId,
                    Name = "Images"
                };
                sheets.Append(sheet);
                sheetData = new SheetData();
                AddRow(sheetData, "Image", "Type", "Size", "File Offset");
                foreach (ImageListViewItem item in imageItems) {
                    AddRow(sheetData, item.SubItems[1].Text, item.SubItems[2].Text,
                        int.Parse(item.SubItems[3].Text), long.Parse(item.SubItems[4].Text));
                }
                worksheet.Append(sheetData);

            } catch (Exception ex) {
                MessageBox.Show("Failed to create spreadsheet: " + ex.Message, "Error", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            } finally {
                if (spreadSheet != null) {
                    spreadSheet.Close();
                }
            }

        }

        private void AddRow(SheetData sheetData, params Object[] values)
        {
            Row row = new Row();
            foreach (object val in values) {
                if (val is String) {
                    AddCell(row, (string) val);
                } else if (val is int) {
                    AddCell(row, (int) val);
                } else if (val is long) {
                    AddCell(row, (long) val);
                }
            }
            sheetData.Append(row);
        }

        private void AddCell(Row row, string str)
        {
            Cell cell = new Cell()
                            {
                                DataType = CellValues.String,
                                CellValue = new CellValue(str)
                            };
            row.Append(cell);
        }

        private void AddCell(Row row, int val)
        {
            Cell cell = new Cell()
                            {
                                DataType = CellValues.Number,
                                CellValue = new CellValue(val.ToString())
                            };
            row.Append(cell);
        }

        private void AddCell(Row row, long val)
        {
            Cell cell = new Cell()
                            {
                                DataType = CellValues.Number,
                                CellValue = new CellValue(val.ToString())
                            };
            row.Append(cell);
        }
        #endregion


        private void buttonClHideUpdate_Click(object sender, EventArgs e)
        {
            if (radioClHideNothing.Checked) {
                PopulateCallLogView(true, true, true);
            } else if (radioClHideChecked.Checked) {
                PopulateCallLogView(false, true, false);
            } else {
                PopulateCallLogView(false, false, true);
            }
        }

        private void tabResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabResults.SelectedIndex) {
                case 0:
                    currentTab = TabSelections.CALLLOG;
                    break;
                case 1:
                    currentTab = TabSelections.ADRBOOK;
                    break;
                case 2:
                    currentTab = TabSelections.SMS;
                    break;
                case 3:
                    currentTab = TabSelections.IMAGES;
                    break;
            }
            UpdateSummaryText(currentTab);
            EnableDisableButtons();

        }

        private void EnableDisableButtons()
        {
            switch (currentTab) {
                case TabSelections.CALLLOG:
                case TabSelections.ADRBOOK:
                case TabSelections.SMS:
                    toolStripButtonExportCSV.Enabled = true;
                    toolStripButtonImage.Enabled = false;
                    break;
                case TabSelections.IMAGES:
                    toolStripButtonExportCSV.Enabled = false;
                    toolStripButtonImage.Enabled = (listViewImages.SelectedItems.Count > 0);
                    break;
            }
        }

        #region ColumnSort
        private void listViewCallLogs_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (callLogSortColumn != e.Column) {
                listViewCallLogs.Sorting = System.Windows.Forms.SortOrder.Ascending;
                callLogSortColumn = e.Column;
            } else {
                if (listViewCallLogs.Sorting == System.Windows.Forms.SortOrder.Ascending) {
                    listViewCallLogs.Sorting = System.Windows.Forms.SortOrder.Descending;
                } else {
                    listViewCallLogs.Sorting = System.Windows.Forms.SortOrder.Ascending;
                }
            }
            listViewCallLogs.ListViewItemSorter = new ListViewSorter(e.Column, listViewCallLogs.Sorting, ListViewSorter.ViewType.CALLLOG);
            listViewCallLogs.Sort();
        }

        private void listViewAdrBook_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (adrBookSortColumn != e.Column) {
                listViewAdrBook.Sorting = System.Windows.Forms.SortOrder.Ascending;
                adrBookSortColumn = e.Column;
            } else {
                if (listViewAdrBook.Sorting == System.Windows.Forms.SortOrder.Ascending) {
                    listViewAdrBook.Sorting = System.Windows.Forms.SortOrder.Descending;
                } else {
                    listViewAdrBook.Sorting = System.Windows.Forms.SortOrder.Ascending;
                }
            }
            listViewAdrBook.ListViewItemSorter = new ListViewSorter(e.Column, listViewAdrBook.Sorting, ListViewSorter.ViewType.ADRBOOK);
            listViewAdrBook.Sort();

        }

        private void listViewSMS_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (smsSortColumn != e.Column) {
                listViewSMS.Sorting = System.Windows.Forms.SortOrder.Ascending;
                smsSortColumn = e.Column;
            } else {
                if (listViewSMS.Sorting == System.Windows.Forms.SortOrder.Ascending) {
                    listViewSMS.Sorting = System.Windows.Forms.SortOrder.Descending;
                } else {
                    listViewSMS.Sorting = System.Windows.Forms.SortOrder.Ascending;
                }
            }
            listViewSMS.ListViewItemSorter = new ListViewSorter(e.Column, listViewSMS.Sorting, ListViewSorter.ViewType.SMS);
            listViewSMS.Sort();
        }

        private void listViewImages_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (imagesSortColumn != e.Column) {
                listViewImages.Sorting = System.Windows.Forms.SortOrder.Ascending;
                imagesSortColumn = e.Column;
            } else {
                if (listViewImages.Sorting == System.Windows.Forms.SortOrder.Ascending) {
                    listViewImages.Sorting = System.Windows.Forms.SortOrder.Descending;
                } else {
                    listViewImages.Sorting = System.Windows.Forms.SortOrder.Ascending;
                }
            }
            listViewImages.ListViewItemSorter = new ListViewSorter(e.Column, listViewImages.Sorting, ListViewSorter.ViewType.IMAGES);
            listViewImages.Sort();
        }
        #endregion

        private void RemoveCheckedCallLogs()
        {
            List<CallLogListViewItem> newCallLogItems = new List<CallLogListViewItem>();
            bool changed = false;
            foreach (CallLogListViewItem item in callLogItems) {
                if (item.Hidden) {
                    newCallLogItems.Add(item);
                } else {
                    if (!item.Checked) {
                        newCallLogItems.Add(item);
                    } else {
                        changed = true;
                    }
                }
            }
            callLogItems = newCallLogItems;
            if (changed) {
                PopulateCallLogView();
            }
        }

        private void RemoveHiddenCallLogs()
        {
            bool deleted = false;
            List<CallLogListViewItem> newCallLogItems = new List<CallLogListViewItem>();
            foreach (CallLogListViewItem item in callLogItems) {
                if (!item.Hidden) {
                    newCallLogItems.Add(item);
                } else {
                    deleted = true;
                }
            }
            callLogItems = newCallLogItems;
            if (deleted) {
                UpdateSummaryText(TabSelections.CALLLOG);
            }
        }

        private void buttonClRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Remove entries from results?", "Remove?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) {
                return;
            }
            if (radioClRemoveChecked.Checked) {
                RemoveCheckedCallLogs();
            } else if (radioClRemoveHidden.Checked) {
                RemoveHiddenCallLogs();
            }
        }

        private void radioClHighlightFind_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxClHighlight.Enabled = radioClHighlightCriteria.Checked;
        }

        private void CallLogFindAndHighlight()
        {
            switch (comboBoxClHighlight.SelectedIndex) {
                case 0:
                    CallLogHighlightSimilarNum();
                    break;
                case 1:
                    CallLogHighlightNumbersInAdrBook();
                    break;
                case 2:
                    CallLogHighlightNamesInAdrBook();
                    break;
                case 3:
                    CallLogHighlightNumbersWithNamesInAdrBook();
                    break;
                case 4:
                    CallLogHighlightNumbersInSms();
                    break;
            }
        }

        private void CallLogHighlightSimilarNum()
        {
            if (listViewCallLogs.SelectedItems.Count != 1) {
                SimpleMessage("You must have a single item selected");
                return;
            }
            listViewCallLogs.BeginUpdate();
            try {
                CallLogListViewItem clvi = listViewCallLogs.SelectedItems[0] as CallLogListViewItem;
                string number = clvi.CallLog.MetaData.Number;
                foreach (CallLogListViewItem item in listViewCallLogs.Items) {
                    // Include selected value to be highlighted.
                    if (SimilarNumbers(number, item.CallLog.MetaData.Number)) {
                        item.BackColor = SimilarNumberColor;
                        item.Highlighted = true;
                    }
                }
            } catch {
            } finally {
                listViewCallLogs.EndUpdate();
            }
        }

        private void CallLogHighlightNumbersInAdrBook()
        {
            listViewCallLogs.BeginUpdate();
            try {
                foreach (CallLogListViewItem item in listViewCallLogs.Items) {
                    string number = item.CallLog.MetaData.Number;
                    foreach (AddressBookListViewItem abItem in adrBookItems) {
                        // Include selected value to be highlighted.
                        if (SimilarNumbers(number, abItem.AddressBook.MetaData.Number)) {
                            item.BackColor = EntryInAdrBookColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewCallLogs.EndUpdate();
            }
        }

        private void CallLogHighlightNamesInAdrBook()
        {
            listViewCallLogs.BeginUpdate();
            try {
                foreach (CallLogListViewItem item in listViewCallLogs.Items) {
                    string name1 = item.CallLog.MetaData.Name;
                    if (String.IsNullOrEmpty(name1)) continue;
                    if (name1 == MetaField.DEFAULT_STRING) continue;
                    name1 = name1.ToLower();
                    foreach (AddressBookListViewItem abItem in adrBookItems) {
                        // Include selected value to be highlighted.
                        string name2 = abItem.AddressBook.MetaData.Name;
                        if (String.IsNullOrEmpty(name2)) continue;
                        if (name2 == MetaField.DEFAULT_STRING) continue;
                        if (name1 == name2.ToLower()) {
                            item.BackColor = EntryInAdrBookColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewCallLogs.EndUpdate();
            }
        }

        private void CallLogHighlightNumbersWithNamesInAdrBook()
        {
            listViewCallLogs.BeginUpdate();
            try {
                foreach (CallLogListViewItem item in listViewCallLogs.Items) {
                    string number = item.CallLog.MetaData.Number;
                    string name1 = item.CallLog.MetaData.Name;
                    if (String.IsNullOrEmpty(name1)) continue;
                    if (name1 == MetaField.DEFAULT_STRING) continue;
                    name1 = name1.ToLower();
                    foreach (AddressBookListViewItem abItem in adrBookItems) {
                        // Include selected value to be highlighted.
                        string name2 = abItem.AddressBook.MetaData.Name;
                        if (String.IsNullOrEmpty(name2)) continue;
                        if (name2 == MetaField.DEFAULT_STRING) continue;
                        if (name1 != name2.ToLower()) continue;
                        if (SimilarNumbers(number, abItem.AddressBook.MetaData.Number)) {
                            item.BackColor = EntryInAdrBookColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewCallLogs.EndUpdate();
            }
        }

        private void CallLogHighlightNumbersInSms()
        {
            listViewCallLogs.BeginUpdate();
            try {
                foreach (CallLogListViewItem item in listViewCallLogs.Items) {
                    string number = item.CallLog.MetaData.Number;
                    foreach (SmsListViewItem smsItem in smsItems) {
                        // Include selected value to be highlighted.
                        if (SimilarNumbers(number, smsItem.SmsEntry.MetaData.Number) ||
                            SimilarNumbers(number, smsItem.SmsEntry.MetaData.Number2)) {
                            item.BackColor = EntryInSmsColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewCallLogs.EndUpdate();
            }
        }

        private void CallLogSelectHighlighted()
        {
            listViewCallLogs.Focus();
            listViewCallLogs.BeginUpdate();
            for (int indx = 0; indx < listViewCallLogs.Items.Count; indx++) {
                try {
                    CallLogListViewItem item = listViewCallLogs.Items[indx] as CallLogListViewItem;
                    if (item.Highlighted && !item.Selected) {
                        item.Selected = true;
                        item.Focused = true;
                    } else if (!item.Highlighted && item.Selected) {
                        item.Selected = false;
                        item.Focused = false;
                    }
                } catch {
                }
            }
            listViewCallLogs.EndUpdate();
        }

        private void CallLogClearHighlighted()
        {
            ListViewItem lvi = new ListViewItem();
            listViewCallLogs.BeginUpdate();
            foreach (CallLogListViewItem item in callLogItems) {
                if (item.Highlighted) {
                    item.BackColor = lvi.BackColor;
                    item.Highlighted = false;
                }
            }
            listViewCallLogs.EndUpdate();
        }

        private void buttonClHighlightGo_Click(object sender, EventArgs e)
        {
            if (radioClHighlightCriteria.Checked) {
                CallLogFindAndHighlight();
            } else if (radioClHighlightSelect.Checked) {
                CallLogSelectHighlighted();
            } else if (radioClHighlightClear.Checked) {
                CallLogClearHighlighted();
            }
        }

        private bool SimilarNumbers(string num1, string num2)
        {
            if (num1 == num2) {
                return true;
            }
            if ((num1.Length == num2.Length) || (num1.Length < 7) || (num2.Length < 7)) {
                return false;
            }
            if (num1.Length > num2.Length) {
                return num1.EndsWith(num2);
            } else {
                return num2.EndsWith(num1);
            }
        }

        private void SimpleMessage(string text)
        {
            MessageBox.Show(text, "Decode", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonAbHideUpdate_Click(object sender, EventArgs e)
        {
            if (radioAbHideNothing.Checked) {
                PopulateAdrBookView(true, true, true);
            } else if (radioAbHideChecked.Checked) {
                PopulateAdrBookView(false, true, false);
            } else {
                PopulateAdrBookView(false, false, true);
            }
        }

        private void buttonAbRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Remove entries from results?", "Remove?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) {
                return;
            }
            if (radioAbRemoveChecked.Checked) {
                RemoveCheckedAdrBook();
            } else if (radioClRemoveHidden.Checked) {
                RemoveHiddenAdrBook();
            }
        }

        private void RemoveCheckedAdrBook()
        {
            List<AddressBookListViewItem> newAdrBookItems = new List<AddressBookListViewItem>();
            bool changed = false;
            foreach (AddressBookListViewItem item in adrBookItems) {
                if (item.Hidden) {
                    newAdrBookItems.Add(item);
                } else {
                    if (!item.Checked) {
                        newAdrBookItems.Add(item);
                    } else {
                        changed = true;
                    }
                }
            }
            adrBookItems = newAdrBookItems;
            if (changed) {
                PopulateAdrBookView();
            }
        }

        private void RemoveHiddenAdrBook()
        {
            bool deleted = false;
            List<AddressBookListViewItem> newAdrBookItems = new List<AddressBookListViewItem>();
            foreach (AddressBookListViewItem item in adrBookItems) {
                if (!item.Hidden) {
                    newAdrBookItems.Add(item);
                } else {
                    deleted = true;
                }
            }
            adrBookItems = newAdrBookItems;
            if (deleted) {
                UpdateSummaryText(TabSelections.ADRBOOK);
            }
        }

        private void AdrBookFindAndHighlight()
        {
            switch (comboBoxAbHighlight.SelectedIndex) {
                case 0:
                    AdrBookHighlightSimilarNum();
                    break;
                case 1:
                    AdrBookHighlightNumbersInCallLog();
                    break;
                case 2:
                    AdrBookHighlightNamesInCallLog();
                    break;
                case 3:
                    AdrBookHighlightNumbersWithNamesInCallLog();
                    break;
                case 4:
                    AdrBookHighlightNumbersInSms();
                    break;
            }
        }

        private void AdrBookHighlightSimilarNum()
        {
            if (listViewAdrBook.SelectedItems.Count != 1) {
                SimpleMessage("You must have a single item selected");
                return;
            }
            listViewAdrBook.BeginUpdate();
            try {
                AddressBookListViewItem abvi = listViewAdrBook.SelectedItems[0] as AddressBookListViewItem;
                string number = abvi.AddressBook.MetaData.Number;
                foreach (AddressBookListViewItem item in listViewAdrBook.Items) {
                    // Include selected value to be highlighted.
                    if (SimilarNumbers(number, item.AddressBook.MetaData.Number)) {
                        item.BackColor = SimilarNumberColor;
                        item.Highlighted = true;
                    }
                }
            } catch {
            } finally {
                listViewAdrBook.EndUpdate();
            }
        }

        private void AdrBookHighlightNumbersInCallLog()
        {
            listViewAdrBook.BeginUpdate();
            try {
                foreach (AddressBookListViewItem item in listViewAdrBook.Items) {
                    string number = item.AddressBook.MetaData.Number;
                    foreach (CallLogListViewItem clItem in callLogItems) {
                        // Include selected value to be highlighted.
                        if (SimilarNumbers(number, clItem.CallLog.MetaData.Number)) {
                            item.BackColor = EntryInCallLogColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewAdrBook.EndUpdate();
            }
        }

        private void AdrBookHighlightNamesInCallLog()
        {
            listViewAdrBook.BeginUpdate();
            try {
                foreach (AddressBookListViewItem item in listViewAdrBook.Items) {
                    string name1 = item.AddressBook.MetaData.Name;
                    if (String.IsNullOrEmpty(name1)) continue;
                    if (name1 == MetaField.DEFAULT_STRING) continue;
                    name1 = name1.ToLower();
                    foreach (CallLogListViewItem clItem in callLogItems) {
                        // Include selected value to be highlighted.
                        string name2 = clItem.CallLog.MetaData.Name;
                        if (String.IsNullOrEmpty(name2)) continue;
                        if (name2 == MetaField.DEFAULT_STRING) continue;
                        if (name1 == name2.ToLower()) {
                            item.BackColor = EntryInCallLogColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewAdrBook.EndUpdate();
            }
        }

        private void AdrBookHighlightNumbersWithNamesInCallLog()
        {
            listViewAdrBook.BeginUpdate();
            try {
                foreach (AddressBookListViewItem item in listViewAdrBook.Items) {
                    string number = item.AddressBook.MetaData.Number;
                    string name1 = item.AddressBook.MetaData.Name;
                    if (String.IsNullOrEmpty(name1)) continue;
                    if (name1 == MetaField.DEFAULT_STRING) continue;
                    name1 = name1.ToLower();
                    foreach (CallLogListViewItem clItem in callLogItems) {
                        // Include selected value to be highlighted.
                        string name2 = clItem.CallLog.MetaData.Name;
                        if (String.IsNullOrEmpty(name2)) continue;
                        if (name2 == MetaField.DEFAULT_STRING) continue;
                        if (name1 != name2.ToLower()) continue;
                        if (SimilarNumbers(number, clItem.CallLog.MetaData.Number)) {
                            item.BackColor = EntryInCallLogColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewAdrBook.EndUpdate();
            }
        }

        private void AdrBookHighlightNumbersInSms()
        {
            listViewAdrBook.BeginUpdate();
            try {
                foreach (AddressBookListViewItem item in listViewAdrBook.Items) {
                    string number = item.AddressBook.MetaData.Number;
                    foreach (SmsListViewItem smsItem in smsItems) {
                        // Include selected value to be highlighted.
                        if (SimilarNumbers(number, smsItem.SmsEntry.MetaData.Number) ||
                            SimilarNumbers(number, smsItem.SmsEntry.MetaData.Number2)) {
                            item.BackColor = EntryInSmsColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewAdrBook.EndUpdate();
            }
        }

        private void AdrBookSelectHighlighted()
        {
            listViewAdrBook.Focus();
            listViewAdrBook.BeginUpdate();
            for (int indx = 0; indx < listViewAdrBook.Items.Count; indx++) {
                try {
                    AddressBookListViewItem item = listViewAdrBook.Items[indx] as AddressBookListViewItem;
                    if (item.Highlighted && !item.Selected) {
                        item.Selected = true;
                        item.Focused = true;
                    } else if (!item.Highlighted && item.Selected) {
                        item.Selected = false;
                        item.Focused = false;
                    }
                } catch {
                }
            }
            listViewAdrBook.EndUpdate();
        }

        private void AdrBookClearHighlighted()
        {
            ListViewItem lvi = new ListViewItem();
            listViewAdrBook.BeginUpdate();
            foreach (AddressBookListViewItem item in adrBookItems) {
                if (item.Highlighted) {
                    item.BackColor = lvi.BackColor;
                    item.Highlighted = false;
                }
            }
            listViewAdrBook.EndUpdate();
        }

        private void buttonAbHighlightGo_Click(object sender, EventArgs e)
        {
            if (radioAbHighlightCriteria.Checked) {
                AdrBookFindAndHighlight();
            } else if (radioAbHighlightSelect.Checked) {
                AdrBookSelectHighlighted();
            } else if (radioAbHighlightClear.Checked) {
                AdrBookClearHighlighted();
            }
        }

        private void buttonSmsHideUpdate_Click(object sender, EventArgs e)
        {
            if (radioSmsHideNothing.Checked) {
                PopulateSmsView(true, true, true);
            } else if (radioSmsHideChecked.Checked) {
                PopulateSmsView(false, true, false);
            } else {
                PopulateSmsView(false, false, true);
            }
        }

        private void buttonSmsRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Remove entries from results?", "Remove?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) {
                return;
            }
            if (radioSmsRemoveChecked.Checked) {
                RemoveCheckedSms();
            } else if (radioClRemoveHidden.Checked) {
                RemoveHiddenSms();
            }
        }

        private void RemoveCheckedSms()
        {
            List<SmsListViewItem> newSmsItems = new List<SmsListViewItem>();
            bool changed = false;
            foreach (SmsListViewItem item in smsItems) {
                if (item.Hidden) {
                    newSmsItems.Add(item);
                } else {
                    if (!item.Checked) {
                        newSmsItems.Add(item);
                    } else {
                        changed = true;
                    }
                }
            }
            smsItems = newSmsItems;
            if (changed) {
                PopulateSmsView();
            }
        }

        private void RemoveHiddenSms()
        {
            bool deleted = false;
            List<SmsListViewItem> newSmsItems = new List<SmsListViewItem>();
            foreach (SmsListViewItem item in smsItems) {
                if (!item.Hidden) {
                    newSmsItems.Add(item);
                } else {
                    deleted = true;
                }
            }
            smsItems = newSmsItems;
            if (deleted) {
                UpdateSummaryText(TabSelections.SMS);
            }
        }

        private void buttonSmsHighlightGo_Click(object sender, EventArgs e)
        {
            if (radioSmsHighlightCriteria.Checked) {
                SmsFindAndHighlight();
            } else if (radioSmsHighlightSelect.Checked) {
                SmsSelectHighlighted();
            } else if (radioSmsHighlightClear.Checked) {
                SmsClearHighlighted();
            }
        }

        private void SmsSelectHighlighted()
        {
            listViewSMS.Focus();
            listViewSMS.BeginUpdate();
            for (int indx = 0; indx < listViewSMS.Items.Count; indx++) {
                try {
                    SmsListViewItem item = listViewSMS.Items[indx] as SmsListViewItem;
                    if (item.Highlighted && !item.Selected) {
                        item.Selected = true;
                        item.Focused = true;
                    } else if (!item.Highlighted && item.Selected) {
                        item.Selected = false;
                        item.Focused = false;
                    }
                } catch {
                }
            }
            listViewSMS.EndUpdate();
        }

        private void SmsClearHighlighted()
        {
            ListViewItem lvi = new ListViewItem();
            listViewSMS.BeginUpdate();
            foreach (SmsListViewItem item in smsItems) {
                if (item.Highlighted) {
                    item.BackColor = lvi.BackColor;
                    item.Highlighted = false;
                }
            }
            listViewSMS.EndUpdate();
        }

        private void SmsFindAndHighlight()
        {
            switch (comboBoxSmsHighlight.SelectedIndex) {
                case 0:
                    SmsHighlightSimilarNum(true);
                    break;
                case 1:
                    SmsHighlightSimilarNum(false);
                    break;
                case 2:
                    SmsHighlightNumbersInCallLog();
                    break;
                case 3:
                    SmsHighlightNumbersInAdrBook();
                    break;
            }
        }

        private void SmsHighlightSimilarNum(bool num1)
        {
            if (listViewSMS.SelectedItems.Count != 1) {
                SimpleMessage("You must have a single item selected");
                return;
            }
            listViewSMS.BeginUpdate();
            try {
                SmsListViewItem smslvi = listViewSMS.SelectedItems[0] as SmsListViewItem;
                string number = (num1) ? smslvi.SmsEntry.MetaData.Number : smslvi.SmsEntry.MetaData.Number2;
                if (String.IsNullOrEmpty(number)) return;
                foreach (SmsListViewItem item in listViewSMS.Items) {
                    // Include selected value to be highlighted.
                    if (SimilarNumbers(number, item.SmsEntry.MetaData.Number) || SimilarNumbers(number, item.SmsEntry.MetaData.Number2)) {
                        item.BackColor = SimilarNumberColor;
                        item.Highlighted = true;
                    }
                }
            } catch {
            } finally {
                listViewSMS.EndUpdate();
            }
        }

        private void SmsHighlightNumbersInCallLog()
        {
            listViewSMS.BeginUpdate();
            try {
                foreach (SmsListViewItem item in listViewSMS.Items) {
                    string number1 = item.SmsEntry.MetaData.Number;
                    string number2 = item.SmsEntry.MetaData.Number2;
                    foreach (CallLogListViewItem clItem in callLogItems) {
                        // Include selected value to be highlighted.
                        if (SimilarNumbers(number1, clItem.CallLog.MetaData.Number) ||
                            SimilarNumbers(number2, clItem.CallLog.MetaData.Number)) {
                            item.BackColor = EntryInCallLogColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewSMS.EndUpdate();
            }
        }

        private void SmsHighlightNumbersInAdrBook()
        {
            listViewSMS.BeginUpdate();
            try {
                foreach (SmsListViewItem item in listViewSMS.Items) {
                    string number1 = item.SmsEntry.MetaData.Number;
                    string number2 = item.SmsEntry.MetaData.Number2;
                    foreach (AddressBookListViewItem abItem in adrBookItems) {
                        // Include selected value to be highlighted.
                        if (SimilarNumbers(number1, abItem.AddressBook.MetaData.Number) ||
                            SimilarNumbers(number2, abItem.AddressBook.MetaData.Number)) {
                            item.BackColor = EntryInAdrBookColor;
                            item.Highlighted = true;
                            break;
                        }
                    }
                }
            } catch {
            } finally {
                listViewSMS.EndUpdate();
            }
        }

        private void listViewCallLogs_DoubleClick(object sender, EventArgs e)
        {
#if DEBUG
            try {
                if (listViewCallLogs.SelectedItems.Count == 0) return;
                CallLogListViewItem item = listViewCallLogs.SelectedItems[0] as CallLogListViewItem;
                ShowItemOffset(item.CallLog.MetaData.Offset);
            } catch {
            }
#endif
        }

        private void ShowItemOffset(long offset)
        {
            Clipboard.SetText(offset.ToString());
            MessageBox.Show("Record offset (placed in clipboard): " + offset, "Offset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void listViewAdrBook_DoubleClick(object sender, EventArgs e)
        {
#if DEBUG
            try {
                if (listViewAdrBook.SelectedItems.Count == 0) return;
                AddressBookListViewItem item = listViewAdrBook.SelectedItems[0] as AddressBookListViewItem;
                ShowItemOffset(item.AddressBook.MetaData.Offset);
            } catch {
            }
#endif
        }

        private void listViewSMS_DoubleClick(object sender, EventArgs e)
        {
#if DEBUG
            try {
                if (listViewSMS.SelectedItems.Count == 0) return;
                SmsListViewItem item = listViewSMS.SelectedItems[0] as SmsListViewItem;
                ShowItemOffset(item.SmsEntry.MetaData.Offset);
            } catch {
            }
#endif
        }

    }
}