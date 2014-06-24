
#define _GENERAL_PARSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.IO;
using Dec0de.Bll.Filter;
using Dec0de.Bll.UserStates;

namespace Dec0de.Bll.Viterbi
{
    public class ThreadedViterbi
    {
        private List<Block> _unfilteredBlocks;
        private string _filePath;
        private string _fileSha1;
        private List<StateMachine> _machines;
        private List<State> _states;
        private List<UserState> _userStates;
        private State _startState;
        private ViterbiResult _viterbiResults;
        private RunType _runType;        

        public ThreadedViterbi(List<Block> UnfilteredBlocks, RunType runType, List<UserState> userStates, string file_path, string fileSha1)
        {
            _unfilteredBlocks = UnfilteredBlocks;
            _userStates = userStates;
            _filePath = file_path;
            _machines = new List<StateMachine>();
            _states = new List<State>();
            _viterbiResults = new ViterbiResult();
            _viterbiResults.Fields = new List<ViterbiField>();
            _fileSha1 = fileSha1;
            _viterbiResults.MemoryId = this._fileSha1;
            _runType = runType;

            //TODO: (RJW) I am not convinced this bit of code loads the machines as Shaksham intended. 
            // This call is to load _machines, _states, _startState and _userStates variables, so that we do not execute same code for every block to load values
            // into these variables, since they are the same for all blocks.
            if (_runType == RunType.GeneralParse)
            {
                Viterbi viterbi = new Viterbi(RunType.GeneralParse, true, ref _machines, ref _states, ref _startState, ref _userStates);
            }
            else if (_runType == RunType.Meta)
            {
                Viterbi viterbi = new Viterbi(RunType.Meta, false, ref _machines, ref _states, ref _startState, ref _userStates);
                _unfilteredBlocks = Split_On_Binary_Large_Fields(_unfilteredBlocks[0].Bytes);
            }
            else
            {
                Viterbi viterbi = new Viterbi(_runType, false, ref _machines, ref _states, ref _startState, ref _userStates);
            }

        }

        public ViterbiResult RunThreadedViterbi()
        {     
            DateTime start = DateTime.Now;

            if (File.Exists(_filePath + ".vtf") && (_runType != RunType.Meta))
            {
                this.Load__Intermediate_Field_Results();
            }
            else
            {
                if (_unfilteredBlocks.Count > 0)
                {
                    List<ViterbiResult> ResultsOnBlocks = new List<ViterbiResult>();
                    ManualResetEvent manual = new ManualResetEvent(false);
                    object mutex = new object();
                    ThreadPool.SetMaxThreads(10, 10);  // Using 10 threads in the pool
                    int block_job_count = 0;

                    for (int i = 0; i < _unfilteredBlocks.Count; i += 10)
                    {
                        List<Block> next_unfiltered_blockset = new List<Block>();  // send 10 blocks per thread
                        for (int j = i; j < Math.Min(i + 10, _unfilteredBlocks.Count); j++)
                        {
                            next_unfiltered_blockset.Add(_unfilteredBlocks[j]);
                        }
                        Interlocked.Increment(ref block_job_count);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state)
                        {
                            RunBlockThread(next_unfiltered_blockset, ref ResultsOnBlocks, ref mutex, ref block_job_count, ref manual);
                        }));
                    }
                    manual.WaitOne(); // catch the manual.set() signal and do not block the main thread anymore

                    for (int i = 0; i < ResultsOnBlocks.Count; i++)
                    {
                        _viterbiResults.Fields.AddRange(ResultsOnBlocks[i].Fields);
                    }
                }
                if (_runType != RunType.Meta)
                {
                   // this.Write_Intermediate_Field_Results();
                }
            }
            DateTime end = DateTime.Now;
            TimeSpan time_taken = end - start;
            _viterbiResults.Duration = time_taken;

            //this.Write_Field_CSV();          // will be putting the Write_Field_CSV method elsewhere.
            
            return _viterbiResults;
        }

        private void RunBlockThread(List<Block> BlockSet, ref List<ViterbiResult> ResultsOnBlocks, ref object mutex, ref int job_count, ref ManualResetEvent manual)
        {
            ViterbiResult viterbiResultFields = null;
            if (_runType != RunType.Meta)
            {
#if !_GENERAL_PARSE
                AnchorViterbi anchor_viterbi = new AnchorViterbi(RunType.GeneralParse, _filePath, new List<UserState>(_userStates));
                viterbiResultFields = anchor_viterbi.RunThreaded(BlockSet, ref _machines, ref _states, ref _startState, ref _userStates);
#else
            Viterbi viterbi = new Viterbi(_runType, false, ref _machines, ref _states, ref _startState, ref _userStates);
            viterbiResultFields = viterbi.Run(BlockSet, _filePath);
#endif
            }
            else
            {
                Viterbi viterbi = new Viterbi(RunType.Meta, false, ref _machines, ref _states, ref _startState, ref _userStates);
                viterbiResultFields = viterbi.Run(BlockSet, _filePath);
            }
            lock (mutex)
            {
                ResultsOnBlocks.Add(viterbiResultFields);
                job_count--;
                if (job_count == 0)
                {
                    manual.Set(); // signal that all threads are done
                }
            }
        }

        private List<Block> Split_On_Binary_Large_Fields(byte[] bytes)
        {
            List<Block> blocks = new List<Block>();
            List<byte> tmp = new List<byte>();
            for (int j = 0; j < bytes.Length; j++)
            {
                if (bytes[j] == (byte)MetaMachine.BinaryLarge)
                {
                    tmp.Add(bytes[j]);
                    if (tmp.Count != 1)
                    {
                        Block block = new Block();
                        block.Bytes = new byte[tmp.Count];
                        tmp.ToArray().CopyTo(block.Bytes, 0);                        
                        blocks.Add(block);
                        tmp.Clear();
                    }
                }
                else
                {
                    tmp.Add(bytes[j]);
                }
            }
            tmp.Add((byte)MetaMachine.BinaryLarge);
            Block b = new Block();
            b.Bytes = new byte[tmp.Count];            
            tmp.ToArray().CopyTo(b.Bytes, 0);
            blocks.Add(b);
            
            blocks[0].OffsetFile = 0;
            for (int j = 1; j < blocks.Count; j++)
            {
                blocks[j].OffsetFile = blocks[j - 1].OffsetFile + blocks[j - 1].Bytes.Length;
            }
            return blocks;
        }

        private void Write_Intermediate_Field_Results()
        {
            string outputfile = _filePath + ".vtf";

            using (Stream outstream = File.Create(outputfile))
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(outstream, _viterbiResults);
            }            
        }

        private void Load__Intermediate_Field_Results()
        {
            string resfile = _filePath + ".vtf";

            using (Stream instream = File.OpenRead(resfile))
            {
                BinaryFormatter serializer = new BinaryFormatter();
                var results = (ViterbiResult)serializer.Deserialize(instream);

                // Check if the results file was created from the same binary input file
                if (_viterbiResults.MemoryId == results.MemoryId)
                {
                    _viterbiResults = results;
                }
            }
        }
    }
}
