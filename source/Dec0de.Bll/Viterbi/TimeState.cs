using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public static class TimeConstants
    {
        //public const int START_YEAR = 2000;
        //public const int END_YEAR = 2009;
        public static readonly int START_YEAR = Math.Max(DateTime.UtcNow.AddYears(-6).Year, 2007);
        public static readonly int END_YEAR = DateTime.UtcNow.AddMonths(1).Year;
        public static readonly int[] MonthDays = {31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
    }

    public class SamsungTimeState : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from Samsung time state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the Samsung time state is required.</param>
        /// <returns>The emission probability of an output byte, given the Samsung time state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            var input = new byte[4];
            input[3] = values[index];
            input[2] = values[index-1];
            input[1] = values[index - 2];
            input[0] = values[index - 3];

            var yearBytes = new byte[] { input[3], input[2], 0x00, 0x00 };
            var monthBytes = new byte[] { 0x00, (byte)(input[2] & 0x0F), 0x00, 0x00 };
            var dayBytes = new byte[] { 0x00, 0x00, input[1], 0x00 };
            var hourBytes = new byte[] { 0x00, 0x00, (byte)(input[1] & 0x07), input[0] };
            var minuteBytes = new byte[] { 0x00, 0x00, 0x00, (byte)(input[0] & 0x3F) };

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

            bool isValidYear = (year >= TimeConstants.START_YEAR && year <= TimeConstants.END_YEAR);
            bool isValidMonth = (month >= 1 && month <= 12);
            bool isValidDay = (day >= 1 && day <= TimeConstants.MonthDays[month-1]);
            bool isValidHour = (hour >= 0 && hour <= 23);
            bool isValidMinute = (minute >= 0 && minute <= 59);

            if (isValidYear && isValidMonth && isValidHour && isValidDay && isValidMinute) {
                if ((month == 2) && (day == 29)) {
                    try {
                        var timestamp = new DateTime(year, month, day, hour, minute, 0);
                    } catch (Exception) {
                        return ALMOST_ZERO;
                    }
                }
                return baseProb;
            }
            return ALMOST_ZERO;
        }
    }

    public class NokiaTimeState : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from Nokia time state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the Nokia time state is required.</param>
        /// <returns>The emission probability of an output byte, given the Nokia time state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            
            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            var input = new byte[7];
            input[6] = values[index];
            input[5] = values[index-1];
            input[4] = values[index-2];
            input[3] = values[index-3];
            input[2] = values[index-4];
            input[1] = values[index-5];
            input[0] = values[index-6];

            var yearByte = new Byte[] { 0x00, 0x00, input[0], input[1] };

            if (BitConverter.IsLittleEndian)
                Array.Reverse(yearByte);

            var year = BitConverter.ToInt32(yearByte, 0);

            var month = (int)input[2];
            var day = (int)input[3];
            var hour = (int)input[4];
            var minute = (int)input[5];
            var second = (int)input[6];

            bool isValidYear = (year >= TimeConstants.START_YEAR && year <= TimeConstants.END_YEAR);
            bool isValidMonth = (month >= 1 && month <= 12);
            bool isValidDay = (day >= 1 && day <= TimeConstants.MonthDays[month - 1]);
            bool isValidHour = (hour >= 0 && hour <= 23);
            bool isValidMinute = (minute >= 0 && minute <= 59);
            bool isValidSecond = (second >= 0 && second <= 59);

            //if (minute == second && second == 0)
            //    return ALMOST_ZERO;

            if (isValidYear && isValidMonth && isValidHour && isValidDay && isValidMinute && isValidSecond) {
                if ((month == 2) && (day == 29)) {
                    try {
                        var timestamp = new DateTime(year, month, day, hour, minute, second);
                    } catch (Exception) {
                        return ALMOST_ZERO;
                    }
                }
                return baseProb;
            }
            return ALMOST_ZERO;
        }
    }

    public class NokiaEndianTimeState : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from NokiaEndian time state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the NokiaEndian time state is required.</param>
        /// <returns>The emission probability of an output byte, given the NokiaEndian time state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);

            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            var input = new byte[7];
            input[6] = values[index];
            input[5] = values[index - 1];
            input[4] = values[index - 2];
            input[3] = values[index - 3];
            input[2] = values[index - 4];
            input[1] = values[index - 5];
            input[0] = values[index - 6];

            var yearByte = new Byte[] { 0x00, 0x00, input[1], input[0] };

            if (BitConverter.IsLittleEndian)
                Array.Reverse(yearByte);

            var year = BitConverter.ToInt32(yearByte, 0);

            var month = (int)input[2];
            var day = (int)input[3];
            var hour = (int)input[4];
            var minute = (int)input[5];
            var second = (int)input[6];

            bool isValidYear = (year >= TimeConstants.START_YEAR && year <= TimeConstants.END_YEAR);
            bool isValidMonth = (month >= 1 && month <= 12);
            bool isValidDay = (day >= 1 && day <= TimeConstants.MonthDays[month - 1]);
            bool isValidHour = (hour >= 0 && hour <= 23);
            bool isValidMinute = (minute >= 0 && minute <= 59);
            bool isValidSecond = (second >= 0 && second <= 59);

            if (isValidYear && isValidMonth && isValidHour && isValidDay && isValidMinute && isValidSecond) {
                if ((month == 2) && (day == 29)) {
                    try {
                        var timestamp = new DateTime(year, month, day, hour, minute, second);
                    } catch (Exception) {
                        return ALMOST_ZERO;
                    }
                }
                return baseProb;
            }
            return ALMOST_ZERO;
        }
    }

    public class SmsTimeState : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from SMS time state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the SMS time state is required.</param>
        /// <returns>The emission probability of an output byte, given the SMS time state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            var input = new byte[6];
            input[5] = values[index];
            input[4] = values[index - 1];
            input[3] = values[index - 2];
            input[2] = values[index - 3];
            input[1] = values[index - 4];
            input[0] = values[index - 5];

            var year = 2000 + Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[0])));
            var month = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[1])));
            var day = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[2])));
            var hour = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[3])));
            var minute = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[4])));
            var second = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[5])));


            bool isValidYear = (year >= TimeConstants.START_YEAR && year <= TimeConstants.END_YEAR);
            bool isValidMonth = (month >= 1 && month <= 12);
            bool isValidDay = (day >= 1 && day <= TimeConstants.MonthDays[month - 1]);
            bool isValidHour = (hour >= 0 && hour <= 23);
            bool isValidMinute = (minute >= 0 && minute <= 59);
            bool isValidSecond = (second >= 0 && second <= 59);

            if (isValidYear && isValidMonth && isValidHour && isValidDay && isValidMinute && isValidSecond)
            {
                if ((month == 2) && (day == 29)) {
                    try {
                        var timestamp = new DateTime(year, month, day, hour, minute, second);
                    } catch (Exception) {
                        return ALMOST_ZERO;
                    }
                }
                return baseProb;
            }
            return ALMOST_ZERO;

        }
    }

    public class SmsGsmTimeState : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from SMS time state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the SMS 
        /// with TZ time state is required.</param>
        /// <returns>The emission probability of an output byte, given the SMS time state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            var input = new byte[7];
            input[6] = values[index];
            input[5] = values[index - 1];
            input[4] = values[index - 2];
            input[3] = values[index - 3];
            input[2] = values[index - 4];
            input[1] = values[index - 5];
            input[0] = values[index - 6];

            var year = 2000 + Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[0])));
            var month = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[1])));
            var day = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[2])));
            var hour = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[3])));
            var minute = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[4])));
            var second = Convert.ToInt32(Printer.GetNibbleString(Printer.SwapNibbles(input[5])));
            byte bv  = Printer.SwapNibbles(input[6]);
            byte bcd = (byte)(bv & 0x37);
            byte[] nibbles = Printer.GetNibbles(bcd);
            int tz = (nibbles[0]*10) + nibbles[1];
            if ((bv & 0x80) != 0) tz = -tz;

            bool isValidYear = (year >= TimeConstants.START_YEAR && year <= TimeConstants.END_YEAR);
            bool isValidMonth = (month >= 1 && month <= 12);
            bool isValidDay = (day >= 1 && day <= TimeConstants.MonthDays[month - 1]);
            bool isValidHour = (hour >= 0 && hour <= 23);
            bool isValidMinute = (minute >= 0 && minute <= 59);
            bool isValidSecond = (second >= 0 && second <= 59);
            bool isValidTz = (tz > -96 && tz < 96);

            if (isValidYear && isValidMonth && isValidHour && isValidDay && isValidMinute && isValidSecond && isValidTz) {
                if ((month == 2) && (day == 29)) {
                    try {
                        var timestamp = new DateTime(year, month, day, hour, minute, second);
                    } catch (Exception) {
                        return ALMOST_ZERO;
                    }
                }
                return baseProb;
            }
            return ALMOST_ZERO;
        }
    }

    public class MotoSmsTimeState : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from Motorola's SMS time state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the Motorola's SMS time state is required.</param>
        /// <returns>The emission probability of an output byte, given the Motorola's SMS time state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            var input = new byte[6];
            input[5] = values[index];
            input[4] = values[index - 1];
            input[3] = values[index - 2];
            input[2] = values[index - 3];
            input[1] = values[index - 4];
            input[0] = values[index - 5];

            var year = 1970 + Convert.ToInt32(input[0]);
            var month = Convert.ToInt32(input[1]);
            var day = Convert.ToInt32(input[2]);
            var hour = Convert.ToInt32(input[3]);
            var minute = Convert.ToInt32(input[4]);
            var second = Convert.ToInt32(input[5]);

            bool isValidYear = (year >= TimeConstants.START_YEAR && year <= TimeConstants.END_YEAR);
            bool isValidMonth = (month >= 1 && month <= 12);
            bool isValidDay = (day >= 1 && day <= TimeConstants.MonthDays[month - 1]);
            bool isValidHour = (hour >= 0 && hour <= 23);
            bool isValidMinute = (minute >= 0 && minute <= 59);
            bool isValidSecond = (second >= 0 && second <= 59);

            if (isValidYear && isValidMonth && isValidHour && isValidDay && isValidMinute && isValidSecond) {
                if ((month == 2) && (day == 29)) {
                    try {
                        var timestamp = new DateTime(year, month, day, hour, minute, second);
                    } catch (Exception) {
                        return ALMOST_ZERO;
                    }
                }
                return baseProb;
            }
            return ALMOST_ZERO;
        }
    }

    public class UnixTimeState : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from Unix time state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the Unix time state is required.</param>
        /// <returns>The emission probability of an output byte, given the Unix time state.</returns>   
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            var input = new byte[4];
            input[3] = values[index];
            input[2] = values[index - 1];
            input[1] = values[index - 2];
            input[0] = values[index - 3];

            if (BitConverter.IsLittleEndian)
                Array.Reverse(input);

            var seconds = BitConverter.ToUInt32(input, 0); // B Lynn: make unsigned
            
            try
            {
                var dateTime = new DateTime(1970, 1, 1).AddSeconds(seconds);

                if (dateTime.Year >= TimeConstants.START_YEAR && dateTime.Year <= TimeConstants.END_YEAR)
                    return baseProb;
                else
                {
                    return ALMOST_ZERO;
                }
            }
            catch (Exception)
            {
                return ALMOST_ZERO;
            }
        }
    }

    public class Epoch1900Tuple : State
    {
        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from Epoch1900Tuple state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the Epoch1900Tuple state is required.</param>
        /// <returns>The emission probability of an output byte, given the Epoch1900Tuple state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {

            double baseProb = base.GetValueProbability(values, index, viterbi);
            //If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO)
                return ALMOST_ZERO;

            // Two 4-byte little-endian timestamps. Use the first timestamp.
            byte[] input1 = new byte[4];
            input1[3] = values[index - 4];
            input1[2] = values[index - 5];
            input1[1] = values[index - 6];
            input1[0] = values[index - 7];
            byte[] input2 = new byte[4];
            input2[3] = values[index];
            input2[2] = values[index - 1];
            input2[1] = values[index - 2];
            input2[0] = values[index - 3];

            try {
                uint seconds1 = BitConverter.ToUInt32(input1, 0);
                uint seconds2 = BitConverter.ToUInt32(input2, 0);
                if (Math.Abs(seconds2 - seconds1) > 172800) {
                    // Two days diff is too long.
                    return ALMOST_ZERO;
                }
                var dateTime1 = new DateTime(1900, 1, 1).AddSeconds(seconds1);
                if (dateTime1.Year >= TimeConstants.START_YEAR && dateTime1.Year <= TimeConstants.END_YEAR) {
                    return baseProb;
                }
                return ALMOST_ZERO;
            } catch (Exception) {
                return ALMOST_ZERO;
            }

        }
    }
}
