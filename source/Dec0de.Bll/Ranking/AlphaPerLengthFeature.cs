namespace Dec0de.Bll.Ranking
{
    class AlphaPerLengthFeature
    {
        public static double GetScore(string input)
        {
            return (double) Utilities.GetCountOfAlphaCharacters(input)/input.Length;
        }
    }
}
