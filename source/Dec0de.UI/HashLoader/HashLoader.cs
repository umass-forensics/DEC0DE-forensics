/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 */

//#define _USESQLSERVER_

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Dec0de.Bll.Filter;
#if _USESQLSERVER_
using PhoneDecoder.HashLoader.EmbeddedDal;
#else
using Dec0de.UI.Database;
using System.Windows.Forms;
using System.Threading;

#endif

namespace Dec0de.UI.HashLoader
{
    // Most of this was copy and pasted from Robert's original dec0de work.

    class HashLoader
    {

        public const int DefaultBlockSize = 1024;
        public const int DefaultSlideAmount = 1024;

        // TODO: There is a lot of hardcode BlockSize=1024 and Slide=128 within dec0de. Don't use multiple values!!!
        private static readonly int[] SlideAmounts = new int[] { DefaultSlideAmount }; // new int[] { 1024 }; //new int[] {128, 1024}; 
        private static readonly int[] BlockSizes = new int[] { DefaultBlockSize };

        //The number of blocks to hash before loading the information into the database. This is important for very large phones.
        private const int BLOCK_BUFFER = 16777216;

        /// <summary>
        /// Set this to true if you want to hash the phone with the given slide amounts
        /// </summary>
        private bool UseSlide = true;

        private List<BinaryFile> files = new List<BinaryFile>();
        private string manufacturer;
        private string model;
        private string notes;
        private string memoryIdSha1;
        private List<BlockHashFilter.UnfilterdBlockResult> storedBlocks = null; 

        public int PhoneId { get; private set; }

        public HashLoader(string filePath, string manufacturer, string model, string memoryIdSha1, string notes)
        {
            this.files.Add(new BinaryFile(filePath, manufacturer, model, memoryIdSha1));
            this.manufacturer = manufacturer;
            this.model = model;
            this.notes = notes;
            this.memoryIdSha1 = memoryIdSha1;
            this.PhoneId = -1;
        }

        public HashLoader(string[] filePaths, string manufacturer, string model, string notes)
        {
            string subId = Guid.NewGuid().ToString();
            for (int n = 0; n < filePaths.Length; n++) {
                FileStream fs = new FileStream(filePaths[n], FileMode.Open, FileAccess.Read, FileShare.Read);
                string memIdSha1 = DcUtils.BytesToHex((new SHA1Managed()).ComputeHash(fs));
                fs.Close();
                this.files.Add(new BinaryFile(filePaths[n], manufacturer, model, memIdSha1, subId));
            }
            this.manufacturer = manufacturer;
            this.model = model;
            this.notes = notes;
            this.PhoneId = -1;
        }

        /// <summary>
        /// Finds out if the phone's block hashes are already present in the database.
        /// </summary>
        /// <param name="blockSize">The size of a memory block on phone.</param>
        /// <param name="slideAmount">The distance between the starting offsets of two consecutive blocks in memory.</param>
        /// <returns></returns>
        public bool AlreadyExists(int blockSize, int slideAmount)
        {
            int pid = -1;
            try {
#if _USESQLSERVER_
                using (PhoneDbDataContext dataContext = Dalbase.GetDataContext()) {
                    var results = dataContext.usp_Phoneid_GetByMemoryid(this.memoryId, blockSize, slideAmount);
                    pid = (from result in results select result.phoneId).Single();
                }
#else
                pid = DatabaseAccess.GetPhoneidByMemoryid(this.memoryIdSha1, blockSize, slideAmount);
#endif
            }
            catch (Exception) {
            }
            if (pid < 0) {
                return false;
            }
            this.PhoneId = pid;
            return true;
        }

        /// <summary>
        /// Called when the do not load hashes option is in effect. Generate block hashes
        /// but put nothing in the DB. Use a special phoneId of Int32.MaxValue-1.
        /// </summary>
        public void PseudoLoad()
        {
            this.PhoneId = -1;
            long nextStreamPosition = 0;
            List<string> hashes = ComputeHashes(files[0].Path, DefaultBlockSize, DefaultSlideAmount, ref nextStreamPosition);
            if (hashes.Count == 0) return;
            HashSet<string> loaded = new HashSet<string>();
            List<BlockHashFilter.UnfilterdBlockResult> stored = new List<BlockHashFilter.UnfilterdBlockResult>();
            int blockIndex = 0;
            foreach (string blkhash in hashes) {
                if (!loaded.Contains(blkhash)) {
                        BlockHashFilter.UnfilterdBlockResult ubr = new BlockHashFilter.UnfilterdBlockResult
                                                                        {
                                                                            blockIndexFirst = blockIndex,
                                                                            hash = blkhash
                                                                        };
                        stored.Add(ubr);
                        loaded.Add(blkhash);
                }
                blockIndex++;
            }
            loaded.Clear();
            this.PhoneId = Int32.MaxValue-1;
            this.storedBlocks = stored;
        }

