using System;
using System.Collections.Generic;
using System.Text;

namespace Dec0de.Bll
{
    public static class Utilities
    {
        public static string[] GetSubArray(string[] inputArray, int startIndex, int endIndex, bool endInclusive)
        {
            if (endInclusive)
                endIndex += 1;

            List<string> tmp = new List<string>();

            for (int i = startIndex; i < endIndex; i++)
            {
                tmp.Add(inputArray[i]);
            }

            return tmp.ToArray();
        }

        public static int GetValue(string[] tokens, int begin, int end)
        {
            string subString = "";

            for (int i = begin; i < end; i++)
            {
                subString += tokens[i];
            }

            subString.Replace("0x", "");

            byte byteValue = Convert.ToByte(subString, 16);

            //TODO: Check that this works!!
            return Convert.ToInt32(byteValue);
        }

        public static byte[] GetBytes(string rawLine)
        {
            var tokens = AddHexFormat(rawLine);

            var bytes = new byte[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                bytes[i] = Convert.ToByte(tokens[i], 16);
            }

            return bytes;
        }

        public static string[] AddHexFormat(string rawLine)
        {
            List<string> hexTokens = new List<string>();

            for (int i = 0; i < rawLine.Length; i += 2)
            {
                string hex = "0x" + rawLine.Substring(i, 2).Trim().ToLower();

                hexTokens.Add(hex);
            }

            return hexTokens.ToArray();
        }

        public static string GetByteString(byte[] bytes)
        {
            string result = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                result += Convert.ToString(bytes[i], 16).PadLeft(2, '0');

                if (i < bytes.Length - 1)
                    result += " ";
            }

            return result;
        }

        public static string GetAsciiString(byte[] bytes, bool readableOnly)
        {
            string result = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                bool inRange = (bytes[i] >= 32 && bytes[i] <= 126) || (bytes[i] >= 196 && bytes[i] <= 255);

                if (readableOnly && !inRange)
                {
                    //if (bytes[i] == 0)
                    //    result += "|";
                    //else
                    result += Convert.ToString((char)166);
                }
                else
                    result += Convert.ToString((char)bytes[i]);

            }



            return result;
        }

        public static string GetOffsetString(long index)
        {
            string offsetRaw = Convert.ToString(index, 16).PadLeft(8, '0');

            return string.Format("{0} {1}", offsetRaw.Substring(0, 4), offsetRaw.Substring(4, 4));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length">Length in Septets</param>
        /// <returns></returns>
        public static string Decode7bitIntoString(byte[] bytes, int length, bool isReversed)
        {
            if (!isReversed)
                Array.Reverse(bytes);

            StringBuilder sb = new StringBuilder();

            for (int i = 1; i <= length; i++) {

                int index = bytes.Length - ((i - 1) * 7 / 8) - 1;
                int one = bytes[index];
                int two = (index - 1 < 0) ? 0 : bytes[index - 1];

                int concat = (two << 8) + one;

                int indexOfFirstBit = 6 + ((i - 1) * 7 % 8);

                int val = ((concat >> indexOfFirstBit - 6) & 0x7F);
                char c;
                // We only support a limitted number of the substitutions.
                if (val >= 0x20 && val <= 0x7a) {
                    c = (char)val;
                } else if (val == 0) {
                    c = '@';
                } else if (val == 2) {
                    c = '$';
                } else {
                    // Period is a catch-all
                    c = '.';
                }
                sb.Append(c);
            }

            return sb.ToString();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length">Length in Septets</param>
        /// <returns></returns>
        public static byte[] Decode7bit(byte[] bytes, int length, bool isReversed)
        {
            if (!isReversed)
                Array.Reverse(bytes);

            byte[] resultFast = new byte[length];

            for (int i = 1; i <= length; i++)
            {
                int index = bytes.Length - ((i - 1) * 7 / 8) - 1;
                int one = bytes[index];
                int two = (index-1 < 0) ? 0: bytes[index - 1];

                int concat = (two << 8) + one;

                int indexOfFirstBit = 6 + ((i - 1) * 7 % 8);

                resultFast[i-1] = (byte)((concat >> indexOfFirstBit - 6) & 0x7F);
            }

            return resultFast;
        }

        public static string GetLastSevenDigits(string number)
        {
            if (number != null && number.Length > 7)
                return number.Substring(number.Length - 7, 7);

            return number;
        }

        public static string GetAreaCode(string number)
        {
            if (number.Length < 10)
                return null;

            number = number.Substring(number.Length - 10, 10);

            return number.Substring(0, 3);
        }

        public static int GetCountOfAlphaCharacters(string input)
        {
            var chars = input.ToCharArray();
            int alphaCount = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                //upper case 
                bool isUpper = chars[i] >= 65 && chars[i] <= 90;
                bool isLower = chars[i] >= 97 && chars[i] <= 122;

                if (isUpper || isLower)
                    alphaCount++;
            }

            return alphaCount;
        }

        public static byte[] GetCharBytes(string input)
        {
            var chars = input.ToCharArray();
            var bytes = new byte[chars.Length];

            for (int i = 0; i < chars.Length; i++)
            {
                bytes[i] = (byte)chars[i];
            }

            return bytes;
        }

        public static double CalculateHarmonicMean(List<double> input)
        {
            double numerator = input.Count;
            double denominator = 0;

            for (int i = 0; i < input.Count; i++)
            {
                double tmp = 1 / input[i];

                denominator += tmp;
            }

            double mean = numerator / denominator;

            return mean;
        }

        public static List<DateTime> RemoveDuplicateDates(List<DateTime> dateTimes)
        {
            var results = new List<DateTime>();

            bool isSame;

            for (int i = 0; i < dateTimes.Count; i++)
            {
                isSame = false;

                for (int j = 0; j < results.Count; j++)
                {
                    //If there is a less than 1 second difference, consider the dates the same
                    var secondDiff = Math.Abs((dateTimes[i] - results[j]).TotalSeconds);
                    if (secondDiff < 1)
                    {
                        isSame = true;
                        break;
                    }
                }

                if (!isSame)
                {
                    results.Add(dateTimes[i]);
                }
            }

            return results;
        }
    }
}
