using System.Collections.Generic;
using System.Linq;

namespace Dec0de.Bll.CYK
{
    public class CYK
    {
        #region Declaration & Instantiation

        private Dictionary<int[], Dictionary<string, TableEntry>> _table =
                new Dictionary<int[], Dictionary<string, TableEntry>>();
        private string[] _tokens;
        private Grammar _grammar;
        private Dictionary<string, int[]> _keys;


        public CYK(string[] tokens, Grammar grammar)
        {
            _tokens = tokens;
            _grammar = grammar;
            _keys = new Dictionary<string, int[]>();
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            //init step -- fill bottom level of table
            for (int i = 0; i < _tokens.Length; i++)
            {
                List<ProductionRule> rules = _grammar.TerminalIndex[_tokens[i]];

                for (int j = 0; j < rules.Count; j++)
                {
                    int[] key = GetTableKey(i, 1);
                    AddEntry(new TableEntry(key, rules[j]));

#if DEBUG
                    Console.WriteLine("INITIALIZING " + i + ", " + 1);
#endif
                }
            }
        }

        private void BuildTable()
        {
            // dynamic step -- build up to top of table
            for (int span = 2; span < _tokens.Length + 1; span++)
            {
                for (int begin = 0; begin < _tokens.Length - span + 1; begin++)
                {
                    for (int part = 1; part < span; part++)
                    {
                        int[] tableKey = GetTableKey(begin, part);
                        List<string> keyList = _table[tableKey].Keys.ToList();

                        for (int keyIndex = 0; keyIndex < keyList.Count; keyIndex++)
                        {
                            string key = keyList[keyIndex];

                            for (int ruleIndex = 0; ruleIndex < _grammar.RhsIndex[key].Count; ruleIndex++)
                            {
                                ProductionRule rule = _grammar.RhsIndex[key][ruleIndex];

#if DEBUG
                                Console.WriteLine(
                                    string.Format("***** {0},{1},{2},{3},{4}",
                                    new object[] { span, begin, part, key, rule })
                                    );
#endif


                                bool hasConditions = rule.Conditions.Length > 0;
                                bool isBinary = rule.RHS.Length == 2;
                                
                                int[] tableKey1 = GetTableKey(begin + part, span - part);
                                bool hasMatch = isBinary && _table[tableKey1].ContainsKey(rule.RHS[1].Text);

                                //handle matches for non-special rules (those that are in Conjunctive Normal Form and do not have any conditions)
                                if (!hasConditions && isBinary && hasMatch)
                                {
                                    TableEntry left = _table[GetTableKey(begin, part)][key];
                                    TableEntry right = _table[GetTableKey(begin + part, span - part)][rule.RHS[1].Text];

#if DEBUG
                                    Console.WriteLine("MATCH");
                                    Console.WriteLine("- left: " + left);
                                    Console.WriteLine("- right: " + right);
                                    Console.WriteLine("- parent: " + rule);
#endif

                                    AddEntry(new TableEntry(GetTableKey(begin, span), rule, new TableEntry[] { left, right }));
                                }
                                else if (hasConditions)
                                {
                                    HandleRulesWithSpecialConditions(rule, begin, part, span, key);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void HandleRulesWithSpecialConditions(
            ProductionRule rule, int begin, int part, int span, string key)
        {
            bool isBinary = rule.RHS.Length == 2;
            int[] tableKey = GetTableKey(begin + part, span - part);
            bool hasMatch = _table[tableKey].ContainsKey(rule.RHS[1].Text);

            if (isBinary && hasMatch)
            {
                bool satisfied = true;

                TableEntry left = _table[GetTableKey(begin, part)][key];
                TableEntry right = _table[GetTableKey(begin + part, span - part)][rule.RHS[1].Text];

                // Loop through all special conditions
                for (int i = 0; i < rule.Conditions.Length; i++)
                {
                    SpecialCondition condition = rule.Conditions[i];

                    satisfied = condition.Check(left, right, _tokens);

                    if (!satisfied)
                        break;
                }

                // add entry to table if all rule conditions are satisfied
                if (satisfied)
                    AddEntry(new TableEntry(GetTableKey(begin, span), rule, new TableEntry[] { left, right }));
            }
        }

        private int[] GetTableKey(int x, int y)
        {
            string keyString = string.Format("{0},{1}", x, y);

            if (!_keys.ContainsKey(keyString))
                _keys.Add(keyString, new int[]{x,y});

            return _keys[keyString];
        }

        private void AddEntry(TableEntry entry)
        {
            string entryBase = entry.Rule.LHS.Text;

            if(!_table.ContainsKey(entry.Key))
                _table.Add(entry.Key, new Dictionary<string,TableEntry>());

            // if table already has an entry with the same base, keep the more probable of the two
            if (_table[entry.Key].ContainsKey(entryBase))
            {
                TableEntry oldEntry = _table[entry.Key][entryBase];

                if (oldEntry.Probability < entry.Probability)
                {
                    _table[entry.Key][entry.Base] = entry;

#if DEBUG
                    Console.WriteLine("OVERWRITING " + oldEntry + " WITH " + entry + " AT " + string.Format("({0},{1})", entry.Key[0], entry.Key[1]));
#endif
                }

#if DEBUG
                Console.WriteLine("FAILED TO OVERWRITE " + oldEntry + " WITH " + entry + " AT " + string.Format("({0},{1})", entry.Key[0], entry.Key[1]));
#endif
            }
            else
            {
                _table[entry.Key].Add(entryBase, entry);

#if DEBUG
                Console.WriteLine("ADDING " + entry  + " AT " + string.Format("({0},{1})", entry.Key[0], entry.Key[1]));
#endif
            }

            // handle alias rules (rules of the form A --> B)
            for (int i = 0; i < _grammar.RhsIndex[entry.Base].Count; i++)
            {
                ProductionRule rule = _grammar.RhsIndex[entry.Base][i];

                if (rule.RHS.Length == 1)
                    AddEntry(new TableEntry(entry.Key, rule, new TableEntry[] { entry }));
            }
        }

        #endregion

        #region Public Methods

        public static Field[] GetFields(TableEntry root)
        {
            List<Field> fields = new List<Field>();

            foreach (TableEntry child in root.Children)
            {
                if (root.Base == "Field")
                {
                    Field newField = new Field(){Type=root.Children[0].Base,Start=root.Key[0],Length=root.Key[1]};

                    fields.Add(newField);
                }

                fields.AddRange(GetFields(child));
            }

            return fields.ToArray();
        }

        public CYKResult Parse()
        {
            Initialize();
            BuildTable();

            int[] key = GetTableKey(0, _tokens.Length);

            object foo =_table[key];
            object foobar =_table[GetTableKey(0, _tokens.Length)][_grammar.Root.Text];

            CYKResult result = new CYKResult() 
            {
                Table = _table,
                Root = _table[GetTableKey(0, _tokens.Length)][_grammar.Root.Text]
            };

            return result;
        }

        #endregion
    }

    public class CYKResult
    {
        public Dictionary<int[], Dictionary<string, TableEntry>> Table =
        new Dictionary<int[], Dictionary<string, TableEntry>>();

        public TableEntry Root;
    }
}
