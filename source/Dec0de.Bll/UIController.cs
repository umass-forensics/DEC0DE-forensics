                                                                                                                                using System.Text;

namespace Dec0de.Bll
{
    public class UIController
    {
        public string BinaryFileName { get; set; }
        public string GrammarFileName { get; set; }
        public string ComparisonFileName { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(string.Format("Binary File: {0}", BinaryFileName));
            builder.AppendLine(string.Format("Grammar File: {0}", GrammarFileName));

            return builder.ToString();
        }
    }
}
