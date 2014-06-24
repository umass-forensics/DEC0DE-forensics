using System.Collections.Generic;
using System.IO;

namespace Dec0de.Bll
{
    public class Grammar
    {
        public const double RULE_SCALE = 0.7d;
        public const double INVERSE_RULE_SCALE = 0.3d;

        private Symbol _rootSymbol;
        private List<ProductionRule> _rules = new List<ProductionRule>();
        private Dictionary<string, List<Symbol[]>> _lhsIndex = new Dictionary<string, List<Symbol[]>>();
        private Dictionary<string, List<ProductionRule>> _rhsIndex = new Dictionary<string, List<ProductionRule>>();
        private Dictionary<string, List<ProductionRule>> _terminalIndex = new Dictionary<string, List<ProductionRule>>();
        private Dictionary<string, List<ProductionRule>> _nonterminalIndex = new Dictionary<string, List<ProductionRule>>();

        public Dictionary<string, List<ProductionRule>> RhsIndex
        {
            get { return _rhsIndex; }
        }

        public Dictionary<string, List<ProductionRule>> TerminalIndex
        {
            get { return _terminalIndex; }
        }

        public Symbol Root { get { return _rootSymbol; } }

        public Grammar(string filePath)
        {
            string[] fileLines = File.ReadAllLines(filePath);
            
            for (int i = 0; i < fileLines.Length; i++)
            {
                if (fileLines[i].Length == 0)
                    continue;

                char firstChar = fileLines[i][0];

                if (firstChar != '\n' && firstChar != '#')
                    ParseRule(fileLines[i]);
            }
        }

        private void ParseRule(string line)
        {
            ProductionRule rule = new ProductionRule(line);

            // Add the inverse rule for any binary rule
            if (rule.RHS.Length > 1)
            {
                ProductionRule ruleInverse = new ProductionRule(line);
                ruleInverse.FlipRHS();

                rule.ScaleProbability(RULE_SCALE);
                ruleInverse.ScaleProbability(INVERSE_RULE_SCALE);

                AddRule(ruleInverse);
            }

            AddRule(rule);
        }

        private void AddRule(ProductionRule rule)
        {
            //ASSUMPTION: LHS of first rule in grammar file is root symbol
            if (_rules.Count == 0)
            {
                _rootSymbol = rule.LHS;
                
                _rhsIndex.Add(_rootSymbol.Text, new List<ProductionRule>());
            }

            _rules.Add(rule);

            AddToLhsIndex(rule);

            AddToRhsIndex(rule);

            if (rule.RHS.Length == 1 && rule.RHS[0].Type == SymbolType.Terminal)
                AddToTerminalIndex(rule);

            if (rule.RHS[0].Type == SymbolType.Nonterminal)
                AddToNonterminalIndex(rule);
        }

        private void AddToLhsIndex(ProductionRule rule)
        {
            if (!_lhsIndex.ContainsKey(rule.LHS.Text))
                _lhsIndex.Add(rule.LHS.Text, new List<Symbol[]>());

            _lhsIndex[rule.LHS.Text].Add(rule.RHS);
        }

        private void AddToRhsIndex(ProductionRule rule)
        {
            if (!_rhsIndex.ContainsKey(rule.RHS[0].Text))
                _rhsIndex.Add(rule.RHS[0].Text, new List<ProductionRule>());

            _rhsIndex[rule.RHS[0].Text].Add(rule);
        }

        private void AddToTerminalIndex(ProductionRule rule)
        {
            if (!_terminalIndex.ContainsKey(rule.RHS[0].Text))
                _terminalIndex.Add(rule.RHS[0].Text, new List<ProductionRule>());

            _terminalIndex[rule.RHS[0].Text].Add(rule);
        }

        private void AddToNonterminalIndex(ProductionRule rule)
        {
            if (!_nonterminalIndex.ContainsKey(rule.RHS[0].Text))
                _nonterminalIndex.Add(rule.RHS[0].Text, new List<ProductionRule>());

            _nonterminalIndex[rule.RHS[0].Text].Add(rule);
        }
    }
}
