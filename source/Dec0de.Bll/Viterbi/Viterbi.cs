//#define PRINT_ALL
//#define PRINT_NONBINARY
//#define PRINT_FIELD

using System;
using System.Collections.Generic;
using System.IO;
using Dec0de.Bll.Filter;
using Dec0de.Bll.UserStates;

namespace Dec0de.Bll.Viterbi
{
    public class Viterbi
    {
        #region Declarations

        private const int BLOCK_SIZE = 16384;

        private /*static*/ byte[] _observations;

        private readonly List<StateMachine> _machines;
        private readonly List<State> _states;
        private readonly State _startState;
        private long _fileOffset;

        private /*static*/ int[][] _bestPrevState;

        private readonly List<string> _textList;
        private readonly List<ViterbiField> _fieldList;

        private readonly bool _isAnchor;

        private readonly List<UserState> _userStates;

        private RunType _runType;

        #endregion

        #region Instantiation

        /// <summary>
        /// Calls static methods of StateMachine to prepare the state machines with their emission/transition probabilities for inference, according to the Runtype.
        /// </summary>
        /// <param name="type">The Run type, whether field level, record level, only for phone numbers etc.</param>
        /// <param name="isAnchor"></param>
        /// <param name="userStates">Any user defined state machines.</param>
        public Viterbi(RunType type, bool isAnchor, List<UserState> userStates=null)
        {
#if !PRINT_FIELD
            Console.WriteLine("Not Printing fields!");
#endif

            _runType = type;
            _isAnchor = isAnchor;

            _machines = new List<StateMachine>();
            _states = new List<State>();
            _startState = new State();
            _textList = new List<string>();
            _fieldList = new List<ViterbiField>();
            _userStates = userStates ?? new List<UserState>();

            switch (_runType)
            {
                case RunType.BinaryOnly:
                    StateMachine.TestBinaryOnly(ref _machines, ref _states, ref _startState);
                    break;
                case RunType.GeneralParse:
                    StateMachine.GeneralParse(ref _machines, ref _states, ref _startState, _userStates);
                    break;

                case RunType.PhoneNumberOnly:
                    StateMachine.TestPhoneOnly(ref _machines, ref _states, ref _startState);
                    break;

                case RunType.PhoneNumberAndText:
                    //StateMachine.TestPhoneNumberAndText(ref _machines, ref _states, ref _startState);
                    StateMachine.TestPhoneNumberAndTextMachine(ref _machines, ref _states, ref _startState);
                    break;

                case RunType.TextOnly:
                    StateMachine.TestTextOnly(ref _machines, ref _states, ref _startState);
                    break;

                case RunType.PhoneNumberTextAndTimeStamp:
                    StateMachine.TestPhoneNumberTextAndTimeStamp(ref _machines, ref _states, ref _startState);
                    break;

                case RunType.Meta:
                    StateMachine.TestMeta(ref _machines, ref _states, ref _startState);
                    break;

                case RunType.AnchorPoints:
                    StateMachine.TestAnchorFieldsOnly(ref _machines, ref _states, ref _startState, _userStates);
                    break;

                case RunType.Moto:
                    StateMachine.TestMoto(ref _machines, ref _states, ref _startState);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }

            Console.WriteLine("Viterbi set to {0}", Convert.ToString(type));
        }

        public Viterbi(RunType type, bool isAnchor, ref List<StateMachine> machines, ref List<State> states, ref State startState, ref List<UserState> userStates)
        {
#if !PRINT_FIELD
            Console.WriteLine("Not Printing fields!");
#endif
            _runType = type;

            _isAnchor = isAnchor;

            _machines = new List<StateMachine>();
            _states = new List<State>();
            _startState = new State();
            _textList = new List<string>();
            _fieldList = new List<ViterbiField>();
            _userStates = userStates ?? new List<UserState>();

            if (machines.Count == 0 || states.Count == 0)
            {
                switch (_runType)
                {
                    case RunType.GeneralParse:
                        StateMachine.GeneralParse(ref _machines, ref _states, ref _startState, _userStates);
                        break;

                    case RunType.SqliteOnly:
                        StateMachine.SqliteLiteOnly(ref _machines, ref _states, ref _startState, _userStates);
                        break;

                    case RunType.Meta:
                        StateMachine.TestMeta(ref _machines, ref _states, ref _startState);
                        break;
                }
                machines = _machines;
                states = _states;
                startState = _startState;
                userStates = _userStates;
            }
            else
            {
                _machines = machines;
                _states = states;
                _startState = startState;
                _userStates = userStates;
            }
        }


        public Viterbi(List<StateMachine> machines, List<State> states, State startState)
        {
#if !PRINT_FIELD
            Console.WriteLine("Not Printing fields!");
#endif

            _machines = machines;
            _states = states;
            _startState = startState;
            _textList = new List<string>();
            _fieldList = new List<ViterbiField>();
            _userStates = new List<UserState>();
        }

