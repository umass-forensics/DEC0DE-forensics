using System;

namespace Dec0de.Bll.Filter
{
    public class Cell
    {
        public byte Value;
        public int Index;
        public bool IsMatched;

        public override string ToString()
        {
            return Convert.ToString(Value);
        }
    }
}
