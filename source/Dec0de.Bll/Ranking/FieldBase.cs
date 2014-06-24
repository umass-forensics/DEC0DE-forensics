using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Ranking
{
    class FieldBase
    {
        /// <summary>
        /// Returns a normalized confidence score for the
        /// given record. Values should be in the range [0,1]
        /// </summary>
        public virtual double NormalizedScore { get; internal set; }
    }
}