        #endregion

        #region Public Methods

        public List<string> Run(string filePath)
        {
            _textList.Clear();

            byte[] observations = new byte[BLOCK_SIZE];

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                while (stream.Position < stream.Length)
                {
                    long offset = stream.Position;

                    stream.Read(observations, 0, observations.Length);

                    Run(observations, offset);
                }
            }

            return _textList;
        }

        /// <summary>
        /// Runs the Viterbi algorithm to infer the most likely sequence of states explaining the input bytes.
        /// </summary>
        /// <param name="observations">List if bytes over which Viterbi inference is to be performed.</param>
        /// <param name="fileOffset">The starting offset of the observations within the file. Used to keep
        /// track of record location</param>
        public void Run(byte[] observations, long fileOffset)
        {
            _observations = observations;

            if (observations == null || observations.Length == 0)
                return;

            _fileOffset = fileOffset;

            /// using a jagged array instead of a rectangular one, to avoid an Out of Memory Exception.
            _bestPrevState = new int[_states.Count][];
            for (int i = 0; i < _bestPrevState.Length; i++)
            {
                _bestPrevState[i] = new int[_observations.Length];
            }

            double[] probs = GetStartProbabilities(0);

            for (int i = 1; i < _observations.Length; i++)
            {
                probs = ProcessObservation(probs, i);
            }

            PrintPath(probs);
        }

        /// <summary>
        /// Runs the Viterbi for each input block using Run(byte[], long) method.
        /// </summary>
        /// <param name="blocks">List of blocks for which Viterbi inference is to be done.</param>
        /// <param name="inputFile">Path to the phone's memory file.</param>
        /// <returns>The list of states in the inferred Viterbi path.</returns>
        public ViterbiResult Run(List<Block> blocks, string inputFile)
        {            
            var start = DateTime.Now;

            FileStream stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read); // BL 8/4
            for (int i = 0; i < blocks.Count; i++)
            {
                //Let's get the block bytes here. They should be null if this is a field level run of viterbi. For record-level runs,
                //they should not be null.
                //var bytes = blocks[i].Bytes ?? BlockHashFilter.GetBytes(inputFile, blocks[i].OffsetFile, blocks[i].Length); 
                var bytes = blocks[i].Bytes ?? BlockHashFilter.GetBytes(stream, blocks[i].OffsetFile, blocks[i].Length); // BL 8/4

                Run(bytes, blocks[i].OffsetFile);
            }
            stream.Close(); // BL 8/4

