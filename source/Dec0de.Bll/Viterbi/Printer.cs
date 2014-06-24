using System;
using System.Linq;
using System.Text;
using Dec0de.Bll.UserStates;

namespace Dec0de.Bll.Viterbi
{
    /// <summary>
    /// Designed to print out the fields in a readable format
    /// </summary>
    public static class Printer
    {
        public static string GetMotoPhoneString(byte[] input)
        {
            var tmp = input.ToList();

            //Remove length
            tmp.RemoveAt(0);
            //Remove type
            tmp.RemoveAt(0);

            return GetInternationalPhoneString(tmp.ToArray());
        }

        public static string GetAsciiPhoneString(byte[] input)
        {
            string result = "";

            for (int i = 0; i < input.Length; i++)
            {
                result += (char)input[i];
            }

            return result;
        }

        public static string GetUnicodePhoneString(byte[] input)
        {
            //First byte is the length
            string result = "";

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == 0x00)
                    continue;

                result += (char)input[i];
            }

            return result;
        }

        public static string GetNokiaPhoneString(byte[] input)
        {
            //First byte is the length
            string result = "";

            for (int i = 1; i < input.Length; i++)
            {
                result += GetNokiaPhoneNibbleString(input[i]);
            }

            return result;
        }

        public static string GetInternationalPhoneString(byte[] input)
        {
            string result = "";

            for (int i = 0; i < input.Length; i++)
            {
                result += GetSmsPhoneNibbleString(input[i]);
            }

            return result;
        }

        public static string GetBCDPhoneString(byte[] input)
        {
            int len = input[0];
            int numLen = (len + 1)/2;
            if ((len >= 2) && (len <= 16) && (input.Length >= (numLen + 2))) {
                byte[] num = new byte[numLen];
                Array.Copy(input, 2, num, 0, numLen);
                return GetInternationalPhoneString(num);
            } else {
                return "";
            }
        }

        public static string GetSmsPhoneNibbleString(byte input)
        {
            string result = "";

            var nibbles = GetNibbles(input);

            //The nibbles need to be reversed
            for (int j = nibbles.Length - 1; j >= 0; j--)
            {
                if (nibbles[j] == 0x0F)
                    continue;
                else
                {
                    result += Convert.ToString(nibbles[j], 16);
                }

            }

            return result;
        }

        public static string GetNokiaPhoneNibbleString(byte input)
        {
            string result = "";

            var nibbles = GetNibbles(input);

            for (int j = 0; j < nibbles.Length; j++)
            {
                if (nibbles[j] == 0x0F)
                    result += '+';
                else if (nibbles[j] == 0x0A)
                    result += '0';
                else if (nibbles[j] == 0x00)
                    continue;
                else
                {
                    result += Convert.ToString(nibbles[j], 16);
                }

            }

            return result;
        }

        public static byte ByteFromNibbles(byte high, byte low)
        {
            byte h = (byte)((high & 0x0F) << 4);
            byte l = (byte) (low & 0x0F);
            return (byte)(h | l);
        }

        public static byte ByteFromNibbles(int high, int low)
        {
            byte h = (byte)((high & 0x0F) << 4);
            byte l = (byte)(low & 0x0F);
            return (byte)(h | l);
        }

        public static byte[] GetNibbles(byte input)
        {
            byte nibbleMostSig = (byte)(input >> 4);
            byte nibbleLeastSig = (byte)(input & 0x0F);

            return new byte[] { nibbleMostSig, nibbleLeastSig };
        }

        public static byte SwapNibbles(byte input)
        {
            byte nibbleMostSig = (byte)(input >> 4);
            byte nibbleLeastSig = (byte)((input & 0x0F) << 4);

            byte newByte = (byte)(nibbleMostSig | nibbleLeastSig);

            return newByte;
        }

        public static string GetTextString(byte[] input)
        {
            string result = "";

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == 0x00)
                    continue;

                result += (char) input[i];
            }

            return result;
        }

        public static string GetUTFPrintableChars(byte[] input)
        {
            return Encoding.UTF8.GetString(input);
        }

        public static string GetNokiaTimeStamp(byte[] input, bool switchEndian)
        {
            var yearByte = (!switchEndian) ? new Byte[] { 0x00, 0x00, input[0], input[1] } : new Byte[] { 0x00, 0x00, input[1], input[0] };
            
            if(BitConverter.IsLittleEndian)
                Array.Reverse(yearByte);

            var year = BitConverter.ToInt32(yearByte, 0);

            var month = (int) input[2];
            var day = (int) input[3];
            var hour = (int) input[4];
            var min = (int) input[5];
            var sec = (int) input[6];

            DateTime dateTime = new DateTime(year, month, day, hour, min, sec);

            return dateTime.ToString();
        }

        public static string GetUnixTimeStamp(byte[] input)
        {
            if(BitConverter.IsLittleEndian)
                Array.Reverse(input);

            var seconds = BitConverter.ToInt32(input, 0);

            var dateTime = new DateTime(1970, 1, 1).AddSeconds(seconds);

            return dateTime.ToString();
        }

        public static string GetEpoch1900TimeStamp(byte[] input)
        {
            // Can be 8 bytes containing 2 timestamps. Use first timestamp.
            byte[] temp = new byte[4];
            Array.Copy(input, temp, 4);

            var seconds = BitConverter.ToUInt32(temp, 0);

            var dateTime = new DateTime(1900, 1, 1).AddSeconds(seconds);

            return dateTime.ToString();
        }

        public static string GetSmsTimeStamp(byte[] input)
        {
            var year = 2000 + Convert.ToInt32(GetNibbleString(SwapNibbles(input[0])));
            var month = Convert.ToInt32(GetNibbleString(SwapNibbles(input[1])));
            var day = Convert.ToInt32(GetNibbleString(SwapNibbles(input[2])));
            var hour = Convert.ToInt32(GetNibbleString(SwapNibbles(input[3])));
            var minute = Convert.ToInt32(GetNibbleString(SwapNibbles(input[4])));
            var second = Convert.ToInt32(GetNibbleString(SwapNibbles(input[5])));

            

            try
            {
                DateTime dateTime = new DateTime(year, month, day, hour, minute, second);


                return dateTime.ToString();
            }
            catch (Exception)
            {
                return "Bad Date!";
            }

            
        }

        public static string GetSmsGsmTimeStamp(byte[] input)
        {
            var year = 2000 + Convert.ToInt32(GetNibbleString(SwapNibbles(input[0])));
            var month = Convert.ToInt32(GetNibbleString(SwapNibbles(input[1])));
            var day = Convert.ToInt32(GetNibbleString(SwapNibbles(input[2])));
            var hour = Convert.ToInt32(GetNibbleString(SwapNibbles(input[3])));
            var minute = Convert.ToInt32(GetNibbleString(SwapNibbles(input[4])));
            var second = Convert.ToInt32(GetNibbleString(SwapNibbles(input[5])));

            try {
                DateTime dateTime = new DateTime(year, month, day, hour, minute, second);
                byte bv = Printer.SwapNibbles(input[6]);
                byte bcd = (byte)(bv & 0x37);
                byte[] nibbles = Printer.GetNibbles(bcd);
                int tz = (nibbles[0] * 10) + nibbles[1];
                if ((bv & 0x80) != 0) tz = -tz;
                double delta1 = DateTimeOffset.Now.Offset.TotalMinutes;
                double delta2 = tz * 15;
                if (delta1 != delta2) {
                    dateTime = dateTime.AddMinutes(delta1 - delta2);
                }
                return dateTime.ToString();
            } catch (Exception) {
                return "Bad Date!";
            }


        }

        public static string GetNibbleString(byte input)
        {
            var nibbles = GetNibbles(input);

            var nibbleString = Convert.ToString(nibbles[0], 16) + Convert.ToString(nibbles[1], 16);

            return nibbleString;
        }

        public static string GetMotoCallLogStatus(byte[] input)
        {
            switch(input[1])
            {
                case 0x03:
                    return "Missed";
                case 0x02:
                    return "Received";
                case 0x00:
                    return "Dialed";
                default:
                    return "???";
            }
        }

        public static string GetSamsungCallLogStatus(byte[] input)
        {
            switch (input[3])
            {
                case 0x05:
                    return "Missed";
                case 0x04:
                    return "Received";
                case 0x03:
                    return "Dialed";
                default:
                    return "???";
            }
        }

        public static string GetSimpleCallLogStatusLE(byte[] input)
        {
            switch (input[0]) {
                case 0x00:
                    return "Called";
                case 0x01:
                    return "Received";
                case 0x02:
                    return "Missed";
                default:
                    return "???";
            }
        }

        public static string GetSamsungTimeStamp(byte[] input)
        {
            var yearBytes = new byte[] {input[3], input[2], 0x00, 0x00};
            var monthBytes = new byte[] {0x00, (byte) (input[2] & 0x0F), 0x00, 0x00};
            var dayBytes = new byte[] {0x00, 0x00, input[1], 0x00};
            var hourBytes = new byte[] { 0x00, 0x00, (byte)(input[1] & 0x07),  input[0] };
            var minuteBytes = new byte[] {0x00, 0x00, 0x00, (byte) (input[0] & 0x3F)};

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(yearBytes);
                Array.Reverse(monthBytes);
                Array.Reverse(dayBytes);
                Array.Reverse(hourBytes);
                Array.Reverse(minuteBytes);
            }


            var year = BitConverter.ToInt32(yearBytes, 0) >> 20;
            var month = BitConverter.ToInt32(monthBytes, 0) >> 16;
            var day = BitConverter.ToInt32(dayBytes, 0) >> 11;
            var hour = BitConverter.ToInt32(hourBytes, 0) >> 6;
            var minute = BitConverter.ToInt32(minuteBytes, 0);

            var dateTime = new DateTime(year, month, day, hour, minute, 0);

            return dateTime.ToString();
        }

        public static string GetStringFromNumber(byte[] input)
        {
            int result = input[0];


            //var number = BitConverter.ToInt32(input, 0);

            return Convert.ToString(result);
        }

        public static string GetMotoSmsTimeStamp(byte[] input)
        {
            var year = 1970 + Convert.ToInt32(input[0]);
            var month = Convert.ToInt32(input[1]);
            var day = Convert.ToInt32(input[2]);
            var hour = Convert.ToInt32(input[3]);
            var minute = Convert.ToInt32(input[4]);
            var second = Convert.ToInt32(input[5]);


            var dateTime = new DateTime(year, month, day, hour, minute, second);

            return dateTime.ToString();
        }

        public static string GetSevenBitWithLength(byte[] input)
        {
            byte[] inputNoLen = new byte[input.Length - 1];

            for (int i = 1; i < input.Length; i++)
            {
                inputNoLen[inputNoLen.Length-i] = input[i];
            }

            int length = input[0];

            return Utilities.Decode7bitIntoString(inputNoLen, length, true);
        }

        /// <summary>
        /// Called by the ExtractField() method of Viterbi class, to get a readable format of a Viterbi field whose corresponding bytes are passed through input.
        /// </summary>
        /// <param name="machineName">Name of the parent state machine to which the field belongs to.</param>
        /// <param name="input">Sequence of bytes corresponding to the field whose readable format is desired.</param>
        /// <returns>A readable format of the Viterbi field.</returns>
        public static string GetField(MachineList machineName, byte[] input)
        {
            string result;

            switch (machineName)
            {
                case MachineList.Sql_SqliteRecord:
                    result = GetUTFPrintableChars(input);
                    break;

                case MachineList.Text_SevenBitWithLength:
                    result = GetSevenBitWithLength(input);
                    break;

                case MachineList.TimeStamp_MotoSms:
                    result = GetMotoSmsTimeStamp(input);
                    break;
                    
                case MachineList.TimeStamp_Sms:
                    result = GetSmsTimeStamp(input);
                    break;

                case MachineList.TimeStamp_SmsGsm:
                    result = GetSmsGsmTimeStamp(input);
                    break;

                case MachineList.CallLogType_Samsung:
                    result = GetSamsungCallLogStatus(input);
                    break;

                case MachineList.TimeStamp_Samsung:
                    result = GetSamsungTimeStamp(input);
                    break;

                case MachineList.TimeStamp_Unix:
                    result = GetUnixTimeStamp(input);
                    break;

                case MachineList.TimeStamp_Epoch1900Tuple:
                    result = GetEpoch1900TimeStamp(input);
                    break;

                case MachineList.CallLogType_SimpleLE:
                    result = GetSimpleCallLogStatusLE(input);
                    break;
                  
                case MachineList.CallLogType_Moto:
                    result = GetMotoCallLogStatus(input);
                    break;

                case MachineList.PhoneNumber_NokiaSevenDigit:
                case MachineList.PhoneNumber_NokiaEightDigit:
                case MachineList.PhoneNumber_NokiaTenDigit:
                case MachineList.PhoneNumber_NokiaElevenDigit:
                case MachineList.PhoneNumber_NokiaTwelveDigit:
                    result = GetNokiaPhoneString(input);
                    break;

                case MachineList.PhoneNumber_InternationalFormatSevenDigit:
                case MachineList.PhoneNumber_InternationalFormatTenDigit:
                case MachineList.PhoneNumber_InternationalFormatElevenDigit:
                case MachineList.PhoneNumber_BCDPrepended:
                    result = GetInternationalPhoneString(input);
                    break;

                case MachineList.PhoneNumber_BCD:
                    result = GetBCDPhoneString(input);
                    break;

                case MachineList.PhoneNumber_SamsungElevenDigitAscii:
                case MachineList.PhoneNumber_SamsungTenDigitAscii:
                case MachineList.PhoneNumber_SamsungSevenDigitAscii:
                    result = GetAsciiPhoneString(input);
                    break;

                case MachineList.PhoneNumber_MotoSevenUnicode:
                case MachineList.PhoneNumber_MotoTenUnicode:
                case MachineList.PhoneNumber_MotoElevenUnicode:
                    result = GetUnicodePhoneString(input);
                    break;

                case MachineList.PhoneNumber_MotoElevenDigit:
                case MachineList.PhoneNumber_MotoTenDigit:
                case MachineList.PhoneNumber_MotoSevenDigit:
                    result = GetMotoPhoneString(input);
                    break;

                case MachineList.PhoneNumberIndex_Nokia:
                    result = GetStringFromNumber(input);
                    break;

                case MachineList.Text_AsciiStringWithLength:
                case MachineList.Text_Unicode:
                case MachineList.Text_UnicodeEndian:
                case MachineList.Text_AsciiBigram:
                case MachineList.Text_AsciiPrintable:
                case MachineList.Marker_SamsungSms:
                    result = GetTextString(input);
                    break;
                    

                case MachineList.TimeStamp_Nokia:
                    result = GetNokiaTimeStamp(input, false);
                    break;
                    
                case MachineList.TimeStamp_NokiaEndian:
                    result = GetNokiaTimeStamp(input, true);
                    break;

                case MachineList.PhoneNumber_User:
                case MachineList.TimeStamp_User:
                case MachineList.Text_User:
                    // Should not get here.
                    result = "???"; 
                    break;

                default:
                    result = "???";
                    break;
            }

            return result;
        }

        /// <summary>
        /// Called by the ExtractField() method of Viterbi class, to get a readable format of a Viterbi field
        /// whose corresponding bytes are passed through input. This metohd is used with user-defined states.
        /// </summary>
        /// <param name="machineName">The state's machine name.</param>
        /// <param name="input">The bytes to ne interpreted.</param>
        /// <param name="uState">The UserState object, which references the method for interpreting the input.</param>
        /// <returns>The string representation.</returns>
        public static string GetUserField(MachineList machineName, byte[] input, UserState uState)
        {
            try {
                if ((machineName == MachineList.PhoneNumber_User) || (machineName == MachineList.Text_User)) {
                    return (string)uState.MethodFormat.Invoke(null, new object[] { input });
                } else {
                    // For timestamps get the DateTime and format it, rather than use the user's
                    // format method. This is for consistency.
                    DateTime dt = (DateTime)uState.MethodDatetime.Invoke(null, new object[] { input });
                    return dt.ToString();
                }
            } catch {
                return "???";
            }
        }

    }
}
