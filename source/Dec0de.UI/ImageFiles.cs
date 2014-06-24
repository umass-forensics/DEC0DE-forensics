/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Collections.Concurrent;
using Dec0de.Bll.Filter;

namespace Dec0de.UI
{
    public class ImageBlock
    {
        // Must use atomic operations to increment.
        private static int counter = 0;

        public enum ImageType : int
        {
            JPG,
            PNG,
            GIF,
            BMP
        }

        /// <summary>
        /// The serial number of the image block.
        /// </summary>
        public int Num;
        /// <summary>
        /// The beginning location of the image block.
        /// </summary>
        public long Offset;
        /// <summary>
        /// The length in bytes of the image block.
        /// </summary>
        public int Length;
        /// <summary>
        /// The type of image viz. JPG, PNG, GIF, BMP.
        /// </summary>
        public ImageType ImgType;

        public ImageBlock(long offset, int len, ImageType typ)
        {
            this.Num = Interlocked.Increment(ref counter);
            this.Offset = offset;
            this.Length = len;
            this.ImgType = typ;
        }

        public string GetImageType()
        {
            return ImageTypeToString(this.ImgType);
        }

        public static String ImageTypeToString(ImageType typ)
        {
            switch (typ) {
                case ImageType.JPG:
                    return "jpg";
                case ImageType.PNG:
                    return "png";
                case ImageType.GIF:
                    return "gif";
                case ImageType.BMP:
                    return "bmp";
                default:
                    return "???";
            }
        }

        public static ImageFormat ImageTypeToImageFormat(ImageType typ)
        {
            switch (typ) {
                case ImageType.JPG:
                    return ImageFormat.Jpeg;
                case ImageType.PNG:
                    return ImageFormat.Png;
                case ImageType.GIF:
                    return ImageFormat.Gif;
                case ImageType.BMP:
                    return ImageFormat.Bmp;
                default:
                    throw new Exception("Invalid image type");
            }
        }
    }

    public class ImageFiles
    {
        public const int MAX_FILE_SIZE = 16384;

        // Defines the maximum number of threads. If we're debugging we use
        // only 1 thread because multiple threads are slower when running under
        // Visual Studio (regardless of whether in debug or release mode). Why?
        // Each test for whether a block represents a valid image can potentially
        // throw an exception. Throughout the scan, lots of exceptions are thrown.
        // If running under VS, this results in first chance exceptions so that a
        // developer can identify the root cause. This greatly slows the algorithm,
        // and it's exacerbated when multiple threads are generating first chance
        // exceptions.
#if DEBUG
        private const int MAX_THREADS = 1;
#else
        private const int MAX_THREADS = 4;
#endif
        private const int MIN_REGION = 32*1024*1000;
        private string fileName = null;
        private List<ImageBlock>[] threadBlocks = null;

        public List<ImageBlock> ImageBlocks = new List<ImageBlock>();
        
        private struct ThreadInfo
        {
            public int id;
            public BufferedStream stream;
            public long start;
            public long end;
        }

        /// <summary>
        /// Constructor. Opens the file for reading.
        /// </summary>
        /// <param name="fname">Path to phone's memory file.</param>
        public ImageFiles(string fname)
        {
            fileName = fname;
        }

        /// <summary>
        /// Alternate constructor with empty list.
        /// </summary>
        public ImageFiles()
        {
        }

        /// <summary>
        /// Deprecated. Was used to close the file.
        /// </summary>
        public void Close()
        {
        }

