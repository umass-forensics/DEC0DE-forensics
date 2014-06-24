using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dec0de.Bll.Viterbi
{
    public class Program
    {
        #region Test Observations
        //XXXXXXX
        //0x58, 0x58,0x58,0x58,0x58,0x58,0x58,0x58
        private static byte[] Test_XXXXXXX = new byte[] { 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58 };

        private static byte[] Test_XXXXXXX_Unicode = new byte[] { 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58 };

        private static byte[] Test_MotoUnicode_bad = new byte[] {0x00, 0x31, 0x00, 0x35, 0x00, 0x30, 0x00, 0x20, 0x00, 0x33, 0x00, 0x41, 0x00, 0x37, 0x00, 0x34, 0x00, 0x31, 0x00, 0x35, 0x00, 0x30};

        private static byte[] Test_SamsungPhone11DigitAscii_bad = new byte[] { 0x0f, 0x33, 0x32, 0x33, 0x38, 0x33, 0x36, 0x38, 0x39, 0x38, 0x31 };

        private static byte[] Test_SamsungPhone11DigitAscii = new byte[] { 0x31, 0x33, 0x32, 0x33, 0x38, 0x33, 0x36, 0x38, 0x39, 0x38, 0x31 };

        private static byte[] Test_NokiaPhone11Digit = new byte[] { 0x0B, 0x17, 0xA2, 0x37, 0x27, 0x91, 0x60 };

        private static byte[] Test_NokiaPhone12Digit = new byte[] { 0x0C, 0xF1, 0x91, 0x62, 0xA8, 0x29, 0x79 };

        private static byte[] Test_NokiaPhone10Digit = new byte[] { 0x0A, 0x91, 0x65, 0x49, 0x58, 0xA4 };

        private static byte[] Test_NokiaPhone7Digit = new byte[] { 0x07, 0x54, 0x95, 0x8A, 0x40 };

        private static byte[] Test_NokiaPhone8Digit = new byte[] { 0x08, 0xF7, 0x17, 0x71, 0x72 };

        private static byte[] Test_SmsPhone = new byte[] { 0x0B, 0x91, 0x61, 0x63, 0x83, 0x84, 0x08, 0xF1 };

        private static byte[] Test_ShortAscii = new byte[] {0x52, 0x6F, 0x62};

        //Bro magnum Entry
        private static byte[] Test_BroMagnum = new byte[]
                                    {
                                        0x42, 0x72, 0x6F, 0x20, 0x4D, 0x61, 0x67, 0x6E, 0x75, 0x6D, 0xFF, 0x06, 0x81, 0x79,
                                        0x12, 0x11, 0x11, 0x11, 0xFF, 0xFF, 0xFF
                                    };

        //Moto Unicode Phone
        private static byte[] Test_MotoUnicodePhone = new byte[]
                                                     {
                                                         0x00, 0x31, 0x00, 0x34, 0x00, 0x30, 0x00, 0x35, 0x00, 0x34, 
                                                         0x00, 0x32, 0x00, 0x30, 0x00, 0x32, 0x00, 0x32, 0x00, 0x38, 
                                                         0x00, 0x33
                                                     };

        private static byte[] Test_Moto7Digit = new byte[]{ 0x05, 0x81, 0x36, 0x76, 0x41, 0xF4};

        private static byte[] Test_Moto10Digit = new byte[] { 0x06, 0x81, 0x16, 0x96, 0x08, 0x42, 0x50 };
        private static byte[] Test_Moto11Digit = new byte[] { 0x07, 0x81, 0x81, 0x00, 0x45, 0x37, 0x00, 0xF0 };

        private static byte[] Test_NokiaTimeStamp = new byte[] { 0x07, 0xD6, 0x02, 0x0E, 0x11, 0x29, 0x18 };

        private static byte[] Test_MotoTimeStamp = new byte[] { 0x00, 0x02, 0x47, 0x01, 0x34, 0x54 };

        private static byte[] Test_MotoCallLog = new byte[] { 0xE6, 0x4A, 0x4F, 0x53, 0x45, 0x50, 0x48, 0x49, 0x4E, 0x45, 0xFF, 0xFF, 0xFF,
                                                            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                            0x06, 0x81, 0x17, 0x24, 0x69, 0x30, 0x73, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                            0xFF, 0xFF, 0x00, 0x00, 0x46, 0x46, 0xE6, 0xDB, 0x00, 0x00, 0x01, 0x2D, 0x02, 0x00, 0x0F, 0xDB };

        private static byte[] Test_SamsungTimeStamp = new byte[] {0x5c, 0x3d, 0x7c, 0x7d};

        private static byte[] Test_SamsungCallLog = new byte[]{0x00, 0x00, 0x00, 0x03};

        private static byte[] Test_SmsTimeStamp = new byte[]{0x00, 0x20, 0x21, 0x50, 0x75, 0x03, 0x21};

        private static byte[] Test_Josephine = new byte[] {0x4A, 0x4F, 0x53, 0x45, 0x50, 0x48, 0x49, 0x4E, 0x45};

        private static byte[] Test_Ed_Dash_Cell = new byte[] {0x45, 0x44, 0x2d, 0x43, 0x45, 0x4c, 0x4c};

        private static byte[] Test_UnicodeShort = new byte[] {0x00, 0x45};

        private static byte[] Test_UnicodeThree = new byte[] { 0x00, 0x45, 0x00, 0x45, 0x00, 0x45 };

        #endregion

        private static List<byte[]> _records = new List<byte[]>();

        private static int[,] _bestPrevState;

        private static List<string> _textList;

        public static void Main(string[] args)
        {

            List<StateMachine> machines = new List<StateMachine>();
            List<State> states = new List<State>();
            State startState = null;
            List<string> textList = new List<string>();

            //Y:/110410/Nokia3200b_C.bin
            string filePath = "Y:/110410/Nokia3200b_C.bin"; //"Y://111210/random.bin"; //"Y:/110410/Nokia3200b.bin";//"Y:/092010/output_binary.bin";
            string outputFilePath = string.Format("Y:/{0}/output_{0}_{1}.txt", DateTime.Now.ToString("MMddyy"), DateTime.Now.ToString("HHmm"));

            var testMachines = new List<StateMachine>
                                   {
                                       //StateMachine.GetText(5),
                                       StateMachine.GetPhoneNumber_All(6),
                                       //StateMachine.GetTimeStamp_All(1),
                                       //StateMachine.GetBinaryFF(),
                                       //StateMachine.GetTimestamp_Unix(1)
                                   };

            
            StateMachine.TestStateMachines(testMachines, ref machines, ref states, ref startState);
            //StateMachine.GeneralParse(ref machines, ref states, ref startState);

            
            Viterbi viterbi = new Viterbi(machines, states, startState);

            //viterbi.Run(Test_MotoTimeStamp, 0);
            //Console.WriteLine();
            //viterbi.Run(Test_MotoCallLog, 0);
            //viterbi.Run(Test_UnicodeThree, 0);

            var start = DateTime.Now;

            var text = viterbi.Run(filePath);

            Console.WriteLine("Runtime: {0}", start - DateTime.Now);

            textList = text.Distinct().ToList();

           
            File.WriteAllLines(outputFilePath, textList);

            Console.ReadLine();
        }

        //public static void RunCYK()
        //{
        //    Grammar grammar = new Grammar(@"Y:\072910\grammar.dat");

        //    int count = 0;
        //    for (int i = 0; i < _records.Count; i++)
        //    {


        //        string[] record = BinaryFile.GetRecord(_records[i]);

        //        CYK cyk = new CYK(record, grammar);

        //        CYKResult result = cyk.Parse();

        //        Field[] fields = CYK.GetFields(result.Root);



        //        if (fields.Count(r => r.Type.ToLower() == "addressbookentry") > 0)
        //        {
        //            foreach (var field in fields)
        //            {
        //                Console.Write(field);
        //            }

        //            Console.WriteLine();



        //            var entries = fields.Where(r => r.Type.ToLower() == "addressbookentry");

        //            foreach (var entry in entries)
        //            {
        //                for (int j = entry.Start; j < entry.Start + entry.Length; j++)
        //                {
        //                    string observation = Convert.ToString(_records[i][j], 16).PadLeft(2, '0');
        //                    Console.WriteLine("{0}\t{1}", observation, (char)_records[i][j]);
        //                }
        //            }

        //            count++;

        //        }
        //    }

        //    Console.WriteLine("Out of {0} records tested, {1} contained interesting fields.", _records.Count, count);
        //}

        public static bool IsBro(byte[] bytes)
        {
            bool foundBro = false;

            for (int i = 0; i < bytes.Length - 6; i++)
            {
                if (bytes[i] == 0x4d && bytes[i + 1] == 0x61 && bytes[i + 2] == 0x67 && bytes[i + 3] == 0x6e && bytes[i + 4] == 0x75 && bytes[i + 5] == 0x6d)
                {
                    Console.WriteLine("Bro Found!");
                    //Console.ReadLine();
                    foundBro = true;
                    break;
                }
            }

            return foundBro;
        }

        //public static void RetrieveRecord(List<int> anchorPoints)
        //{
        //    //I want to prevent overlap
        //    for (int i = 0; i < anchorPoints.Count; i++)
        //    {
        //        int start = anchorPoints[i];
        //        int end = anchorPoints[i];

        //        for (int j = i + 1; j < anchorPoints.Count; j++)
        //        {
        //            if (end + 80 >= anchorPoints[j])
        //            {
        //                end = anchorPoints[j];
        //                i = j;
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        RetrieveRecord(start, end);
        //    }
        //}

        //public static void RetrieveRecord(int start, int end)
        //{
        //    //Do not want to go outside the bounds of the array
        //    start = Math.Max(start - 40, 0);
        //    end = Math.Min(end + 40, _observations.Length - 1);

        //    byte[] buffer = new byte[end - start + 1];

        //    for (int i = 0; i < buffer.Length; i++)
        //    {
        //        buffer[i] = _observations[i + start];
        //    }

        //    _records.Add(buffer);
        //}

    }
}