            return new ViterbiResult {Fields = _fieldList, Duration = DateTime.Now - start};    
        }

        #endregion

        #region Private Methods

        private List<State> FindPath(double[] probs)
        {
            int startIndex = _observations.Length - 1;

            double maxProb = double.MinValue;
            State mostProbableState = null;

            for (int i = 0; i < probs.Length; i++)
            {
                if (probs[i] > maxProb && _states[i].IsEndingState)
                {
                    maxProb = probs[i];
                    mostProbableState = _states[i];
                }
            }

            return FindPath(startIndex, mostProbableState, startIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="state"></param>
        /// <param name="length">Pass startIndex for the full path</param>
        /// <returns></returns>
        internal List<State> FindPath(int startIndex, State state, int length)
        {
            var path = new Stack<State>();
            path.Push(state);

            State prevState = state;

            for (int i = startIndex; i > (startIndex - length) && i>0; i--)
            {
                int prevIndex = _bestPrevState[prevState.ListIndex][i];
                prevState = _states[prevIndex];
                path.Push(prevState);
            }

            var pathList = new List<State>();

            while (path.Count > 0)
            {
                pathList.Add(path.Pop());
            }

            return pathList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        /// <returns>The first index after the end of the field</returns>
        private ViterbiField ExtractField(List<State> path, int index)
        {
            int startIndex = index;
            MachineList machineName = path[index].ParentStateMachine.Name;
            string fieldHex = "0x";
            string fieldAscii = "";
            List<byte> fieldBytes = new List<byte>();

            while (index < path.Count && machineName == path[index].ParentStateMachine.Name)
            {
                fieldBytes.Add(_observations[index]);

                //If this is an anchor run, no need to print fields
                if (!_isAnchor)
                {
                    string observation = Convert.ToString(_observations[index], 16).PadLeft(2, '0');

                    fieldHex += observation;
                    fieldAscii += (char) _observations[index];
               
#if PRINT_ALL
                Console.WriteLine("{0}\t:\t{1}\t{2}", path[index], observation, (char)_observations[index]);
#endif

                }
                index++;

                //Allows us to distinguish between adjacent fields of the same type
                if (path[index-1].IsSplitState)
                    break;
            }

            string fieldString;
            if (!_isAnchor && ((machineName == MachineList.PhoneNumber_User) ||
                               (machineName == MachineList.TimeStamp_User) ||
                               (machineName == MachineList.TimeStamp_User))) {
                // If we're printing a user-defined field then we need to use the user-defined
                // methods.
                try {
                    UserDefinedState udState = path[index - 1] as UserDefinedState;
                    fieldString = Printer.GetUserField(machineName, fieldBytes.ToArray(), udState.UserStateObj);
                } catch {
                    fieldString = "???";
                }
            } else {
                fieldString = _isAnchor ? "" : Printer.GetField(machineName, fieldBytes.ToArray());
            }

            var field = new ViterbiField
                            {
                                OffsetPath = startIndex,
                                OffsetFile = _fileOffset + startIndex,
                                HexString = fieldHex,
                                AsciiString = fieldAscii,
                                FieldString = fieldString,
                                MachineName = machineName,
                                Raw = fieldBytes.ToArray()
                            };

            _textList.Add(fieldString);
            _fieldList.Add(field);

            #if PRINT_FIELD

            Console.WriteLine(field);

            #endif

            return field;

        }

        private void PrintPath(double[] probs)
        {
            List<State> path = FindPath(probs);

            for (int i = 0; i < path.Count; i++)
            {
                State state = path[i];

                //Check if it is a binary state
                if (!state.IsBinary)
                {
                    var field = ExtractField(path, i);

                    //Have to have this minus one because of the increment in the for loop
                    i = field.OffsetPath+field.Length - 1;
                }
                else
                {

#if PRINT_ALL
                    Console.WriteLine("{0}\t:\t{1}\t{2}", state, Convert.ToString(_observations[i], 16).PadLeft(2, '0'),
                                      (char) _observations[i]);
#endif
                }

#if PRINT_NONBINARY
                //string observation = Convert.ToString(_observations[index], 16).PadLeft(2, '0');
                
                //if (!state.Name.StartsWith("Binary"))
                //    Console.WriteLine("{0}\t:\t{1}\t{2}", state, observation, (char)_observations[index]);
#endif
            }
        }

        private double[] GetStartProbabilities(int index)
        {
            var newProbs = new double[_states.Count];

            for (int i = 0; i < newProbs.Length; i++)
            {
                newProbs[i] = double.MinValue;
            }

            for (int i = 0; i < _startState.TransitionsOut.Count; i++)
            {
                Transition currentTransition = _startState.TransitionsOut[i];
                State toState = currentTransition.ToState;

                double emissionProb = toState.GetValueProbability(_observations, index, this);
                double transitionProb = currentTransition.Probability;

                if (emissionProb == State.ALMOST_ZERO)
                    newProbs[toState.ListIndex] = double.MinValue;
                else
                    newProbs[toState.ListIndex] = Math.Log(emissionProb) + Math.Log(transitionProb);
                       
            }

            return newProbs;
        }

        private double[] ProcessObservation(double[] priorProbs, int index)
        {
            double[] newProbs = new double[_states.Count];
            double maxProb;
            int bestFromStateIndex;
            State currentState;

            for (int stateIndex = 0; stateIndex < _states.Count; stateIndex++)
            {
                maxProb = double.MinValue;
                bestFromStateIndex= -1;
                currentState = _states[stateIndex];

                Transition currentTransition;
                double priorProb;

                //Process each edge with its endpoint in the current state
                for (int i = 0; i < currentState.TransitionsIn.Count; i++)
                {
                    currentTransition = currentState.TransitionsIn[i];
                    FromState = currentTransition.FromState;

                    if (FromState == _startState)
                        continue;

                    priorProb = priorProbs[FromState.ListIndex];
                    
                    //If the priorProbility is zero, then we do not need to calculate the emission probability.
                    double emissionProb = (priorProb == double.MinValue) ? double.MinValue : currentState.GetValueProbability(_observations, index, this);
                    double transitionProb = currentTransition.Probability;
                    
                    double prob;

                    if (priorProb == double.MinValue || emissionProb == State.ALMOST_ZERO)
                        prob = double.MinValue;
                    else
                    {
                        prob = Math.Log(emissionProb) + Math.Log(transitionProb) + priorProb;
                    }

                    if (prob > maxProb)
                    {
                        maxProb = prob;
                        bestFromStateIndex = FromState.ListIndex;

                    }
                }

                //Console.WriteLine("Observation {0}: The greatest prob was for {1} from {2} with a probability of {3}", new object[]{observation, currentState, _states[bestFromStateIndex], maxProb});
                _bestPrevState[currentState.ListIndex][index] = bestFromStateIndex;

                //After each incoming edge has been processed, store the max probability found.
                newProbs[stateIndex] = maxProb;
            }

            return newProbs;
        }

        #endregion

        #region Property Accessors

        public List<string> FieldStrings
        {
            get { return _textList; }
        }

        public List<ViterbiField> Fields
        {
            get { return _fieldList; }
        }

        internal State FromState { get; set; }

        internal byte[] Observations
        {
            get { return _observations; }
        }

        #endregion
    }
}
