using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public class AsciiLengthState : State
    {
        public State LengthState { get; set; }

        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from ASCII length state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the ASCII length state is required.</param>
        /// <returns>The emission probability of an output byte, given the ASCII length state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            var baseProb = base.GetValueProbability(values, index, viterbi);

            if (baseProb == ALMOST_ZERO)
                return baseProb;

            var path = viterbi.FindPath(index-1, viterbi.FromState, index-1);

            int lengthIndex = -1;
            int distance = 0;

            for (int i = path.Count-1; i >= 0; i--)
            {
                distance++;
                if(path[i] == LengthState)
                {
                    lengthIndex = index - distance;
                    break;
                }
            }

            if (lengthIndex == -1)
                return ALMOST_ZERO;

            int length = values[lengthIndex];

            if (length < (distance))
                return ALMOST_ZERO;

            return base.GetValueProbability(values, index, viterbi);
        }
    }
}
