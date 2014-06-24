/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System.Windows.Forms;
using Dec0de.UI.PostProcess;

namespace Dec0de.UI.DecodeResults
{
    public class CallLogListViewItem : ListViewItem
    {
        public ProcessedCallLog CallLog { get; private set; }
        public bool Hidden = false;
        public bool Highlighted = false;

        public CallLogListViewItem(ProcessedCallLog callLog)
            : base()
        {
            this.CallLog = callLog;
        }

        public override string ToString()
        {
            return string.Format("Call Log {0:D6}", CallLog.Id);
        }

    }

    public class AddressBookListViewItem : ListViewItem
    {
        public ProcessedAddressBook AddressBook { get; private set; }
        public bool Hidden = false;
        public bool Highlighted = false;

        public AddressBookListViewItem(ProcessedAddressBook addressBook)
            : base()
        {
            this.AddressBook = addressBook;
        }
    }

    public class SmsListViewItem : ListViewItem
    {
        public ProcessedSms SmsEntry { get; private set; }
        public bool Hidden = false;
        public bool Highlighted = false;

        public SmsListViewItem(ProcessedSms sms)
            : base()
        {
            this.SmsEntry = sms;
        }
    }

    public class ImageListViewItem : ListViewItem
    {
        public ImageBlock ImageBlock { get; private set; }
        public bool Hidden = false;

        public ImageListViewItem(ImageBlock ib)
            : base()
        {
            this.ImageBlock = ib;
        }
    }

}