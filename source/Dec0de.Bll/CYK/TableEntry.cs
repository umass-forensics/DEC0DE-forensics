namespace Dec0de.Bll.CYK
{
    public class TableEntry
    {
        #region Property Accessors

        /// <summary>
        /// tuple with format: (starting byte, number of bytes spanned)
        /// </summary>
        public int[] Key { get; set; }

        /// <summary>
        /// grammar rule tied to this entry
        /// </summary>
        public ProductionRule Rule { get; set; }

        /// <summary>
        /// the lhs of the grammar rule
        /// </summary>
        public string Base { get; set; }

        /// <summary>
        /// the probability of the associated grammar rule
        /// </summary>
        public double Probability { get; set; }
        
        /// <summary>
        /// each entry has 0 (if leaf node) or 2 children. Robert's Note: Not sure if this
        /// is still correct. I think I remember seeing code that only has 1 child.
        /// </summary>
        public TableEntry[] Children { get; set; }

        #endregion

        #region Instantiation

        public TableEntry(int[] key, ProductionRule rule)
            :this(key, rule, new TableEntry[0]){   }

        public TableEntry(int[] key, ProductionRule rule, TableEntry[] children)
        {
            Key = key;
            Rule = rule;
            Base = rule.LHS.Text;
            Probability = rule.Probability;
            Children = children;

            //adjust probability using probabilities of children
            for (int i = 0; i < Children.Length; i++)
            {
                Probability *= Children[i].Probability;
            }
        }

        #endregion

        #region Method Overrides

        public override string ToString()
        {
            string rhs = "";

            for (int i = 0; i < Rule.RHS.Length; i++)
            {
                rhs += ' ' + Rule.RHS[i].Text;
            }

            return string.Format("{0} --> {1} : {2}", Rule.LHS, rhs, Probability);
        }

        #endregion
    }
}
