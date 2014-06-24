using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Ranking
{
    /// <summary>
    /// Count the number of incidents of a phone number in multiple forms, i.e., in both 10-digit and 7-digit
    /// </summary>
    class PhoneFormFeature
    {
        private List<string> _numbers;
        private Dictionary<string, List<string>> _formsBySevenDigit = new Dictionary<string, List<string>>();
        private Dictionary<string, double> _scoreBySevenDigit = new Dictionary<string, double>();

        public PhoneFormFeature(List<string> numbers)
        {
            _numbers = numbers;

            for (int i = 0; i < _numbers.Count; i++)
            {
                var sevenDigit = Utilities.GetLastSevenDigits(_numbers[i]);

                AddNumber(sevenDigit, _numbers[i]);
            }

            CalculateFeatureScore();
        }

        private void AddNumber(string sevenDigit, string number)
        {
            if(!_formsBySevenDigit.ContainsKey(sevenDigit))
                _formsBySevenDigit.Add(sevenDigit, new List<string>());

            if(!_formsBySevenDigit[sevenDigit].Contains(number))
                _formsBySevenDigit[sevenDigit].Add(number);
        }

        private void CalculateFeatureScore()
        {
            int total = 0;

            //Get total count of numbers
            foreach (var pair in _formsBySevenDigit)
            {
                total += pair.Value.Count;
            }

            //Get the normalized count
            foreach (var pair in _formsBySevenDigit)
            {
                double norm = (double)pair.Value.Count / total;

                _scoreBySevenDigit.Add(pair.Key, norm);
            }
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
            if (sevenDigit == null || !_formsBySevenDigit.ContainsKey(sevenDigit))
                return 0d;

            return _scoreBySevenDigit[sevenDigit];
        }

        /// <summary>
        /// Gets the normalized score
        /// </summary>
        /// <param name="number"></param>
        /// <returns>A double in the range [0,1]</returns>
        public double GetScore(string number)
        {
            if (number == "*NONE*")
                return 0f;

            var sevenDigit = Utilities.GetLastSevenDigits(number);

            //If the sevenDigit  has not been seen before return zero. 
            if (sevenDigit == null || !_formsBySevenDigit.ContainsKey(sevenDigit))
                return 0d;

            return _formsBySevenDigit[sevenDigit].Count;
        }
    }
}
