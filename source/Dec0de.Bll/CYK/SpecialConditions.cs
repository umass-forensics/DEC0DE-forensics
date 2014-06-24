using System;
using System.Collections.Generic;

namespace Dec0de.Bll.CYK
{
    public enum SpecialConditionType
    {
        RangeRestrict,
        LengthMatch,
        BrokenLengthMatch
    }


    /// <summary>
    /// the format to be:
    /// specialCondition1,1,2,3;specialCondition2,a,b,c
    /// where the conditions are separated by a semicolon. 
    /// The arguments for each condition will be separated by 
    /// commas. The first argument for each condition should 
    /// be the condition type.
    /// </summary>
    public abstract class SpecialCondition
    {
        public SpecialConditionType ConditionType { get; protected set; }

        public abstract string PrintFormat();

        public abstract bool Check(TableEntry left, TableEntry right, string[] tokens);

        public static SpecialCondition[] ParseConditions(string rawLine)
        {
            List<SpecialCondition> conditions = new List<SpecialCondition>();

            string[] rawConditions = rawLine.Trim().Split(';');

            for (int i = 0; i < rawConditions.Length; i++)
            {
                string[] conditionArgs = rawConditions[i].Trim().Split(',');

                SpecialConditionType type = (SpecialConditionType)Enum.Parse(typeof(SpecialConditionType), conditionArgs[0]);

                SpecialCondition newCondition;

                switch (type)
                {
                    case SpecialConditionType.BrokenLengthMatch:
                        newCondition = new BrokenLengthMatch(conditionArgs);
                        break;
                    case SpecialConditionType.LengthMatch:
                        newCondition = new LengthMatch(conditionArgs);
                        break;
                    case SpecialConditionType.RangeRestrict:
                        newCondition = new RangeRestrict(conditionArgs);
                        break;
                    default:
                        throw new NotImplementedException(string.Format("{0} not implemented!", Convert.ToString(type)));
                }

                conditions.Add(newCondition);
            }

            return conditions.ToArray();
        }
    }

    public class RangeRestrict : SpecialCondition
    {
        public int Index { get; private set; }
        public int Min { get; private set; }
        public int Max { get; private set; }

        public RangeRestrict(string[] args)
        {
            ConditionType = SpecialConditionType.RangeRestrict;

            Index = Convert.ToInt32(args[1]);
            Min = Convert.ToInt32(args[2]);
            Max = Convert.ToInt32(args[3]);
        }

        public override bool Check(TableEntry left, TableEntry right, string[] tokens)
        {
            // format: [('range_restrict', [(n, (min, max))])]
            // matches only if the nth field on the rhs of the rule is within the specified value range
            string[] sublist = Utilities.GetSubArray(tokens, left.Key[0],
                left.Key[0] + left.Key[1], true);

            int value = Utilities.GetValue(sublist, Index, Index + 1);

            if (value < Convert.ToInt32(Min) || value > Convert.ToInt32(Max))
                return false;

            return true;
        }


        public override string PrintFormat()
        {
            return "RangeRestrict,Index,Min,Max";
        }
    }

    public class LengthMatch : SpecialCondition
    {
        public int Adjustment { get; private set; }
        public int Multiplier { get; private set; }

        public LengthMatch(string[] args)
        {
            ConditionType = SpecialConditionType.LengthMatch;

            Adjustment = Convert.ToInt32(args[1]);
            Multiplier = Convert.ToInt32(args[2]);
        }

        public override bool Check(TableEntry left, TableEntry right, string[] tokens)
        {
            // matches if length of 2nd element of RHS matches value of 1st element of RHS plus adjustment times multiplier
            int length = Utilities.GetValue(tokens, left.Key[0], left.Key[0] + left.Key[1]);

            if (length != (right.Key[1] + Adjustment) * Multiplier)
                return false;
            else
                return true;
        }

        public override string PrintFormat()
        {
            return "LengthMatch,Adjustment,Multiplier";
        }
    }

    public class BrokenLengthMatch : SpecialCondition
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public BrokenLengthMatch(string[] args)
        {
            ConditionType = SpecialConditionType.BrokenLengthMatch;

            X = Convert.ToInt32(args[1]);
            Y = Convert.ToInt32(args[2]);
        }

        public override bool Check(TableEntry left, TableEntry right, string[] tokens)
        {

            // tries to match the value of tokens x through y (inclusive) of the LHS with the length of the RHS

            int start = left.Key[0] + X;
            int end = left.Key[0] + Y;

            string[] sublist = Utilities.GetSubArray(tokens, start, end, true);

            int length = Utilities.GetValue(sublist, 0, sublist.Length);

            if (length != right.Key[1])
                return false;
            else
                return true;
        }

        public override string PrintFormat()
        {
            return "BrokenLengthMatch,X,Y";
        }
    }
}
