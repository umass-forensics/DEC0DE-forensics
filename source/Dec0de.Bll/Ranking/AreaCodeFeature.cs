using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Ranking
{
    class AreaCodeFeature
    {
        private List<string> _numbers;
        private Dictionary<string, List<string>> _numbersByAreaCode;
        private Dictionary<string, double> _scoreByAreaCode;

        public AreaCodeFeature(List<string> numbers)
        {
            _numbers = numbers;

            _scoreByAreaCode = new Dictionary<string, double>();
            _numbersByAreaCode = new Dictionary<string, List<string>>();

            for (int i = 0; i < _numbers.Count; i++)
            {
                var areaCode = Utilities.GetAreaCode(_numbers[i]);
                var sevenDigit = Utilities.GetLastSevenDigits(_numbers[i]);

                //We are ignoring all numbers without an area code.
                if(areaCode != null)
                {
                    AddNumber(areaCode, sevenDigit);
                }
            }

            CalculateFeatureScore();
        }

        /// <summary>
        /// Calculate the normalized count of numbers per area code
        /// </summary>
        private void CalculateFeatureScore()
        {
            int total = 0;

            //Get total count of numbers
            foreach (var pair in _numbersByAreaCode)
            {
                total += pair.Value.Count;
            }

            //Get the normalized count
            foreach (var pair in _numbersByAreaCode)
            {
                double norm = (double)pair.Value.Count/total;

                _scoreByAreaCode.Add(pair.Key, norm);
            }
        }

        /// <summary>
        /// Add a number to our area code dictionary. 
        /// </summary>
        /// <param name="areaCode"></param>
        /// <param name="number"></param>
        private void AddNumber(string areaCode, string number)
        {
            //Check if we have seen this area code before. Add it if we have not.
            if(!_numbersByAreaCode.ContainsKey(areaCode))
                _numbersByAreaCode.Add(areaCode, new List<string>());

            //Check if we have seen this number before, if not, add it.
            if(!_numbersByAreaCode[areaCode].Contains(number))
                _numbersByAreaCode[areaCode].Add(number);
        }

        /// <summary>
        /// Gets the normalized score
        /// </summary>
        /// <param name="number"></param>
        /// <returns>A double in the range [0,1]</returns>
        public double GetScoreNormalized(string number)
        {
            var areaCode = Utilities.GetAreaCode(number);

            //If there is not area code, or it has not been seen before return zero. 
            if (areaCode == null || !_numbersByAreaCode.ContainsKey(areaCode))
                return 0f;

            return _scoreByAreaCode[areaCode];
        }

        public double GetScore(string number)
        {
            if (number == "*NONE*")
                return 0f;

            var areaCode = Utilities.GetAreaCode(number);
            var sevenDigit = Utilities.GetAreaCode(number);

            //If the input number has no given area code, try to find a the area code
            if(areaCode == null)
            {
                foreach (var pair in _numbersByAreaCode)
                {
                    if (pair.Value.Contains(sevenDigit))
                    {
                        areaCode = pair.Key;
                        break;
                    }
                }
            }

            //If there is not area code, or it has not been seen before return zero. 
            if (areaCode == null || !_numbersByAreaCode.ContainsKey(areaCode))
                return 0f;

            return _numbersByAreaCode[areaCode].Count;
        }
    }
}
