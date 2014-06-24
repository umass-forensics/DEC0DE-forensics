using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Dec0de.Bll
{
    public class BinaryFile
    {
        public string FilePath { get; private set; }
        public string[] Bytes { get; private set; }
        public byte[] RawBytes { get; private set; }
        public FileInfo Info { get; private set; }

        public BinaryFile(string filePath)
        {
            Info = new FileInfo(filePath);

            //RawBytes = File.ReadAllBytes(filePath);

            //Bytes = RawBytes.Select(r => Convert.ToString(r, 16).PadLeft(2, '0')).ToArray();
        }

        public byte[] GetBytes(long startIndex, int length)
        {
            byte[] bytes = new byte[length];

            using (FileStream stream = File.OpenRead(Info.FullName))
            {
                stream.Seek(startIndex, SeekOrigin.Begin);

                stream.Read(bytes, 0, length);
            }

            return bytes;
        }

        public string[] GetRecord(long startIndex, int length)
        {
            byte[] bytes = GetBytes(startIndex, length);

            return GetRecord(bytes);

            //string[] bytesString = bytes.Select(r => Convert.ToString(r, 16).PadLeft(2, '0')).ToArray();

            //List<string> record = new List<string>();

            //for (int i = 0; i < length; i++)
            //{
            //    string token = string.Format("0x{0}", bytesString[i].ToLower());

            //    record.Add(token);
            //}

            //return record.ToArray();
        }

        public static string[] GetRecord(byte[] bytes)
        {
            string[] bytesString = bytes.Select(r => Convert.ToString(r, 16).PadLeft(2, '0')).ToArray();

            List<string> record = new List<string>();

            for (int i = 0; i < bytesString.Length; i++)
            {
                string token = string.Format("0x{0}", bytesString[i].ToLower());

                record.Add(token);
            }

            return record.ToArray();
        }

        public string GetAscii(long startIndex, int length)
        {
            string result = "";

            byte[] bytes = GetBytes(startIndex, length);

            for (int i = 0; i < bytes.Length; i++)
            {
                
                result += Convert.ToString((char)bytes[i]);
            }

            return result;
        }

    }
}
