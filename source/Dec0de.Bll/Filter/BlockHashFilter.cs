//#define CHECK_HASH

//#define _USESQLSERVER_

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
#if _USESQLSERVER_
using PhoneDecoder.HashLoader.EmbeddedDal;
#else
using System.Data.SQLite;
using System.Threading;

#endif

namespace Dec0de.Bll.Filter
{
    public class BlockHashFilter
    {
        #region Declarations

        private readonly string _inputFile;
        private readonly int _blockSize;
        private readonly int _slideAmount;
        private static readonly SHA1Managed _hash = new SHA1Managed();
        //private readonly List<string> _inputFileHashes = new List<string>();
        public const int MAX_BLOCK_SIZE = 65536;
        // The memory id should be the sha1 of the entire file.
        private readonly string _memoryId;
        private readonly bool _noFilter;
        private FilterResult _filterResult;
        private readonly int _slideAmountLibrary;
        private List<UnfilterdBlockResult> _storedBlocks = null;


#if !_USESQLSERVER_
        public class UnfilterdBlockResult
        {
            public string hash;
            public System.Nullable<long> blockIndexFirst;
        }
#endif

        #endregion

        #region Instantiation

        public BlockHashFilter(string inputFile, int blockSize, int slideAmount, string memoryId, bool doNotFilter,
            List<UnfilterdBlockResult> storedBlocks=null)
        {
            _inputFile = inputFile;

            _blockSize = blockSize;
            _slideAmount = slideAmount;
            _slideAmountLibrary = _slideAmount;
            _memoryId = memoryId;
            _noFilter = doNotFilter;
            _storedBlocks = storedBlocks;
        }

        public BlockHashFilter(string inputFile, int blockSize, int slideAmount, int slideAmountLibrary, string memoryId, bool doNotFilter)
        {
            _inputFile = inputFile;

            _blockSize = blockSize;
            _slideAmount = slideAmount;
            _slideAmountLibrary = slideAmountLibrary;
            _memoryId = memoryId;
            _noFilter = doNotFilter;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Calculates the SHA1 block hashes of the given memory file.
        /// </summary>
        /// <param name="inputFile">The path to the phone's memory file.</param>
        /// <param name="blocksize">Size of a single block for hash comparison.</param>
        /// <param name="slideAmount">The difference between the starting bytes of two overlapping consecutive blocks.</param>
        /// <returns>List of all SHA1 block hashes from phone's memory.</returns>

        public static List<string> ComputeHashes(string inputFile, int blocksize, int slideAmount)
        {
            List<string> hashes = new List<string>();

            using (FileStream stream = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var block = new byte[blocksize];

                while (stream.Position < stream.Length)
                {
                    stream.Read(block, 0, blocksize);

                    byte[] hash = GetBlockHash(block);

                    string hashString = Convert.ToBase64String(hash);

                    hashes.Add(hashString);

                    //Need this check. Otherwise it will not finish
                    if (stream.Position < stream.Length)
                        stream.Position = stream.Position - blocksize + slideAmount;


                }
            }

            return hashes;
        }

        /// <summary>
        /// Calculates the hash for the given block of bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes in a block to be hashed.</param>
        /// <returns>Returns the bytes of the hash value</returns>
        public static byte[] GetBlockHash(byte[] bytes)
        {
            return _hash.ComputeHash(bytes);
        }

        /// <summary>
        /// Gets certain bytes from the phone's memory file.
        /// </summary>
        /// <param name="filePath">The path to the phone's memory file.</param>
        /// <param name="byteIndex">The starting position in the memory file, from where onwards the bytes are required.</param>
        /// <param name="length">The number of bytes required, from byteIndex onwards.</param>
        /// <returns>Requested bytes from the memory file.</returns>

        public static byte[] GetBytes(string filePath, long byteIndex, int length)
        {
            //Check length of file. Might grab last X bytes of file where X < length.

            var fileLength = new FileInfo(filePath).Length;
            var numBytesAfterOffset = fileLength - byteIndex;


            if (numBytesAfterOffset < length)
                length = Convert.ToInt32(numBytesAfterOffset);

            byte[] buffer = new byte[length];

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                stream.Position = byteIndex;

                stream.Read(buffer, 0, length);
            }

            return buffer;
        }

        /// <summary>
        /// Gets certain bytes from the phone's memory file.
        /// </summary>
        /// <param name="stream">The the already opened phone's memory file.</param>
        /// <param name="byteIndex">The starting position in the memory file, from where onwards the bytes are required.</param>
        /// <param name="length">The number of bytes required, from byteIndex onwards.</param>
        /// <returns>Requested bytes from the memory file.</returns>

        public static byte[] GetBytes(FileStream stream, long byteIndex, int length)
        {
            //Check length of file. Might grab last X bytes of file where X < length.

            var fileLength = stream.Length;
            var numBytesAfterOffset = fileLength - byteIndex;

            if (numBytesAfterOffset < length)
                length = Convert.ToInt32(numBytesAfterOffset);

            byte[] buffer = new byte[length];

            stream.Position = byteIndex;

            stream.Read(buffer, 0, length);

            return buffer;
        }

