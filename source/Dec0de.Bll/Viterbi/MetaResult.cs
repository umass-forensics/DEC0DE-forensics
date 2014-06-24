using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public class MetaResult
    {
        /// <summary>
        /// The Viterbi field from field level inference.
        /// </summary>
        public ViterbiField Field;
        /// <summary>
        /// The name of the Meta machine to which Field belongs to.
        /// </summary>
        public MetaMachine Name;

        public override string ToString()
        {
            return string.Format("{0} : {1}", Convert.ToString(Name), Field.FieldString);
        }
    }
}
