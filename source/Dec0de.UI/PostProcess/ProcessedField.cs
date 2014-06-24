/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dec0de.Bll.AnswerLoader;

namespace Dec0de.UI.PostProcess
{
    public class ProcessedInformation
    {
        private static int idCounter = 0;

        public const int DISTANCE_RANGE_CALLLOG = 128;
        public const int DISTANCE_RANGE_ADR = 1024;
        public const int DISTANCE_RANGE_SMS = 1024;
        public const double DISTANCE_DAYS = 30;

        public int Id = ++idCounter;
        public bool Hide = false;
        public long MinMemoryDistance = long.MaxValue;
        public int MemProximityCount = 0;
        public double MinDayDistance = int.MaxValue;
        public int DayProximityCount = 0;
        public bool NumberInAdrBook = false;
        public bool NumberInCallLog = false;
        public bool NumberInSms = false;
    }

    public class ProcessedCallLog : ProcessedInformation
    {
        public readonly MetaCallLog MetaData;
        public bool InAddressBook;
        public bool SuspectPhoneNumber;
        public int NameBadness;

        public ProcessedCallLog(MetaCallLog mf)
        {
            this.MetaData = mf;
            this.InAddressBook = false;
            this.SuspectPhoneNumber = false;
            this.NameBadness = 0;
        }

        public string KeyString()
        {
            string timestamp =
                MetaData.TimeStamp.HasValue ? ((DateTime) MetaData.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss") : "";
            string number = String.IsNullOrEmpty(MetaData.Number) ? "" : MetaData.Number;
            string name = String.IsNullOrEmpty(MetaData.Name) ? "" : MetaData.Name;
            string type = String.IsNullOrEmpty(MetaData.Type) ? "" : MetaData.Type;
            return String.Format("{0}:::{1}:::{2}:::{3}", name, number, timestamp, type);
        }

    }

    public class ProcessedAddressBook : ProcessedInformation
    {
        public readonly MetaAddressBookEntry MetaData;
        public bool InCallLog;
        public bool SuspectPhoneNumber;
        public int NameBadness;

        public ProcessedAddressBook(MetaAddressBookEntry mf)
        {
            this.MetaData = mf;
            this.InCallLog = false;
            this.SuspectPhoneNumber = false;
            this.NameBadness = 0;
        }

        public string KeyString()
        {
            string number = String.IsNullOrEmpty(MetaData.Number) ? "" : MetaData.Number;
            string name = String.IsNullOrEmpty(MetaData.Name) ? "" : MetaData.Name;
            return String.Format("{0}:::{1}", name, number);
        }
    }

    public class ProcessedSms : ProcessedInformation
    {
        public readonly MetaSms MetaData;
        public int Number1Frequency;
        public int SevenDigits1Frequency;
        public int Number2Frequency;
        public int SevenDigits2Frequency;
        public bool SuspectPhoneNumber;
        public bool SuspectPhoneNumber2;

        public ProcessedSms(MetaSms mf)
        {
            this.MetaData = mf;
            this.SuspectPhoneNumber = false;
            this.SuspectPhoneNumber2 = false;
        }

        public string KeyString()
        {
            string timestamp =
                MetaData.TimeStamp.HasValue ? ((DateTime)MetaData.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss") : "";
            string number = String.IsNullOrEmpty(MetaData.Number) ? "" : MetaData.Number;
            string number2 = String.IsNullOrEmpty(MetaData.Number2) ? "" : MetaData.Number2;
            string text = String.IsNullOrEmpty(MetaData.Name) ? "" : MetaData.Message;
            return String.Format("{0}:::{1}:::{2}", text, number, timestamp);
        }
    }
}
