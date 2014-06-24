using System;

namespace Dec0de.Bll.Viterbi
{
    [Serializable()]
    public class ViterbiField
    {
        /// <summary>
        /// The index of the start of the field in the Viterbi path
        /// </summary>
        public int OffsetPath { get; set; }

        /// <summary>
        /// The index of the start of the field in the binary file
        /// </summary>
        public long OffsetFile { get; set; }

        /// <summary>
        /// The length of the field in bytes
        /// </summary>
        public int Length
        {
            get
            {
                if (Raw != null)
                    return Raw.Length;
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// The hexadecimal string of the bytes in the field/record.
        /// </summary>
        public string HexString { get; set; }
        /// <summary>
        /// The ASCII string of the bytes in the field/record.
        /// </summary>
        public string AsciiString { get; set; }
        /// <summary>
        /// Human readable form of the field/record.
        /// </summary>
        public string FieldString { get; set; }
        /// <summary>
        /// The name of the state machine, to which the state corresponding to this field/record belongs to.
        /// </summary>
        public MachineList MachineName { get; set; }
        /// <summary>
        /// Array of bytes in the field/record.
        /// </summary>
        public byte[] Raw { get; set; }

        public override string ToString()
        {
            return string.Format("{4}: {0} \t{1} \t{2} \t{3}", Convert.ToString(MachineName), HexString, AsciiString.Replace("\n", @"\n"), FieldString, OffsetFile);
        }

        public string ToCsvString()
        {
            return string.Format("{0},{1},{2},{3}", Convert.ToString(MachineName), HexString, FieldString, OffsetFile);
        }

    }
}
