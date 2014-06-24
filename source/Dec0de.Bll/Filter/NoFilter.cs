using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Filter
{
    /// <summary>
    /// This class does not filter the image at all. Useful for images
    /// that have already been prefiltered. It will spilt up the image based
    /// on the input blocksize.
    /// </summary>
    public class NoFilter
    {
        private readonly string _imagePath;
        private readonly int _blockSize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="blockSize">In bytes</param>
        public NoFilter(string imagePath, int blockSize)
        {
            _imagePath = imagePath;
            _blockSize = blockSize;
        }

        public FilterResult Filter()
        {
            int count = 0;
            var fileInfo = new FileInfo(_imagePath);
            var blocks = new List<Block>();

            while(count < fileInfo.Length)
            {
                int newLength = Math.Min(_blockSize, Convert.ToInt32(fileInfo.Length - count));

                var block = new Block() { Length = newLength, OffsetFile = count };
                blocks.Add(block);

                count += newLength;

            }

            var result = new FilterResult() {UnfilteredBlocks = blocks};

            return result;
        }

    }
}
