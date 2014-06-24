using System.Collections.Generic;
using System.Linq;
using System.IO;
using Dec0de.Bll.CYK;
using Dec0de.Bll.Filter;

namespace Dec0de.Bll
{
    public class AresController
    {

        private const int MINIMUM_RECORD_SIZE = 3;
        private const int WINDOW_SIZE = 300;

        private static int _numResults;

        private static int _count;

        public static void OpenBinaryFile(string binaryFile, string grammarFile, string comparisonFile)
        {
            BinaryFile file = new BinaryFile(binaryFile);

            List<byte[]> interestingBytes = FilterController.Filter(binaryFile, comparisonFile);

            string outputFile = @"Y:\092010\output_binary.bin";

            using (var stream = File.Create(outputFile))
            {
                for (int i = 0; i < interestingBytes.Count; i++)
                {
                    stream.Write(interestingBytes[i], 0, interestingBytes[i].Length);
                }
            }

            List<byte[]> interestingBytes2 = FilterController.Filter(comparisonFile, binaryFile);

            string outputFile2 = @"Y:\092010\output_comparision.bin";

            using (var stream = File.Create(outputFile2))
            {
                for (int i = 0; i < interestingBytes2.Count; i++)
                {
                    stream.Write(interestingBytes2[i], 0, interestingBytes2[i].Length);
                }
            }

            return;

            int windowSize = 300;
            int shiftAmount = 50;

            Grammar grammar = new Grammar(grammarFile);


            ProcessWindow(interestingBytes, grammar);



            //string startOffsetS = "01d70300";


            //Int64 startOffsetI = Convert.ToInt64(startOffsetS, 16);

            //List<long> startIndexes = new List<long>();            

            //for (long i = startOffsetI; i < file.Info.Length-windowSize; i+=shiftAmount)
            //{
            //    startIndexes.Add(i);
            //}

            //Parallel.For(0, startIndexes.Count, i => ProcessWindow(file, grammar, startIndexes[i],i));
        }
        
        private static void ProcessWindow(List<byte[]> bytes, Grammar grammar)
        {
            List<byte[]> tmp = new List<byte[]>();

            for (int i = 0; i < bytes.Count; i++)
            {
                if (bytes[i].Length < MINIMUM_RECORD_SIZE)
                    continue;

                string[] record = BinaryFile.GetRecord(bytes[i]);

                CYK.CYK cyk = new CYK.CYK(record, grammar);

                CYKResult result = cyk.Parse();

                Field[] fields = CYK.CYK.GetFields(result.Root);

                //var tmp = fields.Where(r => r.Type == "AddressBookEntry").ToList();

                if (fields.Where(r => r.Type == "AddressBookEntry").ToList().Count > 0)
                {
                    _numResults++;

                    tmp.Add(bytes[i]);

                    //fields = Field.CompressBinaryFields(fields);

                    //string fieldString = "";

                    //for (int j = 0; j < fields.Length; j++)
                    //{
                    //    fieldString += fields[j].ToString();
                    //}

                    //Console.WriteLine(fieldString);

                    //foreach (var item in fields.Where(r => r.Type == "AddressBookEntry"))
                    //{
                    //    string byteString = file.GetAscii(i+item.Start, item.Length);

                    //    string offset = Convert.ToString(i+item.Start, 16);
                    //}
                }
            }
        }

        private static void ProcessWindow(BinaryFile file, Grammar grammar, long startIndex, int i)
        {
            _count++;

            string[] record = file.GetRecord(startIndex, WINDOW_SIZE);

            CYK.CYK cyk = new CYK.CYK(record, grammar);

            CYKResult result = cyk.Parse();

            Field[] fields = CYK.CYK.GetFields(result.Root);

            //var tmp = fields.Where(r => r.Type == "AddressBookEntry").ToList();

            if (fields.Where(r => r.Type == "AddressBookEntry").ToList().Count > 0)
            {
                _numResults++;
                //fields = Field.CompressBinaryFields(fields);

                //string fieldString = "";

                //for (int j = 0; j < fields.Length; j++)
                //{
                //    fieldString += fields[j].ToString();
                //}

                //Console.WriteLine(fieldString);

                //foreach (var item in fields.Where(r => r.Type == "AddressBookEntry"))
                //{
                //    string byteString = file.GetAscii(i+item.Start, item.Length);

                //    string offset = Convert.ToString(i+item.Start, 16);
                //}
            }
        }
    }
}
