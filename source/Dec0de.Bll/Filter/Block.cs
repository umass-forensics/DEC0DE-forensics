using System;
using System.Collections.Generic;

namespace Dec0de.Bll.Filter
{
    [Serializable()]
    public class Block
    {
        /// <summary>
        /// The index of this block's first byte from the beginning of the binary file 
        /// </summary>
        public long OffsetFile { get; set; }
        /// <summary>
        /// The index of this block's last byte from the beginning of the binary file 
        /// </summary>
        public long OffsetFile_End { get { return OffsetFile + Bytes.Length; } }
        /// <summary>
        /// List of bytes in the block.
        /// </summary>
        public byte[] Bytes { get; set; }
        /// <summary>
        /// Length of the block in bytes.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Get the total number of bytes in the collection
        /// of blocks
        /// </summary>
        /// <param name="blocks">List of blocks whose collective byte count is to be determined.</param>
        /// <returns>Number of bytes in block collection.</returns>
        public static long GetByteTotal(List<Block> blocks)
        {
            long count = 0;

            for (int i = 0; i < blocks.Count; i++)
            {
                count += blocks[i].Bytes.Length;
            }

            return count;
        }
    }
}
