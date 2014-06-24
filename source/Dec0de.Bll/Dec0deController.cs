using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dec0de.Bll.AnswerLoader;
using Dec0de.Bll.EmbeddedDal;
using Dec0de.Bll.Filter;
using Dec0de.Bll.Viterbi;

namespace Dec0de.Bll
{
    public class Dec0deController
    {
        public const int LONG_GAP_BYTES = 200;

        private int _phoneId;
        private string _inputFile;
        private string _answerFile;
        private int _blockSize;
        private int _slideAmount;
        private readonly string _memoryId;
        private readonly bool _noFilter;

        public Dec0deController(int phoneId, string inputFile, string memoryId, int blockSize, int slideAmount)
        {
            _phoneId = phoneId;
            _inputFile = inputFile;
            _memoryId = memoryId;
            _blockSize = blockSize;
            _slideAmount = slideAmount;
        }

        public Dec0deController(int phoneId, string inputFile, string memoryId, int blockSize, int slideAmount, bool doNotFilter)
        {
            _phoneId = phoneId;
            _inputFile = inputFile;
            _memoryId = memoryId;
            _blockSize = blockSize;
            _slideAmount = slideAmount;
            _noFilter = doNotFilter;
        }

        public void Run(RunType type)
        {
            //Console.WriteLine("\nLoading hashes. {0}", start);
            //phoneId = HashLoaderProgram.ProcessFile(new BinaryFile(inputFile), blockSize, slideAmount);



            Console.WriteLine("\n\nPhone {0}", _phoneId);

            Console.WriteLine("Starting the Block Hash Filtering. {0}", DateTime.Now);

            var filterResult = RunBlockHashFilter();

            Console.WriteLine("Finished the Block Hash Filtering. Duration: {0}", filterResult.Duration);

            Console.WriteLine("Starting Viterbi.");

            var viterbiResult = RunViterbi(filterResult.UnfilteredBlocks, type);

            Console.WriteLine("Finished Viterbi. Duration: {0}", viterbiResult.Duration);

            var start = DateTime.Now;

            Console.WriteLine("Inserting results into database. {0}", start);

            int parseId;

            using (var dataContext = Dalbase.GetDataContext())
            {
                var records = dataContext.usp_Parse_Insert(DateTime.UtcNow, _phoneId, _memoryId, _blockSize,
                                             _slideAmount, filterResult.FilteredBytesCount,
                                             filterResult.UnfilteredBytesCount,
                                             Convert.ToInt32(filterResult.Duration.TotalSeconds),
                                             Convert.ToInt32(viterbiResult.Duration.TotalSeconds),
                                             Convert.ToString(type));

                parseId = (from record in records select record.parseId).First();

                Console.WriteLine("ParseId {0}", parseId);

                var fields = viterbiResult.Fields;

                for (int i = 0; i < fields.Count; i++)
                {
                    dataContext.usp_ParsedFields_Insert(parseId, fields[i].OffsetFile,
                                                        Convert.ToString(fields[i].MachineName), fields[i].HexString,
                                                        fields[i].FieldString);
                }
                try
                {

                    dataContext.usp_ParsedPhoneNum_AutoUpdate(parseId);
                }
                catch (Exception)
                {
                }
            }

            Console.WriteLine("Finished inserting. Duration: {0}", DateTime.Now - start);

            Console.WriteLine("Beging Viterbi Record Parse");

            RunMetaViterbi(parseId, viterbiResult.Fields);
        }

        public FilterResult RunBlockHashFilter()
        {
            var filter = new BlockHashFilter(_inputFile, _blockSize, _slideAmount, _memoryId, _noFilter);
            return filter.Filter(_phoneId);
        }

        public ViterbiResult RunViterbi(List<Block> unfilteredBlocks, RunType type)
        {

            if (type == RunType.AnchorPoints)
            {
                var viterbiAnchor = new AnchorViterbi(RunType.GeneralParse, _inputFile);

                return viterbiAnchor.Run(unfilteredBlocks);
            }
            else
            {
                var viterbi = new Viterbi.Viterbi(type, false);
                return viterbi.Run(unfilteredBlocks, _inputFile);
            }
        }