        #endregion

        #region Public Methods

        private class BlockInfo
        {
            /// <summary>
            /// Beginning of the block in memory.
            /// </summary>
            public long Offset;
            /// <summary>
            /// Length of the block in memory.
            /// </summary>
            public int Length;
        }

        public FilterResult Filter(int phoneId)
        {
            return Filter(phoneId, null);
        }

        public FilterResult Filter(int phoneId, object sqlConnect, bool saveResult)
        {
            //Check if there is a .fil file in the same directory
            if (saveResult && LoadFilterResults())
                return _filterResult;

            Filter(phoneId, sqlConnect);

            if (saveResult)
                SaveFilterResults();

            return _filterResult;
        }

        /// <summary>
        /// Performs Block Hash Filtering for the phone by returning those blocks of the phone's memory that did not match the existing block hash library.
        /// </summary>
        /// <param name="phoneId">The unique identification for this phone in the database.</param>
        /// <param name="sqlConnect">Handler for the database that contains the block hashes for this phone.</param>
        /// <returns>The set of blocks remaining in the phone's memory after block hash filtering is complete.</returns>
        public FilterResult Filter(int phoneId, object sqlConnect)
        {
            var start = DateTime.Now;

            var blockInfos = new List<BlockInfo>();

            var allInterestingBlocks = new List<Block>();

#if _USESQLSERVER_
            List<usp_Hash_GetUnfilteredBlocksResult> unfilteredBlocks;
            using (PhoneDbDataContext dataContext = Dalbase.GetDataContext())
            {
                // BL:TEMP added try catch for debugging
                try {
                    ISingleResult<usp_Hash_GetUnfilteredBlocksResult> results = dataContext.usp_Hash_GetUnfilteredBlocks(_blockSize, _slideAmount, phoneId, _memoryId, _noFilter, _slideAmountLibrary);
                    unfilteredBlocks = (from result in results select result).ToList();
                } catch (Exception ex) {
                    throw ex;
                }
            }
#else
            DateTime started = DateTime.Now;
            // TODO: Clean up and remove unused methods.
            // Note: Only 7a supports blocks passed directly to us, blocks that are not in the DB.
            //List<UnfilterdBlockResult> unfilteredBlocks = HashGetUnfilteredBlocks5(phoneId, (SQLiteConnection)sqlConnect);
            //List<UnfilterdBlockResult> unfilteredBlocks = HashGetUnfilteredBlocks6(phoneId, (SQLiteConnection)sqlConnect);
            //List<UnfilterdBlockResult> unfilteredBlocks = HashGetUnfilteredBlocks7(phoneId, (SQLiteConnection)sqlConnect);
            List<UnfilterdBlockResult> unfilteredBlocks = HashGetUnfilteredBlocks7a(phoneId, (SQLiteConnection)sqlConnect);
            //List<UnfilterdBlockResult> unfilteredBlocks = HashGetUnfilteredBlocks8(phoneId, (SQLiteConnection)sqlConnect);
            DateTime finished = DateTime.Now;
            System.Console.WriteLine("Block Hash Filter took " + (finished - started).TotalSeconds + " seconds");
#endif

            long numBytes = new FileInfo(_inputFile).Length;

            //This is to cover a weird issue where the memory is not divisible by the block size
            long numBlocks = 0;

            if(numBytes < _blockSize)
                numBlocks = 1;
            else
                numBlocks = Convert.ToInt64(Math.Round((numBytes - _blockSize) / (double)_slideAmount + 1, MidpointRounding.AwayFromZero));

            int unfilteredIndex = 0;
            long i = 0;
            long lastBlockIndex = long.MinValue;

            while (i < numBlocks && unfilteredIndex < unfilteredBlocks.Count)
            {
                //Check if this block was not filtered. 
                if (unfilteredBlocks[unfilteredIndex].blockIndexFirst == i)
                {
                    
#if CHECK_HASH

                    // We need to do a sanity check to make sure the logic works
                     var blockBytes = GetBytes(_inputFile, unfilteredBlocks[unfilteredIndex].blockIndexFirst.Value * _slideAmount, _blockSize);

                    string blockHash = Convert.ToBase64String(_hash.ComputeHash(blockBytes));

                    bool hashesMatch = (blockHash == unfilteredBlocks[unfilteredIndex].hash);

                    if (!hashesMatch)
                    {
                        //throw new ApplicationException("Wha? Why did the hashes not match?!");
                        Console.WriteLine("Wha? Why did the hashes not match?!");
                    }

#endif
                    BlockInfo blockInfo = new BlockInfo();
                    byte[] interestingBytes;             

                    //if not the last block, add the first X bytes where X=slide_amount
                    if (unfilteredBlocks[unfilteredIndex].blockIndexFirst < numBlocks - 1)
                    {
                        blockInfo.Offset = unfilteredBlocks[unfilteredIndex].blockIndexFirst.Value*_slideAmount;
                        blockInfo.Length = _slideAmount;
                    }
                    //else add the whole block
                    else
                    {
                        blockInfo.Offset = unfilteredBlocks[unfilteredIndex].blockIndexFirst.Value * _slideAmount;
                        blockInfo.Length = _blockSize;
                    }

                    //Concatenate this block with the previous if they are contiguous and less than max block size
                    if (unfilteredBlocks[unfilteredIndex].blockIndexFirst - 1 == lastBlockIndex && blockInfos[blockInfos.Count-1].Length < MAX_BLOCK_SIZE)
                    {
                        blockInfos[blockInfos.Count - 1].Length += blockInfo.Length;
                    }
                    else
                    {
                        blockInfos.Add(blockInfo);
                    }

                    lastBlockIndex = unfilteredBlocks[unfilteredIndex].blockIndexFirst.Value;
                    unfilteredIndex++;
                    i++;
                }
                //Check if this block is filtered
                else if (i < unfilteredBlocks[unfilteredIndex].blockIndexFirst)
                {
                    //skip to end of matched block
                    i = i + (_blockSize / _slideAmount);

                    //We do not want to skip past the last block
                    if (i > numBlocks - 1)
                        i = numBlocks - 1;

                    //Skip all unfiltered blocks after the matched block that overlap with the matched block
                    while (unfilteredIndex < unfilteredBlocks.Count && unfilteredBlocks[unfilteredIndex].blockIndexFirst < i)
                    {
                        unfilteredIndex++;
                    }
                }
                else if (i > unfilteredBlocks[unfilteredIndex].blockIndexFirst)
                    throw new ApplicationException("This should never happen...");

            }

            long count = 0;

            //Get the bytes for the blocks
            for (int j = 0; j < blockInfos.Count; j++)
            {
                //This is going to be an issue for the blocks that contain fewer bytes than the block size, e.g. the last block.
                //The length won't be calculated right in th
                count += blockInfos[j].Length;

                //var bytes = GetBytes(_inputFile, blockInfos[j].Offset, blockInfos[j].Length);

                allInterestingBlocks.Add(new Block {OffsetFile = blockInfos[j].Offset, Length = blockInfos[j].Length});
            }


            //Count the number of bytes remaining
            /* BL
            for (int j = 0; j < allInterestingBlocks.Count; j++)
            {
               

                //count += allInterestingBlocks[j].Bytes.Length;
            }
            */

            _filterResult = new FilterResult
                                   {

                                       FilteredBytesCount = numBytes - count,
                                       UnfilteredBytesCount = count,
                                       UnfilteredBlocks = allInterestingBlocks,
                                       Duration = DateTime.Now - start,
                                       MemoryId = _memoryId
                                   };
            
            Console.WriteLine("Filtered {0} out of {1} bytes: {2}.", numBytes - count, numBytes, (numBytes - count) / (double)numBytes);

            return _filterResult;
        }