        /// <summary>
        /// Calls methods to calculate and load the phone's block hashes as well as general information to the database.
        /// </summary>
        public void Load()
        {
            //GetConstants();

            DateTime start = DateTime.Now;

            Console.WriteLine("Started at {0}.", start);

            

            for (int i = 0; i < this.files.Count; i++) {
                Console.WriteLine("Processing {0}", this.files[i].Path);

                this.PhoneId = ProcessFile(this.files[i], this.notes);
            }

            DateTime end = DateTime.Now;

            Console.WriteLine("Ended at {0}. Duration: {1}", end, end - start);
        }

        public void GetEmptyHash()
        {
            byte[] emptyBytes = new byte[32768];

            var hash = BlockHashFilter.GetBlockHash(emptyBytes);

            var hashString = Convert.ToBase64String(hash);
        }

#if _GETCONSTANTS
        public static void GetConstants()
        {
            int[] sizes = new int[] { 1, 4, 16 };//{32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768};

            using (PhoneDbDataContext dataContext = Dalbase.GetDataContext()) {

                for (int i = 0; i < sizes.Length; i++) {


                    for (int j = 0; j < 256; j++) {
                        byte value = (byte)j;

                        byte[] bytes = (new byte[sizes[i]].Select(r => value)).ToArray();

                        var hash = BlockHashFilter.GetBlockHash(bytes);

                        var hashString = Convert.ToBase64String(hash);

                        var valString = "0x" + Convert.ToString(value, 16).PadLeft(2, '0');

                        dataContext.usp_Constants_Insert(valString, sizes[i], hashString);
                    }
                }
            }

        }
#endif

        public int ProcessFile(BinaryFile file, string notes)
        {
            int phoneId = GetPhoneId(file);

            // TODO: We assume a single block size, a single slide amount! 
            for (int i = 0; i < BlockSizes.Length; i++) {
                for (int j = 0; j < SlideAmounts.Length; j++) {
                    if (SlideAmounts[j] > BlockSizes[i])
                        continue;


                    if (!UseSlide && SlideAmounts[j] != BlockSizes[i])
                        continue;


                    int hashRunId = GetHashRunId(BlockSizes[i], phoneId, file.MemoryId, SlideAmounts[j], -1, -1,
                                                 "sha1", notes);

                    ProcessFile(file, BlockSizes[i], SlideAmounts[j], hashRunId, phoneId, file.MemoryId);

                }
            }

            return phoneId;

        }

        /// <summary>
        /// Calls ComputeHahses() method of BlockHashFilter class to calculate phone's block hashes, calls GetHashRunId() to insert phone's
        /// record to the database, calls InsertHahses() of DatabaseAccess to insert the block hashes into the database and HashRunUpdate() of
        /// DatabaseAccess to update the number of blocks and time to hash fields in the database.
        /// </summary>
        /// <param name="file">The BinaryFile reference corresponding to the phone's memory file.</param>
        /// <param name="blockSize">The size of a memory block on phone.</param>
        /// <param name="slideAmount">The distance between the starting offsets of two consecutive blocks in memory.</param>
        /// <param name="hashRunId">The ID of the row to which the block hashes of the phone are to be inserted.</param>
        /// <param name="pid">Phone id to be used.</param>
        /// <param name="memId">Memory id for the phone.</param>
        public void ProcessFile(BinaryFile file, int blockSize, int slideAmount, int hashRunId, int pid, string memId)
        {
            //The starting point in the phone file stream. Needed to maintain position state across multiple calls of the method.
            long nextStreamPosition = 0;
            long lastStreamPositon = -1;
            int blockIndex = 0;

            DateTime start = DateTime.Now;

            while (lastStreamPositon < nextStreamPosition) {

                lastStreamPositon = nextStreamPosition;

                List<string> hashes = ComputeHashes(file.Path, blockSize, slideAmount, ref nextStreamPosition);

                if (hashes.Count == 0)
                    continue;
#if _USESQLSERVER_
                long numBlocks = hashes.Count;

                List<HashInfo> hashInfos = new List<HashInfo>();

                for (int k = 0; k < hashes.Count; k++) {
                    hashInfos.Add(new HashInfo { BlockIndex = blockIndex, Hash = hashes[k], HashRunId = hashRunId });

                    blockIndex++;
                }

                BulkInsertBase bulky = BulkInsertBase.Load(hashInfos);

                bulky.Flush();
#else
                List<BlockHashFilter.UnfilterdBlockResult> stored = new List<BlockHashFilter.UnfilterdBlockResult>();
                try {
                    blockIndex = DatabaseAccess.InsertHashes(hashes, hashRunId, stored);
                } catch (ThreadAbortException e) {
                    DatabaseAccess.ForgetPhone(memId, pid, hashRunId);
                    throw e;
                }
                if (blockIndex < 0) {
                    DatabaseAccess.ForgetPhone(memId, pid, hashRunId);
                    return;
                }
                storedBlocks = stored;
#endif

            }

            DateTime end = DateTime.Now;
            TimeSpan duration = end - start;
            long timeToHashSeconds = Convert.ToInt32(Math.Round(duration.TotalSeconds));

#if _USESQLSERVER_
            using (PhoneDbDataContext dataContext = Dalbase.GetDataContext()) {
                dataContext.usp_HashRun_Update(hashRunId, blockIndex, timeToHashSeconds);
            }
#else
            DatabaseAccess.HashRunUpdate(hashRunId, blockIndex, (int) timeToHashSeconds);
#endif
        }

