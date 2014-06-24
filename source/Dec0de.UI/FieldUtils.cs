/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dec0de.UI
{
    public static class FieldUtils
    {
        private static readonly Regex reAllDigits = new Regex(@"^[0-9]+$");
        private static readonly Regex rePlusDigits = new Regex(@"^\+[0-9]+$");
        private static readonly Regex rePlusDigits1 = new Regex(@"^\+1[0-9]+$");
        private static readonly Regex reLastSevenDigitsUS = new Regex(@"^.*?[2-9][0-9]{6,6}$");
        private static readonly Regex reLastTenDigitsUS = new Regex(@"^.*?[2-9][0-9]{2,2}[2-9][0-9]{6,6}$");
        private static readonly Regex reLongDistanceNumber = new Regex(@"^0[0-9]{8,19}$");

        /// <summary>
        /// Formats a phone number. This is US-centric.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string FormatPhoneNumber(string number)
        {
            // Empty, null or *NONE* ...
            if (String.IsNullOrEmpty(number)) {
                return "";
            }
            if (number == Dec0de.Bll.AnswerLoader.MetaField.DEFAULT_STRING) {
                return "";
            }
            number = number.Trim();
            int len = number.Length;
            if (len < 7) {
                return number;
            }
            bool allDigits = reAllDigits.IsMatch(number);
            if (allDigits) {
                if (len == 7) {
                    return String.Format("{0}-{1}", number.Substring(0,3), number.Substring(3));
                }
                if ((len == 10) && !number.StartsWith("0") && !number.StartsWith("1")) {
                    return String.Format("{0}-{1}-{2}", number.Substring(0, 3), number.Substring(3, 3), number.Substring(6));
                }
                if ((len == 11) && number.StartsWith("1")) {
                    return String.Format("{0}-{1}-{2}-{3}", number.Substring(0, 1), number.Substring(1, 3),
                        number.Substring(4, 3), number.Substring(7));
                }
                return number;
            }
            bool plusDigits = false;
            // If the phone number is not all digits then it may begin
            // with a plus sign.
            if (!allDigits) {
                plusDigits = rePlusDigits.IsMatch(number);
            }
            if (plusDigits && (len == 12)) {
                if (rePlusDigits1.IsMatch(number)) {
                    return String.Format("{0}{1}-{2}-{3}-{4}", number.Substring(0, 1), number.Substring(1, 1),
                        number.Substring(2, 3), number.Substring(5, 3), number.Substring(8));
                }
            }
            return number;
        }

        /// <summary>
        /// Given a phone number, attempt to determine if it's suspect, i.e.,
        /// it doesn't appear to be valid.
        /// 
        /// NOTE: US-centric.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="sms"></param>
        /// <returns></returns>
        public static bool SuspectPhoneNumber(string number, bool emptyOK, bool sms)
        {
            // Empty, null or *NONE* is suspect ...
            if (!emptyOK && String.IsNullOrEmpty(number)) {
                return true;
            }
            if (!emptyOK && (number == Dec0de.Bll.AnswerLoader.MetaField.DEFAULT_STRING)) {
                return true;
            }
            int len = number.Length;
            if (len > 20) {
                // Too long.
                return true;
            }
            bool allDigits = reAllDigits.IsMatch(number);
            bool plusDigits = false;
            bool usNumber = false;
            if (len <= 7) {
                // We're assuming US phones. US phone numbers are at least 7
                // digits, but allow for 5 and 6 digit SMS numbers.
                if (sms && allDigits && ((len == 5) || (len == 6))) {
                    return false;
                }
                if (allDigits && (len == 7)) {
                    usNumber = true;
                } else {
                    // Suspect.
                    return true;
                }
            }
            // If the phone number is not all digits then it may begin
            // with a plus sign.
            if (!allDigits) {
                plusDigits = rePlusDigits.IsMatch(number);
            }
            if (allDigits && (len == 10)) {
                // Exactly ten digits is good for a US number, but it shouldn't
                // start with a 0 or 1.
                if (!number.StartsWith("0") && !number.StartsWith("1")) {
                    usNumber = true;
                }
            } else if (allDigits && (len == 11)) {
                if (number.StartsWith("1")) {
                    // Looks like a US 10-digit number with the 1 prefix.
                    usNumber = true;
                }
            } else if (plusDigits && (len == 12)) {
                // Looks like a US 10-digit number with a +1 prefix.
                usNumber = rePlusDigits1.IsMatch(number);
            }
            if (usNumber) {
                // It appears to be a US phone number.
                if (RepeatingDigits(number)) {
                    // A repeating pattern can happen, but it's unlikely.
                    // Make exceptions for 888 and 999, except if that's the
                    // only value.
                    if (number.StartsWith("888") || number.StartsWith("999")) {
                        if (number[number.Length - 1] == number[0]) {
                            return true;
                        }
                    } else {
                        return true;
                    }
                }
                if (len < 10) {
                    // If less than 10 digits (i.e., 7) then make certain it meets
                    // certain rules for the prefix.
                    if (!reLastSevenDigitsUS.IsMatch(number)) {
                        return true;
                    }
                } else {
                    // Validate the area code and prefix.
                    if (!reLastTenDigitsUS.IsMatch(number)) {
                        return true;
                    }
                }
            } else {
                // International number.
                if (!plusDigits && !reLongDistanceNumber.IsMatch(number)) {
                    return true;
                }
                if (RepeatingDigits(number)) {
                    return true;
                }
            }
            if ((number.Length > 5) && SeriesOfDigits(number)) {
                return true;
            }
            if (GroupsOfDigits(number)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Overload, defaulting the sms value.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static bool SuspectPhoneNumber(string number, bool emptyOK)
        {
            return SuspectPhoneNumber(number, emptyOK, false);
        }

        /// <summary>
        /// Determines of the phone number is a series of repeating digits.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static bool RepeatingDigits(string number)
        {
            // Remove any leading plus sign.
            string digits;
            if (number.StartsWith("+")) {
                digits = number.Substring(1);
            } else {
                digits = number;
            }
            // Is it all the same number?
            bool allSame = true;
            int repCnt = 1;
            char d = digits[0];
            for (int n = 1; n < digits.Length; n++) {
                if (d != digits[n]) {
                    allSame = false;
                    break;
                }
                repCnt++;
            }
            if (allSame) return true;
            // Not all the same, but check for groups of repeating digits.
            if ((digits.Length >= 10) && (repCnt >= 4) && ((digits.Length - repCnt) >= 4)) {
                return RepeatingDigits(digits.Substring(repCnt));
            }
            return false;
        }

        /// <summary>
        /// See if the phone number digits is a series of incrementing or
        /// decrementing values.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static bool SeriesOfDigits(string number)
        {
            string digits;
            if (number.StartsWith("+")) {
                digits = number.Substring(1);
            } else {
                digits = number;
            }
            bool incrementing = true;
            bool decrementing = true;
            int stop = digits.Length - 1;
            for (int n = 0; n < stop; n++) {
                if (incrementing) {
                    char next;
                    if (digits[n] == '9') {
                        next = '0';
                    } else {
                        int val = (int) digits[n] + 1;
                        next = (char) val;
                    }
                    if (digits[n + 1] != next) {
                        incrementing = false;
                    }
                }
                if (decrementing) {
                    char next;
                    if (digits[n] == '0') {
                        next = '9';
                    } else {
                        int val = (int) digits[n] - 1;
                        next = (char) val;
                    }
                    if (digits[n + 1] != next) {
                        decrementing = false;
                    }
                }
                if (!incrementing && !decrementing) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks for some commonly seen patterns in bad numbers.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static bool GroupsOfDigits(string number)
        {
            string digits;
            if (number.StartsWith("+")) {
                digits = number.Substring(1);
            } else {
                digits = number;
            }
            if (digits.Length < 10) return false;
            if (DigitsRepeat(digits, 4)) {
                return true;
            }
            if (DigitsRepeat(digits, 3)) {
                return true;
            }
            return DigitsRepeat(digits, 2);
        }

        /// <summary>
        /// Determines if the string of digits is a repeating sequence of digits.
        /// </summary>
        /// <param name="digits">Digits to check.</param>
        /// <param name="len">Size of the sequence.</param>
        /// <returns></returns>
        private static bool DigitsRepeat(string digits, int len)
        {
            string sub = digits.Substring(0, len);
            for (int x = len; x < digits.Length; x += len) {
                if ((digits.Length - x) >= len) {
                    if (digits.Substring(x, len) != sub) {
                        return false;
                    }
                } else {
                    string end = digits.Substring(x);
                    if (!sub.StartsWith(end)) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// TODO
        /// Placeholder for determining if a name doesn't look right. The intent
        /// would be for the higher the value the less confiendence that it is
        /// valid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int ScoreNameBadness(string name)
        {
            int badness = 0;
            if (String.IsNullOrEmpty(name)) {
                return badness + 1;
            }
            if (name == Dec0de.Bll.AnswerLoader.MetaField.DEFAULT_STRING) {
                return badness + 1;
            }
            int len = name.Length;
            if (len == 1) {
                badness += 1;
            } else if (len >= 30) {
                badness += 2;
            }
            if (String.IsNullOrWhiteSpace(name)) {
                if (len < 5) {
                    badness += 1;
                } else {
                    badness += 2;
                }
                return badness;
            }
            int spaceCount = 0;
            int alphanumCount = 0;
            int otherCount = 0;
            for (int n = 0; n < len; n++) {
                char c = name[n];
                if (Char.IsWhiteSpace(c)) {
                    spaceCount++;
                }
                if (Char.IsLetterOrDigit(c)) {
                    alphanumCount++;
                } else {
                    otherCount++;
                }
            }
            if (spaceCount == 0) {
                if (len >= 15) {
                    badness += 1;
                    if (len >= 20) {
                        badness += 1;
                    }
                }
            } else {
                int half = (len + 1)/2;
                if (spaceCount > half) badness += 1;
                if ((spaceCount * 15) < len) badness += 1;
            }
            if (otherCount > alphanumCount) badness += 2;
            return badness;
        }
    }

}