        public void RunMetaViterbi(int parseId, List<ViterbiField> viterbiFields)
        {
            var metaResults = CreateMetaInfo(viterbiFields);

            var block = new Block() { Bytes = metaResults.Select(r => (byte)r.Name).ToArray(), OffsetFile = 0 };

            var blockList = new List<Block> { block };

            var viterbiResult = RunViterbi(blockList, RunType.Meta);

            var addressBookEntries = new List<MetaField>();
            var callLogs = new List<MetaField>();
            var sms = new List<MetaField>();

            for (int i = 0; i < viterbiResult.Fields.Count; i++)
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
                        break;
                }
            }

            ///TODO: UNCOMMent these lines of code to insert records into the database
#if _INSERT_
            MetaField.Insert(parseId, addressBookEntries, true, "Dec0de");
            MetaField.Insert(parseId, callLogs, true, "Dec0de");
            MetaField.Insert(parseId, sms, true, "Dec0de");
#endif
        }

        public List<MetaAddressBookEntry> GetMetaAddressBookEntry(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            List<string> numbers = new List<string>();
            long startOffset = long.MaxValue;

            for (int i = 0; i < field.Raw.Length; i++)
            {
                if (field.Raw[i] == (byte)MetaMachine.Text && name == null)
                    name = metaResults[(int)field.OffsetFile + i].Field.FieldString;
                else if (field.Raw[i] == (byte)MetaMachine.Text)
                    name += " " + metaResults[(int)field.OffsetFile + i].Field.FieldString;
                else if (field.Raw[i] == (byte)MetaMachine.PhoneNumber)
                    numbers.Add(metaResults[(int)field.OffsetFile + i].Field.FieldString);
                if (i == 0)
                    startOffset = Math.Min(startOffset, metaResults[(int)field.OffsetFile + i].Field.OffsetFile);
            }

            var entries = new List<MetaAddressBookEntry>();

            for (int i = 0; i < numbers.Count; i++)
            {

                var entry = new MetaAddressBookEntry
                                {
                                    Name = name,
                                    Number = numbers[i],
                                    SevenDigit = Utilities.GetLastSevenDigits(numbers[i]),
                                    Offset = startOffset
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

        public List<MetaCallLog> GetMetaCallLogNokia(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            var numbers = new List<string>();
            List<DateTime> timeStamps = new List<DateTime>();
            string type = null;
            long startOffset = -1;

            var phoneIndex = new List<string>();
            var timeStampIndex = new List<string>();

            for (int i = 0; i < field.Raw.Length; i++)
            {
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

                    i++;
                }

                else if (field.Raw[i] == (byte)MetaMachine.CallLogType && type == null)
                    type = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                if (startOffset == -1)
                    startOffset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
            }

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
                                    Offset = startOffset
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

        public List<MetaCallLog> GetMetaCallLog(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            string number = null;
            List<DateTime> timeStamps = new List<DateTime>();
            string type = null;
            long startOffset = -1;

            for (int i = 0; i < field.Raw.Length; i++)
            {
                if (field.Raw[i] == (byte)MetaMachine.Text && name == null)
                    name = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                else if (field.Raw[i] == (byte)MetaMachine.PhoneNumber && number == null)
                    number = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                else if (field.Raw[i] == (byte)MetaMachine.TimeStamp)
                    timeStamps.Add(DateTime.Parse(metaResults[(int)field.OffsetFile + i].Field.FieldString));

                else if (field.Raw[i] == (byte)MetaMachine.CallLogType && type == null)
                    type = metaResults[(int)field.OffsetFile + i].Field.FieldString;

                if (startOffset == -1)
                    startOffset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
            }

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
                                    Offset = startOffset
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

        public MetaSms GetMetaSms(ViterbiField field, List<MetaResult> metaResults)
        {
            string name = null;
            string number = null;
            string number2 = null;
            DateTime? timeStamp = null;
            string message = null;
            long startOffset = -1;

            for (int i = 0; i < field.Raw.Length; i++)
            {
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
                    startOffset = metaResults[(int)field.OffsetFile + i].Field.OffsetFile;
            }

     

            var entry = new MetaSms()
                {
                    Name = name,
                    Number = number,
                    Number2 = number2,
                    SevenDigit = Utilities.GetLastSevenDigits(number),
                    SevenDigit2 = Utilities.GetLastSevenDigits(number2),
                    TimeStamp = timeStamp,
                    Message = message,
                    Offset = startOffset
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

        public List<MetaResult> CreateMetaInfo(List<ViterbiField> fields)
        {
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
        /// <returns>Returns the meta machine. If the machine is not know, then it will return binary</returns>
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


        public static void Load(MetaFieldType type, int phoneId, string file)
        {
            List<MetaField> entries = null;

            switch (type)
            {
                case MetaFieldType.CallLog:
                    entries = ParseCsvFile_CallLogs(file);
                    break;
                case MetaFieldType.Sms:
                    entries = ParseCsvFile_SMS(file);
                    break;
                case MetaFieldType.AddressBookEntry:
                    entries = ParseCsvFile_AddressBookEntries(file);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }

            Console.WriteLine("File contained {0} valid entries.", entries.Count);

#if _INSERT_
            MetaField.Insert(phoneId, entries, false, "xry");
#endif
        }

        /// <summary>
        /// The format of the address book csv file is Name, number 1, number 2, ..., number n. The file line is ignored.
        /// </summary>
        /// <param name="phoneId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static List<MetaField> ParseCsvFile_AddressBookEntries(string file)
        {
            string[] lines = File.ReadAllLines(file);

            var entries = new List<MetaField>();

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    continue;

                string name = parts[0].Trim();

                for (int j = 1; j < parts.Length; j++)
                {
                    string number = parts[j].Trim();
                    string sevenDigit = Utilities.GetLastSevenDigits(number);

                    entries.Add(new MetaAddressBookEntry { Name = name, Number = number, SevenDigit = sevenDigit });
                }
            }

            return entries;
        }

        /// <summary>
        /// Parses a CSV file containing call logs. The format of the file should be Type, Name, Number, TimeStamp. Ignores the
        /// first line of the file (used for the column headers)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static List<MetaField> ParseCsvFile_CallLogs(string file)
        {
            //Index of the revelant field
            const int TYPE = 0;
            const int NAME = 1;
            const int NUMBER = 2;
            const int TIMESTAMP = 3;

            string[] lines = File.ReadAllLines(file);

            var entries = new List<MetaField>();

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 4)
                {
                    Console.WriteLine("Skipped line {0} : {1}", i, lines[i]);
                    continue;
                }

                string type = parts[TYPE].Trim().ToLower();
                string name = parts[NAME].Trim();
                string number = parts[NUMBER].Trim();
                string sevenDigit = Utilities.GetLastSevenDigits(number);
                DateTime timeStamp = DateTime.Parse(parts[TIMESTAMP]);

                entries.Add(new MetaCallLog() { Name = name, Number = number, SevenDigit = sevenDigit, TimeStamp = timeStamp, Type = type });
            }

            return entries;
        }

        /// <summary>
        /// Parses a CSV file containing SMS records. The format of the file should be Number, Name, Message, TimeStamp. Ignores the
        /// first line of the file (used for the column headers)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static List<MetaField> ParseCsvFile_SMS(string file)
        {
            //Index of the revelant field
            const int NUMBER = 0;
            const int NAME = 1;
            const int MESSAGE = 2;
            const int TIMESTAMP = 3;

            string[] lines = File.ReadAllLines(file);

            var entries = new List<MetaField>();

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 4)
                {
                    Console.WriteLine("Skipped line {0} : {1}", i, lines[i]);
                    continue;
                }

                string message = parts[MESSAGE].Trim();
                string name = parts[NAME].Trim();
                string number = parts[NUMBER].Trim();
                string sevenDigit = Utilities.GetLastSevenDigits(number);

                if (parts[TIMESTAMP].Contains("("))
                    parts[TIMESTAMP] = parts[TIMESTAMP].Split('(')[0];

                DateTime timeStamp = DateTime.Parse(parts[TIMESTAMP]);

                entries.Add(new MetaSms() { Name = name, Number = number, SevenDigit = sevenDigit, TimeStamp = timeStamp, Message = message });
            }

            return entries;
        }

    }
}
