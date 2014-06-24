using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Dec0de.Bll.Filter;
using Dec0de.Bll.UserStates;

namespace Dec0de.Bll.Viterbi
{
    public class AnchorViterbi
    {
        public const int LONG_GAP_BYTES = 200;
        public const int BLOCK_PADDING_BYTES = 100;

        private string _filePath;
        private long _fileLength;
        private RunType _runType;

        private ViterbiResult _anchorResults;
        private ViterbiResult _fieldResults;

        private List<UserState> _userStates;

        /// <summary>
        /// The id of the bin file used to create these results. Typically a
        /// sha1 value.
        /// </summary>
        public string MemoryId { get; set; }

        /// <summary>
        /// Constructor, initializing members of AnchorViterbi.
        /// </summary>
        /// <param name="runType">Determines the kind of run, whether GeneralParse (for regular field level inference), Meta (for record level inference), etc.</param>
        /// <param name="filePath">Path to phone's memory file.</param>
        public AnchorViterbi(RunType runType, string filePath)
        {
            _filePath = filePath;

            var info = new FileInfo(filePath);

            _fileLength = info.Length;

            _runType = runType;

            // No user-defined state machines.
            _userStates = new List<UserState>();
        }

        /// <summary>
        /// Constructor, initializing members of AnchorViterbi.
        /// </summary>
        /// <param name="runType">Determines the kind of run, whether GeneralParse (for regular field level inference), Meta (for record level inference), etc.</param>
        /// <param name="filePath">Path to phone's memory file.</param>
        /// <param name="userStates">User-defined state machines.</param>
        public AnchorViterbi(RunType runType, string filePath, List<UserState> userStates)
        {
            _filePath = filePath;

            var info = new FileInfo(filePath);

            _fileLength = info.Length;

            _runType = runType;

            _userStates = (userStates == null) ? new List<UserState>() : userStates;
        }


        public List<Block> GetAnchorPointBlocks(List<Block> unfilteredBlocks)
        {
            var anchor = new Viterbi(RunType.AnchorPoints, true, _userStates);

            var blocks = new List<Block>();
            //The list of Fields in the result are the anchor points
            _anchorResults = anchor.Run(unfilteredBlocks, _filePath);

            if (_anchorResults.Fields.Count == 0)
                //throw new ApplicationException("No anchor points found!");
                return new List<Block>();


            //The next index after the end of the anchor point
            long nextIndex = _anchorResults.Fields[0].OffsetFile + _anchorResults.Fields[0].Length;

            //Initialize the start of the first block to the first anchor point - X bytes. If this goes past the start of the file,
            // we will just use the start of the file.
            long blockStart = Math.Max(0, _anchorResults.Fields[0].OffsetFile - BLOCK_PADDING_BYTES);

            //Initialize the end of the first block to be X bytes past the first anchor points. If this 
            // goes past the end of the file, we will just use the end of the file.
            long blockEnd = Math.Min(_fileLength, _anchorResults.Fields[0].OffsetFile + _anchorResults.Fields[0].Length + BLOCK_PADDING_BYTES);


            FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read); // BL 8/4
            //Get blocks on data based on the anchor points.
            for (int i = 1; i < _anchorResults.Fields.Count; i++)
            {
                //The size of the gap in bytes between the two anchor points
                long gap = _anchorResults.Fields[i].OffsetFile - nextIndex;

                //If the gap is too long, we need to create a new block
                if (gap > LONG_GAP_BYTES || i == _anchorResults.Fields.Count - 1)
                {
                    //Create block using current block start and end
                    // blocks.Add(GetBlock(blockStart, blockEnd)); // BL 8/4
                    blocks.Add(GetBlock(stream, blockStart, blockEnd));  // BL 8/4

                    blockStart = Math.Max(0, _anchorResults.Fields[i].OffsetFile - BLOCK_PADDING_BYTES);
                }

                blockEnd = Math.Min(_fileLength, _anchorResults.Fields[i].OffsetFile + _anchorResults.Fields[i].Length + BLOCK_PADDING_BYTES);

                nextIndex = _anchorResults.Fields[i].OffsetFile + _anchorResults.Fields[i].Length;
            }
            stream.Close(); // BL 8/4

            return blocks;
        }



        public ViterbiResult Run(List<Block> unfilteredBlocks, bool saveResults)
        {
            if (saveResults && LoadResults())
                return _fieldResults;


            Run(unfilteredBlocks);

            if (saveResults)
                SaveResults();

            return _fieldResults;
        }

        /// <summary>
        /// Calls the Run() method of Viterbi to start field level inference.
        /// </summary>
        /// <param name="unfilteredBlocks">List of blocks over which inference is to be performed.</param>
        /// <returns>The most likely Viterbi path of fields explaining the set of outputs.</returns>
        public ViterbiResult Run(List<Block> unfilteredBlocks)
        {
            var anchorBlocks = GetAnchorPointBlocks(unfilteredBlocks);

            Console.WriteLine("{0} Anchor block bytes.", Block.GetByteTotal(anchorBlocks));

            var viterbi = new Viterbi(_runType, false, _userStates);

            _fieldResults = viterbi.Run(anchorBlocks, _filePath);
            _fieldResults.Duration = _fieldResults.Duration.Add(_anchorResults.Duration);

            _fieldResults.MemoryId = MemoryId;

            return _fieldResults;
        }

        // Using this method instead of the Run() method above.
        public ViterbiResult RunThreaded(List<Block> unfilteredBlocks, ref List<StateMachine> machines,
                                  ref List<State> states, ref State startState, ref List<UserState> userStates)
        {
            var anchorBlocks = GetAnchorPointBlocks(unfilteredBlocks);

            Console.WriteLine("{0} Anchor block bytes.", Block.GetByteTotal(anchorBlocks));

            var viterbi = new Viterbi(_runType, false, ref machines, ref states, ref startState, ref userStates);

            var result = viterbi.Run(anchorBlocks, _filePath);
            result.Duration = result.Duration.Add(_anchorResults.Duration);           

            return result;
        }

        private Block GetBlock(long start, long end)
        {
            int length = (int)(end - start);

            var bytes = BlockHashFilter.GetBytes(_filePath, start, length);

            return new Block(){Bytes = bytes, OffsetFile = start};
        }

        // BL 8/4
        private Block GetBlock(FileStream stream, long start, long end)
        {
            int length = (int)(end - start);

            var bytes = BlockHashFilter.GetBytes(stream, start, length);

            return new Block() { Bytes = bytes, OffsetFile = start };
        }

        #region Private Methods

        private bool LoadResults()
        {
            string resfile = _filePath + ".vtf";

            if (File.Exists(resfile))
            {
                using (Stream instream = File.OpenRead(resfile))
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    var results = (ViterbiResult)serializer.Deserialize(instream);

                    // Check if the results file was created from the same binary input file
                    if (results.MemoryId == MemoryId)
                        _fieldResults = results;
                }
            }

            return _fieldResults != null;
        }

        private string SaveResults()
        {
            string outputfile = _filePath + ".vtf";

            using (Stream outstream = File.Create(outputfile))
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(outstream, _fieldResults);
            }

            return outputfile;
        }

        #endregion
    }



}