        public List<BlockHashFilter.UnfilterdBlockResult> GetAndForgetStoredHashes()
        {
            List<BlockHashFilter.UnfilterdBlockResult> stored = storedBlocks;
            storedBlocks = null;
            return stored;
        }

        /// <summary>
        /// Calls ComputeHahses() method of BlockHashFilter class to calculate phone's block hashes, calls GetHashRunId() to insert phone's
        /// record to the database, calls InsertHahses() of DatabaseAccess to insert the block hashes into the database.
        /// </summary>
        /// <param name="file">The BinaryFile reference corresponding to the phone's memory file.</param>
        /// <param name="blockSize">The size of a memory block on phone.</param>
        /// <param name="slideAmount">The distance between the starting offsets of two consecutive blocks in memory.</param>
        /// <returns>The PhoneId of the phone, i.e. the row ID of the phone's record in the database.</returns>
        public int ProcessFile(BinaryFile file, int blockSize, int slideAmount)
        {
            int phoneId = GetPhoneId(file);

            DateTime start = DateTime.Now;
            List<string> hashes = BlockHashFilter.ComputeHashes(file.Path, blockSize, slideAmount);
            DateTime end = DateTime.Now;

            TimeSpan duration = end - start;
            long numBlocks = hashes.Count;
            long timeToHashSeconds = Convert.ToInt32(Math.Round(duration.TotalSeconds));
            string hashType = "sha1";

            int hashRunId = GetHashRunId(blockSize, phoneId, file.MemoryId, slideAmount, numBlocks, timeToHashSeconds,
                                         hashType, "");

#if _USESQLSERVER_
            List<HashInfo> hashInfos = new List<HashInfo>();

            for (int k = 0; k < hashes.Count; k++) {
                hashInfos.Add(new HashInfo { BlockIndex = k, Hash = hashes[k], HashRunId = hashRunId });
            }

            BulkInsertBase bulky = BulkInsertBase.Load(hashInfos);

            bulky.Flush();
#else
            DatabaseAccess.InsertHashes(hashes, hashRunId);
#endif

            return phoneId;
        }

        /// <summary>
        /// Gets the row ID for the record it inserts in the database, consisting of phone's general and 
        /// block hash information viz. memoryID, slide amount, block size, notes etc.
        /// </summary>
        /// <param name="blocksize">The size of a memory block on phone.</param>
        /// <param name="phoneId">The unique identification for this phone in the database.</param>
        /// <param name="memoryId">The FileSHA1 of the memory file.</param>
        /// <param name="slideAmount">The distance between the starting offsets of two consecutive blocks in memory.</param>
        /// <param name="numBlocks">The total number of blocks in phone's memory.</param>
        /// <param name="timeToHashSeconds">Time taken to calculate all block hashes of the phone.</param>
        /// <param name="hashType">The kind of hash, for example SHA1.</param>
        /// <param name="notes">General information regarding the phone.</param>
        /// <returns>The row ID of the record just inserted for this phone in the table.</returns>
        public int GetHashRunId(int blocksize, int phoneId, string memoryId, int slideAmount, long numBlocks, long timeToHashSeconds, string hashType, string notes)
        {
#if _USESQLSERVER_
            using (PhoneDbDataContext dataContext = Dalbase.GetDataContext()) {
                var results = dataContext.usp_HashRun_Insert(DateTime.Now, phoneId, memoryId, slideAmount, blocksize, numBlocks,
                                                             timeToHashSeconds, hashType, notes);

                return (from result in results select result.hashRunId).Single();
            }
#else
            return DatabaseAccess.HashRunInsert(DateTime.Now, phoneId, memoryId, slideAmount, blocksize, (int)numBlocks,
                                                (int)timeToHashSeconds, hashType, notes);
#endif
        }

