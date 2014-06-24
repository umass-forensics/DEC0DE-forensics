using System;
using System.Collections.Generic;

namespace Dec0de.Bll.Filter
{
    public class SequenceAligner
    {
        private int[,] _a;
        private readonly Cell[] _inputCells;
        private readonly Cell[] _comparisonCells;

        private const int GAP_PENALTY = 1;
        private const int MISMATCH_PENALTY = 5;


        private readonly List<Pair> _pairs = new List<Pair>();


        public SequenceAligner(Cell[] inputCells, Cell[] comparisonCells)
        {
            _inputCells = inputCells;
            _comparisonCells = comparisonCells;
        }

        #region Sequence Alignment

        public static Cell[] CreateCellSequence(byte[] bytes)
        {
            Cell[] cells = new Cell[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
            {
                cells[i] = new Cell { Index = i, Value = bytes[i] };
            }

            return cells;
        }

        public List<Pair> Alignment()
        {
            _a = new int[_inputCells.Length + 1, _comparisonCells.Length + 1];

            for (int i = 0; i < _inputCells.Length + 1; i++)
            {
                _a[i, 0] = i * GAP_PENALTY;
            }

            for (int j = 0; j < _comparisonCells.Length + 1; j++)
            {
                _a[0, j] = j * GAP_PENALTY;
            }

            for (int j = 1; j < _comparisonCells.Length + 1; j++)
            {
                for (int i = 1; i < _inputCells.Length + 1; i++)
                {
                    //I have to use X[i-1] due to a mismatch between 1 based indexing and zero based indexing in the algorithm
                    _a[i, j] = Min
                        (
                            MismatchCost(_inputCells[i - 1].Value, _comparisonCells[j - 1].Value) + _a[i - 1, j - 1],
                            GAP_PENALTY + _a[i - 1, j],
                            GAP_PENALTY + _a[i, j - 1]
                        );
                }
            }


            FindSolution(_inputCells.Length, _comparisonCells.Length);

            return _pairs;
        }

        private void FindSolution(int i, int j)
        {
            if (i == 0 || j == 0)
            {
                AddUnmatchedPairs();
                return;
            }

            int mismatch = MismatchCost(_inputCells[i - 1].Value, _comparisonCells[j - 1].Value) + _a[i - 1, j - 1];
            int gapJ = GAP_PENALTY + _a[i - 1, j];
            int gapI = GAP_PENALTY + _a[i, j - 1];

            if (mismatch <= gapJ && mismatch <= gapI)
            {
                //Add Pair
                _pairs.Add(new Pair { I = _inputCells[i - 1].Index, J = _comparisonCells[j - 1].Index });

                _inputCells[i - 1].IsMatched = true;
                _comparisonCells[i - 1].IsMatched = true;

                //Recursive Call
                FindSolution(i - 1, j - 1);
            }
            else if (gapJ <= mismatch && gapJ <= gapI)
            {
                //Recursive Call
                FindSolution(i - 1, j);
            }
            else if (gapI <= mismatch && gapI <= gapJ)
            {
                //Recursive Call
                FindSolution(i, j - 1);
            }
            else
            {
                throw new ApplicationException("How can this be?!");
            }
        }

        private void AddUnmatchedPairs()
        {
            for (int i = 0; i < _inputCells.Length; i++)
            {
                if (!_inputCells[i].IsMatched)
                    _pairs.Add(new Pair { I = _inputCells[i].Index, J = -1 });
            }

            for (int j = 0; j < _comparisonCells.Length; j++)
            {
                if (!_comparisonCells[j].IsMatched)
                    _pairs.Add(new Pair { I = -1, J = _comparisonCells[j].Index });
            }
        }

        private static int Min(int one, int two, int three)
        {
            return Math.Min(one, Math.Min(two, three));

        }

        private static int MismatchCost(byte b1, byte b2)
        {
            if (b1 == b2)
            {
                return 0;
            }
            else
            {
                return MISMATCH_PENALTY;
            }
        }

        #endregion
    }
}