        /// <summary>
        /// Called to invoke finding images.
        /// </summary>
        public void Process()
        {
            if (fileName == null) {
                throw new Exception("File name not defined");
            }
            ThreadInfo[] threads = null;
            BufferedStream stream = null;
            try {
                // Calculate number of threads.
                stream = new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read));
                long fsz = stream.Length;
                long nRegions = fsz/MIN_REGION;
                if (nRegions == 0) nRegions = 1;
                int nThreads;
                if (nRegions > MAX_THREADS) {
                    nThreads = MAX_THREADS;
                } else {
                    nThreads = (int) nRegions;
                }
                // Calculate where each thread is to start and end it's search.
                // Open the file stream for the thread to use.
                threads = new ThreadInfo[nThreads];
                threadBlocks = new List<ImageBlock>[nThreads];
                long regionsz = fsz / nThreads;
                long offset = 0;
                for (int n = 0; n < nThreads; n++) {
                    threadBlocks[n] = new List<ImageBlock>();
                    threads[n].id = n;
                    if (n == 0) {
                        threads[n].stream = stream;
                        stream = null;
                    } else {
                        threads[n].stream = new BufferedStream(
                            new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan),
                            MAX_FILE_SIZE*2);
                    }
                    threads[n].start = offset;
                    if (n < (nThreads - 1)) {
                        threads[n].end = offset + regionsz - 1;
                    } else {
                        threads[n].end = fsz - 1;
                    }
                    offset += regionsz;
                }
                // Now start each thread.
                BlockingCollection<int> queue = new BlockingCollection<int>(nThreads);
                foreach (ThreadInfo ti in threads) {
                    ImageFilesThread ift = new ImageFilesThread(ti.id, ti.stream, ti.start, ti.end, queue, this);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ift.Process));
                }
                // Wait for each thread to complete.
                for (int n = 0; n < nThreads; n++) {
                    queue.Take();
                }
                // Combine the lists of image blocks.
                CollateList(nThreads);
                // Allow for some garbage collection.
                for (int n = 0; n < threadBlocks.Length; n++) {
                    threadBlocks[n].Clear();
                    threadBlocks[n] = null;
                }
            } catch (Exception ex) {
                if (stream != null) {
                    stream.Close();
                }
                throw ex;
            } finally {
                if (threads != null) {
                    foreach (ThreadInfo ti in threads) {
                        if (ti.stream != null) {
                            try {
                                ti.stream.Close();
                            } catch {
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Each thread creates its own list of ImageBlocks. This avoids locking
        /// but now we need to combine the lists into a single sorted list. Each
        /// thread's list is already sorted by offset.
        /// </summary>
        /// <param name="nThreads">Number of threads.</param>
        private void CollateList(int nThreads)
        {
            if (nThreads == 1) {
                ImageBlocks.AddRange(threadBlocks[0]);
                return;
            }
            // It's possible that we can have an image that overlaps with another
            // image found by separate threads. use the first image, which is
            // euqivalent to what would happen if we had a single thread.
            long prevEnd = -1;
            for (int n = 0; n < nThreads; n++) {
                for (int i = 0; i < threadBlocks[n].Count; i++) {
                    ImageBlock ib = threadBlocks[n][i];
                    if (ib.Offset > prevEnd) {
                        ImageBlocks.Add(ib);
                        prevEnd = ib.Offset + ib.Length - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Called by a thread when it discovers an image. Adds the image to a list.
        /// </summary>
        /// <param name="block">ImageBlock object represeting the located image.</param>
        /// <param name="id">Xero-based index identifying the thread.</param>
        public void AddBlock(ImageBlock block, int id)
        {
            threadBlocks[id].Add(block);
        }

        /// <summary>
        /// Reduces the list of images by removing those that exist completely within
        /// filtered blocks.
        /// </summary>
        /// <param name="imageBlocks">List of those blocks in phone's memory that represent images.</param>
        /// <param name="filterResult">The blocks remianing after block hash filtering.</param>
        /// <returns>The list of image blocks remaining after the removing those existing completely within block hash filtered blocks.</returns>
        private static List<ImageBlock> RemoveImages(List<ImageBlock> imageBlocks, FilterResult filterResult)
        {
            List<ImageBlock> reduced = new List<ImageBlock>();
            foreach (ImageBlock ib in imageBlocks) {
                long imgStart = ib.Offset;
                long imgEnd = imgStart + ib.Length - 1;
                foreach (Block block in filterResult.UnfilteredBlocks) {
                    long blockStart = block.OffsetFile;
                    long blockEnd = blockStart + block.Length - 1; // inclusive
                    if (((blockStart >= imgStart) && (blockStart <= imgEnd)) ||
                        ((blockEnd >= imgStart) && (blockEnd <= imgEnd)) ||
                        ((imgStart >= blockStart) && (imgStart <= blockEnd)) ||
                        ((imgEnd >= blockStart) && (imgEnd <= blockEnd))) {
                        reduced.Add(ib);
                        break;
                    }
                }
            }
            return reduced;
        }

        /// <summary>
        /// Begins the process of removing image block from phone's memory by calling several methods of ImageFiles.
        /// </summary>
        /// <param name="filterResult">The blocks remianing after block hash filtering. It is modified to being free from any image blocks too, by the end of the method.</param>
        /// <param name="imageFiles">The image blocks that were previosuly located on the phone's memory.</param>
        public static FilterResult FilterOutImages(FilterResult filterResult, ImageFiles imageFiles)
        {
            List<ImageBlock> reduced = RemoveImages(imageFiles.ImageBlocks, filterResult);
            WorkerThread.write("Filtered {0} images", imageFiles.ImageBlocks.Count - reduced.Count);
            long oldFilteredCount = filterResult.FilteredBytesCount;
            filterResult = FilterImageBlocks(imageFiles, filterResult);
            WorkerThread.write("Filtered an additional {0} bytes", filterResult.FilteredBytesCount - oldFilteredCount);
            imageFiles.ImageBlocks = reduced;
            return filterResult;
        }

        /// <summary>
        /// Calls methods to begin locating image blocks in the memory file.
        /// </summary>
        /// <param name="filePath">Path of the phone's memory file.</param>
        /// <returns>A list of image blocks present in the phone's memory.</returns>
        public static ImageFiles LocateImages(string filePath)
        {
            ImageFiles imageFiles = null;
            try {
                imageFiles = new ImageFiles(filePath);
                imageFiles.Process();
                imageFiles.Close();
            } catch (Exception ex) {
                WorkerThread.write("Error locating images: " + ex.Message);
            } finally {
                if (imageFiles != null) {
                    imageFiles.Close();
                } else {
                    imageFiles = new ImageFiles();
                }
            }
            return imageFiles;
        }

        /// <summary>
        /// Called after two independant operations have been performed: finding
        /// images in the memory, and excluding memory blocks because their hashes
        /// were known. Here we further reduce the memory that we'll use for
        /// searching for fields by removing memory consumed by images. 
        /// </summary>
        /// <param name="imageFiles">Locations of found memory files.</param>
        /// <param name="filterResult">The filtered memory blocks.</param>
        /// <returns>An updated filtered memory block object.</returns>
        private static FilterResult FilterImageBlocks(ImageFiles imageFiles, FilterResult filterResult)
        {
            int imgCount = imageFiles.ImageBlocks.Count;
            if (imgCount == 0) {
                // No images had been found: nothing to do.
                return filterResult;
            }
            bool foundOverlap = false;
            List<Block> reducedUnfilteredBlocks = new List<Block>();
            // For each memory block, update it if it contains all or part of an image.
            foreach (Block block in filterResult.UnfilteredBlocks) {
                long blockStart = block.OffsetFile;
                long blockEnd = blockStart + block.Length - 1; // inclusive
                bool modified = false;
                int indx = 0;
                // Go through the images and see if one is contained in the
                // memory block.
                while (!modified && (indx < imgCount)) {
                    long imgStart = imageFiles.ImageBlocks[indx].Offset;
                    if (blockEnd < imgStart) {
                        // We can assume image locations are ordered, so we can
                        // stop looking. All subsequent images will start after
                        // this memory block. Move on to the next block.
                        break;
                    }
                    long imgEnd = imgStart + imageFiles.ImageBlocks[indx].Length - 1; // inclusive
                    if (blockStart > imgEnd) {
                        // The image ends before the block starts.
                        indx++;
                        continue;
                    }
                    if (((blockStart >= imgStart) && (blockStart <= imgEnd)) ||
                        ((blockEnd >= imgStart) && (blockEnd <= imgEnd)) ||
                        ((imgStart >= blockStart) && (imgStart <= blockEnd)) ||
                        ((imgEnd >= blockStart) && (imgEnd <= blockEnd))) {
                        // All or part of the image is within the memory block.
                        modified = true;
                        foundOverlap = true;
                        List<Block> reduced = RemoveImageBlock(block, imageFiles.ImageBlocks[indx]);
                        if (reduced.Count == 1) {
                            reducedUnfilteredBlocks.Add(reduced[0]);
                        } else if (reduced.Count > 0) {
                            reducedUnfilteredBlocks.AddRange(reduced);
                        }
                        // It's possible there are more images that overlap, but
                        // we'll get them in a recursive call.
                        break;
                    }
                    indx++;
                }
                if (!modified) {
                    reducedUnfilteredBlocks.Add(block);
                }
            }
            // If we didn't find any overlaps, we're done.
            if (!foundOverlap) {
                return filterResult;
            }
            long unfilteredCount = 0;
            foreach (Block block in reducedUnfilteredBlocks) {
                unfilteredCount += block.Length;
            }
            long filteredCount = filterResult.FilteredBytesCount + (filterResult.UnfilteredBytesCount - unfilteredCount);
            // A block can have multiple images within it. We remove one image
            // at a time, so we recursively call this function to remove images
            // from updated blocks. It may not be the most efficient method,
            // but it's a little cleaner.
            FilterResult newFilterResult = new FilterResult
            {
                Duration = filterResult.Duration,
                FilteredBytesCount = filteredCount,
                UnfilteredBytesCount = unfilteredCount,
                UnfilteredBlocks = reducedUnfilteredBlocks
            };
            return FilterImageBlocks(imageFiles, newFilterResult);
        }

        /// <summary>
        /// Given a block of memory that all or partially contains an image,
        /// reduce the memory to not contain the image.
        /// </summary>
        /// <param name="block">Block of memory</param>
        /// <param name="imageBlock">Image location and length in memory.</param>
        /// <returns>A list of new blocks that exclude the image.</returns>
        private static List<Block> RemoveImageBlock(Block block, ImageBlock imageBlock)
        {
            long blockStart = block.OffsetFile;
            long blockEnd = blockStart + block.Length - 1; // inclusive
            long imgStart = imageBlock.Offset;
            long imgEnd = imgStart + imageBlock.Length - 1; // inclusive
            if ((blockStart >= imgStart) && (blockEnd <= imgEnd)) {
                // Simple case: block entirely within the image.
                return new List<Block>();
            }
            List<Block> reduced = new List<Block>();
            Block blockBefore = null;
            Block blockAfter = null;
            if ((imgStart >= blockStart) && (imgEnd <= blockEnd)) {
                // Image entirely within the block. Can have memory before and
                // after the image. May need to split memory block into two
                // blocks.
                int len = 0;
                if (blockStart < imgStart) {
                    // Have memory before.
                    len = (int)(imgStart - blockStart);
                    byte[] data = null;
                    if (block.Bytes != null) {
                        data = new byte[len];
                        Array.Copy(block.Bytes, 0, data, 0, len);
                    }
                    blockBefore = new Block { OffsetFile = blockStart, Length = len, Bytes = data };
                }
                // Have memory after.
                if (imgEnd < blockEnd) {
                    long fileOffset = imgEnd + 1;
                    int arrayOffset = len + imageBlock.Length;
                    len = (int)(blockEnd - imgEnd);
                    byte[] data = null;
                    if (block.Bytes != null) {
                        data = new byte[len];
                        Array.Copy(block.Bytes, arrayOffset, data, 0, len);
                    }
                    blockAfter = new Block { OffsetFile = fileOffset, Length = len, Bytes = data };
                }
            } else if (blockStart < imgStart) {
                // Block overlaps the image at the higher end of the memory.
                int len = (int)(imgStart - blockStart);
                byte[] data = null;
                if (block.Bytes != null) {
                    data = new byte[len];
                    Array.Copy(block.Bytes, 0, data, 0, len);
                }
                blockBefore = new Block { OffsetFile = blockStart, Length = len, Bytes = data };
            } else if (imgEnd < blockEnd) {
                // Block overlaps the image at the lower end of the memory.
                long fileOffset = imgEnd + 1;
                int len = (int)(blockEnd - imgEnd);
                byte[] data = null;
                if (block.Bytes != null) {
                    data = new byte[len];
                    int arrayOffset = block.Length - len;
                    Array.Copy(block.Bytes, arrayOffset, data, 0, len);
                }
                blockAfter = new Block { OffsetFile = fileOffset, Length = len, Bytes = data };
            }
            if (blockBefore != null) reduced.Add(blockBefore);
            if (blockAfter != null) reduced.Add(blockAfter);
            return reduced;
        }

    }

    public class ImageFilesThread
    {
        private int id;
        private BufferedStream stream;
        private ImageFiles imgFiles;
        private long start;
        private long end;
        private BlockingCollection<int> queue;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="stream"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="queue"></param>
        /// <param name="imgFiles"></param>
        public ImageFilesThread(int id, BufferedStream stream, long start, long end, BlockingCollection<int> queue, ImageFiles imgFiles)
        {
            this.id = id;
            this.stream = stream;
            this.imgFiles = imgFiles;
            this.start = start;
            this.end = end;  // inclusive
            this.queue = queue;
        }

        /// <summary>
        /// Processes the memory file, so as to locate images like JPG, PNG, GIF and BMP in it.
        /// </summary>
        public void Process(Object si)
        {
            try {
                long eof = stream.Length;
                stream.Seek(start, SeekOrigin.Begin);
                long offset = stream.Position - 1;
                int pngMarker = 0;
                int jpgMarker = 0;
                int gifMarker = 0;
                int bmpMarker = 0;
                while (true) {
                    offset++;
                    if (offset > end) {
                        // If we're multi-threaded then we mave the start of a block
                        // in this thread's allocated region and the end in the next
                        // thread's allocated region. It's OK to keep going if the
                        // block starts in this thread's region.
                        if ((pngMarker == 0) && (jpgMarker == 0) && (gifMarker == 0) && (bmpMarker == 0)) {
                            return;
                        }
                    }
                    int idata = stream.ReadByte();
                    if (idata < 0) {
                        break;
                    }
                    switch (idata) {
                        case 0x0A: // LF
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            if (pngMarker == 5) {
                                pngMarker = 6;
                            } else {
                                if (pngMarker == 7) {
                                    FindPNG();
                                    offset = stream.Position - 1;
                                }
                                pngMarker = 0;
                            }
                            break;
                        case 0x0D: // CR
                            pngMarker = (pngMarker == 4) ? 5 : 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            break;
                        case 0x1A:
                            pngMarker = (pngMarker == 6) ? 7 : 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            break;
                        case 0x38: // '8'
                            pngMarker = 0;
                            jpgMarker = 0;
                            gifMarker = (gifMarker == 3) ? 4 : 0;
                            bmpMarker = 0;
                            break;
                        case 0x39: // '9'
                            pngMarker = 0;
                            jpgMarker = 0;
                            gifMarker = (gifMarker == 4) ? 5 : 0;
                            bmpMarker = 0;
                            break;
                        case 0x42: // 'B'
                            pngMarker = 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = (offset <= end) ? 1 : 0;
                            break;
                        case 0x46: // 'F'
                            pngMarker = 0;
                            jpgMarker = 0;
                            gifMarker = (gifMarker == 2) ? 3 : 0;
                            bmpMarker = 0;
                            break;
                        case 0x47: // 'G'
                            pngMarker = (pngMarker == 3) ? 4 : 0;
                            jpgMarker = 0;
                            gifMarker = (offset <= end) ? 1 : 0;
                            bmpMarker = 0;
                            break;
                        case 0x49: // 'I'
                            pngMarker = 0;
                            jpgMarker = 0;
                            gifMarker = (gifMarker == 1) ? 2 : 0;
                            bmpMarker = 0;
                            break;
                        case 0x4D: // 'M'
                            pngMarker = 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            if (bmpMarker == 1) {
                                FindBMP();
                                offset = stream.Position - 1;
                            }
                            bmpMarker = 0;
                            break;
                        case 0x4E: // 'N'
                            pngMarker = (pngMarker == 2) ? 3 : 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            break;
                        case 0x50: // 'P'
                            pngMarker = (pngMarker == 1) ? 2 : 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            break;
                        case 0x61: // 'a'
                            pngMarker = 0;
                            jpgMarker = 0;
                            bmpMarker = 0;
                            if (gifMarker == 5) {
                                FindGIF();
                                offset = stream.Position - 1;
                            }
                            gifMarker = 0;
                            break;
                        case 0x89:
                            pngMarker = (offset <= end) ? 1 : 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            break;
                        case 0xD8:
                            pngMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            if (jpgMarker == 1) {
                                FindJPG();
                                offset = stream.Position - 1;
                            }
                            jpgMarker = 0;
                            break;
                        case 0xFF:
                            pngMarker = 0;
                            jpgMarker = (offset <= end) ? 1 : 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            break;
                        default:
                            pngMarker = 0;
                            jpgMarker = 0;
                            gifMarker = 0;
                            bmpMarker = 0;
                            continue;
                    }
                }
            } catch {
            } finally {
                queue.Add(id);
            }
        }

        private void FindPNG()
        {
            long offset = stream.Position;
            bool failed = true;
            Stream memFile = null;
            int nRead = 8;
            byte[] data = new byte[ImageFiles.MAX_FILE_SIZE];
            try {
                // Find the end marker.
                bool found = false;
                int endMarker = 0;
                while (nRead < ImageFiles.MAX_FILE_SIZE - 4) {
                    int idata = stream.ReadByte();
                    if (idata < 0) return;
                    data[nRead] = (byte) idata;
                    nRead++;
                    switch (idata) {
                        case 0x49: // 'I'
                            endMarker = 1;
                            break;
                        case 0x45: // 'E'
                            endMarker = (endMarker == 1) ? 2 : 0;
                            break;
                        case 0x4E: // 'N'
                            endMarker = (endMarker == 2) ? 3 : 0;
                            break;
                        case 0x44: // 'D'
                            if (endMarker == 3) {
                                found = true;
                            }
                            endMarker = 0;
                            break;
                        default:
                            endMarker = 0;
                            break;
                    }
                    if (found) {
                        // Read CRC after marker.
                        if (stream.Read(data, nRead, 4) == 4) {
                            nRead += 4;
                            break;
                        }
                        return;
                    }
                }
                if (!found) return;
                data[0] = 0x89;
                data[1] = 0x50;
                data[2] = 0x4E;
                data[3] = 0x47;
                data[4] = 0x0D;
                data[5] = 0x0A;
                data[6] = 0x1A;
                data[7] = 0x0A;
                if (nRead != ImageFiles.MAX_FILE_SIZE) {
                    Array.Resize(ref data, nRead);
                }
                memFile = new MemoryStream(data);
                Bitmap bmp = new Bitmap(memFile);
                if (bmp.RawFormat.Equals(ImageFormat.Png)) {
                    DecoderTest(memFile, ImageBlock.ImageType.PNG);
                    //ThumbnailTest(bmp);
                    imgFiles.AddBlock(new ImageBlock(offset - 8, nRead, ImageBlock.ImageType.PNG), id);
                    failed = false;
                }
                bmp.Dispose();
            } catch {
            } finally {
                if (failed) {
                    stream.Position = offset;
                }
                if (memFile != null) {
                    try {
                        memFile.Close();
                    } catch {
                    }
                }
            }
        }

        private void FindJPG()
        {
            long offset = stream.Position;
            bool failed = true;
            Stream memFile = null;
            int nRead = 2;
            byte[] data = new byte[ImageFiles.MAX_FILE_SIZE];
            try {
                // Find the end marker.
                bool found = false;
                int endMarker = 0;
                while (nRead < ImageFiles.MAX_FILE_SIZE) {
                    int idata = stream.ReadByte();
                    if (idata < 0) return;
                    data[nRead] = (byte) idata;
                    nRead++;
                    // Byte stuffing should prevent finding a false marker.
                    switch (idata) {
                        case 0xFF:
                            endMarker = 1;
                            break;
                        case 0xD9:
                            if (endMarker == 1) {
                                found = true;
                            }
                            endMarker = 0;
                            break;
                        default:
                            endMarker = 0;
                            break;
                    }
                    if (found) {
                        break;
                    }
                }
                if (!found) return;
                data[0] = 0xFF;
                data[1] = 0xD8;
                if (nRead != ImageFiles.MAX_FILE_SIZE) {
                    Array.Resize(ref data, nRead);
                }
                memFile = new MemoryStream(data);
                Bitmap bmp = new Bitmap(memFile);
                if (bmp.RawFormat.Equals(ImageFormat.Jpeg)) {
                    DecoderTest(memFile, ImageBlock.ImageType.JPG);
                    //ThumbnailTest(bmp);
                    imgFiles.AddBlock(new ImageBlock(offset - 2, nRead, ImageBlock.ImageType.JPG), id);
                    failed = false;
                }
                bmp.Dispose();
            } catch {
            } finally {
                if (failed) {
                    stream.Position = offset;
                }
                if (memFile != null) {
                    try {
                        memFile.Close();
                    } catch {
                    }
                }
            }
        }

        private void FindGIF()
        {
            long offset = stream.Position;
            bool failed = true;
            Stream memFile = null;
            int nRead = 6;
            byte[] data = new byte[ImageFiles.MAX_FILE_SIZE];
            try {
                // Find the end marker.
                bool found = false;
                int endMarker = 0;
                while (nRead < ImageFiles.MAX_FILE_SIZE) {
                    int idata = stream.ReadByte();
                    if (idata < 0) return;
                    data[nRead] = (byte) idata;
                    nRead++;
                    switch (idata) {
                        case 0:
                            endMarker = 1;
                            break;
                        case 0x3B: // ';'
                            if (endMarker == 1) {
                                found = true;
                            }
                            endMarker = 0;
                            break;
                        default:
                            endMarker = 0;
                            break;
                    }
                    if (found) {
                        break;
                    }
                }
                if (!found) return;
                data[0] = 0x47;
                data[1] = 0x49;
                data[2] = 0x46;
                data[3] = 0x38;
                data[4] = 0x39;
                data[5] = 0x61;
                if (nRead != ImageFiles.MAX_FILE_SIZE) {
                    Array.Resize(ref data, nRead);
                }
                memFile = new MemoryStream(data);
                Bitmap bmp = new Bitmap(memFile);
                if (bmp.RawFormat.Equals(ImageFormat.Gif)) {
                    DecoderTest(memFile, ImageBlock.ImageType.GIF);
                    //ThumbnailTest(bmp);
                    imgFiles.AddBlock(new ImageBlock(offset - 6, nRead, ImageBlock.ImageType.GIF), id);
                    failed = false;
                }
                bmp.Dispose();
            } catch {
            } finally {
                if (failed) {
                    stream.Position = offset;
                }
                if (memFile != null) {
                    try {
                        memFile.Close();
                    } catch {
                    }
                }
            }
        }

        private void FindBMP()
        {
            long offset = stream.Position;
            bool failed = true;
            Stream memFile = null;
            try {
                byte[] header = new byte[14];
                if (stream.Read(header, 2, 12) != 12) {
                    return;
                }
                header[0] = (byte) 'B';
                header[1] = (byte) 'M';
                // Stored as little endian.
                int len = BitConverter.ToInt32(header, 2);
                if (len == 0) {
                    // Length can be zero if not compressed, but we don't
                    // support that format.
                    return;
                } else {
                    if ((len <= 14) || (len > (ImageFiles.MAX_FILE_SIZE - 14))) {
                        return;
                    }
                    byte[] data = new byte[len];
                    Buffer.BlockCopy(header, 0, data, 0, 14);
                    if (stream.Read(data, 14, len - 14) != len - 14) {
                        return;
                    }
                    memFile = new MemoryStream(data);
                }
                Bitmap bmp = new Bitmap(memFile);
                DecoderTest(memFile, ImageBlock.ImageType.BMP);
                //ThumbnailTest(bmp);
                imgFiles.AddBlock(new ImageBlock(offset - 2, (int) memFile.Length, ImageBlock.ImageType.BMP), id);
                failed = false;
                bmp.Dispose();
            } catch {
            } finally {
                if (failed) {
                    stream.Position = offset;
                }
                if (memFile != null) {
                    try {
                        memFile.Close();
                    } catch {
                    }
                }
            }
        }

        private void DecoderTest(Stream strm, ImageBlock.ImageType imgType)
        {
            strm.Position = 0;
            try {
                BitmapDecoder bd = null;
                switch (imgType) {
                    case ImageBlock.ImageType.JPG:
                        bd = new JpegBitmapDecoder(strm, BitmapCreateOptions.PreservePixelFormat,
                                                   BitmapCacheOption.Default);
                        break;
                    case ImageBlock.ImageType.PNG:
                        bd = new PngBitmapDecoder(strm, BitmapCreateOptions.PreservePixelFormat,
                                                  BitmapCacheOption.Default);
                        break;
                    case ImageBlock.ImageType.GIF:
                        bd = new GifBitmapDecoder(strm, BitmapCreateOptions.PreservePixelFormat,
                                                  BitmapCacheOption.Default);
                        break;
                    case ImageBlock.ImageType.BMP:
                        bd = new BmpBitmapDecoder(strm, BitmapCreateOptions.PreservePixelFormat,
                                                  BitmapCacheOption.Default);
                        break;
                    default:
                        throw new Exception("Invalid image type");
                }
                if (bd.Frames.Count == 0) {
                    throw new Exception("No frames");
                }
                BitmapSource bms = bd.Frames[0];
                if (bms == null) {
                    throw new Exception("Invalid frame");
                }
            } catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Deprecated. Use DecoderTest instead.
        /// </summary>
        /// <param name="bmp"></param>
        private void ThumbnailTest(Bitmap bmp)
        {
            try {
                const int TestHeight = 120;
                const int TestWidth = 120;
                int height;
                int width;
                // Adjust the height to fit within the box and then
                // scale the width.
                if (TestHeight > bmp.Height) {
                    height = bmp.Height;
                    width = bmp.Width;
                } else {
                    height = TestHeight;
                    double scale = ((double) height)/((double) bmp.Height);
                    width = (int) (((double) bmp.Width)*scale);
                }
                // If the width is too large, then we need to put the
                // width within the box and scale the height.
                if (width > TestWidth) {
                    width = TestWidth;
                    double scale = ((double) width)/((double) bmp.Width);
                    height = (int) (((double) bmp.Height)*scale);
                }
                bmp.GetThumbnailImage(width, height, ThumbnailCallback, IntPtr.Zero);
            } catch (Exception ex) {
                throw ex;
            }
        }

        private bool ThumbnailCallback()
        {
            return false;
        }
    }

}

