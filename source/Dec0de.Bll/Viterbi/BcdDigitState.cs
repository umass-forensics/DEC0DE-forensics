using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public class BcdDigitState : State
    {
        /// <summary>
        /// State at whose location is the length of the phone number.
        /// </summary>
        public State LengthState { get; set; }

        /// <summary>
        /// Returns the emission probability of a particular byte. This is used to ensure we
        /// don't look further than defined by the assoicated length state. If we're beyond
        /// the length return ALMOST_ZERO, else return the existing base probability.
        /// </summary>
        /// <param name="value">List of byte outputs from the State.</param>
        /// <param name="index">Index of the byte in values, whose emission probability,
        /// given the BcdDigitState State is required.</param>
        /// <returns>The emission probability of an output byte, given the BcdDigitState state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            var baseProb = base.GetValueProbability(values, index, viterbi);

            if (baseProb == ALMOST_ZERO) return baseProb;

            var path = viterbi.FindPath(index - 1, viterbi.FromState, 24);

            int lengthIndex = -1;
            int byteDistance = 0;

            for (int i = path.Count - 1; i >= 0; i--) {
                byteDistance++;
                if (path[i] == LengthState) {
                    lengthIndex = index - byteDistance;
                    break;
                }
            }

            if (lengthIndex == -1) return ALMOST_ZERO;
            // Subtract 1 from the distance to allow for the Type of Address (TOA)
            // byte between the length and start of BCD phone number.
            int phoneNumLength = values[lengthIndex];
            if (((phoneNumLength + 1) / 2) < (byteDistance - 1)) {
                return ALMOST_ZERO;
            }

            return baseProb;
        }
    }
}
