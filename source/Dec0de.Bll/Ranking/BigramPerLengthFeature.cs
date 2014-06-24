using System;
using Dec0de.Bll.Viterbi;

namespace Dec0de.Bll.Ranking
{
    public class BigramPerLengthFeature
    {
        private BigramState _state = new BigramState();

        public double GetScore(string input)
        {
            var inputBytes = Utilities.GetCharBytes(input);

            double prob = 1d;

            for (int i = 0; i < inputBytes.Length; i++)
            {
                double newProb = _state.GetValueProbability(inputBytes, i, null) * input.Length;
                prob += Math.Log(newProb);
            }

            if (prob == double.NegativeInfinity)
                prob = double.MinValue;

            return prob;
        }
    }
}
