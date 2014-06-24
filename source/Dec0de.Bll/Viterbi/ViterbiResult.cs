using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    [Serializable()]
    public class ViterbiResult
    {
        /// <summary>
        /// List of all fields in the inferred Viterbi path.
        /// </summary>
        public List<ViterbiField> Fields { get; set; }
        /// <summary>
        /// Time taken for field level Viterbi run.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The id of the bin file used to create these results. Typically a
        /// sha1 value.
        /// </summary>
        public string MemoryId { get; set; }
    }
}
