using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Ranking
{
    class PhoneCrossRecordFeature
    {
        public class Tuple
        {
            public string Number;
            public string RecordType;
        }

        private List<Tuple> _tuples;
        private Dictionary<string, List<string>> _recordTypesBySevenDigit = new Dictionary<string, List<string>>();
        private Dictionary<string, double> _scoreBySevenDigit = new Dictionary<string, double>();

        public PhoneCrossRecordFeature(List<Tuple> tuples)
        {
            _tuples = tuples;

            for (int i = 0; i < _tuples.Count; i++)
            {
                var sevenDigit = Utilities.GetLastSevenDigits(_tuples[i].Number);

                AddNumber(sevenDigit, _tuples[i].RecordType);
            }

            CalculateFeatureScore();
        }

        private void AddNumber(string sevenDigit, string recordType)
        {
            if(!_recordTypesBySevenDigit.ContainsKey(sevenDigit))
                _recordTypesBySevenDigit.Add(sevenDigit, new List<string>());

            if(!_recordTypesBySevenDigit[sevenDigit].Contains(recordType))
                _recordTypesBySevenDigit[sevenDigit].Add(recordType);
        }

        private void CalculateFeatureScore()
        {
            int total = 0;

            //Get total count of numbers
            foreach (var pair in _recordTypesBySevenDigit)
            {
                total += pair.Value.Count;
            }

            //Get the normalized count
            foreach (var pair in _recordTypesBySevenDigit)
            {
                double norm = (double)pair.Value.Count / total;

                _scoreBySevenDigit.Add(pair.Key, norm);
            }
        }

        public double GetScore(string number)
        {
            if (number == "*NONE*")
                return 0f;

            var sevenDigit = Utilities.GetLastSevenDigits(number);

            //If the sevenDigit  has not been seen before return zero. 
            if (sevenDigit == null || !_recordTypesBySevenDigit.ContainsKey(sevenDigit))
                return 0d;

            return _recordTypesBySevenDigit[sevenDigit].Count;
        }

        /// <summary>
        /// Gets the normalized score
        /// </summary>
        /// <param name="number"></param>
        /// <returns>A double in the range [0,1]</returns>
        public double GetScoreNormalized(string number)
        {
            var sevenDigit = Utilities.GetLastSevenDigits(number);

            //If the sevenDigit  has not been seen before return zero. 
            if (sevenDigit == null || !_recordTypesBySevenDigit.ContainsKey(sevenDigit))
                return 0d;

            return _scoreBySevenDigit[sevenDigit];
        }
    }
}
