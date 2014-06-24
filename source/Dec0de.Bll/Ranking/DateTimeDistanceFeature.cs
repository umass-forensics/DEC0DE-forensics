using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Ranking
{
    class DateTimeDistanceFeature
    {
        private static readonly DateTime EPOCH = new DateTime(1900, 1,1);

        private List<DateTime> _dateTimes = new List<DateTime>();
        private List<double> _daysFromEpoch = new List<double>();
        private double _average;


        public DateTimeDistanceFeature(List<DateTime> dateTimes)
        {
            _dateTimes = Utilities.RemoveDuplicateDates(dateTimes);

            for (int i = 0; i < _dateTimes.Count; i++)
            {
                var days = (_dateTimes[i] - EPOCH).TotalDays;

                _daysFromEpoch.Add(days);
            }

            _average = Utilities.CalculateHarmonicMean(_daysFromEpoch);
        }



        /// <summary>
        /// Returns the absolute value of the number of days from the mean
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public double GetScore(DateTime input)
        {
            var days = (input - EPOCH).TotalDays;

            var distance = days - _average;

            return Math.Abs(distance);
        }
    }
}
