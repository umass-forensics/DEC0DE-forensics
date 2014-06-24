using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public class SqliteRecordLengthState : State
    {
        private Dictionary<int, int> _lengthDict = new Dictionary<int, int>();

        public State LengthState { get; set; }

        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            var baseProb = base.GetValueProbability(values, index, viterbi);

            if (baseProb == ALMOST_ZERO)
                return baseProb;

            var path = viterbi.FindPath(index - 1, viterbi.FromState, index - 1);

            int lengthIndex = -1;
            int distance = 0;

            for (int i = path.Count - 1; i >= 0; i--)
            {
                distance++;
                if (path[i] == LengthState)
                {
                    lengthIndex = index - distance;
                    break;
                }
            }

            if (lengthIndex == -1)
                return ALMOST_ZERO;

            int headerLength = values[lengthIndex];

            if (headerLength >= (distance))
                return ALMOST_ZERO;

            var serialTypes = new List<int>();
            var varint = new List<byte>();

            //Grab the varints that sum to the length of the record size (sans header)
            for (int i = 0; i < headerLength-1; i++ )
            {
                byte b = values[lengthIndex + 1 + i];
                varint.Add(b);

                if( b < 0x80 || varint.Count() == 9)
                {
                    serialTypes.Add(ParseVarint(varint.ToArray()));
                    varint.Clear();
                }

            }

            int recordLength;

            if (_lengthDict.ContainsKey(lengthIndex))
            {
                recordLength = _lengthDict[lengthIndex];
            }
            else
            {
                recordLength = CalculateRecordLength(serialTypes);
                _lengthDict.Add(lengthIndex, recordLength);
            }

            if (recordLength != (distance + 1 - headerLength))
                return ALMOST_ZERO;

            return base.GetValueProbability(values, index, viterbi);
        }

        private int CalculateRecordLength(List<int> serialTypes)
        {
            int recordLength = 0;

            for (int i = 0; i < serialTypes.Count; i++)
            {
                //Check if the column is a string, i.e. must be
                //more than 13 and odd
                if (serialTypes[i] >= 13 && serialTypes[i] % 2 == 1)
                    recordLength += (serialTypes[i] - 13) / 2;
                //blob: more than 12 and even
                else if (serialTypes[i] >= 12)
                    recordLength += (serialTypes[i] - 12) / 2;
                //8-bit twos complement int
                else if (serialTypes[i] == 1)
                    recordLength += 1;
                //16-bit twos complement int
                else if (serialTypes[i] == 2)
                    recordLength += 2;
                //24-bit twos complement int
                else if (serialTypes[i] == 3)
                    recordLength += 3;
                //32-bit twos complement int
                else if (serialTypes[i] == 4)
                    recordLength += 4;
                //48-bit twos complement int
                else if (serialTypes[i] == 5)
                    recordLength += 6;
                //64-bit twos complement int
                else if (serialTypes[i] == 6)
                    recordLength += 8;
                //64-bit floating point
                else if (serialTypes[i] == 7)
                    recordLength += 8;
                //int constant 0
                else if (serialTypes[i] == 8)
                    recordLength += 0;
                //int constant 1
                else if (serialTypes[i] == 9)
                    recordLength += 0;
                //Not used currently, reserved
                else if (serialTypes[i] == 10 || serialTypes[i] == 11)
                    recordLength += 0;
            }

            return recordLength;
        }

        private int ParseVarint(byte[] bytes)
        {
            int num = 0;

            int lastbit = (bytes.Length == 9) ? 1 : 0;

            byte[] revBytes = bytes.Reverse().ToArray();

            for (int i = 0; i < revBytes.Length; i++)
            {
                if(lastbit == 1 && i==0)
                {
                    num += revBytes[i];
                }
                else
                {
                    byte b = (byte)(revBytes[i] & 0x7F);
                    num += b << (i*7 + lastbit);
                }

            }

            return num;
        }
    }
}
