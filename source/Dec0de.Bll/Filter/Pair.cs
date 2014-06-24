namespace Dec0de.Bll.Filter
{
    public class Pair
    {
        public int I;
        public int J;

        public override string ToString()
        {
            return string.Format("({0},{1})", I, J);
        }


        public static bool operator ==(Pair p1, Pair p2)
        {
            return (p1.I == p2.I && p1.J == p2.J);
        }

        public static bool operator !=(Pair p1, Pair p2)
        {
            return (p1.I != p2.I || p1.J != p2.J);
        }
    }
}