        /// <summary>
        /// Gets the ID of the phone by adding its information to the database table and getting the corresponding row ID.
        /// </summary>
        /// <param name="file">The BinaryFile reference for the phone's memory file.</param>
        /// <returns>The ID corresponding to the phone, from the database.</returns>
        public int GetPhoneId(BinaryFile file)
        {
#if _USESQLSERVER_
            using (PhoneDbDataContext dataContext = Dalbase.GetDataContext()) {
                var results = dataContext.usp_Phone_Insert(file.Make, file.Model, file.SubId,
                                             string.Format("Inserted on {0}", DateTime.Now));

                return (from result in results select result.phoneId).Single();
            }
#else
            return DatabaseAccess.PhoneInsert(file.Make, file.Model, file.SubId,
                                              string.Format("Inserted on {0}", DateTime.Now));
#endif
        }

        /// <summary>
        /// Calls the Filter() method of BlockHashFilter class for calculating the phone's block hashes.
        /// </summary>
        /// <param name="inputFile">The path to phone's memory file.</param>
        /// <param name="blocksize">The size of a memory block on phone.</param>
        /// <param name="slideAmount">The distance between the starting offsets of two consecutive blocks in memory.</param>
        /// <param name="nextStreamPosition">The starting position in memory of the block to be hashed.</param>
        /// <returns>A list of block hashes for the phone.</returns>
        public List<string> ComputeHashes(string inputFile, int blocksize, int slideAmount, ref long nextStreamPosition)
        {
            List<string> hashes = new List<string>();

            int countBlocks = 0;

            using (FileStream stream = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var block = new byte[blocksize];

                //Start where we left off. Should be zero for the first run of the method.
                stream.Position = nextStreamPosition;

                while (stream.Position < stream.Length && countBlocks < BLOCK_BUFFER) {
                    stream.Read(block, 0, blocksize);

                    byte[] hash = BlockHashFilter.GetBlockHash(block);

                    string hashString = Convert.ToBase64String(hash);

                    hashes.Add(hashString);           

                    //Need this check. Otherwise it will not finish
                    if (stream.Position < stream.Length)
                        stream.Position = stream.Position - blocksize + slideAmount;

                    countBlocks++;

                    nextStreamPosition = stream.Position;
                }
            }

            return hashes;
        }
		
        /// <summary>
        /// Called by the Run() method of WorkerThread class load the phone's block hashes into the database.
        /// </summary>
        /// <param name="fileSha1">The SHA1 hash of the entire memory file.</param>
        /// <param name="filePath">The path to the phone's memory file.</param>
        /// <param name="manufacturer">The name of the phone's manufacturer.</param>
        /// <param name="model">The model of phone.</param>
        /// <param name="note">General information regarding the phone.</param>
        /// <returns>Information about the phone and its ID encapsulated in a HashLoader object.</returns>
		public static HashLoader LoadHashesIntoDB(string fileSha1, string filePath, string manufacturer,
            string model, string note, bool doNotStoreHashes)
		{		    
            HashLoader hashLoader;
            try
            {
                hashLoader =
                    new HashLoader(filePath, manufacturer, model, fileSha1, note);
                if (!hashLoader.AlreadyExists(DefaultBlockSize, DefaultSlideAmount))
                {
                    WorkerThread.write("Loading block hashes");
                    if (doNotStoreHashes) {
                        hashLoader.PseudoLoad();
                    } else {
                        hashLoader.Load();
                    }
                }
                else
                {
                    WorkerThread.write("Block hashes are already in the database");
                }                
            }
            catch (ThreadAbortException)
            {
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loading Hashes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return hashLoader;
		}
    }


    /// <summary>
    /// Stores details about the phone and its memory file.
    /// </summary>
    public class BinaryFile
    {
        /// <summary>
        /// The make of the phone.
        /// </summary>
        public string Make { get; set; }
        /// <summary>
        /// The model of the phone.
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// A globally unique identifier for the phone's record.
        /// </summary>
        public string SubId { get; set; }
        /// <summary>
        /// Path of the phone's memory file.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// The SHA1 hash of the phone's memory file.
        /// </summary>
        public string MemoryId { get; set; }

        public BinaryFile(string path, string make, string model, string memid, string subid)
        {
            Path = path;
            Make = make;
            Model = model;
            MemoryId = memid;
            SubId = subid;
        }

        public BinaryFile(string path, string make, string model, string memid)
        {
            Path = path;
            Make = make;
            Model = model;
            MemoryId = memid;
            SubId = Guid.NewGuid().ToString();
        }
        
        public BinaryFile(string path)
        {
            Path = path;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            try {
                //file name should be in the format "make model subid"
                var parts = fileName.Split(' ');

                Make = parts[0].ToLower();
                Model = parts[1].ToLower();
                SubId = parts[2].ToLower();

                if (!parts[3].StartsWith("Phys"))
                    MemoryId = parts[3];
                else {
                    MemoryId = "Ind";
                }
            } catch (Exception ex) {
                string message = string.Format("filename ({0}) was in the wrong format", fileName);
                throw new ArgumentException(message, ex);
            }

        }
    }
}
