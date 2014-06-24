using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public class SevenBitState : State
    {
        /// <summary>
        /// Indicates whether or not this state is the ending state of the parent state machine.
        /// </summary>
        public bool IsEnd { get; set; }

        public State LengthState { get; set; }


        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="values">List of byte outputs from SevenBit State.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the SevenBit State is required.</param>
        /// <param name="viterbi"></param>
        /// <returns>The emission probability of an output byte, given the SevenBit state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            var baseProb = base.GetValueProbability(values, index, viterbi);

            if (baseProb == ALMOST_ZERO)
                return baseProb;

            var path = viterbi.FindPath(index - 1, viterbi.FromState, 141);

            int lengthIndex = -1;
            int byteDistance = 0;

            for (int i = path.Count - 1; i >= 0; i--) {
                byteDistance++;
                if (path[i] == LengthState) {
                    lengthIndex = index - byteDistance;
                    break;
                }
            }

            if (lengthIndex == -1)
                return ALMOST_ZERO;

            int septetLength = values[lengthIndex];

            //Convert from number of septets to number of bytes
            int byteLength = (int)Math.Ceiling((double)septetLength * 7 / 8);

            if (!IsEnd) {
                if (byteLength < byteDistance) {
                    return ALMOST_ZERO;
                } else if (byteLength > byteDistance) {
                    //Perform a partial decode of the bytes. If there are any invalid characters we want to quit now. This (should) prevent problems of long fields subsuming smaller fields.
                    if (!IsValid(byteDistance, index, values, byteDistance)) {
                        return ALMOST_ZERO;
                    }
                }
            } else {
                if (byteLength != byteDistance) {
                    return ALMOST_ZERO;
                } else {
                    if (!IsValid(byteLength, index, values, septetLength)) {
                        return ALMOST_ZERO;
                    }
                }
            }

            return base.GetValueProbability(values, index, viterbi);
        }

        /// <summary>
        /// Returns true if the string only contains printable characters
        /// </summary>
        /// <param name="length"></param>
        /// <param name="index"></param>
        /// <param name="values"></param>
        /// <param name="septetLength"></param>
        /// <returns></returns>
        private static bool IsValid(int length, int index, byte[] values, int septetLength)
        {
            byte[] bytes = new byte[length];

            for (int i = 0; i < bytes.Length; i++) {
                bytes[bytes.Length - i - 1] = values[index - i];
            }

            var result = Utilities.Decode7bit(bytes, septetLength, false);

            for (int i = 0; i < result.Length; i++) {
                // 0 and 2 are substitution chracters that we permit (@, $). Disallow
                // 0x24, which is not $. We exclude some Latin characters in the
                // substitution table, but if we're not restrictive everything matches.
                byte val = result[i];
                if ((val == 0x24) || (val >= 0x5b && val <= 0x60)) {
                    return false;
                }
                if (val >= 0x20 && val <= 0x7a) {
                    continue;
                }
                // Don't allow low sub chars in beginning or we'll have a lot of
                // false positives. Unfortunately, we can miss some text.
                if ((i >= 3) && (val == 0 || val == 2)) {
                    continue;
                }
                return false;
            }

            return true;
        }

    }
}
