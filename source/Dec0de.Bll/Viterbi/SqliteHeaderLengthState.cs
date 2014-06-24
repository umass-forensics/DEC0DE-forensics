using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public class SqliteHeaderLengthState : State
    {
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

            int length = values[lengthIndex];

            if (length < distance + 1)
                return ALMOST_ZERO;

            return base.GetValueProbability(values, index, viterbi);
        }
    }
}
