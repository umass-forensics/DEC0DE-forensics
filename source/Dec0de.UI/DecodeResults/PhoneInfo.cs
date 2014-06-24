/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.UI.DecodeResults
{
    public class PhoneInfo
    {
        public string Manufacturer;
        public string Model;
        public string Note;
        public bool DoNotStore;

        public PhoneInfo(string manufacturer, string model, string note, bool doNotStore)
        {
            this.Manufacturer = manufacturer;
            this.Model = model;
            this.Note = note;
            this.DoNotStore = doNotStore;
        }
    }
}
