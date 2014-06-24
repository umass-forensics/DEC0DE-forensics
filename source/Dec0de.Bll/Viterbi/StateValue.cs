using System;

namespace Dec0de.Bll.Viterbi
{
    public class StateValue
    {
        public byte Value { get; set; }
        public double Probability { get; set; }

        public override string ToString()
        {
            return Convert.ToString(Value, 16).PadLeft(2, '0') + " : " + Probability;
        }
    }
}
