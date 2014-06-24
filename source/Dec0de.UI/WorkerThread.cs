/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

//#define LOADONLY
//#define SKIPIMAGES
//#define _GENERALPARSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using Dec0de.Bll;
using Dec0de.Bll.AnswerLoader;
using Dec0de.Bll.Viterbi;
using Dec0de.Bll.Filter;
using Dec0de.Bll.UserStates;
using Dec0de.UI.Database;
using Dec0de.UI.DecodeResults;
using Dec0de.UI.PostProcess;
using System.Collections.Concurrent;
#if !_USESQLSERVER_
using System.Data.SQLite;
#endif

namespace Dec0de.UI
{
    class WorkerThread
    {
        public Thread WorkThread = null;
        private readonly string filePath;
        private readonly string manufacturer;
        private readonly string model;
        private readonly string note;
        private readonly bool doNotStoreHashes;
        private readonly string memId;
        private volatile bool _cancel = false;
        private volatile bool _canAbort = false;
        private string fileSha1;
        private List<MetaResult> metaResults = null;
        private List<FieldPaths> fieldPaths = null;

        public WorkerThread(string memFilePath, string manufacturer, string model, string note, bool doNotStore)
        {
            this.WorkThread = null;
            this.filePath = memFilePath;
            this.manufacturer = manufacturer;
            this.model = model;
            this.note = note;
            this.doNotStoreHashes = true; /*doNotStore;*/
            this.memId = (Guid.NewGuid()).ToString();
            this.fieldPaths = new List<FieldPaths>();
        }

        /// <summary>
        /// It creates and starts a new thread for the Run() method of WorkerThread.
        /// </summary>
        public void Start()
        {
            this.WorkThread = new Thread(this.Run);
            this.WorkThread.Name = "Worker_Thread";
            this.WorkThread.Start();
        }

        public void Cancel()
        {
            _cancel = true;
            try
            {
                if (_canAbort)
                {
                    WorkThread.Abort();
                }
            }
            catch
            {
            }
        }


        /// <summary>
        /// This is where all of the work is done to extract information from
        /// the phone's binary file. 
        /// It calls methods for image block identification, removal, block hash filtering, field and record level Viterbi infrerence, postprocessing of results.
        /// </summary>
        private void Run()
        {
            bool success = false;
            PostProcessor postProcess = null;
            PhoneInfo phoneInfo = null;
            try
            {
                // Use the SHA1 of the binary file to identify it.
                _canAbort = true;
                StartStep(1);
                write("Calculating file SHA1");
                fileSha1 = DcUtils.CalculateFileSha1(this.filePath);
                if (_cancel) return;
                NextStep(1);
                // We scan the file to locate images. This is done independent
                // of block hashes.
                write("Extracting graphical images");
#if LOADONLY || SKIPIMAGES
                ImageFiles imageFiles = new ImageFiles();
#else
                ImageFiles imageFiles = ImageFiles.LocateImages(this.filePath);
                if (_cancel) return;
                _canAbort = false;
                write("Extracted {0} graphical images", imageFiles.ImageBlocks.Count);
#endif
                NextStep(2);
                if (_cancel) return;
                // Load the block hashes into the DB (if they're not already
                // there).
                HashLoader.HashLoader hashloader = HashLoader.HashLoader.LoadHashesIntoDB(fileSha1, this.filePath, this.manufacturer,
                    this.model, this.note, this.doNotStoreHashes);
                if (_cancel || (hashloader == null)) return;
                int phoneId = hashloader.PhoneId;
#if LOADONLY
                _cancel = true;
                return;
#endif
                _canAbort = true;
                NextStep(3);
                if (_cancel) return;
                // Identify block hashes that are already in the DB, which we
                // will then filter out.
                FilterResult filterResult = RunBlockHashFilter(fileSha1, phoneId, hashloader.GetAndForgetStoredHashes());
                hashloader = null;
                NextStep(4);
                // Since images were located before block hash filter, forget
                // about any image that overlaps a filtered block hash.
                write("Filtering image locations");
                //filterResult = ImageFiles.FilterOutImages(filterResult, imageFiles);
                //Dump_Filtered_Blocks(filterResult, filePath);
                NextStep(5);
                // Finally, we're ready to use the Viterbi state machine.
                // Start by identifying fields.
                ViterbiResult viterbiResultFields = RunViterbi(filterResult, fileSha1);
                // Allow garbage collection.
                filterResult.UnfilteredBlocks.Clear();
                NextStep(6);
                List<MetaField> addressBookEntries = new List<MetaField>();
                List<MetaField> callLogs = new List<MetaField>();
                List<MetaField> sms = new List<MetaField>();
                // Second run of Viterbi, this time for records.
                //ViterbiResult viterbiResultRecord = RunMetaViterbi(viterbiResultFields,addressBookEntries,callLogs,sms);
                RunMetaViterbi(viterbiResultFields, addressBookEntries, callLogs, sms);
                viterbiResultFields = null;
                NextStep(7);
                // Perform post processing. This may remove some records.
                postProcess = new PostProcessor(callLogs, addressBookEntries, sms, imageFiles.ImageBlocks);
                success = PerformPostProcessing(postProcess);
                NextStep(8);

                GTC_CSV_Writer wr = new GTC_CSV_Writer(this.metaResults, postProcess, filePath);
                wr.Write_CSV();
                wr.Write_Field_Paths(this.fieldPaths);

                // Finished.
                phoneInfo = new PhoneInfo(manufacturer, model, note, doNotStoreHashes);
                write("Finished work");
                FinishedStep(9);
            }
            finally
            {
                if (_cancel)
                {
                    MainForm.Program.EndWork(false, null, null, null);
                }
                else
                {
                    MainForm.Program.EndWork(success, postProcess, this.filePath, phoneInfo);
                }
            }
        }

