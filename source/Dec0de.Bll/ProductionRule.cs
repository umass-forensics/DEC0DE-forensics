using System;
using System.Collections.Generic;
using System.Linq;
using Dec0de.Bll.CYK;

namespace Dec0de.Bll
{
    public class ProductionRule
    {
        #region Property Accessors

        public double Probability { get; private set; }

        /// <summary>
        /// Left hand side
        /// </summary>
        public Symbol LHS { get; private set; }

        public Symbol[] RHS { get; private set; }

        public SpecialCondition[] Conditions { get; private set; }

        #endregion

        #region Instantiation

        public ProductionRule(string line)
        {
            string[] ruleAndConditions = line.Trim().Split('&');

            //Parse the production rule
            string[] ruleParts = ruleAndConditions[0].Trim().Split(' ');
            
            Probability = Convert.ToDouble(ruleParts[0]);
            LHS = new Symbol() { Type = SymbolType.Nonterminal, Text = ruleParts[1].Trim() };

            List<Symbol> rhs = new List<Symbol>();

            for (int i = 3; i < ruleParts.Length; i++)
            {
                Symbol newSymbol;

                ruleParts[i] = ruleParts[i].Trim();

                if (ruleParts[i].StartsWith(@"'"))
                    newSymbol = new Symbol() { Type = SymbolType.Terminal, Text = ruleParts[i].Replace('\'', ' ').Trim() };
                else
                    newSymbol = new Symbol() { Type = SymbolType.Nonterminal, Text = ruleParts[i] };

                rhs.Add(newSymbol);
            }

            RHS = rhs.ToArray();

            //Parse the special conditions
            if (ruleAndConditions.Length > 1)
                Conditions = SpecialCondition.ParseConditions(ruleAndConditions[1]);
            else
                Conditions = new SpecialCondition[0];

        }

        #endregion

        #region Public Methods

        public void FlipRHS()
        {
            //TODO: Check this!
            RHS = RHS.Reverse().ToArray();
        }

        public void ScaleProbability(double scale)
        {
            Probability = Probability * scale;
        }

        #endregion

        #region Method Overrides

        public override string ToString()
        {
            string rhs = "";

            for (int i = 0; i < RHS.Length; i++)
            {
                rhs += ' ' + RHS[i].Text;
            }

            return string.Format("{0}  {1} --> {2}", Probability, LHS.Text, rhs);
        }

        #endregion
    }

    public enum SymbolType
    {
        Terminal,
        Nonterminal
    }

    public class Symbol
    {
        public SymbolType Type { get; set; }
        public string Text {get; set;}

        public override string ToString()
        {
            return Text;
        }
    }
}
