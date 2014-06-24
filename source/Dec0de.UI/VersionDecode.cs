/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;

namespace Dec0de.UI
{
    static class VersionDecode
    {
        public const int Major = 0;
        public const int Minor = 10;
        static public string VersionString { get { return String.Format("{0}.{1:D2}", Major, Minor); } }
        static public Version Version = new Version(Major, Minor);
    }
}