        private void Dump_Filtered_Blocks(FilterResult result, string filepath)
        {
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            List<Block> blocks = new List<Block>(result.UnfilteredBlocks);
            System.IO.FileStream FileStream = new System.IO.FileStream("blocks_left", System.IO.FileMode.Create, System.IO.FileAccess.Write);

            for (int i = 0; i < blocks.Count; i++)
            {
                Block b = blocks[i];
                var bytes = BlockHashFilter.GetBytes(stream, b.OffsetFile, b.Length);
                FileStream.Write(bytes, 0, bytes.Length);
            }
            FileStream.Close();
        }

        private void StartStep(int n)
        {
            MainForm.Program.SetStepStarted(n);
        }

        private void NextStep(int n)
        {
            MainForm.Program.SetNextStep(n);
        }

        private void FinishedStep(int n)
        {
            MainForm.Program.SetStepCompleted(n);
        }



        public static void write(params Object[] values)
        {
#if false
            string text;
            if (values.Length == 1) {
                text = (string)values[0];
            } else if (values.Length == 2) {
                text = String.Format((string)values[0], values[1]);
            } else {
                Object[] parms = new Object[values.Length - 1];
                Array.Copy(values, 1, parms, 0, parms.Length);
                text = String.Format((string) values[0], parms);
            }
            Console.WriteLine(text);
            MainForm.Program.Output(DateTime.Now.ToString("HH:mm:ss - ") + text);
#endif
        }


        // NOTE: Most of the code below was taken from the original
        // dec0de console application. 

