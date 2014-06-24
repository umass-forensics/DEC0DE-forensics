using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Filter
{
    [Serializable()]
    public class FilterResult
    {
        #region Property Accessors
        /// <summary>
        /// Number of bytes filtered during block hash filtering.
        /// </summary>
        public long FilteredBytesCount { get; set; }
        /// <summary>
        /// Number of bytes remaining unfiltered after block hash filtering.
        /// </summary>
        public long UnfilteredBytesCount { get; set; }
        /// <summary>
        /// List of blocks remaining unfiltered after block hash filtering (these are used for inference after image filtering).
        /// </summary>
        public List<Block> UnfilteredBlocks { get; set; }
        /// <summary>
        /// The time taken in the block hash filtering process.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The id of the bin file used to create these results. Typically a
        /// sha1 value.
        /// </summary>
        public string MemoryId { get; set; }

        #endregion
    }
}