        private List<UnfilterdBlockResult> HashGetUnfilteredBlocks5(int phoneId, SQLiteConnection sql)
        {
            try {
                (new SQLiteCommand("PRAGMA temp_store=2", sql)).ExecuteNonQuery();
            } catch {
            }
            try {
                // Default is 2000 1K pages
                (new SQLiteCommand("PRAGMA cache_size=8000", sql)).ExecuteNonQuery();
            } catch {
            }
            int hashRunId = -1;
            try {
                const string stmt =
                    "SELECT hashRunId " +
                    "FROM tbl_HashRun " +
                    "WHERE slideAmount=@SA AND " +
                    "blockSizeBytes=@BS AND " +
                    "phoneId=@PID AND " +
                    "memoryId=@MID " +
                    "ORDER BY hashRunId DESC " +
                    "LIMIT 1";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@SA", _slideAmount);
                cmd.Parameters.AddWithValue("@BS", _blockSize);
                cmd.Parameters.AddWithValue("@PID", phoneId);
                cmd.Parameters.AddWithValue("@MID", _memoryId);
                SQLiteDataReader rdr = null;
                try {
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read()) {
                        hashRunId = Convert.ToInt32(rdr["hashRunId"]);
                    }
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
            } catch {
            }
            if (hashRunId < 0) {
                return new List<UnfilterdBlockResult>();
            }
            try {
                SQLiteCommand cmd = null;
                const string stmt2 =
                    "SELECT hash, MIN(blockIndex) AS blockIndexFirst " +
                    "FROM tbl_Hash " +
                    "WHERE hashRunId=@HRID " +
                    "GROUP BY hash " +
                    "ORDER BY blockIndexFirst ASC";
                cmd = new SQLiteCommand(stmt2, sql);
                cmd.Parameters.AddWithValue("@HRID", hashRunId);
                SQLiteDataReader rdr = null;
                List<UnfilterdBlockResult> tempResults = new List<UnfilterdBlockResult>();
                try {
                    rdr = cmd.ExecuteReader();
                    while (rdr.Read()) {
                        UnfilterdBlockResult result = new UnfilterdBlockResult()
                                                          {
                                                              hash = Convert.ToString(rdr["hash"]),
                                                              blockIndexFirst =
                                                                  Convert.ToInt32(rdr["blockIndexFirst"])
                                                          };
                        tempResults.Add(result);
                    }
                } catch (Exception e) {
                    System.Console.WriteLine("SQL exception: " + e.Message);
                    return new List<UnfilterdBlockResult>();
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
                List<UnfilterdBlockResult> results = null;
                if (_noFilter) {
                    results = tempResults;
                } else {
                    results = new List<UnfilterdBlockResult>();
                    foreach (UnfilterdBlockResult result in tempResults) {
                        rdr = null;
                        try {
                            const string stmt =
                                "SELECT hash FROM tbl_Hash WHERE hashRunId<>@HRID AND hash=@HASH LIMIT 1";
                            cmd = new SQLiteCommand(stmt, sql);
                            cmd.Parameters.AddWithValue("@HRID", hashRunId);
                            cmd.Parameters.AddWithValue("@HASH", result.hash);
                            rdr = cmd.ExecuteReader();
                            if (!rdr.HasRows) {
                                results.Add(result);
                            }
                        } catch (Exception e) {
                            System.Console.WriteLine("SQL exception: " + e.Message);
                        } finally {
                            if (rdr != null) {
                                rdr.Close();
                            }
                        }
                    }
                    tempResults.Clear();
                }
                return results;
            } catch (Exception ex) {
                System.Console.WriteLine("SQL exception: " + ex.Message);
                return new List<UnfilterdBlockResult>();
            }
        }

        private List<UnfilterdBlockResult> HashGetUnfilteredBlocks6(int phoneId, SQLiteConnection sql)
        {
            try {
                // Default is 2000 1K pages
                (new SQLiteCommand("PRAGMA cache_size=8000", sql)).ExecuteNonQuery();
            } catch {
            }
            int hashRunId = -1;
            try {
                const string stmt =
                    "SELECT hashRunId " +
                    "FROM tbl_HashRun " +
                    "WHERE slideAmount=@SA AND " +
                    "blockSizeBytes=@BS AND " +
                    "phoneId=@PID AND " +
                    "memoryId=@MID " +
                    "ORDER BY hashRunId DESC " +
                    "LIMIT 1";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@SA", _slideAmount);
                cmd.Parameters.AddWithValue("@BS", _blockSize);
                cmd.Parameters.AddWithValue("@PID", phoneId);
                cmd.Parameters.AddWithValue("@MID", _memoryId);
                SQLiteDataReader rdr = null;
                try {
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read()) {
                        hashRunId = Convert.ToInt32(rdr["hashRunId"]);
                    }
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
            } catch {
            }
            if (hashRunId < 0) {
                return new List<UnfilterdBlockResult>();
            }
            try {
                SQLiteCommand cmd = null;
                if (_noFilter) {
                    const string stmt2 =
                        "SELECT hash, MIN(blockIndex) AS blockIndexFirst " +
                        "FROM tbl_Hash " +
                        "WHERE hashRunId=@HRID " +
                        "GROUP BY hash " +
                        "ORDER BY blockIndexFirst ASC";
                    cmd = new SQLiteCommand(stmt2, sql);
                    cmd.Parameters.AddWithValue("@HRID", hashRunId);
                } else {
                    const string stmt2 =
                        "SELECT hash, MIN(blockIndex) AS blockIndexFirst " +
                        "FROM tbl_Hash " +
                        "WHERE hashRunId=@HRID1 " +
                        "AND hash NOT IN (SELECT hash FROM tbl_Hash WHERE  hashRunId<>@HRID2) " +
                        "GROUP BY hash " +
                        "ORDER BY blockIndexFirst ASC";
                    cmd = new SQLiteCommand(stmt2, sql);
                    cmd.Parameters.AddWithValue("@HRID1", hashRunId);
                    cmd.Parameters.AddWithValue("@HRID2", hashRunId);
                }
                SQLiteDataReader rdr = null;
                List<UnfilterdBlockResult> results = new List<UnfilterdBlockResult>();
                try {
                    rdr = cmd.ExecuteReader();
                    while (rdr.Read()) {
                        UnfilterdBlockResult result = new UnfilterdBlockResult()
                        {
                            hash = Convert.ToString(rdr["hash"]),
                            blockIndexFirst =
                                Convert.ToInt32(rdr["blockIndexFirst"])
                        };
                        results.Add(result);
                    }
                    return results;
                } catch (Exception e) {
                    System.Console.WriteLine("SQL exception: " + e.Message);
                    return new List<UnfilterdBlockResult>();
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
            } catch (Exception ex) {
                System.Console.WriteLine("SQL exception: " + ex.Message);
                return new List<UnfilterdBlockResult>();
            }
        }

        private List<UnfilterdBlockResult> HashGetUnfilteredBlocks7(int phoneId, SQLiteConnection sql)
        {
            try {
                (new SQLiteCommand("PRAGMA temp_store=2", sql)).ExecuteNonQuery();
            } catch {
            }
            try {
                // Default is 2000 1K pages
                (new SQLiteCommand("PRAGMA cache_size=8000", sql)).ExecuteNonQuery();
            } catch {
            }
            int hashRunId = -1;
            try {
                const string stmt =
                    "SELECT hashRunId " +
                    "FROM tbl_HashRun " +
                    "WHERE slideAmount=@SA AND " +
                    "blockSizeBytes=@BS AND " +
                    "phoneId=@PID AND " +
                    "memoryId=@MID " +
                    "ORDER BY hashRunId DESC " +
                    "LIMIT 1";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@SA", _slideAmount);
                cmd.Parameters.AddWithValue("@BS", _blockSize);
                cmd.Parameters.AddWithValue("@PID", phoneId);
                cmd.Parameters.AddWithValue("@MID", _memoryId);
                SQLiteDataReader rdr = null;
                try {
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read()) {
                        hashRunId = Convert.ToInt32(rdr["hashRunId"]);
                    }
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
            } catch {
            }
            if (hashRunId < 0) {
                return new List<UnfilterdBlockResult>();
            }
            try {
                DateTime started = DateTime.Now;
                SQLiteCommand cmd = null;
                const string stmt2 =
                    "SELECT hash, blockIndex " +
                    "FROM tbl_Hash " +
                    "WHERE hashRunId=@HRID " +
                    "ORDER BY blockIndex ASC";
                cmd = new SQLiteCommand(stmt2, sql);
                cmd.Parameters.AddWithValue("@HRID", hashRunId);
                SQLiteDataReader rdr = null;
                List<UnfilterdBlockResult> tempResults = new List<UnfilterdBlockResult>();
                HashSet<string> skip = new HashSet<string>();
                int dropped = 0;
                try {
                    rdr = cmd.ExecuteReader();
                    while (rdr.Read()) {
                        UnfilterdBlockResult result = new UnfilterdBlockResult()
                        {
                            hash = Convert.ToString(rdr["hash"]),
                            blockIndexFirst =
                                Convert.ToInt32(rdr["blockIndex"])
                        };
                        if (!skip.Contains(result.hash)) {
                            tempResults.Add(result);
                            skip.Add(result.hash);
                        } else {
                            dropped++;
                        }
                    }
                } catch (Exception e) {
                    System.Console.WriteLine("SQL exception: " + e.Message);
                    return new List<UnfilterdBlockResult>();
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
                DateTime finished = DateTime.Now;
                System.Console.WriteLine("It took " + (finished - started).TotalSeconds + " seconds to read hashes");
                System.Console.WriteLine("Skipped " + dropped + " blocks");
                skip.Clear();
                List<UnfilterdBlockResult> results = null;
                dropped = 0;
                if (_noFilter) {
                    results = tempResults;
                } else {
                    results = new List<UnfilterdBlockResult>();
                    foreach (UnfilterdBlockResult result in tempResults) {
                        rdr = null;
                        try {
                            const string stmt =
                                "SELECT hash FROM tbl_Hash WHERE hashRunId<>@HRID AND hash=@HASH LIMIT 1";
                            cmd = new SQLiteCommand(stmt, sql);
                            cmd.Parameters.AddWithValue("@HRID", hashRunId);
                            cmd.Parameters.AddWithValue("@HASH", result.hash);
                            rdr = cmd.ExecuteReader();
                            if (!rdr.HasRows) {
                                results.Add(result);
                            } else {
                                dropped++;
                            }
                        } catch (Exception e) {
                            System.Console.WriteLine("SQL exception: " + e.Message);
                        } finally {
                            if (rdr != null) {
                                rdr.Close();
                            }
                        }
                    }
                    tempResults.Clear();
                }
                System.Console.WriteLine("Skipped " + dropped + " blocks");
                return results;
            } catch (Exception ex) {
                System.Console.WriteLine("SQL exception: " + ex.Message);
                return new List<UnfilterdBlockResult>();
            }
        }

        /// <summary>
        /// Returns the those block hashes from the phone's database which do not match the existing ones.
        /// This version is threaded, allows for memory-based sqlite tables, and can process phone blocks
        /// that aren't actually in the database.
        /// </summary>
        /// <param name="phoneId">The unique identification for this phone in the database.</param>
        /// <param name="sql">The handler for the database of hashes.</param>
        /// <returns>A list of block hashes that did not match other existing ones in the database.</returns>
        private List<UnfilterdBlockResult> HashGetUnfilteredBlocks7a(int phoneId, SQLiteConnection sql)
        {
            // On 32-bit systems it's easy to run out of memory. Limit threads.
            int maxThreads = (Environment.Is64BitProcess) ? 8 : 2;
            const int minListLen = 100000;
            try {
                (new SQLiteCommand("PRAGMA temp_store=2", sql)).ExecuteNonQuery();
            } catch {
            }
            // Select the Run Id which is used to identify which hashes are ours.
            int hashRunId = -1;
            if (phoneId > (Int32.MaxValue - 100)) {
                // Special large phone id indicates the hashes weren't stored in the
                // DB. The unfiltered hashes should have been passed to us.
                if (_storedBlocks != null) {
                    hashRunId = Int32.MaxValue - 1;  // dummy hashRunId
                }
            } else {
                try {
                    const string stmt =
                        "SELECT hashRunId " +
                        "FROM tbl_HashRun " +
                        "WHERE slideAmount=@SA AND " +
                        "blockSizeBytes=@BS AND " +
                        "phoneId=@PID AND " +
                        "memoryId=@MID " +
                        "ORDER BY hashRunId DESC " +
                        "LIMIT 1";
                    SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                    cmd.Parameters.AddWithValue("@SA", _slideAmount);
                    cmd.Parameters.AddWithValue("@BS", _blockSize);
                    cmd.Parameters.AddWithValue("@PID", phoneId);
                    cmd.Parameters.AddWithValue("@MID", _memoryId);
                    SQLiteDataReader rdr = null;
                    try {
                        rdr = cmd.ExecuteReader();
                        if (rdr.Read()) {
                            hashRunId = Convert.ToInt32(rdr["hashRunId"]);
                        }
                    } finally {
                        if (rdr != null) {
                            rdr.Close();
                        }
                    }
                } catch {
                }
            }
            if (hashRunId < 0) {
                return new List<UnfilterdBlockResult>();
            }
            try {
                // Use ArrayList rather than List so that the threaded version can
                // efficiently index into the list.
                ArrayList tempResults = new ArrayList();
                int dropped = 0;
                if (_storedBlocks != null) {
                    // We were passed the blocks stored in the DB. No need to
                    // read them out. Also, we can assume there are no duplicate
                    // blocks.
                    foreach (UnfilterdBlockResult r in _storedBlocks) {
                        tempResults.Add(r);
                    }
                    _storedBlocks.Clear();
                    _storedBlocks = null;
                } else {
                    // Tests have shown that it's quickest to get a list of hashes and their
                    // block index first. Trying to perform filtering in the query tends to
                    // be slower.
                    SQLiteDataReader rdr = null;
                    SQLiteCommand cmd = null;
                    DateTime started = DateTime.Now;                   
                    const string stmt2 =
                        "SELECT hash, blockIndex " +
                        "FROM tbl_Hash " +
                        "WHERE hashRunId=@HRID " +
                        "ORDER BY blockIndex ASC";
                    cmd = new SQLiteCommand(stmt2, sql);
                    cmd.Parameters.AddWithValue("@HRID", hashRunId);
                    try {
                        HashSet<string> skip = new HashSet<string>();
                        rdr = cmd.ExecuteReader();
                        while (rdr.Read()) {
                            UnfilterdBlockResult result = new UnfilterdBlockResult()
                                                              {
                                                                  hash = Convert.ToString(rdr["hash"]),
                                                                  blockIndexFirst =
                                                                      Convert.ToInt32(rdr["blockIndex"])
                                                              };
                            // Do not keep the block if it's got a hash that we've previously
                            // seen.
                            // TODO: If we don't save duplicate hashes then we don't need to
                            // perform this check. Dec0de behaves this way now, but we can't
                            // yet rely on the database not having duplicates.
                            if (!skip.Contains(result.hash)) {
                                tempResults.Add(result);
                                skip.Add(result.hash);
                            } else {
                                dropped++;
                            }
                        }
                    } catch (Exception e) {
                        System.Console.WriteLine("SQL exception: " + e.Message);
                        return new List<UnfilterdBlockResult>();
                    } finally {
                        if (rdr != null) {
                            rdr.Close();
                        }
                    }
                    DateTime finished = DateTime.Now;
                    System.Console.WriteLine("It took " + (finished - started).TotalSeconds + " seconds to read hashes");
                    System.Console.WriteLine("Skipped " + dropped + " blocks");
                }
                List<UnfilterdBlockResult> results = null;
                dropped = 0;
                if (_noFilter) {
                    results = new List<UnfilterdBlockResult>();
                    foreach (Object r in tempResults) {
                        results.Add((UnfilterdBlockResult) r);
                    }
                } else {
                    // Eliminate blocks whose hashes match those of other phones. Here we
                    // assume constant block hashes are in tbl_Hashes rather than in the
                    // deprecated tbl_Constants.
                    // Our tests have shown that testing each individual hash is quicker
                    // then doing it in a single query. The good news though is that we
                    // can perform explicit threading.
                    int nThreads = (tempResults.Count + minListLen - 1) / minListLen;
                    if (nThreads > maxThreads) nThreads = maxThreads;
                    if (nThreads <= 1) {
                        SQLiteDataReader rdr = null;
                        SQLiteCommand cmd = null;
                        // No need to create a thread.
                        results = new List<UnfilterdBlockResult>();
                        foreach (UnfilterdBlockResult result in tempResults) {
                            rdr = null;
                            try {
                                const string stmt =
                                    "SELECT hash FROM tbl_Hash WHERE hashRunId<>@HRID AND hash=@HASH LIMIT 1";
                                cmd = new SQLiteCommand(stmt, sql);
                                cmd.Parameters.AddWithValue("@HRID", hashRunId);
                                cmd.Parameters.AddWithValue("@HASH", result.hash);
                                rdr = cmd.ExecuteReader();
                                if (!rdr.HasRows) {
                                    results.Add(result);
                                } else {
                                    dropped++;
                                }
                            } catch (Exception e) {
                                System.Console.WriteLine("SQL exception: " + e.Message);
                            } finally {
                                if (rdr != null) {
                                    rdr.Close();
                                }
                            }
                        }
                    } else {
                        // Perform the query in threads.
                        results = ThreadedBlockHashFilter(nThreads, hashRunId, tempResults, sql);
                    }
                    tempResults.Clear();
                }
                System.Console.WriteLine("Skipped " + dropped + " blocks");
                return results;
            } catch (Exception ex) {
                System.Console.WriteLine("SQL exception: " + ex.Message);
                return new List<UnfilterdBlockResult>();
            }
        }

        /// <summary>
        /// Creates threads to filter separate portions of the candidate blocks.
        /// </summary>
        /// <param name="nThreads"></param>
        /// <param name="runId"></param>
        /// <param name="tempResults"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        private List<UnfilterdBlockResult> ThreadedBlockHashFilter(int nThreads, int runId, 
            ArrayList tempResults, SQLiteConnection sql)
        {
            // Start the threads. Each threads adds unfiltered blocks to a separate
            // list. This avoids locking and preserves the order.
            List<UnfilterdBlockResult>[] thrdResults = new List<UnfilterdBlockResult>[nThreads];
            int startx = 0;
            int offset = tempResults.Count / nThreads;
            BlockingCollection<int> queue = new BlockingCollection<int>();
            for (int tid = 0; tid < nThreads; tid++) {
                thrdResults[tid] = new List<UnfilterdBlockResult>();
                int count = (tid < (nThreads - 1)) ? offset : tempResults.Count - startx;
                BlockHashFilterThread bhft = new BlockHashFilterThread(tid, runId, startx, count, tempResults,
                                                                       thrdResults[tid], sql, queue);
                ThreadPool.QueueUserWorkItem(new WaitCallback(bhft.Process));
                startx += offset;
            }
            // Wait for all threads to complete. Each thread places something on our
            // queue when it has finished.
            for (int i = 0; i < nThreads; i++) {
                queue.Take();  // blocks
            }
            tempResults.Clear();
            // Combine all of the results, preserving the order of the blocks.
            List<UnfilterdBlockResult> results = new List<UnfilterdBlockResult>();
            try {
                for (int tid = 0; tid < nThreads; tid++) {
                    results.AddRange(thrdResults[tid]);
                }
                return results;
            } catch (Exception ex) {
                System.Console.WriteLine("Exception caught: " + ex.Message);
                return new List<UnfilterdBlockResult>();
            }
        }

        private List<UnfilterdBlockResult> HashGetUnfilteredBlocks8(int phoneId, SQLiteConnection sql)
        {
            try {
                (new SQLiteCommand("PRAGMA temp_store=2", sql)).ExecuteNonQuery();
            } catch {
            }
            try {
                // Default is 2000 1K pages
                (new SQLiteCommand("PRAGMA cache_size=8000", sql)).ExecuteNonQuery();
            } catch {
            }
            int hashRunId = -1;
            try {
                const string stmt =
                    "SELECT hashRunId " +
                    "FROM tbl_HashRun " +
                    "WHERE slideAmount=@SA AND " +
                    "blockSizeBytes=@BS AND " +
                    "phoneId=@PID AND " +
                    "memoryId=@MID " +
                    "ORDER BY hashRunId DESC " +
                    "LIMIT 1";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@SA", _slideAmount);
                cmd.Parameters.AddWithValue("@BS", _blockSize);
                cmd.Parameters.AddWithValue("@PID", phoneId);
                cmd.Parameters.AddWithValue("@MID", _memoryId);
                SQLiteDataReader rdr = null;
                try {
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read()) {
                        hashRunId = Convert.ToInt32(rdr["hashRunId"]);
                    }
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
            } catch {
            }
            if (hashRunId < 0) {
                return new List<UnfilterdBlockResult>();
            }
            try {
                DateTime started = DateTime.Now;
                SQLiteCommand cmd = null;
                const string stmt2 =
                    "SELECT hash, MIN(blockIndex) AS blockIndexFirst " +
                    "FROM tbl_Hash " +
                    "WHERE hashRunId=@HRID " +
                    "GROUP BY hash " +
                    "ORDER BY blockIndexFirst ASC";
                cmd = new SQLiteCommand(stmt2, sql);
                cmd.Parameters.AddWithValue("@HRID", hashRunId);
                SQLiteDataReader rdr = null;
                List<UnfilterdBlockResult> tempResults = new List<UnfilterdBlockResult>();
                try {
                    rdr = cmd.ExecuteReader();
                    while (rdr.Read()) {
                        UnfilterdBlockResult result = new UnfilterdBlockResult()
                        {
                            hash = Convert.ToString(rdr["hash"]),
                            blockIndexFirst =
                                Convert.ToInt32(rdr["blockIndexFirst"])
                        };
                        tempResults.Add(result);
                    }
                } catch (Exception e) {
                    System.Console.WriteLine("SQL exception: " + e.Message);
                    return new List<UnfilterdBlockResult>();
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
                DateTime finished = DateTime.Now;
                System.Console.WriteLine("It took " + (finished - started).TotalSeconds + " seconds to read hashes");
                List<UnfilterdBlockResult> results = null;
                int dropped = 0;
                if (_noFilter) {
                    results = tempResults;
                } else {
                    results = new List<UnfilterdBlockResult>();
                    foreach (UnfilterdBlockResult result in tempResults) {
                        rdr = null;
                        try {
                            const string stmt =
                                "SELECT hash FROM tbl_Hash WHERE hashRunId<>@HRID AND hash=@HASH LIMIT 1";
                            cmd = new SQLiteCommand(stmt, sql);
                            cmd.Parameters.AddWithValue("@HRID", hashRunId);
                            cmd.Parameters.AddWithValue("@HASH", result.hash);
                            rdr = cmd.ExecuteReader();
                            if (!rdr.HasRows) {
                                results.Add(result);
                            } else {
                                dropped++;
                            }
                        } catch (Exception e) {
                            System.Console.WriteLine("SQL exception: " + e.Message);
                        } finally {
                            if (rdr != null) {
                                rdr.Close();
                            }
                        }
                    }
                    tempResults.Clear();
                }
                System.Console.WriteLine("Skipped " + dropped + " blocks");
                return results;
            } catch (Exception ex) {
                System.Console.WriteLine("SQL exception: " + ex.Message);
                return new List<UnfilterdBlockResult>();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Saves the filter results using a BinaryFormatter.
        /// </summary>
        /// <returns>The filepath of the saved file.</returns>
        private string SaveFilterResults()
        {
            string outputfile = _inputFile + ".fil";

            using (Stream outstream = File.Create(outputfile))
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(outstream, _filterResult);
            }

            return outputfile;
        }

        /// <summary>
        /// Attempts to log the filter results from the same directory as the input file
        /// </summary>
        /// <returns>Returns true if successful.</returns>
        private bool LoadFilterResults()
        {
            string resfile = _inputFile + ".fil";

            if (File.Exists(resfile))
            {
                using (Stream instream = File.OpenRead(resfile))
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    var filterResult = (FilterResult)serializer.Deserialize(instream);

                    // Check if the results file was created from the same binary input file
                    if (filterResult.MemoryId == _memoryId)
                        _filterResult = filterResult;
                }
            }

            return _filterResult != null;
        }

        #endregion

        #region Threading

        /// <summary>
        /// Class to determine within a thread if a hash is in tbl_Hash.
        /// </summary>
        private class BlockHashFilterThread
        {
            private readonly int tid;
            private readonly int runId;
            private readonly int startx;
            private readonly int count;
            private readonly ArrayList tempResults;
            private readonly List<UnfilterdBlockResult> results;
            private readonly string sqlLiteConnectionString;
            private readonly BlockingCollection<int> queue;

            public BlockHashFilterThread(int tid, int runId, int startx, int count, ArrayList tempResults,
                List<UnfilterdBlockResult> results, SQLiteConnection sql, BlockingCollection<int> queue)
            {
                this.tid = tid;
                this.runId = runId;
                this.startx = startx;
                this.count = count;
                this.tempResults = tempResults;
                this.results = results;
                this.sqlLiteConnectionString = sql.ConnectionString;
                this.queue = queue;
            }

            /// <summary>
            /// Where the thread runs, saving only those hashes that aren't duplicates
            /// of other phones. 
            /// </summary>
            /// <param name="si">A list of the distinct blocks.</param>
            public void Process(Object si)
            {
                // We need our own sql connection. Sharing a connection amongst threads
                // works but then access is serialized.
                SQLiteConnection sql = null;
                try {
                    sql = new SQLiteConnection(sqlLiteConnectionString);
                    sql.Open();
                } catch (Exception ex) {
                    System.Console.WriteLine("SQL exception opening new connection: " + ex.Message);
                    queue.Add(tid);
                    return;
                }
                System.Console.WriteLine("Started thread " + tid);
                string stmt = String.Format("SELECT hash FROM tbl_Hash WHERE hashRunId<>{0} AND hash=@HASH LIMIT 1", runId);
                int stopx = startx + count - 1;
                for (int n = startx; n <= stopx; n++) {
                    SQLiteDataReader rdr = null;
                    try {
                        UnfilterdBlockResult result = (UnfilterdBlockResult) tempResults[n];
                        SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                        cmd.Parameters.AddWithValue("@HASH", result.hash);
                        rdr = cmd.ExecuteReader();
                        if (!rdr.HasRows) {
                            results.Add(result);
                        }
                        tempResults[n] = null; // OK to GC
                    } catch (ThreadAbortException e) {
                        break;
                    } catch (Exception e) {
                        System.Console.WriteLine("SQL exception: " + e.Message);
                        break;
                    } finally {
                        if (rdr != null) {
                            rdr.Close();
                            rdr.Dispose();
                        }
                    }
                }
                sql.Close();
                sql.Dispose();
                System.Console.WriteLine("Finished thread " + tid);
                queue.Add(tid);
            }
        }

        #endregion

    }

}