        private void InterpretResults(List<MetaResult> metaResults, ViterbiResult viterbiResult,
            List<MetaField> addressBookEntries, List<MetaField> callLogs, List<MetaField> sms)
        {
            for (int i = 0; i < viterbiResult.Fields.Count; i++)
            {
                try
                {
                    switch (viterbiResult.Fields[i].MachineName)
                    {
                        case MachineList.Meta_AddressBookNokia:
                        case MachineList.Meta_AddressBook:
                            var results = GetMetaAddressBookEntry(viterbiResult.Fields[i], metaResults);
                            addressBookEntries.AddRange(results);
                            break;

                        case MachineList.Meta_CallLogNokiaMulti_v2:
                        case MachineList.Meta_CallLogNokiaMulti:
                            var results2 = GetMetaCallLogNokia(viterbiResult.Fields[i], metaResults);
                            callLogs.AddRange(results2);
                            break;

                        case MachineList.Meta_CallLogAll:
                        case MachineList.Meta_CallLogGeneric:
                        case MachineList.Meta_CallLogGeneric2:
                        case MachineList.Meta_CallLogGeneric3:
                        case MachineList.Meta_CallLogGeneric4:
                        case MachineList.Meta_CallLogNokiaSingle:
                        case MachineList.Meta_CallLogMoto:
                        case MachineList.Meta_CallLogSamsung:
                            var results1 = GetMetaCallLog(viterbiResult.Fields[i], metaResults);
                            callLogs.AddRange(results1);
                            break;
                        case MachineList.Meta_Sms:
                            var result = GetMetaSms(viterbiResult.Fields[i], metaResults);
                            sms.Add(result);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private List<MetaAddressBookEntry> GetMetaAddressBookEntry(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            List<string> numbers = new List<string>();
            long startOffset = long.MaxValue;
            List<long> proximityOffsets = new List<long>();

            FieldPaths path = new FieldPaths();
            for (int i = 0; i < field.Raw.Length; i++)
            {
                if (i == 0)
                {
                    path._path_beg_offset = metaResults[(int)field.OffsetFile].Field.OffsetFile;
                }
                if (i == field.Raw.Length - 1)
                {
                    path._path_end_offset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
                }
                path._fields_in_path.Add(metaResults[(int)field.OffsetFile + i].Field.MachineName.ToString());

                if (field.Raw[i] == (byte)MetaMachine.Text && name == null)
                    name = metaResults[(int)field.OffsetFile + i].Field.FieldString;
                else if (field.Raw[i] == (byte)MetaMachine.Text)
                    name += " " + metaResults[(int)field.OffsetFile + i].Field.FieldString;
                else if (field.Raw[i] == (byte)MetaMachine.PhoneNumber)
                {
                    numbers.Add(metaResults[(int)field.OffsetFile + i].Field.FieldString);
                    proximityOffsets.Add(metaResults[(int)field.OffsetFile + i].Field.OffsetFile);
                }
                if (i == 0)
                {
                    startOffset = Math.Min(startOffset, metaResults[(int)field.OffsetFile + i].Field.OffsetFile);
                }
            }
            path.find_actual_path();
            fieldPaths.Add(path);

            var entries = new List<MetaAddressBookEntry>();

            for (int i = 0; i < numbers.Count; i++)
            {

                var entry = new MetaAddressBookEntry
                {
                    Name = name,
                    Number = numbers[i],
                    SevenDigit = Utilities.GetLastSevenDigits(numbers[i]),
                    Offset = startOffset,
                    MachineName = field.MachineName,
                    ProximityOffset = proximityOffsets[i]
                };


                if (entry.Name == null)
                    entry.Name = MetaField.DEFAULT_STRING;

                if (entry.Number == null)
                    entry.Number = MetaField.DEFAULT_STRING;

                if (entry.SevenDigit == null)
                    entry.SevenDigit = MetaField.DEFAULT_STRING;

                entries.Add(entry);

            }

            return entries;
        }

        private List<MetaCallLog> GetMetaCallLogNokia(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            var numbers = new List<string>();
            List<DateTime> timeStamps = new List<DateTime>();
            string type = null;
            long startOffset = -1;
            List<long> proximityOffsets = new List<long>();

            var phoneIndex = new List<string>();
            var timeStampIndex = new List<string>();

            FieldPaths path = new FieldPaths();
            for (int i = 0; i < field.Raw.Length; i++)
            {
                if (i == 0)
                {
                    path._path_beg_offset = metaResults[(int)field.OffsetFile].Field.OffsetFile;
                }
                if (i == field.Raw.Length - 1)
                {
                    path._path_end_offset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
                }
                path._fields_in_path.Add(metaResults[(int)field.OffsetFile + i].Field.MachineName.ToString());

                if (field.Raw[i] == (byte)MetaMachine.Text && name == null)
                    name = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                //There should be a binary field in between.
                else if (field.Raw[i] == (byte)MetaMachine.CallLogNumberIndex && field.Raw[i + 2] == (byte)MetaMachine.PhoneNumber)
                {
                    phoneIndex.Add(metaResults[(int)field.OffsetFile + i].Field.FieldString);

                    numbers.Add(metaResults[(int)field.OffsetFile + i + 2].Field.FieldString);

                    i += 2;
                }
                else if (field.Raw[i] == (byte)MetaMachine.TimeStamp && field.Raw[i + 1] == (byte)MetaMachine.CallLogNumberIndex)
                {
                    timeStamps.Add(DateTime.Parse(metaResults[(int)field.OffsetFile + i].Field.FieldString));
                    timeStampIndex.Add(metaResults[(int)field.OffsetFile + i + 1].Field.FieldString);
                    proximityOffsets.Add(metaResults[(int)field.OffsetFile + i].Field.OffsetFile);
                    i++;
                }
                else if (field.Raw[i] == (byte)MetaMachine.CallLogType && type == null)
                    type = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                if (startOffset == -1)
                {
                    startOffset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
                }
            }
            path.find_actual_path();
            fieldPaths.Add(path);

            var entries = new List<MetaCallLog>();

            for (int i = 0; i < timeStamps.Count; i++)
            {
                int numberI = -1;
                //Get phone number
                for (int j = 0; j < phoneIndex.Count; j++)
                {
                    if (timeStampIndex[i] == phoneIndex[j])
                    {
                        numberI = j;
                        break;
                    }
                }

                string number = (numberI != -1) ? numbers[numberI] : "*NONE*";

                var entry = new MetaCallLog()
                {
                    Name = name,
                    Number = number,
                    SevenDigit = Utilities.GetLastSevenDigits(number),
                    TimeStamp = timeStamps[i],
                    Type = type,
                    Offset = startOffset,
                    MachineName = field.MachineName,
                    ProximityOffset = proximityOffsets[i]
                };

                if (entry.Name == null)
                    entry.Name = MetaField.DEFAULT_STRING;

                if (entry.Number == null)
                    entry.Number = MetaField.DEFAULT_STRING;

                if (entry.SevenDigit == null)
                    entry.SevenDigit = MetaField.DEFAULT_STRING;

                if (entry.Type == null)
                    entry.Type = MetaField.DEFAULT_STRING;

                if (entry.TimeStamp == null)
                    entry.TimeStamp = MetaField.DEFAULT_DATE;

                entries.Add(entry);

            }

            return entries;
        }

        private List<MetaCallLog> GetMetaCallLog(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            string number = null;
            List<DateTime> timeStamps = new List<DateTime>();
            string type = null;
            long startOffset = -1;
            List<long> proximityOffsets = new List<long>();

            FieldPaths path = new FieldPaths();
            for (int i = 0; i < field.Raw.Length; i++)
            {
                if (i == 0)
                {
                    path._path_beg_offset = metaResults[(int)field.OffsetFile].Field.OffsetFile;
                }
                if (i == field.Raw.Length - 1)
                {
                    path._path_end_offset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
                }
                path._fields_in_path.Add(metaResults[(int)field.OffsetFile + i].Field.MachineName.ToString());

                if (field.Raw[i] == (byte)MetaMachine.Text && name == null)
                    name = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                else if (field.Raw[i] == (byte)MetaMachine.PhoneNumber && number == null)
                    number = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                else if (field.Raw[i] == (byte)MetaMachine.TimeStamp)
                {
                    timeStamps.Add(DateTime.Parse(metaResults[(int)field.OffsetFile + i].Field.FieldString));
                    proximityOffsets.Add(metaResults[(int)field.OffsetFile + i].Field.OffsetFile);
                }
                else if (field.Raw[i] == (byte)MetaMachine.CallLogType && type == null)
                    type = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                if (startOffset == -1)
                {
                    startOffset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
                }
            }
            path.find_actual_path();
            fieldPaths.Add(path);

            var entries = new List<MetaCallLog>();

            for (int i = 0; i < timeStamps.Count; i++)
            {
                var entry = new MetaCallLog()
                {
                    Name = name,
                    Number = number,
                    SevenDigit = Utilities.GetLastSevenDigits(number),
                    TimeStamp = timeStamps[i],
                    Type = type,
                    Offset = startOffset,
                    MachineName = field.MachineName,
                    ProximityOffset = proximityOffsets[i]
                };

                if (entry.Name == null)
                    entry.Name = MetaField.DEFAULT_STRING;

                if (entry.Number == null)
                    entry.Number = MetaField.DEFAULT_STRING;

                if (entry.SevenDigit == null)
                    entry.SevenDigit = MetaField.DEFAULT_STRING;

                if (entry.Type == null)
                    entry.Type = MetaField.DEFAULT_STRING;

                if (entry.TimeStamp == null)
                    entry.TimeStamp = MetaField.DEFAULT_DATE;

                entries.Add(entry);

            }

            return entries;
        }

        private MetaSms GetMetaSms(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            string number = null;
            string number2 = null;
            DateTime? timeStamp = null;
            string message = null;
            long startOffset = -1;

            FieldPaths path = new FieldPaths();
            for (int i = 0; i < field.Raw.Length; i++)
            {
                if (i == 0)
                {
                    path._path_beg_offset = metaResults[(int)field.OffsetFile].Field.OffsetFile;
                }
                if (i == field.Raw.Length - 1)
                {
                    path._path_end_offset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
                }
                path._fields_in_path.Add(metaResults[(int)field.OffsetFile + i].Field.MachineName.ToString());

                //if (field.Raw[i] == (byte)MetaMachine.Text && name == null)
                //    name = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                if (field.Raw[i] == (byte)MetaMachine.PhoneNumber && number == null)
                    number = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                else if (field.Raw[i] == (byte)MetaMachine.PhoneNumber && number2 == null)
                    number2 = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                else if (field.Raw[i] == (byte)MetaMachine.TimeStamp && timeStamp == null)
                    timeStamp = DateTime.Parse(metaResults[(int)field.OffsetFile + i].Field.FieldString);

                else if (field.Raw[i] == (byte)MetaMachine.Text && message == null)
                    message = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                if (startOffset == -1)
                {
                    startOffset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
                }
            }
            path.find_actual_path();
            fieldPaths.Add(path);


            var entry = new MetaSms()
            {
                Name = name,
                Number = number,
                Number2 = number2,
                SevenDigit = Utilities.GetLastSevenDigits(number),
                SevenDigit2 = Utilities.GetLastSevenDigits(number2),
                TimeStamp = timeStamp,
                Message = message,
                Offset = startOffset,
                MachineName = field.MachineName,
                ProximityOffset = startOffset
            };

            if (entry.Name == null)
                entry.Name = MetaField.DEFAULT_STRING;

            if (entry.Number == null)
                entry.Number = MetaField.DEFAULT_STRING;

            if (entry.SevenDigit == null)
                entry.SevenDigit = MetaField.DEFAULT_STRING;

            if (entry.Number2 == null)
                entry.Number2 = MetaField.DEFAULT_STRING;

            if (entry.SevenDigit2 == null)
                entry.SevenDigit2 = MetaField.DEFAULT_STRING;

            if (entry.Message == null)
                entry.Message = MetaField.DEFAULT_STRING;

            if (entry.TimeStamp == null)
                entry.TimeStamp = MetaField.DEFAULT_DATE;

            return entry;
        }

        private List<MetaResult> CreateMetaInfo(List<ViterbiField> fields)
        {
            const int LONG_GAP_BYTES = 200;
            var metaMachines = new List<MetaResult>();

            long nextIndex = 0;

            //Run through the fields determining if it is a metafield, i.e. phone number, timestamp, text, etc.
            for (int i = 0; i < fields.Count; i++)
            {

                //Check if there are gaps
                bool isGap = fields[i].OffsetFile > nextIndex;

                if (isGap)
                {
                    //If there is a gap, then we need to add a binary or binary large field

                    long gap = fields[i].OffsetFile - nextIndex;

                    ViterbiField binary = new ViterbiField { FieldString = "" };

                    if (gap > LONG_GAP_BYTES)
                        metaMachines.Add(new MetaResult { Name = MetaMachine.BinaryLarge, Field = binary });
                    else
                        metaMachines.Add(new MetaResult { Name = MetaMachine.Binary, Field = binary });
                }

                nextIndex = fields[i].OffsetFile + fields[i].Length;

                var meta = GetMetaMachine(fields[i].MachineName);

                metaMachines.Add(new MetaResult { Name = meta, Field = fields[i] });
            }

            return metaMachines;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns>Returns the meta machine. If the machine is not known, then it will return binary</returns>
        private MetaMachine GetMetaMachine(MachineList field)
        {
            bool isPhone = (Convert.ToString(field).StartsWith("PhoneNumber_"));

            if (isPhone)
                return MetaMachine.PhoneNumber;

            bool isTimeStamp = Convert.ToString(field).StartsWith("TimeStamp_");

            if (isTimeStamp)
                return MetaMachine.TimeStamp;

            bool isText = Convert.ToString(field).StartsWith("Text_");

            if (isText)
                return MetaMachine.Text;

            bool isSmsPrepend = Convert.ToString(field).StartsWith("Prepend_");

            if (isSmsPrepend)
                return MetaMachine.SmsPrepend;

            bool isCallLogType = Convert.ToString(field).StartsWith("CallLogType_");

            if (isCallLogType)
                return MetaMachine.CallLogType;

            bool isCallLogNumberIndex = Convert.ToString(field).StartsWith("PhoneNumberIndex_");

            if (isCallLogNumberIndex)
                return MetaMachine.CallLogNumberIndex;

            bool isCallLogTypePrepend = Convert.ToString(field).StartsWith("CallLogTypePrepend_");

            if (isCallLogTypePrepend)
                return MetaMachine.CallLogTypePrepend;


            bool isSamsungSmsMarker = Convert.ToString(field).StartsWith("Marker_SamsungSms");

            if (isSamsungSmsMarker)
                return MetaMachine.MarkerSamsungSms;

            return MetaMachine.Binary;
        }


        private FilterResult RunBlockHashFilter(string fileSha1, int phoneId, List<BlockHashFilter.UnfilterdBlockResult> storedBlocks)
        {
            FilterResult filterResult = null;
            try
            {
                write("Starting block hash filtering");
                DateTime dt = DateTime.Now;
                BlockHashFilter bhf = new BlockHashFilter(this.filePath, HashLoader.HashLoader.DefaultBlockSize,
                    HashLoader.HashLoader.DefaultSlideAmount, fileSha1, true, storedBlocks);
#if _USESQLSERVER_
                    filterResult = bhf.Filter(phoneId);
#else
                SQLiteConnection sql = DatabaseAccess.OpenSql(true);
                try
                {
                    filterResult = bhf.Filter(phoneId, sql, true);
                }
                finally
                {
                    sql.Close();
                    sql.Dispose();
                }
#endif
                TimeSpan ts = DateTime.Now.Subtract(dt);
                write("Time elapsed for block hash filtering: {0}", ts.ToString("c"));
                long nBytes = filterResult.FilteredBytesCount + filterResult.UnfilteredBytesCount;
                write("Filtered {0} out of {1} bytes: {2:0.00###}", filterResult.FilteredBytesCount,
                      nBytes, (nBytes - filterResult.UnfilteredBytesCount) / (double)nBytes);
            }
            catch (ThreadAbortException)
            {
                return null;
            }
            catch (Exception ex)
            {
                DisplayExceptionMessages(ex, "Block Hash Filter");
                return null;
            }
            return filterResult;
        }

        private ViterbiResult RunViterbi(FilterResult filterResult, string fileSha1)
        {
            // Load any user-defined state machines.
            List<UserState> userStates;
            try
            {
                userStates = Loader.LoadUserStates(MainForm.Program.UserStatesEnabled,
                                                   MainForm.Program.UserStatesPath);
            }
            catch (Exception ex)
            {
                DisplayExceptionMessages(ex, "User-Defined States");
                return null;
            }
            ViterbiResult viterbiResultFields = null;           

            try
            {
                write("Running Viterbi on fields");
                DateTime dt = DateTime.Now;
#if _GENERALPARSE
                Viterbi viterbi = new Viterbi(RunType.GeneralParse, false);
                viterbiResultFields = viterbi.Run(filterResult.UnfilteredBlocks, this.filePath);
#else
#if SKIPREALWORK
                    viterbiResultFields = new ViterbiResult(); 
                    viterbiResultFields.Fields = new List<ViterbiField>();
#else                
                if (filterResult.UnfilteredBlocks.Count > 0)
                {
                    ThreadedViterbi tv = new ThreadedViterbi(filterResult.UnfilteredBlocks, RunType.GeneralParse, userStates, this.filePath, this.fileSha1);
                    viterbiResultFields = tv.RunThreadedViterbi();
                    TimeSpan ts = DateTime.Now.Subtract(dt);
                    write("Time elapsed for Viterbi fields: {0}", ts.ToString("c"));
                    write("Field count: {0}", viterbiResultFields.Fields.Count);
                }
#endif
#endif
                filterResult.UnfilteredBlocks.Clear(); // Allow gc to clean things up
            }
            catch (ThreadAbortException)
            {
                return null;
            }
            catch (Exception ex)
            {
                DisplayExceptionMessages(ex, "Viterbi Fields");
                return null;
            }
            return viterbiResultFields;
        }        

        private ViterbiResult RunMetaViterbi(ViterbiResult viterbiResultFields, List<MetaField> addressBookEntries, List<MetaField> callLogs, List<MetaField> sms)
        {
            ViterbiResult viterbiResultRecord = null;
            try
            {
                if (viterbiResultFields != null)
                {
                    write("Running Viterbi on records");
                    DateTime dt = DateTime.Now;
                    metaResults = CreateMetaInfo(viterbiResultFields.Fields);
                    Block block = new Block()
                    {
                        Bytes = metaResults.Select(r => (byte)r.Name).ToArray(),
                        OffsetFile = 0
                    };
                    var blockList = new List<Block> { block };
                    List<UserState> user_state = new List<UserState>();
                    ThreadedViterbi tv = new ThreadedViterbi(blockList, RunType.Meta, user_state, this.filePath, this.fileSha1);
                    viterbiResultRecord = tv.RunThreadedViterbi();                                       
#if false
                    TextWriter tw = null;
                    try {
                        if (viterbiResultRecord == null) throw new Exception("No results");
                        tw = new StreamWriter(Path.Combine(@"C:\temp", String.Format("Records_{0}.csv", DateTime.Now.ToString("yyyyMMdd_HHmm"))));
                        foreach (ViterbiField f in viterbiResultRecord.Fields) {
                            tw.WriteLine("{0}\t{1}\t{2}", f.OffsetFile, f.FieldString, f.MachineName.ToString());
                        }
                    } catch (Exception ex) {
                    } finally {
                        if (tw != null) tw.Close();
                    }
#endif
                    TimeSpan ts = DateTime.Now.Subtract(dt);
                    write("Time elapsed for Viterbi records: {0}", ts.ToString("c"));
                    InterpretResults(metaResults, viterbiResultRecord, addressBookEntries, callLogs, sms);
                    write("Entries: call log = {0}, address book = {1}, sms = {2}", callLogs.Count,
                          addressBookEntries.Count,
                          sms.Count);
                }
            }
            catch (ThreadAbortException)
            {
                return null;
            }
            catch (Exception ex)
            {
                DisplayExceptionMessages(ex, "Viterbi Records");
                return null;
            }
            return viterbiResultRecord;
        }

        private bool PerformPostProcessing(PostProcessor postProcess)
        {
            try
            {
                write("Begin post-processing");
                // PostProcessor new_ref = new PostProcessor(callLogs, addressBookEntries, sms, imageFiles.ImageBlocks);
                // new_ref.Process();
                // postProcess.addressBookFields = new_ref.addressBookFields;
                // postProcess.callLogFields = new_ref.callLogFields;
                // postProcess.smsFields = new_ref.smsFields;
                postProcess.Process();
                return true;
            }
            catch (ThreadAbortException)
            {
                return false;
            }
            catch (Exception ex)
            {
                DisplayExceptionMessages(ex, "Post Processing");
                return false;
            }
        }

        /*
         * Displays the exception message along with inner exceptions in a
         * message box.
         */
        private void DisplayExceptionMessages(Exception ex, string title)
        {
            string msg = ex.Message;
            Exception innerEx = ex.InnerException;
            for (int n = 0; (n < 6) && (innerEx != null); n++)
            {
                msg += Environment.NewLine + Environment.NewLine + innerEx.Message;
                innerEx = innerEx.InnerException;
            }
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }
}
