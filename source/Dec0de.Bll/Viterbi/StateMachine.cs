using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dec0de.Bll.UserStates;

//using System.Linq;

namespace Dec0de.Bll.Viterbi
{
    public class StateMachine
    {

        #region Declarations

        /// <summary>
        /// List of states in the state machine.
        /// </summary>
        private List<State> _states = new List<State>();
        /// <summary>
        /// The name of the state machine.
        /// </summary>
        public MachineList Name { get; set; }
        /// <summary>
        /// Starting state of the state machine.
        /// </summary>
        public State StartingState { get; set; }
        /// <summary>
        /// List of possible starting states of the state machine.
        /// </summary>
        public List<State> StartingStates { get; set; }
        /// <summary>
        /// List of possible ending states of the state machine.
        /// </summary>
        public List<State> EndingStates { get; set; }
        /// <summary>
        /// Prior probability of the observations being from this state machine.
        /// </summary>
        public double Probability { get; set; }
        private int _weight = 1;

        #endregion

        #region Instantiation

        public StateMachine()
        {
            StartingStates = new List<State>();
            EndingStates = new List<State>();
        }

        #endregion

        #region Test Methods

        public static void TestStateMachine(StateMachine machineToTest, ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            TestStateMachines(new List<StateMachine> { machineToTest }, ref machines, ref states, ref startState);
        }

        /// <summary>
        /// Uses the state machines in the list initialized by GeneralParse() method and adds transitions to the beginnings 
        /// and endings of those state machines, so as to get the final, aggregated HMM.
        /// </summary>
        /// <param name="machinesToTest">List of state machines to be aggregated.</param>
        /// <param name="machines">List of state machines to be aggregated, with the ListIndex field of each state in each state machine, storing its serial number.</param>
        /// <param name="states">List of all states present in the aggregated HMM.</param>
        /// <param name="startState">The starting state of the final, aggreagted HMM.</param>
        public static void TestStateMachines(List<StateMachine> machinesToTest, ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            StateMachine start = GetStart();
            StateMachine binary = GetBinary();
            //StateMachine binaryFF = GetBinaryFF();

            binary.Probability = 0.1d;   //0.9
            //binaryFF.Probability = 0.01d;
            double otherMachinesProbability = 1d - binary.Probability; // -binaryFF.Probability;

            startState = start.StartingState;

            machines.Add(binary);
            //machines.Add(binaryFF);

            int weightSum = (from machine in machinesToTest select machine._weight).Sum();



            for (int i = 0; i < machinesToTest.Count; i++)
            {

                Console.WriteLine("Testing machine {0}", machinesToTest[i].Name);

                machinesToTest[i].Probability = (otherMachinesProbability * machinesToTest[i]._weight) / weightSum;

                machines.Add(machinesToTest[i]);


            }

            AddStatesToMainList(ref machines, ref states);

            //Need to add transitions to and from all state machines. Exception, don't add transition from binary to itself
            for (int i = 0; i < machines.Count; i++)
            {
                AddTransitionToStateMachine(start, machines[i], machines[i].Probability);

                for (int j = 0; j < machines.Count; j++)
                {
                    if (machines[i] == binary && machines[j] == binary)
                        continue;
                    AddTransitionToStateMachine(machines[i], machines[j], machines[j].Probability);
                }

                for (int j = 0; j < machines[i].EndingStates.Count; j++)
                {
                    machines[i].EndingStates[j].IsEndingState = true;
                }
            }
        }

        public static void TestMotoPhoneOnly(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetPhoneNumber_MotoSevenDigit(),
                                       GetPhoneNumber_MotoTenDigit(),
                                       GetPhoneNumber_MotoElevenDigit()
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        public static void TestAnchorFieldsOnly(ref List<StateMachine> machines, ref List<State> states, ref State startState,
            List<UserState> userStates)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetPhoneNumber_All(8, userStates)
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        public static void TestPhoneOnly(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetPhoneNumber_All(1)
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        public static void TestBinaryOnly(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }


        public static void TestPhoneNumberAndText(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetPhoneNumber_All(1),
                                       GetText(1)
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        public static void TestTextOnly(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetText(1)
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        public static void TestPhoneNumberAndTextMachine(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetPhoneNumberAndText(5),
                                       GetPhoneNumber_All(1)
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        public static void TestPhoneNumberTextAndTimeStamp(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetText(2),
                                       GetPhoneNumber_All(3),
                                       GetTimeStamp_All(1),
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        /// <summary>
        /// Initializes a list of state machines, to be aggregated together to form the final HMM for Viterbi inference at record level.
        /// </summary>
        /// <param name="machines">Stores the list of state machines which are aggregated.</param>
        /// <param name="states">List of all the states, present in the aggregated HMM.</param>
        /// <param name="startState">The starting state of the HMM.</param>
        public static void TestMeta(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       
                                       GetMeta_AddressBook_Multi(1),
                                       GetMeta_AddressBookAll(1),
                                       GetMeta_CallLogAll(1),
                                       GetMeta_SmsGeneric(1),
                                       GetMeta_SmsGeneric1(1),
                                       GetMeta_SmsSamsung(1),
                                       GetMeta_SmsMotorola(1),
                                       GetMeta_SmsMotorola1(1)
                                   };

            TestMetaStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        /// <summary>
        /// Uses the state machines in the list initialized by TestMeta() method and adds transitions to the beginnings and endings of those state machines, so as to get the final, aggregated HMM.
        /// </summary>
        /// <param name="machinesToTest">List of state machines to be aggregated.</param>
        /// <param name="machines">List of state machines to be aggregated, with the ListIndex field of each state in each state machine, storing its serial number.</param>
        /// <param name="states">List of all states present in the aggregated HMM.</param>
        /// <param name="startState">The starting state of the final, aggreagted HMM.</param>
        public static void TestMetaStateMachines(List<StateMachine> machinesToTest, ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {

            StateMachine start = GetStart();
            StateMachine binary = GetMeta_Binary(1);


            ///TODO: Check this value
            binary.Probability = 0.1d;

            double otherMachinesProbability = 1d - binary.Probability;

            startState = start.StartingState;

            machines.Add(binary);

            int weightSum = (from machine in machinesToTest select machine._weight).Sum();

            for (int i = 0; i < machinesToTest.Count; i++)
            {
                Console.WriteLine("Testing machine {0}", machinesToTest[i].Name);

                machinesToTest[i].Probability = (otherMachinesProbability * machinesToTest[i]._weight) / weightSum;

                machines.Add(machinesToTest[i]);
            }

            AddStatesToMainList(ref machines, ref states);

            //Need to add transitions to and from all state machines.
            for (int i = 0; i < machines.Count; i++)
            {
                AddTransitionToStateMachine(start, machines[i], machines[i].Probability);

                for (int j = 0; j < machines.Count; j++)
                {
                    AddTransitionToStateMachine(machines[i], machines[j], machines[j].Probability);
                }

                for (int j = 0; j < machines[i].EndingStates.Count; j++)
                {
                    machines[i].EndingStates[j].IsEndingState = true;
                }
            }
        }

        /// <summary>
        /// Initializes a list of state machines, to be aggregated together to form the final HMM for Viterbi inference at field level.
        /// </summary>
        /// <param name="machines">Stores the list of state machines which are aggregated.</param>
        /// <param name="states">List of all the states, present in the aggregated HMM.</param>
        /// <param name="startState">The starting state of the HMM.</param>
        public static void GeneralParse(ref List<StateMachine> machines, ref List<State> states, ref State startState,
            List<UserState> userStates = null)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetText(10),
                                       GetPhoneNumber_All(8, userStates),
                                       GetTimeStamp_All(4),
                                       GetSamsungSMSMarker(3),
                                       GetNokiaRecordEnd(1),
                                       //Get7BitString_WithLength(8)
                                   };
            // Add user-defined states. GetPhoneNumber_All() will add the user-defined
            // phone states.
            if (userStates != null)
            {
                foreach (UserState us in userStates)
                {
                    if (us.MachineType == MachineList.TimeStamp_User)
                    {
                        testMachines.Add(GetTimestamp_UserDefined(us, 4));
                    }
                    else if (us.MachineType == MachineList.Text_User)
                    {
                        testMachines.Add(GetText_UserDefined(us, 8));
                    }
                }
            }

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        /// <summary>
        /// Initializes a list of state machines, to be aggregated together to form the final HMM for Viterbi inference at field level.
        /// </summary>
        /// <param name="machines">Stores the list of state machines which are aggregated.</param>
        /// <param name="states">List of all the states, present in the aggregated HMM.</param>
        /// <param name="startState">The starting state of the HMM.</param>
        /// <param name="userStates">Not used.</param>
        public static void SqliteLiteOnly(ref List<StateMachine> machines, ref List<State> states, ref State startState,
            List<UserState> userStates = null)
        {
            var testMachines = new List<StateMachine>
                                   {
                                        GetSqliteRecord(100)
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }


        public static void TestMoto(ref List<StateMachine> machines, ref List<State> states, ref State startState)
        {
            var testMachines = new List<StateMachine>
                                   {
                                       GetText(10),
                                        GetPhoneNumber_MotoSevenUnicode(),
                                        GetPhoneNumber_MotoTenUnicode(),
                                        GetPhoneNumber_MotoElevenUnicode(),
                                        GetPhoneNumber_MotoElevenDigit(),
                                        GetPhoneNumber_MotoSevenDigit(),
                                        GetPhoneNumber_MotoTenDigit(),
                                        GetCallLog_MotoTypeAndTime(1),
                                        GetTimestamp_Sms(1),
                                        GetPhoneNumber_InternationalFormatSevenDigit(),
                                        GetPhoneNumber_InternationalFormatTenDigit(),
                                        GetPhoneNumber_InternationalFormatElevenDigit()
                                   };

            TestStateMachines(testMachines, ref machines, ref states, ref startState);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Find the probability of a random match for a given state machine
        /// </summary>
        public double GetRandomMatchProbability()
        {
            double totalProb = 1d;

            for (int i = 0; i < _states.Count; i++)
            {
                double stateProb;

                if (_states[i].AllValuesPossible)
                    stateProb = 1;
                else
                {
                    stateProb = _states[i].PossibleValueProbabilities.Where(r => r > State.ALMOST_ZERO).Count() / 256d;
                }

                totalProb *= stateProb;
            }

            return totalProb;
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Adds every state from all the input state machines into the states variable.
        /// </summary>
        /// <param name="machines">List of all the state machines, whose states are to be stored in states variable.</param>
        /// <param name="states">Stores the list of states from all the input state machines.</param>
        private static void AddStatesToMainList(ref List<StateMachine> machines, ref List<State> states)
        {
            for (int i = 0; i < machines.Count; i++)
            {
                for (int j = 0; j < machines[i]._states.Count; j++)
                {
                    machines[i]._states[j].ListIndex = states.Count;
                    states.Add(machines[i]._states[j]);
                }
            }
        }

        /// <summary>
        /// Adds a transition to a between states of two different state machines.
        /// </summary>
        /// <param name="fromState">The state from which the transition occurs.</param>
        /// <param name="toState">The state to which the transition occurs.</param>
        /// <param name="probability">The probability of transition.</param>
        private static void AddTransition(State fromState, State toState, double probability)
        {
            fromState.AddTransition(fromState, toState, probability);

            //We do not want to add the same transition twice to the same state
            //if (toState != fromState)

            toState.AddTransition(fromState, toState, probability);
        }

        /// <summary>
        /// Adding transitions between all combinations of starting and ending states of two state machines.
        /// </summary>
        /// <param name="fromMachine">The state machine whose ending states participate in the transition.</param>
        /// <param name="toMachine">The state machine whose starting states participate in the transition.</param>
        /// <param name="probability">The probability of having a transition between the two given state machines.</param>
        private static void AddTransitionToStateMachine(StateMachine fromMachine, StateMachine toMachine, double probability)
        {
            double normalizedProb_To = probability / toMachine.StartingStates.Count;

            for (int i = 0; i < fromMachine.EndingStates.Count; i++)
            {
                var endState = fromMachine.EndingStates[i];

                double normalizedProb_From = endState.RemainingProbability * normalizedProb_To;

                for (int j = 0; j < toMachine.StartingStates.Count; j++)
                {
                    var startState = toMachine.StartingStates[j];

                    AddTransition(endState, startState, normalizedProb_From);
                }
            }
        }

        /// <summary>
        /// Defines the value probabilities for a user defined byte.
        /// </summary>
        /// <param name="state">State machine.</param>
        /// <param name="userByte">UserByte object representing the byte.</param>
        public static void UserDefinedByteProbabilities(State state, UserByte userByte)
        {
            if (userByte.All)
            {
                state.IsBinary = true;
            }
            else
            {
                foreach (byte b in userByte.Values)
                {
                    state.PossibleValueProbabilities[b] = 1d;
                }
                state.NormalizeProbabilities();
            }
        }

        #endregion

        #region Machines

        #region Record Level

        /// <summary>
        /// Gets the meta state machine for an SMS on a Samsung phone.
        /// </summary>
        /// <param name="weight">Weight of Meta_SmsSamsung state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to Meta_SmsSamsung state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_SmsSamsung(int weight)
        {
            StateMachine metaSmsSent = new StateMachine { Name = MachineList.Meta_Sms, _weight = weight };

            State marker = new State { Name = "Marker", ParentStateMachine = metaSmsSent };
            State binary = new State { Name = "Binary", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaSmsSent, IsBinary = true };
            State prepend = new State { Name = "Prepend", ParentStateMachine = metaSmsSent };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaSmsSent };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaSmsSent };

            metaSmsSent.AddState(marker);
            metaSmsSent.AddState(binary);
            metaSmsSent.AddState(phoneNumber);
            metaSmsSent.AddState(prepend);
            metaSmsSent.AddState(timeStamp);
            metaSmsSent.AddState(binary1);

            metaSmsSent.StartingStates.Add(marker);
            metaSmsSent.EndingStates.Add(timeStamp);

            AddTransition(marker, binary, 1f);
            AddTransition(binary, binary, 0.01f);
            AddTransition(binary, prepend, 0.99f);
            AddTransition(prepend, phoneNumber, 1d);
            AddTransition(phoneNumber, binary1, 1d);
            AddTransition(binary1, binary1, 0.01d);
            AddTransition(binary1, timeStamp, 0.99d);

            marker.PossibleValueProbabilities[(byte)MetaMachine.MarkerSamsungSms] = 1d;
            binary.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            prepend.PossibleValueProbabilities[(byte)MetaMachine.SmsPrepend] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaSmsSent;
        }

        /// <summary>
        /// Gets the meta state machine for an SMS on a Motorola phone.
        /// </summary>
        /// <param name="weight">Weight of Meta_SmsMotorola state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to Meta_SmsMotorola state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_SmsMotorola(int weight)
        {
            StateMachine metaSmsSent = new StateMachine { Name = MachineList.Meta_Sms, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaSmsSent };
            State binary = new State { Name = "Binary", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaSmsSent, IsBinary = true };
            State prepend = new State { Name = "Prepend", ParentStateMachine = metaSmsSent };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaSmsSent };
            State phoneNumber1 = new State { Name = "PhoneNumber1", ParentStateMachine = metaSmsSent };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaSmsSent };

            metaSmsSent.AddState(text);
            metaSmsSent.AddState(binary);
            metaSmsSent.AddState(phoneNumber);
            metaSmsSent.AddState(phoneNumber1);
            metaSmsSent.AddState(prepend);
            metaSmsSent.AddState(timeStamp);
            metaSmsSent.AddState(binary1);
            metaSmsSent.AddState(binary2);

            metaSmsSent.StartingStates.Add(timeStamp);
            metaSmsSent.EndingStates.Add(text);

            AddTransition(timeStamp, binary, 0.8f);
            AddTransition(timeStamp, prepend, 0.2f);
            AddTransition(binary, prepend, 1f);
            AddTransition(prepend, phoneNumber, 1f);
            AddTransition(phoneNumber, binary1, 0.8f);
            AddTransition(phoneNumber, phoneNumber1, 0.2f);
            AddTransition(binary1, phoneNumber1, 1f);
            AddTransition(phoneNumber1, binary2, 0.8f);
            AddTransition(phoneNumber1, text, 0.2f);
            AddTransition(binary2, text, 1f);

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            phoneNumber1.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            prepend.PossibleValueProbabilities[(byte)MetaMachine.SmsPrepend] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaSmsSent;
        }

        /// <summary>
        /// Gets the meta state machine for an SMS on a Motorola phone.
        /// </summary>
        /// <param name="weight">Weight of Meta_SmsMotorola state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to Meta_SmsMotorola state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_SmsMotorola1(int weight)
        {
            StateMachine metaSmsSent = new StateMachine { Name = MachineList.Meta_Sms, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaSmsSent };
            State binary = new State { Name = "Binary", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaSmsSent, IsBinary = true };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaSmsSent };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaSmsSent };

            metaSmsSent.AddState(text);
            metaSmsSent.AddState(binary);
            metaSmsSent.AddState(phoneNumber);
            metaSmsSent.AddState(timeStamp);
            metaSmsSent.AddState(binary1);

            metaSmsSent.StartingStates.Add(timeStamp);
            metaSmsSent.EndingStates.Add(text);

            AddTransition(timeStamp, binary, 1f);
            AddTransition(binary, phoneNumber, 1f);
            AddTransition(phoneNumber, binary1, 0.8f);
            AddTransition(phoneNumber, text, 0.2f);
            AddTransition(binary1, text, 1f);

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaSmsSent;
        }

        /// <summary>
        /// Gets the meta state machine for a binary byte.
        /// </summary>
        /// <param name="weight">Weight of Binary state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to Binary state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_Binary(int weight)
        {
            StateMachine binary = new StateMachine { Name = MachineList.Meta_Binary, _weight = weight };

            State binaryByte = new State { Name = "BinaryByte", ParentStateMachine = binary, AllValuesPossible = true, IsBinary = true };

            binary.AddState(binaryByte);
            binary.StartingStates.Add(binaryByte);
            binary.EndingStates.Add(binaryByte);

            return binary;
        }

        /// <summary>
        /// Gets the meta state machine for an address book entry.
        /// </summary>
        /// <param name="weight">Weight of AddressBook state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to AddressBook state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_AddressBook1(int weight)
        {
            StateMachine metaAddressBook = new StateMachine { Name = MachineList.Meta_AddressBook, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaAddressBook };
            State binary = new State { Name = "Binary", ParentStateMachine = metaAddressBook, IsBinary = true };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaAddressBook };


            metaAddressBook.AddState(text);
            metaAddressBook.AddState(binary);

            metaAddressBook.AddState(phoneNumber);


            metaAddressBook.StartingStates.Add(phoneNumber);
            metaAddressBook.EndingStates.Add(text);


            AddTransition(phoneNumber, binary, 0.5f);
            AddTransition(phoneNumber, text, 0.5f);
            AddTransition(binary, text, 1.0f);



            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1f;

            binary.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;

            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;


            return metaAddressBook;
        }

        /// <summary>
        /// Gets the meta state machine for an address book entry.
        /// </summary>
        /// <param name="weight">Weight of AddressBook state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to AddressBook state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_AddressBook(int weight)
        {
            StateMachine metaAddressBook = new StateMachine { Name = MachineList.Meta_AddressBook, _weight = weight };

            State text0 = new State { Name = "Text0", ParentStateMachine = metaAddressBook };
            State text = new State { Name = "Text", ParentStateMachine = metaAddressBook };
            State binary = new State { Name = "Binary", ParentStateMachine = metaAddressBook, IsBinary = true };
            State binary0 = new State { Name = "Binary0", ParentStateMachine = metaAddressBook, IsBinary = true };
            State prepend = new State { Name = "Prepend", ParentStateMachine = metaAddressBook };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaAddressBook };


            metaAddressBook.AddState(text);
            metaAddressBook.AddState(binary);
            metaAddressBook.AddState(binary0);
            metaAddressBook.AddState(phoneNumber);
            metaAddressBook.AddState(text0);
            metaAddressBook.AddState(prepend);

            metaAddressBook.StartingStates.Add(text);
            metaAddressBook.StartingStates.Add(text0);
            metaAddressBook.EndingStates.Add(phoneNumber);

            AddTransition(text0, binary0, 1f);
            AddTransition(binary0, text, 1f); //Allow a separation of binary between the two text fields
            AddTransition(text, binary, 0.5f);
            AddTransition(text, phoneNumber, 0.5f);
            AddTransition(binary, prepend, 0.5f);
            AddTransition(binary, phoneNumber, 0.49f);
            AddTransition(binary, binary, 0.01f);
            AddTransition(prepend, phoneNumber, 1f);
            AddTransition(phoneNumber, binary, 0.5f);

            phoneNumber.RemainingProbability = 0.5f;

            text0.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1f;

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1f;

            binary0.PossibleValueProbabilities[(byte)MachineList.Binary] = 1f;

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;

            binary.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;

            prepend.PossibleValueProbabilities[(byte)MetaMachine.SmsPrepend] = 1d;

            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;

            return metaAddressBook;
        }

        /// <summary>
        /// Gets the meta state machine for an address book entry of a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of AddressBook_Nokia state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to AddressBook_Nokia state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_AddressBook_Nokia(int weight)
        {
            StateMachine metaAddressBook = new StateMachine { Name = MachineList.Meta_AddressBookNokia, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaAddressBook };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaAddressBook, IsBinary = true };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaAddressBook, IsBinary = true };
            State index = new State { Name = "NumberIndex", ParentStateMachine = metaAddressBook };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaAddressBook };


            metaAddressBook.AddState(text);
            metaAddressBook.AddState(binary1);
            metaAddressBook.AddState(binary2);
            metaAddressBook.AddState(phoneNumber);
            metaAddressBook.AddState(index);

            metaAddressBook.StartingStates.Add(text);
            metaAddressBook.EndingStates.Add(phoneNumber);

            AddTransition(text, binary1, 1d);
            AddTransition(binary1, index, 1d);
            AddTransition(index, binary2, 1d);
            AddTransition(binary2, phoneNumber, 1d);
            AddTransition(phoneNumber, binary1, 0.5d);

            phoneNumber.RemainingProbability = 0.5d;

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;

            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            index.PossibleValueProbabilities[(byte)MetaMachine.CallLogNumberIndex] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;

            return metaAddressBook;
        }

        /// <summary>
        /// Gets the meta state machine for a generic SMS.
        /// </summary>
        /// <param name="weight">Weight of Sms_Generic state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to Sms_Generic state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_SmsGeneric(int weight)
        {
            StateMachine metaSmsSent = new StateMachine { Name = MachineList.Meta_Sms, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaSmsSent };
            State binary = new State { Name = "Binary", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaSmsSent, IsBinary = true };
            State prepend = new State { Name = "Prepend", ParentStateMachine = metaSmsSent };
            State prepend1 = new State { Name = "Prepend1", ParentStateMachine = metaSmsSent };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaSmsSent };
            State phoneNumber1 = new State { Name = "PhoneNumber1", ParentStateMachine = metaSmsSent };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaSmsSent };

            metaSmsSent.AddState(text);
            metaSmsSent.AddState(binary);
            metaSmsSent.AddState(phoneNumber);
            metaSmsSent.AddState(phoneNumber1);
            metaSmsSent.AddState(prepend);
            metaSmsSent.AddState(prepend1);
            metaSmsSent.AddState(timeStamp);
            metaSmsSent.AddState(binary1);
            metaSmsSent.AddState(binary2);

            metaSmsSent.StartingStates.Add(prepend);
            metaSmsSent.EndingStates.Add(timeStamp);
            metaSmsSent.EndingStates.Add(text);

            AddTransition(prepend, phoneNumber, 1d);
            AddTransition(phoneNumber, binary, 0.5d);
            AddTransition(phoneNumber, prepend, 0.5d);
            AddTransition(binary, binary, 0.01d);
            AddTransition(binary, prepend1, 0.99d);
            AddTransition(prepend1, phoneNumber1, 1d);
            AddTransition(phoneNumber1, binary1, 0.5d);
            AddTransition(phoneNumber1, timeStamp, 0.5d);
            AddTransition(binary1, binary1, 0.01d);
            AddTransition(binary1, timeStamp, 0.50d);
            AddTransition(binary1, text, 0.49d);
            AddTransition(timeStamp, binary2, 0.5d);
            AddTransition(timeStamp, text, 0.5d);
            AddTransition(binary2, binary2, 0.01d);
            AddTransition(binary2, text, 0.99d);

            //timeStamp.RemainingProbability = 0.01d;

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            phoneNumber1.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            prepend.PossibleValueProbabilities[(byte)MetaMachine.SmsPrepend] = 1d;
            prepend1.PossibleValueProbabilities[(byte)MetaMachine.SmsPrepend] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaSmsSent;
        }

        /// <summary>
        /// Gets the meta state machine for a generic SMS.
        /// </summary>
        /// <param name="weight">Weight of Sms_Generic state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to Sms_Generic state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_SmsGeneric1(int weight)
        {
            StateMachine metaSmsSent = new StateMachine { Name = MachineList.Meta_Sms, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaSmsSent };
            State binary = new State { Name = "Binary", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaSmsSent, IsBinary = true };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaSmsSent, IsBinary = true };
            State prepend = new State { Name = "Prepend", ParentStateMachine = metaSmsSent };
            State prepend1 = new State { Name = "Prepend1", ParentStateMachine = metaSmsSent };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaSmsSent };
            State phoneNumber1 = new State { Name = "PhoneNumber1", ParentStateMachine = metaSmsSent };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaSmsSent };

            metaSmsSent.AddState(text);
            metaSmsSent.AddState(binary);
            metaSmsSent.AddState(phoneNumber);
            metaSmsSent.AddState(phoneNumber1);
            metaSmsSent.AddState(prepend);
            metaSmsSent.AddState(prepend1);
            metaSmsSent.AddState(timeStamp);
            metaSmsSent.AddState(binary1);
            metaSmsSent.AddState(binary2);

            metaSmsSent.StartingStates.Add(timeStamp);
            metaSmsSent.EndingStates.Add(phoneNumber1);
            metaSmsSent.EndingStates.Add(text);

            AddTransition(timeStamp, binary, 0.5d);
            AddTransition(timeStamp, prepend, 0.5d);
            AddTransition(binary, binary, 0.01d);
            AddTransition(binary, prepend, 0.99d);
            AddTransition(prepend, phoneNumber, 1d);
            AddTransition(phoneNumber, binary1, 0.5d);
            AddTransition(phoneNumber, prepend, 0.5d);
            AddTransition(binary1, binary1, 0.01d);
            AddTransition(binary1, prepend1, 0.99d);
            AddTransition(prepend1, phoneNumber1, 1d);
            AddTransition(phoneNumber1, binary2, 0.50d);
            AddTransition(phoneNumber1, text, 0.50d);
            AddTransition(binary2, binary2, 0.01d);
            AddTransition(binary2, text, 0.99d);

            //phoneNumber1.RemainingProbability = 0.50d;

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            phoneNumber1.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            prepend.PossibleValueProbabilities[(byte)MetaMachine.SmsPrepend] = 1d;
            prepend1.PossibleValueProbabilities[(byte)MetaMachine.SmsPrepend] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaSmsSent;
        }


        /// <summary>
        /// Gets the meta state machine for a multiple address book entries.
        /// </summary>
        /// <param name="weight">Weight of AddressBook_Multi state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to AddressBook_Multi state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_AddressBook_Multi(int weight)
        {
            StateMachine addressBookMulti = new StateMachine { Name = MachineList.Meta_AddressBookMulti, _weight = weight };
            StateMachine binary = GetMeta_Binary(1);
            StateMachine addressBookAll1 = GetMeta_AddressBookAll(1);
            StateMachine addressBookAll2 = GetMeta_AddressBookAll(1);

            addressBookMulti.StartingStates.AddRange(addressBookAll1.StartingStates);
            addressBookMulti.EndingStates.AddRange(addressBookAll2.EndingStates);

            AddTransitionToStateMachine(addressBookAll1, binary, 1d);
            AddTransitionToStateMachine(binary, addressBookAll2, 1d);

            addressBookMulti.AddState(binary._states);
            addressBookMulti.AddState(addressBookAll1._states);
            addressBookMulti.AddState(addressBookAll2._states);

            return addressBookMulti;
        }

        /// <summary>
        /// Gets the meta state machine for any possible address book record.
        /// </summary>
        /// <param name="weight">Weight of AddressBookAll state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to AddressBookAll state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_AddressBookAll(int weight)
        {
            StateMachine addressBookAll = new StateMachine { Name = MachineList.Meta_AddressBookAll, _weight = weight };

            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                  GetMeta_AddressBook_Nokia(4),
                                                  GetMeta_AddressBook(2),
                                                  GetMeta_AddressBook1(1)
                                              };


            for (int i = 0; i < machines.Count; i++)
            {
                addressBookAll.StartingStates.AddRange(machines[i].StartingStates);
                addressBookAll.EndingStates.AddRange(machines[i].EndingStates);
                addressBookAll.AddState(machines[i]._states);
            }

            return addressBookAll;
        }

        /// <summary>
        /// Gets the meta state machine for any possible call log record, with states coming from several state machines corresponding to Motorola Call Log, Nokia Call Log, Samsung Call Log., Generic Call Log.
        /// </summary>
        /// <param name="weight">Weight of CallLogAll state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogAll state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogAll(int weight)
        {
            StateMachine timeStampAll = new StateMachine { Name = MachineList.Meta_CallLogAll, _weight = weight };

            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                    //GetMeta_CallLogNokiaSingle(2),
                                                    GetMeta_CallLogNokiaMulti_v2(2),
                                                    GetMeta_CallLogMoto(2),
                                                    GetMeta_CallLogSamsung(2),
                                                    GetMeta_CallLogGeneric(1)
                                                    ,GetMeta_CallLogGeneric2(1) // BL
                                                    ,GetMeta_CallLogGeneric3(1) // BL
                                                    ,GetMeta_CallLogGeneric4(1) // BL
                                              };


            for (int i = 0; i < machines.Count; i++)
            {
                timeStampAll.StartingStates.AddRange(machines[i].StartingStates);
                timeStampAll.EndingStates.AddRange(machines[i].EndingStates);
                timeStampAll.AddState(machines[i]._states);
            }

            return timeStampAll;
        }

        /// <summary>
        /// Gets the meta state machine for a generic Call Log.
        /// </summary>
        /// <param name="weight">Weight of CallLogGeneric state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogGeneric state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogGeneric(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogGeneric, _weight = weight };

            State textStart = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State text = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State binaryA = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State binaryB = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State phoneNumberStartWText = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State phoneNumberStart = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaCallLog, IsBinary = true };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };

            metaCallLog.AddState(text);
            metaCallLog.AddState(binaryA);
            metaCallLog.AddState(binaryB);
            metaCallLog.AddState(phoneNumber);
            metaCallLog.AddState(binary2);
            metaCallLog.AddState(timeStamp);
            metaCallLog.AddState(textStart);
            metaCallLog.AddState(phoneNumberStartWText);
            metaCallLog.AddState(phoneNumberStart);

            metaCallLog.StartingStates.Add(textStart);
            metaCallLog.StartingStates.Add(phoneNumberStart);
            metaCallLog.StartingStates.Add(phoneNumberStartWText);
            metaCallLog.EndingStates.Add(timeStamp);

            //Starting path txt -> number
            AddTransition(textStart, binaryA, 1f);
            AddTransition(binaryA, phoneNumber, 0.99f);
            AddTransition(binaryA, binaryA, 0.01f);
            AddTransition(phoneNumber, binary2, 1f);

            //starting path number -> txt
            AddTransition(phoneNumberStartWText, binaryB, 1f);
            AddTransition(binaryB, text, 0.99f);
            AddTransition(binaryB, binaryB, 0.01f);
            AddTransition(text, binary2, 1f);

            //starting path number -> no text
            AddTransition(phoneNumberStart, binary2, 1f);

            AddTransition(binary2, timeStamp, 0.99f);
            AddTransition(binary2, binary2, 0.01f);
            AddTransition(timeStamp, binary2, 0.9f);
            timeStamp.RemainingProbability = 0.1f;


            for (int i = 0; i < 256; i++)
            {
                binaryA.PossibleValueProbabilities[i] = 1f;
                binaryB.PossibleValueProbabilities[i] = 1f;
                binary2.PossibleValueProbabilities[i] = 1f;
            }

            binaryA.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryB.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binary2.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryA.NormalizeProbabilities();
            binaryB.NormalizeProbabilities();
            binary2.NormalizeProbabilities();

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1f;
            textStart.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1f;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1f;
            phoneNumberStart.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1f;
            phoneNumberStartWText.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1f;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1f;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a generic Call Log.
        /// </summary>
        /// <param name="weight">Weight of CallLogGeneric2 state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogGeneric2 state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogGeneric2(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogGeneric2, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State binary1 = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State binaryA = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State binaryB = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State binaryX = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumber1 = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State phoneNumberA = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State timeStamp1 = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };
            State timeStampA = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };

            metaCallLog.AddState(text);
            metaCallLog.AddState(binary1);
            metaCallLog.AddState(binaryA);
            metaCallLog.AddState(binaryB);
            metaCallLog.AddState(binaryX);
            metaCallLog.AddState(phoneNumber1);
            metaCallLog.AddState(phoneNumberA);
            metaCallLog.AddState(timeStamp1);
            metaCallLog.AddState(timeStampA);

            metaCallLog.StartingStates.Add(timeStamp1);
            metaCallLog.StartingStates.Add(timeStampA);
            metaCallLog.EndingStates.Add(phoneNumber1);
            metaCallLog.EndingStates.Add(text);

            //Starting path timestamp -> number
            AddTransition(timeStamp1, binary1, 1f);
            AddTransition(binary1, phoneNumber1, 1f);
            //AddTransition(binary1, binary1, 0.01f);
            AddTransition(phoneNumber1, binaryX, 1f);

            //Starting path timestamp -> number -> txt
            AddTransition(timeStampA, binaryA, 1f);
            AddTransition(binaryA, phoneNumberA, 1f);
            //AddTransition(binaryA, binaryA, 0.01f);
            AddTransition(phoneNumberA, binaryB, 1f);
            AddTransition(binaryB, text, 1f);
            //AddTransition(phoneNumberA, text, 1f);
            //AddTransition(binaryB, binaryB, 0.01f);
            AddTransition(text, binaryX, 1f);

            for (int i = 0; i < 256; i++)
            {
                binary1.PossibleValueProbabilities[i] = 1f;
                binaryA.PossibleValueProbabilities[i] = 1f;
                binaryB.PossibleValueProbabilities[i] = 1f;
                binaryX.PossibleValueProbabilities[i] = 1f;
            }

            binary1.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryA.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryB.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryX.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binary1.NormalizeProbabilities();
            binaryA.NormalizeProbabilities();
            binaryB.NormalizeProbabilities();
            binaryX.NormalizeProbabilities();

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1f;
            phoneNumber1.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1f;
            phoneNumberA.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1f;
            timeStamp1.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1f;
            timeStampA.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1f;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a generic Call Log.
        /// </summary>
        /// <param name="weight">Weight of CallLogGeneric3 state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogGeneric3 state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogGeneric3(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogGeneric3, _weight = weight };
            State binary1 = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State binaryX = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumber1 = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State timeStamp1 = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };

            metaCallLog.AddState(binary1);
            metaCallLog.AddState(binaryX);
            metaCallLog.AddState(phoneNumber1);
            metaCallLog.AddState(timeStamp1);

            metaCallLog.StartingStates.Add(timeStamp1);
            metaCallLog.EndingStates.Add(phoneNumber1);

            //Starting path timestamp -> number
            AddTransition(timeStamp1, binary1, 1f);
            AddTransition(binary1, phoneNumber1, 1f);
            //AddTransition(binary1, binary1, 0.01f);
            AddTransition(phoneNumber1, binaryX, 1f);

            for (int i = 0; i < 256; i++)
            {
                binary1.PossibleValueProbabilities[i] = 1f;
                binaryX.PossibleValueProbabilities[i] = 1f;
            }

            binary1.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryX.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binary1.NormalizeProbabilities();
            binaryX.NormalizeProbabilities();

            phoneNumber1.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1f;
            timeStamp1.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1f;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a generic Call Log.
        /// </summary>
        /// <param name="weight">Weight of CallLogGeneric4 state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogGeneric4 state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogGeneric4(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogGeneric4, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State binaryA = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State binaryB = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State binaryX = new State { Name = "Binary", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumberA = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State timeStampA = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };

            metaCallLog.AddState(text);
            metaCallLog.AddState(binaryA);
            metaCallLog.AddState(binaryB);
            metaCallLog.AddState(binaryX);
            metaCallLog.AddState(phoneNumberA);
            metaCallLog.AddState(timeStampA);

            metaCallLog.StartingStates.Add(timeStampA);
            metaCallLog.EndingStates.Add(text);

            //Starting path timestamp -> number -> txt
            AddTransition(timeStampA, binaryA, 1f);
            AddTransition(binaryA, phoneNumberA, 1f);
            //AddTransition(binaryA, binaryA, 0.01f);
            AddTransition(phoneNumberA, binaryB, 1f);
            AddTransition(binaryB, text, 1f);
            //AddTransition(phoneNumberA, text, 1f);
            //AddTransition(binaryB, binaryB, 0.01f);
            AddTransition(text, binaryX, 1f);

            for (int i = 0; i < 256; i++)
            {
                binaryA.PossibleValueProbabilities[i] = 1f;
                binaryB.PossibleValueProbabilities[i] = 1f;
                binaryX.PossibleValueProbabilities[i] = 1f;
            }


            binaryA.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryA.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryB.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryB.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryX.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.Text] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 0f;
            binaryX.PossibleValueProbabilities[(byte)MetaMachine.BinaryLarge] = 0f;

            binaryA.NormalizeProbabilities();
            binaryB.NormalizeProbabilities();
            binaryX.NormalizeProbabilities();

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1f;
            phoneNumberA.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1f;
            timeStampA.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1f;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a call log record on a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of CallLogNokiaSingle state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogNokiaSingle state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogNokiaSingle(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogNokiaSingle, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaCallLog, IsBinary = true };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };

            metaCallLog.AddState(text);
            metaCallLog.AddState(binary1);
            metaCallLog.AddState(binary2);
            metaCallLog.AddState(phoneNumber);
            metaCallLog.AddState(timeStamp);

            metaCallLog.StartingStates.Add(phoneNumber);
            metaCallLog.EndingStates.Add(timeStamp);


            AddTransition(phoneNumber, binary1, 1d);
            AddTransition(binary1, text, 1d);
            AddTransition(text, binary2, 1d);
            AddTransition(binary2, timeStamp, 1d);

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a call log record on a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of CallLogNokiaMulti state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogNokiaMulti state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogNokiaMulti(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogNokiaMulti, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaCallLog, IsBinary = true };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };

            metaCallLog.AddState(text);
            metaCallLog.AddState(binary1);
            metaCallLog.AddState(binary2);
            metaCallLog.AddState(phoneNumber);
            metaCallLog.AddState(timeStamp);

            metaCallLog.StartingStates.Add(text);
            metaCallLog.EndingStates.Add(timeStamp);


            AddTransition(text, binary1, 1d);
            AddTransition(binary1, phoneNumber, 1d);
            AddTransition(phoneNumber, binary2, 1d);
            AddTransition(binary2, timeStamp, 1d);
            AddTransition(timeStamp, binary2, 0.99d);

            timeStamp.RemainingProbability = 0.01d;

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a call log record on a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of CallLogNokiaMulti_v2 state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogNokiaMulti_v2 state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogNokiaMulti_v2(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogNokiaMulti_v2, _weight = weight };

            State text = new State { Name = "Text_clm", ParentStateMachine = metaCallLog };
            State binary1 = new State { Name = "Binary1_clm", ParentStateMachine = metaCallLog, IsBinary = true };
            State binary2 = new State { Name = "Binary2_clm", ParentStateMachine = metaCallLog, IsBinary = true };
            State binary3 = new State { Name = "Binary3_clm", ParentStateMachine = metaCallLog, IsBinary = true };
            State index1 = new State { Name = "NumberIndex1_clm", ParentStateMachine = metaCallLog };
            State index2 = new State { Name = "NumberIndex2_clm", ParentStateMachine = metaCallLog };
            State phoneNumber = new State { Name = "PhoneNumber_clm", ParentStateMachine = metaCallLog };
            State timeStamp = new State { Name = "TimeStamp_clm", ParentStateMachine = metaCallLog };


            metaCallLog.AddState(text);
            metaCallLog.AddState(binary1);
            metaCallLog.AddState(binary2);
            metaCallLog.AddState(binary3);
            metaCallLog.AddState(phoneNumber);
            metaCallLog.AddState(timeStamp);
            metaCallLog.AddState(index1);
            metaCallLog.AddState(index2);

            metaCallLog.StartingStates.Add(text);
            metaCallLog.StartingStates.Add(index1);
            metaCallLog.EndingStates.Add(index2);


            AddTransition(text, binary1, 1d);
            AddTransition(binary1, index1, 1d);
            AddTransition(index1, binary2, 1d);
            AddTransition(binary2, phoneNumber, 1d);
            AddTransition(phoneNumber, binary3, 0.5d);
            AddTransition(phoneNumber, binary1, 0.5d);
            AddTransition(binary3, timeStamp, 1d);
            AddTransition(timeStamp, index2, 1d);
            AddTransition(index2, binary3, 0.99d);

            index2.RemainingProbability = 0.01d;



            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary3.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            index1.PossibleValueProbabilities[(byte)MetaMachine.CallLogNumberIndex] = 1d;
            index2.PossibleValueProbabilities[(byte)MetaMachine.CallLogNumberIndex] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a call log record on a Motorola phone.
        /// </summary>
        /// <param name="weight">Weight of CallLogMoto state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogMoto state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogMoto(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogMoto, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaCallLog, IsBinary = true };
            State typePrepend = new State { Name = "TypePrepend", ParentStateMachine = metaCallLog };
            State type = new State { Name = "Type", ParentStateMachine = metaCallLog };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };

            metaCallLog.AddState(text);
            metaCallLog.AddState(binary1);
            metaCallLog.AddState(phoneNumber);
            metaCallLog.AddState(binary2);
            metaCallLog.AddState(typePrepend);
            metaCallLog.AddState(type);
            metaCallLog.AddState(timeStamp);

            metaCallLog.StartingStates.Add(text);
            metaCallLog.StartingStates.Add(phoneNumber);
            metaCallLog.EndingStates.Add(timeStamp);


            AddTransition(text, binary1, 1d);
            AddTransition(binary1, phoneNumber, 1d);
            AddTransition(phoneNumber, binary2, 1d);
            AddTransition(binary2, typePrepend, 1d);
            AddTransition(typePrepend, type, 1d);
            AddTransition(type, timeStamp, 1d);

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            typePrepend.PossibleValueProbabilities[(byte)MetaMachine.CallLogTypePrepend] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;
            type.PossibleValueProbabilities[(byte)MetaMachine.CallLogType] = 1d;

            return metaCallLog;
        }

        /// <summary>
        /// Gets the meta state machine for a call log record on a Samsung phone.
        /// </summary>
        /// <param name="weight">Weight of CallLogSamsung state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogSamsung state machine.</param>
        /// <returns></returns>
        public static StateMachine GetMeta_CallLogSamsung(int weight)
        {
            StateMachine metaCallLog = new StateMachine { Name = MachineList.Meta_CallLogSamsung, _weight = weight };

            State text = new State { Name = "Text", ParentStateMachine = metaCallLog };
            State binary1 = new State { Name = "Binary1", ParentStateMachine = metaCallLog, IsBinary = true };
            State phoneNumber = new State { Name = "PhoneNumber", ParentStateMachine = metaCallLog };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = metaCallLog, IsBinary = true };
            State type = new State { Name = "Type", ParentStateMachine = metaCallLog };
            State timeStamp = new State { Name = "TimeStamp", ParentStateMachine = metaCallLog };
            State binary3 = new State { Name = "Binary3", ParentStateMachine = metaCallLog, IsBinary = true };

            metaCallLog.AddState(text);
            metaCallLog.AddState(binary1);
            metaCallLog.AddState(phoneNumber);
            metaCallLog.AddState(binary2);
            metaCallLog.AddState(type);
            metaCallLog.AddState(timeStamp);
            metaCallLog.AddState(binary3);

            metaCallLog.StartingStates.Add(text);
            metaCallLog.StartingStates.Add(phoneNumber);
            metaCallLog.EndingStates.Add(type);


            AddTransition(text, binary1, 1d);
            AddTransition(binary1, phoneNumber, 1d);
            AddTransition(phoneNumber, binary2, 1d);
            AddTransition(binary2, timeStamp, 1d);
            AddTransition(timeStamp, binary3, 1d);
            AddTransition(binary3, type, 1d);

            text.PossibleValueProbabilities[(byte)MetaMachine.Text] = 1d;
            binary1.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary2.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            binary3.PossibleValueProbabilities[(byte)MetaMachine.Binary] = 1d;
            phoneNumber.PossibleValueProbabilities[(byte)MetaMachine.PhoneNumber] = 1d;
            timeStamp.PossibleValueProbabilities[(byte)MetaMachine.TimeStamp] = 1d;
            type.PossibleValueProbabilities[(byte)MetaMachine.CallLogType] = 1d;

            return metaCallLog;
        }

        #endregion

        #region Text

        /// <summary>
        /// Gets the state machine for Text, consisting of states coming from state machines corresponding to ASCII string bigrams, Unicode strings and Unicode string endian.
        /// </summary>
        /// <param name="weight">Weight of GetText state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to GetText state machine.</param>
        /// <returns></returns>
        public static StateMachine GetText(int weight)
        {
            StateMachine text = new StateMachine { Name = MachineList.Text_All, _weight = weight };

            StateMachine ascii = GetAsciiString_Bigram();
            StateMachine unicode = GetUnicodeString();
            StateMachine unicode1 = GetUnicodeStringEndian();

            text.AddState(ascii._states);
            text.AddState(unicode._states);
            text.AddState(unicode1._states);

            text.StartingStates.AddRange(ascii.StartingStates);
            text.EndingStates.AddRange(ascii.EndingStates);

            text.StartingStates.AddRange(unicode.StartingStates);
            text.EndingStates.AddRange(unicode.EndingStates);

            text.StartingStates.AddRange(unicode1.StartingStates);
            text.EndingStates.AddRange(unicode1.EndingStates);

            return text;
        }

        /// <summary>
        /// The minimum length for the string is 3 unicode characters.
        /// </summary>
        /// <returns>The state machine for a Unicode string.</returns>
        public static StateMachine GetUnicodeString()
        {
            StateMachine unicodeString = new StateMachine { Name = MachineList.Text_Unicode };

            State uniNull0 = new State { Name = "UniNull0", ParentStateMachine = unicodeString };
            State uniChar0 = new State { Name = "UniChar0", ParentStateMachine = unicodeString };

            State uniNull1 = new State { Name = "UniNull1", ParentStateMachine = unicodeString };
            State uniChar1 = new State { Name = "UniChar1", ParentStateMachine = unicodeString };

            State uniNull2 = new State { Name = "UniNull2", ParentStateMachine = unicodeString };
            State uniChar2 = new State { Name = "UniChar2", ParentStateMachine = unicodeString };
            State uniChar2Punct = new State { Name = "UniChar2Punct", ParentStateMachine = unicodeString };


            unicodeString.AddState(uniNull0);
            unicodeString.AddState(uniChar0);
            unicodeString.AddState(uniNull1);
            unicodeString.AddState(uniChar1);
            unicodeString.AddState(uniNull2);
            unicodeString.AddState(uniChar2);
            unicodeString.AddState(uniChar2Punct);

            unicodeString.StartingStates.Add(uniNull0);
            unicodeString.EndingStates.Add(uniChar2);
            unicodeString.EndingStates.Add(uniChar2Punct);
            unicodeString.EndingStates.Add(uniChar1);

            AddTransition(uniNull0, uniChar0, 1d);
            AddTransition(uniChar0, uniNull1, 1d);
            AddTransition(uniNull1, uniChar1, 1d);

            AddTransition(uniChar1, uniNull2, 0.99d);
            uniChar1.RemainingProbability = 0.01d;

            AddTransition(uniNull2, uniChar2, 0.69d);
            AddTransition(uniNull2, uniChar2Punct, 0.31d);

            AddTransition(uniChar2, uniNull2, 0.99d);
            uniChar2.RemainingProbability = 0.01d;

            AddTransition(uniChar2Punct, uniNull2, 0.99d);
            uniChar2Punct.RemainingProbability = 0.01d;

            uniNull0.PossibleValueProbabilities[0x00] = 1d;
            uniNull1.PossibleValueProbabilities[0x00] = 1d;
            uniNull2.PossibleValueProbabilities[0x00] = 1d;

            uniChar1.PossibleValueProbabilities[0x20] = 1 / 54d;
            uniChar1.PossibleValueProbabilities[0x2D] = 1 / 54d;

            uniChar2.PossibleValueProbabilities[0x20] = 1 / 54d;
            uniChar2.PossibleValueProbabilities[0x2D] = 1 / 54d;

            //Upper case letters
            for (byte i = 0x41; i <= 0x5a; i++)
            {
                uniChar0.PossibleValueProbabilities[i] = 2d;
                uniChar1.PossibleValueProbabilities[i] = 1 / 54d;
                uniChar2.PossibleValueProbabilities[i] = 1 / 54d;
            }

            //Lower case letters
            for (byte i = 0x61; i <= 0x7a; i++)
            {
                uniChar0.PossibleValueProbabilities[i] = 1d;
                uniChar1.PossibleValueProbabilities[i] = 1 / 54d;
                uniChar2.PossibleValueProbabilities[i] = 1 / 54d;
            }

            //printable (non-alpha)
            for (int i = 33; i <= 64; i++)
            {
                uniChar2Punct.PossibleValueProbabilities[i] = 1d;
            }

            uniChar2Punct.PossibleValueProbabilities[0x20] = 1d;

            //printable (non-alpha)
            for (int i = 91; i <= 96; i++)
            {
                uniChar2Punct.PossibleValueProbabilities[i] = 1d;
            }

            //printable (non-alpha)
            for (int i = 123; i <= 127; i++)
            {
                uniChar2Punct.PossibleValueProbabilities[i] = 1d;
            }

            uniChar2Punct.NormalizeProbabilities();
            uniChar0.NormalizeProbabilities();

            return unicodeString;
        }

        /// <summary>
        /// Gets the state machine corresponing to the Endian of a Unicode string.
        /// </summary>
        /// <returns></returns>
        public static StateMachine GetUnicodeStringEndian()
        {
            StateMachine unicodeString = new StateMachine { Name = MachineList.Text_UnicodeEndian };

            State uniNull0 = new State { Name = "UniNull0", ParentStateMachine = unicodeString };
            State uniChar0 = new State { Name = "UniChar0", ParentStateMachine = unicodeString };

            State uniNull1 = new State { Name = "UniNull1", ParentStateMachine = unicodeString };
            State uniChar1 = new State { Name = "UniChar1", ParentStateMachine = unicodeString };

            State uniNull2 = new State { Name = "UniNull2", ParentStateMachine = unicodeString };
            State uniChar2 = new State { Name = "UniChar2", ParentStateMachine = unicodeString };
            State uniChar2Punct = new State { Name = "UniChar2Punct", ParentStateMachine = unicodeString };


            unicodeString.AddState(uniNull0);
            unicodeString.AddState(uniChar0);
            unicodeString.AddState(uniNull1);
            unicodeString.AddState(uniChar1);
            unicodeString.AddState(uniNull2);
            unicodeString.AddState(uniChar2);
            unicodeString.AddState(uniChar2Punct);

            unicodeString.StartingStates.Add(uniChar0);
            unicodeString.EndingStates.Add(uniNull2);
            unicodeString.EndingStates.Add(uniNull1);

            AddTransition(uniChar0, uniNull0, 1f);
            AddTransition(uniNull0, uniChar1, 1f);
            AddTransition(uniChar1, uniNull1, 1f);
            AddTransition(uniNull1, uniChar2, 0.69f);
            AddTransition(uniNull1, uniChar2Punct, 0.30f);
            uniNull1.RemainingProbability = 0.1f;

            AddTransition(uniChar2, uniNull2, 1f);
            AddTransition(uniChar2Punct, uniNull2, 1f);

            AddTransition(uniNull2, uniChar2, 0.69f);
            AddTransition(uniNull2, uniChar2Punct, 0.30f);
            uniNull2.RemainingProbability = 0.01f;

            uniNull0.PossibleValueProbabilities[0x00] = 1d;
            uniNull1.PossibleValueProbabilities[0x00] = 1d;
            uniNull2.PossibleValueProbabilities[0x00] = 1d;

            uniChar1.PossibleValueProbabilities[0x20] = 1 / 54d;
            uniChar1.PossibleValueProbabilities[0x2D] = 1 / 54d;

            uniChar2.PossibleValueProbabilities[0x20] = 1 / 54d;
            uniChar2.PossibleValueProbabilities[0x2D] = 1 / 54d;

            //Upper case letters
            for (byte i = 0x41; i <= 0x5a; i++)
            {
                uniChar0.PossibleValueProbabilities[i] = 2d;
                uniChar1.PossibleValueProbabilities[i] = 1 / 54d;
                uniChar2.PossibleValueProbabilities[i] = 1 / 54d;
            }

            //Lower case letters
            for (byte i = 0x61; i <= 0x7a; i++)
            {
                uniChar0.PossibleValueProbabilities[i] = 1d;
                uniChar1.PossibleValueProbabilities[i] = 1 / 54d;
                uniChar2.PossibleValueProbabilities[i] = 1 / 54d;
            }

            //printable (non-alpha)
            for (int i = 33; i <= 64; i++)
            {
                uniChar2Punct.PossibleValueProbabilities[i] = 1d;
            }

            uniChar2Punct.PossibleValueProbabilities[0x20] = 1d;

            //printable (non-alpha)
            for (int i = 91; i <= 96; i++)
            {
                uniChar2Punct.PossibleValueProbabilities[i] = 1d;
            }

            //printable (non-alpha)
            for (int i = 123; i <= 127; i++)
            {
                uniChar2Punct.PossibleValueProbabilities[i] = 1d;
            }

            uniChar2Punct.NormalizeProbabilities();
            uniChar0.NormalizeProbabilities();

            return unicodeString;
        }

        /// <summary>
        /// Gets the state machine for printable ASCII strings.
        /// </summary>
        /// <returns></returns>
        public static StateMachine GetAsciiPrintable()
        {
            StateMachine asciiString = new StateMachine { Name = MachineList.Text_AsciiPrintable };
            State asciiPart1 = new State { Name = "AsciiPart1", ParentStateMachine = asciiString };
            State asciiPart2 = new State { Name = "AsciiPart2", ParentStateMachine = asciiString };
            State asciiPart3 = new State { Name = "AsciiPart3", ParentStateMachine = asciiString };

            asciiString.AddState(asciiPart1);
            asciiString.AddState(asciiPart2);
            asciiString.AddState(asciiPart3);

            asciiString.StartingStates.Add(asciiPart1);
            asciiString.EndingStates.Add(asciiPart3);

            AddTransition(asciiPart1, asciiPart2, 1.0d);
            //AddTransition(asciiPart1, asciiPart1, 0.9d);
            AddTransition(asciiPart2, asciiPart3, 1.0d);
            //AddTransition(asciiPart2, asciiPart2, 0.9d);


            //Numbers
            for (byte i = 0x30; i < 0x39; i++)
            {
                asciiPart1.PossibleValueProbabilities[i] = 1d;
                asciiPart2.PossibleValueProbabilities[i] = 1d;
                asciiPart3.PossibleValueProbabilities[i] = 1d;
            }

            //Upper Case
            for (byte i = 0x41; i < 0x5a; i++)
            {
                asciiPart1.PossibleValueProbabilities[i] = 2d;
                asciiPart2.PossibleValueProbabilities[i] = 2d;
                asciiPart3.PossibleValueProbabilities[i] = 2d;
            }

            //Lower Case
            for (byte i = 0x61; i < 0x7a; i++)
            {
                asciiPart1.PossibleValueProbabilities[i] = 3d;
                asciiPart2.PossibleValueProbabilities[i] = 3d;
                asciiPart3.PossibleValueProbabilities[i] = 3d;
            }

            //Punct
            for (byte i = 0x20; i < 0x2F; i++)
            {
                asciiPart1.PossibleValueProbabilities[i] = 1d;
                asciiPart2.PossibleValueProbabilities[i] = 1d;
                asciiPart3.PossibleValueProbabilities[i] = 1d;
            }
            for (byte i = 0x3A; i < 0x40; i++)
            {
                asciiPart1.PossibleValueProbabilities[i] = 1d;
                asciiPart2.PossibleValueProbabilities[i] = 1d;
                asciiPart3.PossibleValueProbabilities[i] = 1d;
            }
            for (byte i = 0x5B; i < 0x60; i++)
            {
                asciiPart1.PossibleValueProbabilities[i] = 1d;
                asciiPart2.PossibleValueProbabilities[i] = 1d;
                asciiPart3.PossibleValueProbabilities[i] = 1d;
            }
            for (byte i = 0x7B; i < 0x7e; i++)
            {
                asciiPart1.PossibleValueProbabilities[i] = 1d;
                asciiPart2.PossibleValueProbabilities[i] = 1d;
                asciiPart3.PossibleValueProbabilities[i] = 1d;
            }

            asciiPart1.NormalizeProbabilities();
            asciiPart2.NormalizeProbabilities();
            asciiPart3.NormalizeProbabilities();

            return asciiString;
        }

        /// <summary>
        /// Gets the state machine for ASCII string bigrams.
        /// </summary>
        /// <returns></returns>
        public static StateMachine GetAsciiString_Bigram_v1()
        {
            StateMachine asciiString = new StateMachine { Name = MachineList.Text_AsciiBigram };
            BigramState asciiPart1 = new BigramState { Name = "AsciiPart1", ParentStateMachine = asciiString };
            BigramState asciiPart2 = new BigramState { Name = "AsciiPart2", ParentStateMachine = asciiString };
            BigramState asciiPart3 = new BigramState { Name = "AsciiPart3", ParentStateMachine = asciiString };

            asciiString.AddState(asciiPart1);
            asciiString.AddState(asciiPart2);
            asciiString.AddState(asciiPart3);

            asciiString.StartingStates.Add(asciiPart1);
            asciiString.EndingStates.Add(asciiPart3);

            AddTransition(asciiPart1, asciiPart2, 0.1d);
            AddTransition(asciiPart1, asciiPart1, 0.9d);
            AddTransition(asciiPart2, asciiPart3, 0.1d);
            AddTransition(asciiPart2, asciiPart2, 0.9d);

            return asciiString;
        }

        /// <summary>
        /// Gets the state machine for ASCII string bigrams.
        /// </summary>
        /// <returns></returns>
        public static StateMachine GetAsciiString_Bigram()
        {
            StateMachine asciiString = new StateMachine { Name = MachineList.Text_AsciiBigram };

            BigramState asciiChar0 = new BigramState() { Name = "AsciiChar0", ParentStateMachine = asciiString };
            BigramState asciiChar05 = new BigramState() { Name = "AsciiChar0.5", ParentStateMachine = asciiString };
            //We have issue where the first letter of the ascii string is cut off. Hopefully this state will fix that.
            State asciiChar0_nonBigram = new State { Name = "AsciiChar0_nbg", ParentStateMachine = asciiString };
            State asciiChar1_nonBigram = new State { Name = "AsciiChar1_nbg", ParentStateMachine = asciiString };

            BigramState asciiChar1 = new BigramState() { Name = "AsciiChar1", ParentStateMachine = asciiString };
            BigramState asciiChar2 = new BigramState() { Name = "AsciiChar2", ParentStateMachine = asciiString };

            State printChar = new State { Name = "PrintChar", ParentStateMachine = asciiString };

            asciiString.AddState(printChar);
            asciiString.AddState(asciiChar0_nonBigram);
            asciiString.AddState(asciiChar05);
            asciiString.AddState(asciiChar0);
            asciiString.AddState(asciiChar1);
            asciiString.AddState(asciiChar2);
            asciiString.AddState(asciiChar1_nonBigram);
            asciiString.StartingStates.Add(asciiChar0);
            asciiString.StartingStates.Add(asciiChar0_nonBigram);
            asciiString.EndingStates.Add(asciiChar1);
            asciiString.EndingStates.Add(asciiChar2);
            asciiString.EndingStates.Add(printChar);

            AddTransition(asciiChar0_nonBigram, asciiChar05, 0.9d);
            AddTransition(asciiChar0_nonBigram, asciiChar1_nonBigram, 0.1d);
            AddTransition(asciiChar1_nonBigram, asciiChar05, 1d);
            AddTransition(asciiChar0, asciiChar05, 1d);
            AddTransition(asciiChar0, asciiChar1_nonBigram, 0.1d);
            AddTransition(asciiChar05, asciiChar1, 0.88d);
            AddTransition(asciiChar05, printChar, 0.11d);
            AddTransition(asciiChar1, asciiChar2, 0.68d);
            asciiChar1.RemainingProbability = 0.01d;
            AddTransition(asciiChar2, asciiChar1, 0.68d);
            asciiChar2.RemainingProbability = 0.01d;

            AddTransition(asciiChar1, printChar, 0.31d);
            AddTransition(asciiChar2, printChar, 0.31d);
            AddTransition(printChar, asciiChar1, 0.88d);
            AddTransition(printChar, printChar, 0.11d);
            printChar.RemainingProbability = 0.01d;

            // all printable characters
            for (int i = 32; i < 127; i++)
            {
                printChar.PossibleValueProbabilities[i] = 1d;
                asciiChar0_nonBigram.PossibleValueProbabilities[i] = 1d;
                asciiChar1_nonBigram.PossibleValueProbabilities[i] = 1d;
            }

            printChar.NormalizeProbabilities();
            asciiChar0_nonBigram.NormalizeProbabilities();

            return asciiString;
        }

        /// <summary>
        /// Gets the state machine for printable ASCII bigram strings.
        /// </summary>
        /// <returns></returns>
        public static StateMachine GetAsciiString_BigramWithPrintable()
        {
            StateMachine asciiString = new StateMachine { Name = MachineList.Test_AsciiBigramWithPrintable };

            BigramState asciiChar1 = new BigramState() { Name = "AsciiChar1", ParentStateMachine = asciiString };
            BigramState asciiChar2 = new BigramState() { Name = "AsciiChar2", ParentStateMachine = asciiString };
            State printableChar1 = new State() { Name = "PrintableChar", ParentStateMachine = asciiString };

            asciiString.AddState(asciiChar1);
            asciiString.AddState(asciiChar2);
            asciiString.AddState(printableChar1);

            asciiString.StartingStates.Add(asciiChar1);
            asciiString.EndingStates.Add(asciiChar1);
            asciiString.EndingStates.Add(asciiChar2);
            asciiString.EndingStates.Add(printableChar1);

            AddTransition(asciiChar1, asciiChar2, 0.95d);
            asciiChar1.RemainingProbability = 0.01d;
            AddTransition(asciiChar2, asciiChar1, 0.95d);
            asciiChar2.RemainingProbability = 0.01d;
            AddTransition(asciiChar1, printableChar1, 0.05d);
            AddTransition(asciiChar2, printableChar1, 0.05d);
            AddTransition(printableChar1, asciiChar2, 0.49d);
            AddTransition(printableChar1, asciiChar2, 0.49d);
            printableChar1.RemainingProbability = 0.01;

            //Dash '-'
            printableChar1.PossibleValueProbabilities[0x2D] = 1d;
            return asciiString;
        }

        #endregion

        #region TimeStamp

        /// <summary>
        /// Gets the state machine all possible timestamps, with states from state machines corresponing to timestamps on Nokia, Samsung, Motorola, UNIX etc.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_All state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_All state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimeStamp_All(int weight)
        {
            StateMachine timeStampAll = new StateMachine { Name = MachineList.TimeStamp_All, _weight = weight };

            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                    GetTimestamp_NokiaAll(1),
                                                    GetTimestamp_Samsung(1),
                                                    GetTimestamp_MotoSms(1),
                                                    //GetTimestamp_Sms(1),
                                                    GetTimestamp_SmsGsm(1),
                                                    GetCallLog_MotoTypeAndTime(1),
                                                    GetCallLogTimeStamp_Nokia(1),
                                                    GetTimestamp_Epoch1900Tuple(1)
                                              };


            for (int i = 0; i < machines.Count; i++)
            {
                timeStampAll.StartingStates.AddRange(machines[i].StartingStates);
                timeStampAll.EndingStates.AddRange(machines[i].EndingStates);
                timeStampAll.AddState(machines[i]._states);
            }

            return timeStampAll;
        }

        /// <summary>
        /// Gets the state machine for the timestamp of a Samsung phone.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_Samsung state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_Samsung state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_Samsung(int weight)
        {
            StateMachine timestamp = new StateMachine { Name = MachineList.TimeStamp_Samsung, _weight = weight };

            State byte1 = new State { Name = "Byte1", ParentStateMachine = timestamp };
            State byte2 = new State { Name = "Byte2", ParentStateMachine = timestamp };
            State byte3 = new State { Name = "Byte3", ParentStateMachine = timestamp };
            //State byte4 = new State { Name = "Byte4", ParentStateMachine = timestamp };
            SamsungTimeState byte4 = new SamsungTimeState { Name = "Byte4Checker", ParentStateMachine = timestamp };

            timestamp.AddState(byte1);
            timestamp.AddState(byte2);
            timestamp.AddState(byte3);
            timestamp.AddState(byte4);

            timestamp.StartingStates.Add(byte1);
            timestamp.EndingStates.Add(byte4);

            AddTransition(byte1, byte2, 1d);
            AddTransition(byte2, byte3, 1d);
            AddTransition(byte3, byte4, 1d);


            // BL
            // Last 12 bits represent year. Determine possible
            // last bytes (little-endian)
            int nyears = 0;
            for (int i = (TimeConstants.START_YEAR) >> 4; i <= (TimeConstants.END_YEAR) >> 4; i++)
            {
                byte4.PossibleValueProbabilities[i] = 1d;
                nyears++;
            }
            if (nyears > 1) byte4.NormalizeProbabilities();

            // Month (1-12) 4 bits and 4 bits of year.
            for (int yr = TimeConstants.START_YEAR; yr <= TimeConstants.END_YEAR; yr++)
            {
                for (byte mn = 1; mn <= 12; mn++)
                {
                    int x = ((yr & 0xf) << 4) | mn;
                    byte3.PossibleValueProbabilities[x] = 1d;
                }
            }
            byte3.NormalizeProbabilities();

            // BL
            // Minutes (0-59) 6 bits, Hours (0-23) 5 bits, Days (1-31) 5 bits
            for (int i = 0; i < 256; i++)
            {
                // 4 bits for day will not be zero.
                if ((i & 0xf8) == 0) continue;
                // 3 bits used by hours will not all be set.
                if ((i & 0x07) == 0x07) continue;
                byte2.PossibleValueProbabilities[i] = 1d;
            }
            byte2.NormalizeProbabilities();
            for (int i = 0; i < 256; i++)
            {
                // Don't permit minutes greater than 59
                if ((i & 0x3f) >= 60) continue;
                byte1.PossibleValueProbabilities[i] = 1d;
            }
            byte1.NormalizeProbabilities();

            return timestamp;
        }

        /// <summary>
        /// Gets the state machine for the timestamp of a Nokia endian.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_NokiaEndian state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_NokiaEndian state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_NokiaEndian(int weight)
        {
            StateMachine timestamp = new StateMachine { Name = MachineList.TimeStamp_NokiaEndian, _weight = weight };

            State year1 = new State { Name = "Year1", ParentStateMachine = timestamp };
            State year2 = new State { Name = "Year2", ParentStateMachine = timestamp };

            State month = new State { Name = "Month", ParentStateMachine = timestamp };
            State day = new State { Name = "Day", ParentStateMachine = timestamp };
            State hour = new State { Name = "Hour", ParentStateMachine = timestamp };
            State min = new State { Name = "Min", ParentStateMachine = timestamp };
            NokiaEndianTimeState sec = new NokiaEndianTimeState { Name = "Sec", ParentStateMachine = timestamp };

            timestamp.AddState(year1);
            timestamp.AddState(year2);
            timestamp.AddState(month);
            timestamp.AddState(day);
            timestamp.AddState(hour);
            timestamp.AddState(min);
            timestamp.AddState(sec);

            timestamp.StartingStates.Add(year1);
            timestamp.EndingStates.Add(sec);

            AddTransition(year1, year2, 1d);
            AddTransition(year2, month, 1d);
            AddTransition(month, day, 1d);
            AddTransition(day, hour, 1d);
            AddTransition(hour, min, 1d);
            AddTransition(min, sec, 1d);

            // BL: Year: most significant byte rolls over in 2048
            // so we should be safe
            year2.PossibleValueProbabilities[0x07] = 1d;
            int start = TimeConstants.START_YEAR & 0xff;
            int end = TimeConstants.END_YEAR & 0xff;
            for (int i = start; i <= end; i++)
            {
                year1.PossibleValueProbabilities[i] = 1d;
            }
            year1.NormalizeProbabilities();

            //Month 01 - 12
            for (byte i = 0x01; i <= 0x0C; i++)
            {
                month.PossibleValueProbabilities[i] = 1 / 12d;
            }

            //Day 01 - 31
            for (byte i = 0x01; i <= 0x1F; i++)
            {
                day.PossibleValueProbabilities[i] = 1 / 31d;
            }

            //Hour 00 - 23
            for (byte i = 0x00; i <= 0x17; i++)
            {
                hour.PossibleValueProbabilities[i] = 1 / 24d;
            }

            //Minutes & Seconds 00-59
            for (byte i = 0x00; i <= 0x3B; i++)
            {
                min.PossibleValueProbabilities[i] = 1 / 60d;
                sec.PossibleValueProbabilities[i] = 1 / 60d;
            }
            return timestamp;
        }

        /// <summary>
        /// Gets the state machine for the timestamp of a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_Nokia state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_Nokia state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_Nokia(int weight)
        {
            StateMachine timestamp = new StateMachine { Name = MachineList.TimeStamp_Nokia, _weight = weight };

            State year1 = new State { Name = "Year1", ParentStateMachine = timestamp };
            State year2 = new State { Name = "Year2", ParentStateMachine = timestamp };

            State year1Endian = new State { Name = "Year1Endian", ParentStateMachine = timestamp };
            State year2Endian = new State { Name = "Year2Endian", ParentStateMachine = timestamp };

            State month = new State { Name = "Month", ParentStateMachine = timestamp };
            State day = new State { Name = "Day", ParentStateMachine = timestamp };
            State hour = new State { Name = "Hour", ParentStateMachine = timestamp };
            State min = new State { Name = "Min", ParentStateMachine = timestamp };
            NokiaTimeState sec = new NokiaTimeState { Name = "Sec", ParentStateMachine = timestamp };

            timestamp.AddState(year1);
            timestamp.AddState(year2);
            timestamp.AddState(month);
            timestamp.AddState(day);
            timestamp.AddState(hour);
            timestamp.AddState(min);
            timestamp.AddState(sec);

            timestamp.StartingStates.Add(year1);
            timestamp.EndingStates.Add(sec);

            AddTransition(year1, year2, 1d);
            AddTransition(year2, month, 1d);
            AddTransition(month, day, 1d);
            AddTransition(day, hour, 1d);
            AddTransition(hour, min, 1d);
            AddTransition(min, sec, 1d);

            // BL: Year: most significant byte rolls over in 2048
            // so we should be safe
            year1.PossibleValueProbabilities[0x07] = 1d;
            int start = TimeConstants.START_YEAR & 0xff;
            int end = TimeConstants.END_YEAR & 0xff;
            for (int i = start; i <= end; i++)
            {
                year2.PossibleValueProbabilities[i] = 1d;
            }
            year2.NormalizeProbabilities();

            //Month 01 - 12
            for (byte i = 0x01; i <= 0x0C; i++)
            {
                month.PossibleValueProbabilities[i] = 1 / 12d;
            }

            //Day 01 - 31
            for (byte i = 0x01; i <= 0x1F; i++)
            {
                day.PossibleValueProbabilities[i] = 1 / 31d;
            }

            //Hour 00 - 23
            for (byte i = 0x00; i <= 0x17; i++)
            {
                hour.PossibleValueProbabilities[i] = 1 / 24d;
            }

            //Minutes & Seconds 00-59
            for (byte i = 0x00; i <= 0x3B; i++)
            {
                min.PossibleValueProbabilities[i] = 1 / 60d;
                sec.PossibleValueProbabilities[i] = 1 / 60d;
            }
            return timestamp;
        }

        /// <summary>
        /// Gets the state machine for a UNIX timestamp.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_Unix state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_Unix state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_Unix(int weight)
        {
            var timestamp = new StateMachine { Name = MachineList.TimeStamp_Unix, _weight = weight };

            State unixTime1 = new State { Name = "UnixTime1", ParentStateMachine = timestamp };
            State unixTime2 = new State { Name = "UnixTime2", ParentStateMachine = timestamp };
            State unixTime3 = new State { Name = "UnixTime3", ParentStateMachine = timestamp };
            UnixTimeState unixTime4 = new UnixTimeState { Name = "UnixTime4", ParentStateMachine = timestamp };

            timestamp.AddState(unixTime1);
            timestamp.AddState(unixTime2);
            timestamp.AddState(unixTime3);
            timestamp.AddState(unixTime4); ;

            timestamp.StartingStates.Add(unixTime1);
            timestamp.EndingStates.Add(unixTime4);

            AddTransition(unixTime1, unixTime2, 1d);
            AddTransition(unixTime2, unixTime3, 1d);
            AddTransition(unixTime3, unixTime4, 1d);

            // BL
            DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(Math.Max(TimeConstants.START_YEAR, 2011), 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(TimeConstants.START_YEAR, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new DateTime(TimeConstants.END_YEAR, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            uint startEpoch = (uint)(startTime - epochTime).TotalSeconds;
            uint endEpoch = (uint)(endTime - epochTime).TotalSeconds;
            byte[] startEpochBytes = BitConverter.GetBytes(startEpoch);
            byte[] endEpochBytes = BitConverter.GetBytes(endEpoch);
            // big-endian
            for (byte i = startEpochBytes[3]; i <= endEpochBytes[3]; i++)
            {
                unixTime1.PossibleValueProbabilities[i] = 1d;
            }
            unixTime1.NormalizeProbabilities();


            for (int i = 0; i < 256; i++)
            {
                unixTime2.PossibleValueProbabilities[i] = 1 / 256d;
                unixTime3.PossibleValueProbabilities[i] = 1 / 256d;
                unixTime4.PossibleValueProbabilities[i] = 1 / 256d;
            }
            return timestamp;
        }

        /// <summary>
        /// Two unsigned 4-byte little-endian epoch times.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_Epoch1900Tuple state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_Epoch1900Tuple state machine.</param>
        /// <returns></returns>        
        public static StateMachine GetTimestamp_Epoch1900Tuple(int weight)
        {
            var timestamp = new StateMachine { Name = MachineList.TimeStamp_Epoch1900Tuple, _weight = weight };

            State epochTime1 = new State { Name = "Epoch1900Tuple1", ParentStateMachine = timestamp };
            State epochTime2 = new State { Name = "Epoch1900Tuple2", ParentStateMachine = timestamp };
            State epochTime3 = new State { Name = "Epoch1900Tuple3", ParentStateMachine = timestamp };
            State epochTime4 = new State { Name = "Epoch1900Tuple4", ParentStateMachine = timestamp };
            State epochTime5 = new State { Name = "Epoch1900Tuple5", ParentStateMachine = timestamp };
            State epochTime6 = new State { Name = "Epoch1900Tuple6", ParentStateMachine = timestamp };
            State epochTime7 = new State { Name = "Epoch1900Tuple7", ParentStateMachine = timestamp };
            Epoch1900Tuple epochTime8 = new Epoch1900Tuple { Name = "Epoch1900Tuple8", ParentStateMachine = timestamp };
            //State epochTime8 = new State { Name = "Epoch1900Tuple8", ParentStateMachine = timestamp };

            timestamp.AddState(epochTime1);
            timestamp.AddState(epochTime2);
            timestamp.AddState(epochTime3);
            timestamp.AddState(epochTime4);
            timestamp.AddState(epochTime5);
            timestamp.AddState(epochTime6);
            timestamp.AddState(epochTime7);
            timestamp.AddState(epochTime8);

            timestamp.StartingStates.Add(epochTime1);
            timestamp.EndingStates.Add(epochTime8);

            AddTransition(epochTime1, epochTime2, 1d);
            AddTransition(epochTime2, epochTime3, 1d);
            AddTransition(epochTime3, epochTime4, 1d);
            AddTransition(epochTime4, epochTime5, 1d);
            AddTransition(epochTime5, epochTime6, 1d);
            AddTransition(epochTime6, epochTime7, 1d);
            AddTransition(epochTime7, epochTime8, 1d);

            DateTime epochTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //DateTime startTime = new DateTime(Math.Max(TimeConstants.START_YEAR, 2011), 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime startTime = new DateTime(TimeConstants.START_YEAR, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new DateTime(TimeConstants.END_YEAR, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            uint startEpoch = (uint)(startTime - epochTime).TotalSeconds;
            uint endEpoch = (uint)(endTime - epochTime).TotalSeconds;
            byte[] startEpochBytes = BitConverter.GetBytes(startEpoch);
            byte[] endEpochBytes = BitConverter.GetBytes(endEpoch);
            // little-endian
            for (byte i = startEpochBytes[3]; i <= endEpochBytes[3]; i++)
            {
                epochTime4.PossibleValueProbabilities[i] = 1d;
                epochTime8.PossibleValueProbabilities[i] = 1d;
            }
            epochTime4.NormalizeProbabilities();
            epochTime8.NormalizeProbabilities();

            if (startEpochBytes[3] == endEpochBytes[3])
            {
                for (byte i = startEpochBytes[2]; i <= endEpochBytes[2]; i++)
                {
                    epochTime3.PossibleValueProbabilities[i] = 1d;
                    epochTime7.PossibleValueProbabilities[i] = 1d;
                }
                epochTime3.NormalizeProbabilities();
                epochTime7.NormalizeProbabilities();
            }
            else
            {
                for (int i = 0; i < 256; i++)
                {
                    epochTime3.PossibleValueProbabilities[i] = 1 / 256d;
                    epochTime7.PossibleValueProbabilities[i] = 1 / 256d;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                epochTime1.PossibleValueProbabilities[i] = 1 / 256d;
                epochTime2.PossibleValueProbabilities[i] = 1 / 256d;
                epochTime5.PossibleValueProbabilities[i] = 1 / 256d;
                epochTime6.PossibleValueProbabilities[i] = 1 / 256d;
            }

            return timestamp;
        }

        /// <summary>
        /// Gets the state machine for a timestamp corresponding to a Motorola SMS.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_MotoSms state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_MotoSms state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_MotoSms(int weight)
        {
            var timestamp = new StateMachine { Name = MachineList.TimeStamp_MotoSms, _weight = weight };

            State year = new State { Name = "Year", ParentStateMachine = timestamp };
            State month = new State { Name = "Month", ParentStateMachine = timestamp };
            State day = new State { Name = "Day", ParentStateMachine = timestamp };
            State hour = new State { Name = "Hour", ParentStateMachine = timestamp };
            State minute = new State { Name = "Minute", ParentStateMachine = timestamp };
            MotoSmsTimeState second = new MotoSmsTimeState { Name = "Second", ParentStateMachine = timestamp };

            timestamp.AddState(year);
            timestamp.AddState(month);
            timestamp.AddState(day);
            timestamp.AddState(hour);
            timestamp.AddState(minute);
            timestamp.AddState(second);

            timestamp.StartingStates.Add(year);
            timestamp.EndingStates.Add(second);

            AddTransition(year, month, 1f);
            AddTransition(month, day, 1f);
            AddTransition(day, hour, 1f);
            AddTransition(hour, minute, 1f);
            AddTransition(minute, second, 1f);

            // Year from 1970

            int yearWeight = 0;
            //int startYr = Math.Max(2010, TimeConstants.START_YEAR);
            int startYr = TimeConstants.START_YEAR;
            for (int i = (startYr - 1970); i <= (TimeConstants.END_YEAR - 1970); i++)
            {
                year.PossibleValueProbabilities[i] = 1f + yearWeight;

                yearWeight++;
            }


            for (byte i = 0x01; i <= 0x0c; i++)
            {
                month.PossibleValueProbabilities[i] = 1f;
            }

            for (byte i = 0x01; i <= 0x1f; i++)
            {
                day.PossibleValueProbabilities[i] = 1f;
            }

            for (byte i = 0x00; i <= 0x17; i++)
            {
                hour.PossibleValueProbabilities[i] = 1f;
            }

            for (byte i = 0x00; i <= 0x39; i++)
            {
                minute.PossibleValueProbabilities[i] = 1f;
                second.PossibleValueProbabilities[i] = 1f;
            }

            year.NormalizeProbabilities();
            month.NormalizeProbabilities();
            day.NormalizeProbabilities();
            hour.NormalizeProbabilities();
            minute.NormalizeProbabilities();
            second.NormalizeProbabilities();

            return timestamp;
        }

        /// <summary>
        /// Gets the state machine for a timestamp correponding to an SMS.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_Sms state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_Sms state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_Sms(int weight)
        {
            var timestamp = new StateMachine { Name = MachineList.TimeStamp_Sms, _weight = weight };

            State year = new State { Name = "Year", ParentStateMachine = timestamp };
            State month = new State { Name = "Month", ParentStateMachine = timestamp };
            State day = new State { Name = "Day", ParentStateMachine = timestamp };
            State hour = new State { Name = "Hour", ParentStateMachine = timestamp };
            State minute = new State { Name = "Minute", ParentStateMachine = timestamp };
            SmsTimeState second = new SmsTimeState { Name = "Second", ParentStateMachine = timestamp };

            timestamp.AddState(year);
            timestamp.AddState(month);
            timestamp.AddState(day);
            timestamp.AddState(hour);
            timestamp.AddState(minute);
            timestamp.AddState(second);

            timestamp.StartingStates.Add(year);
            timestamp.EndingStates.Add(second);

            AddTransition(year, month, 1d);
            AddTransition(month, day, 1d);
            AddTransition(day, hour, 1d);
            AddTransition(hour, minute, 1d);
            AddTransition(minute, second, 1d);

            int yearWeight = 0;

            //int startYr = Math.Max(2010, TimeConstants.START_YEAR);
            int startYr = TimeConstants.START_YEAR;
            for (int i = (startYr - 2000); i <= (TimeConstants.END_YEAR - 2000); i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                year.PossibleValueProbabilities[byteVal] = 1f + yearWeight;

                yearWeight++;
            }

            year.NormalizeProbabilities();

            for (int i = 1; i <= 12; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                month.PossibleValueProbabilities[byteVal] = 1 / 12d;
            }

            for (int i = 1; i <= 31; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                day.PossibleValueProbabilities[byteVal] = 1 / 31d;
            }

            for (int i = 0; i <= 23; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                hour.PossibleValueProbabilities[byteVal] = 1 / 24d;
            }

            for (int i = 0; i <= 59; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                minute.PossibleValueProbabilities[byteVal] = 1 / 60d;
                second.PossibleValueProbabilities[byteVal] = 1 / 60d;
            }

            return timestamp;
        }

        /// <summary>
        /// Gets the state machine for a timestamp correponding to an SMS.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_Sms state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_Sms state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_SmsGsm(int weight)
        {
            var timestamp = new StateMachine { Name = MachineList.TimeStamp_SmsGsm, _weight = weight };

            State year = new State { Name = "Year", ParentStateMachine = timestamp };
            State month = new State { Name = "Month", ParentStateMachine = timestamp };
            State day = new State { Name = "Day", ParentStateMachine = timestamp };
            State hour = new State { Name = "Hour", ParentStateMachine = timestamp };
            State minute = new State { Name = "Minute", ParentStateMachine = timestamp };
            State second = new State { Name = "Second", ParentStateMachine = timestamp };
            SmsGsmTimeState timezone = new SmsGsmTimeState { Name = "Timezone", ParentStateMachine = timestamp };

            timestamp.AddState(year);
            timestamp.AddState(month);
            timestamp.AddState(day);
            timestamp.AddState(hour);
            timestamp.AddState(minute);
            timestamp.AddState(second);
            timestamp.AddState(timezone);

            timestamp.StartingStates.Add(year);
            timestamp.EndingStates.Add(timezone);

            AddTransition(year, month, 1d);
            AddTransition(month, day, 1d);
            AddTransition(day, hour, 1d);
            AddTransition(hour, minute, 1d);
            AddTransition(minute, second, 1d);
            AddTransition(second, timezone, 1d);

            int yearWeight = 0;

            //int startYr = Math.Max(2010, TimeConstants.START_YEAR);
            int startYr = TimeConstants.START_YEAR;
            for (int i = (startYr - 2000); i <= (TimeConstants.END_YEAR - 2000); i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                year.PossibleValueProbabilities[byteVal] = 1f + yearWeight;

                yearWeight++;
            }

            year.NormalizeProbabilities();

            for (int i = 1; i <= 12; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                month.PossibleValueProbabilities[byteVal] = 1 / 12d;
            }

            for (int i = 1; i <= 31; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                day.PossibleValueProbabilities[byteVal] = 1 / 31d;
            }

            for (int i = 0; i <= 23; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                hour.PossibleValueProbabilities[byteVal] = 1 / 24d;
            }

            for (int i = 0; i <= 59; i++)
            {
                var byteTest = byte.Parse(Convert.ToString(i), NumberStyles.HexNumber);
                var byteVal = Printer.SwapNibbles(byteTest);

                minute.PossibleValueProbabilities[byteVal] = 1 / 60d;
                second.PossibleValueProbabilities[byteVal] = 1 / 60d;
            }

            // Each interval represents a 15 minute GMT offset. If the most
            // significant bit (before swapping) is set, it's a negative value.
            for (int i = 0; i < 96; i++)
            {
                // Skip 15 minutes time zones, except Nepal's.
                if (((i % 2) == 1) && (i != 23)) continue;
                // Swapped BCD.
                byte byteVal = Printer.SwapNibbles(Printer.ByteFromNibbles(i / 10, i % 10));
                timezone.PossibleValueProbabilities[byteVal] = 1d;
                if ((i != 0) && (i != 23))
                {
                    // Mark negative (bytes are already swapped)
                    byteVal |= 0x08;
                    timezone.PossibleValueProbabilities[byteVal] = 1d;
                }
            }
            timezone.NormalizeProbabilities();

            return timestamp;
        }

        /// <summary>
        /// Gets the state machine for all possible timestamps on a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of TimeStamp_NokiaAll state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to TimeStamp_NokiaAll state machine.</param>
        /// <returns></returns>
        public static StateMachine GetTimestamp_NokiaAll(int weight)
        {
            StateMachine timeStampAll = new StateMachine { Name = MachineList.TimeStamp_All, _weight = weight };

            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                    GetTimestamp_Nokia(1),
                                                    GetTimestamp_NokiaEndian(1)
                                              };


            for (int i = 0; i < machines.Count; i++)
            {
                timeStampAll.StartingStates.AddRange(machines[i].StartingStates);
                timeStampAll.EndingStates.AddRange(machines[i].EndingStates);
                timeStampAll.AddState(machines[i]._states);
            }

            return timeStampAll;
        }

        /// <summary>
        /// Gets the state machine for a user-defined timestamp.
        /// </summary>
        /// <param name="userState">Object representing the user-defined state machine.</param>
        /// <param name="weight"></param>
        /// <returns>The state machine.</returns>
        public static StateMachine GetTimestamp_UserDefined(UserState userState, int weight)
        {
            StateMachine timestamp = new StateMachine { Name = userState.MachineType, _weight = weight };
            State prevState = null;
            // Have a minimum of 2 bytes.
            for (int n = 0; n < userState.Bytes.Count - 1; n++)
            {
                string nm = String.Format("UserTimestampByte{0}", n);
                State state = new State { Name = nm, ParentStateMachine = timestamp };
                timestamp.AddState(state);
                if (n == 0)
                {
                    timestamp.StartingStates.Add(state);
                }
                else
                {
                    AddTransition(prevState, state, 1d);
                }
                prevState = state;
                UserDefinedByteProbabilities(state, userState.Bytes[n]);
            }
            UserDefinedTimestampState endState = new UserDefinedTimestampState(userState) { Name = "EndUserTimestampByte", ParentStateMachine = timestamp };
            timestamp.AddState(endState);
            UserDefinedByteProbabilities(endState, userState.Bytes[userState.Bytes.Count - 1]);
            AddTransition(prevState, endState, 1d);
            timestamp.EndingStates.Add(endState);
            return timestamp;
        }

        #endregion

        #region PhoneNumber

        /// <summary>
        /// Gets the state machine for an international phone number, consisting of states from several state machines corresponing to seven digit, ten digit and eleven digit formats.
        /// </summary>
        /// <param name="weight">Weight of PhoneNumber_International state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to PhoneNumber_Inernational state machine.</param>
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_International(int weight)
        {
            StateMachine phone = new StateMachine { Name = MachineList.PhoneNumber_All, _weight = weight };

            //The moto phone numbers are not needed since they are the same as the international format. (non unicode)
            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                    GetPhoneNumber_InternationalFormatSevenDigit(),
                                                    GetPhoneNumber_InternationalFormatTenDigit(),
                                                    GetPhoneNumber_InternationalFormatElevenDigit()
                                              };


            for (int i = 0; i < machines.Count; i++)
            {
                phone.StartingStates.AddRange(machines[i].StartingStates);
                phone.EndingStates.AddRange(machines[i].EndingStates);
                phone.AddState(machines[i]._states);
            }

            return phone;
        }

        /// <summary>
        /// Gets the state machine for all possible phone number formats, consisting of states from several state machines corresponing to national as well international seven digit, ten digit and eleven digit formats on Nokia, Samsung, Motorola.
        /// </summary>
        /// <param name="weight">Weight of PhoneNumber_All state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to PhoneNumber_All state machine.</param>
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_All(int weight, List<UserState> userStates = null)
        {
            StateMachine phone = new StateMachine { Name = MachineList.PhoneNumber_All, _weight = weight };

            //The moto phone numbers are not needed since they are the same as the international format. (non unicode)
            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                    GetPhoneNumber_NokiaElevenDigit(),
                                                    GetPhoneNumber_NokiaSevenDigit(),
                                                    GetPhoneNumber_NokiaEightDigit(),
                                                    GetPhoneNumber_NokiaTenDigit(),
                                                    GetPhoneNumber_NokiaTwelveDigit(),
                                                    /*
                                                    GetPhoneNumber_InternationalFormatSevenDigit(),
                                                    GetPhoneNumber_InternationalFormatTenDigit(),
                                                    GetPhoneNumber_InternationalFormatElevenDigit(),
                                                    */
                                                    //GetPhoneNumber_BCD(),
                                                    GetPhoneNumber_BCDWithPrepend(),
                                                    GetPhoneNumber_MotoSevenUnicode(),
                                                    GetPhoneNumber_MotoTenUnicode(),
                                                    GetPhoneNumber_MotoElevenUnicode(),
                                                    GetPhoneNumber_SamsungElevenDigitAscii(),
                                                    GetPhoneNumber_SamsungTenDigitAscii(),
                                                    GetPhoneNumber_SamsungSevenDigitAscii(),
                                                    //GetPhoneNumber_MotoElevenDigit(),
                                                    //GetPhoneNumber_MotoSevenDigit(),
                                                    //GetPhoneNumber_MotoTenDigit(),
                                                    GetCallLog_NokiaNumberIndexAndNumber(1)
                                              };
            // Add any user-defined phone number states machines.
            if (userStates != null)
            {
                foreach (UserState us in userStates)
                {
                    if (us.MachineType == MachineList.PhoneNumber_User)
                    {
                        machines.Add(GetPhoneNumber_UserDefined(us, weight));
                    }
                }
            }

            for (int i = 0; i < machines.Count; i++)
            {
                phone.StartingStates.AddRange(machines[i].StartingStates);
                phone.EndingStates.AddRange(machines[i].EndingStates);
                phone.AddState(machines[i]._states);
            }

            return phone;
        }

        /// <summary>
        /// Gets the state machine for all possible phone number formats on a Nokia phone, consisting of states from several state machines corresponing to seven, eight, ten, eleven and twelve digit formats on a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of PhoneNumber_NokiaAll state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to PhoneNumber_NokiaAll state machine.</param>
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_NokiaAll(int weight)
        {
            StateMachine phone = new StateMachine { Name = MachineList.PhoneNumber_NokiaAll, _weight = weight };

            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                    GetPhoneNumber_NokiaElevenDigit(),
                                                    GetPhoneNumber_NokiaSevenDigit(),
                                                    GetPhoneNumber_NokiaEightDigit(),
                                                    GetPhoneNumber_NokiaTenDigit(),
                                                    GetPhoneNumber_NokiaTwelveDigit(),
                                              };


            for (int i = 0; i < machines.Count; i++)
            {
                phone.StartingStates.AddRange(machines[i].StartingStates);
                phone.EndingStates.AddRange(machines[i].EndingStates);
                phone.AddState(machines[i]._states);
            }

            return phone;
        }

        /// <summary>
        /// Gets the state machine for BCD numbers.
        /// Byte 0 - phone number length
        /// Byte 1 - type of address (129 or 145)
        /// Byte 2+ - BCD phone number (swapped nibbles)
        /// 
        /// Note: there is also a version with an explicit prepend.
        /// </summary>
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_BCD()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_BCD };

            State length = new State { Name = "BCDLength", ParentStateMachine = phoneNumber };
            for (int i = 5; i <= 16; i++)
            {
                length.PossibleValueProbabilities[i] = 1d;
            }
            length.NormalizeProbabilities();

            State toa = new State { Name = "BCDTOA", ParentStateMachine = phoneNumber };
            toa.PossibleValueProbabilities[129] = 1d;
            toa.PossibleValueProbabilities[145] = 1d;
            toa.NormalizeProbabilities();

            State digit1 = new State { Name = "BCDDigit1", ParentStateMachine = phoneNumber };
            BcdDigitState digitEven = new BcdDigitState { Name = "BCDDigitEven", ParentStateMachine = phoneNumber, LengthState = length };
            State digitOdd = new State { Name = "BCDDigitOdd", ParentStateMachine = phoneNumber };
            for (uint i = 0; i <= 9; i++)
            {
                for (uint n = 0; n <= 9; n++)
                {
                    uint x = ((n << 4) & 0xF0) | (i & 0x0F);
                    digit1.PossibleValueProbabilities[x] = 1d;
                    digitEven.PossibleValueProbabilities[x] = 1d;
                }
                uint z = 0xF0 | (i & 0x0F);
                digitOdd.PossibleValueProbabilities[z] = 1d;
            }
            digit1.NormalizeProbabilities();
            digitEven.NormalizeProbabilities();
            digitOdd.NormalizeProbabilities();

            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digitEven);
            phoneNumber.AddState(digitOdd);

            phoneNumber.AddState(length);
            phoneNumber.AddState(toa);
            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digitEven);
            phoneNumber.AddState(digitOdd);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digitEven);
            phoneNumber.EndingStates.Add(digitOdd);

            AddTransition(length, toa, 1d);
            AddTransition(toa, digit1, 1d);
            AddTransition(digit1, digitEven, 1d);
            AddTransition(digit1, digitOdd, 1d);
            AddTransition(digitEven, digitEven, 1d);
            AddTransition(digitEven, digitOdd, 1d);

            return phoneNumber;

        }

        /// <summary>
        /// Gets the state machine for BCD numbers.
        /// Byte 0 - phone number length
        /// Byte 1 - type of address (129 or 145)
        /// Byte 2+ - BCD phone number (swapped nibbles)
        /// </summary>
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_BCDWithPrepend()
        {
            StateMachine prepend = new StateMachine { Name = MachineList.Prepend_BCD };

            State length = new State { Name = "BCDLength", ParentStateMachine = prepend };
            for (int i = 4; i <= 11; i++)
            {
                length.PossibleValueProbabilities[i] = 1d;
            }
            length.NormalizeProbabilities();

            State toa = new State { Name = "BCDTOA", ParentStateMachine = prepend };
            toa.PossibleValueProbabilities[129] = 0.5d;
            toa.PossibleValueProbabilities[145] = 0.5d;

            prepend.AddState(length);
            prepend.AddState(toa);

            prepend.StartingStates.Add(length);
            //prepend.StartingStates.Add(toc);
            prepend.EndingStates.Add(toa);

            AddTransition(length, toa, 1d);

            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_BCDPrepended };

            State digit1 = new State { Name = "BCDDigit1", ParentStateMachine = phoneNumber };
            BcdDigitState digitEven = new BcdDigitState { Name = "BCDDigitEven", ParentStateMachine = phoneNumber, LengthState = length };
            State digitOdd = new State { Name = "BCDDigitOdd", ParentStateMachine = phoneNumber };
            for (uint i = 0; i <= 9; i++)
            {
                for (uint n = 0; n <= 9; n++)
                {
                    uint x = ((n << 4) & 0xF0) | (i & 0x0F);
                    digit1.PossibleValueProbabilities[x] = 1d;
                    digitEven.PossibleValueProbabilities[x] = 1d;
                }
                uint z = 0xF0 | (i & 0x0F);
                digitOdd.PossibleValueProbabilities[z] = 1d;
            }
            digit1.NormalizeProbabilities();
            digitEven.NormalizeProbabilities();
            digitOdd.NormalizeProbabilities();

            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digitEven);
            phoneNumber.AddState(digitOdd);

            phoneNumber.StartingStates.Add(digit1);
            phoneNumber.EndingStates.Add(digitEven);
            phoneNumber.EndingStates.Add(digitOdd);

            AddTransition(digit1, digitEven, 1d);
            AddTransition(digit1, digitOdd, 1d);
            AddTransition(digitEven, digitEven, 1d);
            AddTransition(digitEven, digitOdd, 1d);

            StateMachine phoneWithPrepend = new StateMachine { Name = MachineList.PhoneNumber_BCDWithPrepend };

            phoneWithPrepend.AddState(prepend._states);
            phoneWithPrepend.AddState(phoneNumber._states);

            phoneWithPrepend.StartingStates.AddRange(prepend.StartingStates);
            phoneWithPrepend.EndingStates = phoneNumber.EndingStates;

            AddTransitionToStateMachine(prepend, phoneNumber, 1d);

            return phoneWithPrepend;
        }

        /// <summary>
        /// Gets the state machine for an eleven digit Motorola unicode phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_MotoElevenUnicode()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_MotoElevenUnicode };

            State null1 = new State { Name = "Null1", ParentStateMachine = phoneNumber };
            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State null2 = new State { Name = "Null2", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State null3 = new State { Name = "Null3", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State null4 = new State { Name = "Null4", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State null5 = new State { Name = "Null5", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State null6 = new State { Name = "Null6", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State null7 = new State { Name = "Null7", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State null8 = new State { Name = "Null8", ParentStateMachine = phoneNumber };
            State digit8 = new State { Name = "Digit8", ParentStateMachine = phoneNumber };
            State null9 = new State { Name = "Null9", ParentStateMachine = phoneNumber };
            State digit9 = new State { Name = "Digit9", ParentStateMachine = phoneNumber };
            State null10 = new State { Name = "Null10", ParentStateMachine = phoneNumber };
            State digit10 = new State { Name = "Digit10", ParentStateMachine = phoneNumber };
            State null11 = new State { Name = "Null11", ParentStateMachine = phoneNumber };
            State digit11 = new State { Name = "Digit11", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(null1);
            phoneNumber.AddState(digit1);
            phoneNumber.AddState(null2);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(null3);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(null4);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(null5);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(null6);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(null7);
            phoneNumber.AddState(digit7);
            phoneNumber.AddState(null8);
            phoneNumber.AddState(digit8);
            phoneNumber.AddState(null9);
            phoneNumber.AddState(digit9);
            phoneNumber.AddState(null10);
            phoneNumber.AddState(digit10);
            phoneNumber.AddState(null11);
            phoneNumber.AddState(digit11);

            phoneNumber.StartingStates.Add(null1);
            phoneNumber.EndingStates.Add(digit11);

            AddTransition(null1, digit1, 1d);
            AddTransition(digit1, null2, 1d);
            AddTransition(null2, digit2, 1d);
            AddTransition(digit2, null3, 1d);
            AddTransition(null3, digit3, 1d);
            AddTransition(digit3, null4, 1d);
            AddTransition(null4, digit4, 1d);
            AddTransition(digit4, null5, 1d);
            AddTransition(null5, digit5, 1d);
            AddTransition(digit5, null6, 1d);
            AddTransition(null6, digit6, 1d);
            AddTransition(digit6, null7, 1d);
            AddTransition(null7, digit7, 1d);
            AddTransition(digit7, null8, 1d);
            AddTransition(null8, digit8, 1d);
            AddTransition(digit8, null9, 1d);
            AddTransition(null9, digit9, 1d);
            AddTransition(digit9, null10, 1d);
            AddTransition(null10, digit10, 1d);
            AddTransition(digit10, null11, 1d);
            AddTransition(null11, digit11, 1d);

            null1.PossibleValueProbabilities[0x00] = 1d;
            null2.PossibleValueProbabilities[0x00] = 1d;
            null3.PossibleValueProbabilities[0x00] = 1d;
            null4.PossibleValueProbabilities[0x00] = 1d;
            null5.PossibleValueProbabilities[0x00] = 1d;
            null6.PossibleValueProbabilities[0x00] = 1d;
            null7.PossibleValueProbabilities[0x00] = 1d;
            null8.PossibleValueProbabilities[0x00] = 1d;
            null9.PossibleValueProbabilities[0x00] = 1d;
            null10.PossibleValueProbabilities[0x00] = 1d;
            null11.PossibleValueProbabilities[0x00] = 1d;


            //Digit 1 is always a '1'
            digit1.PossibleValueProbabilities[0x31] = 1d;

            //Digits taking values 2-9
            for (byte i = 0x32; i <= 0x39; i++)
            {

                digit2.PossibleValueProbabilities[i] = 1 / 8d;
                digit5.PossibleValueProbabilities[i] = 1 / 8d;
            }

            //Digits taking values 0-9
            for (byte i = 0x30; i <= 0x39; i++)
            {
                digit3.PossibleValueProbabilities[i] = 0.01d;
                digit4.PossibleValueProbabilities[i] = 0.01d;
                digit6.PossibleValueProbabilities[i] = 0.01d;
                digit7.PossibleValueProbabilities[i] = 0.01d;
                digit8.PossibleValueProbabilities[i] = 0.01d;
                digit9.PossibleValueProbabilities[i] = 0.01d;
                digit10.PossibleValueProbabilities[i] = 0.01d;
                digit11.PossibleValueProbabilities[i] = 0.01d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a seven digit Motorola unicode phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_MotoSevenUnicode()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_MotoSevenUnicode };

            State null1 = new State { Name = "Null1", ParentStateMachine = phoneNumber };
            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State null2 = new State { Name = "Null2", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State null3 = new State { Name = "Null3", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State null4 = new State { Name = "Null4", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State null5 = new State { Name = "Null5", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State null6 = new State { Name = "Null6", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State null7 = new State { Name = "Null7", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(null1);
            phoneNumber.AddState(digit1);
            phoneNumber.AddState(null2);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(null3);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(null4);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(null5);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(null6);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(null7);
            phoneNumber.AddState(digit7);

            phoneNumber.StartingStates.Add(null1);
            phoneNumber.EndingStates.Add(digit7);

            AddTransition(null1, digit1, 1d);
            AddTransition(digit1, null2, 1d);
            AddTransition(null2, digit2, 1d);
            AddTransition(digit2, null3, 1d);
            AddTransition(null3, digit3, 1d);
            AddTransition(digit3, null4, 1d);
            AddTransition(null4, digit4, 1d);
            AddTransition(digit4, null5, 1d);
            AddTransition(null5, digit5, 1d);
            AddTransition(digit5, null6, 1d);
            AddTransition(null6, digit6, 1d);
            AddTransition(digit6, null7, 1d);
            AddTransition(null7, digit7, 1d);

            null1.PossibleValueProbabilities[0x00] = 1d;
            null2.PossibleValueProbabilities[0x00] = 1d;
            null3.PossibleValueProbabilities[0x00] = 1d;
            null4.PossibleValueProbabilities[0x00] = 1d;
            null5.PossibleValueProbabilities[0x00] = 1d;
            null6.PossibleValueProbabilities[0x00] = 1d;
            null7.PossibleValueProbabilities[0x00] = 1d;

            //Digits taking values 2-9
            for (byte i = 0x32; i <= 0x39; i++)
            {

                digit1.PossibleValueProbabilities[i] = 1 / 8d;
            }

            //Digits taking values 0-9
            for (byte i = 0x30; i <= 0x39; i++)
            {
                digit3.PossibleValueProbabilities[i] = 0.01d;
                digit5.PossibleValueProbabilities[i] = 0.01d;
                digit6.PossibleValueProbabilities[i] = 0.01d;
                digit7.PossibleValueProbabilities[i] = 0.01d;
                digit4.PossibleValueProbabilities[i] = 0.01d;
                digit2.PossibleValueProbabilities[i] = 0.01d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a ten digit Motorola unicode phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_MotoTenUnicode()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_MotoTenUnicode };

            State null1 = new State { Name = "Null1", ParentStateMachine = phoneNumber };
            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State null2 = new State { Name = "Null2", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State null3 = new State { Name = "Null3", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State null4 = new State { Name = "Null4", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State null5 = new State { Name = "Null5", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State null6 = new State { Name = "Null6", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State null7 = new State { Name = "Null7", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State null8 = new State { Name = "Null8", ParentStateMachine = phoneNumber };
            State digit8 = new State { Name = "Digit8", ParentStateMachine = phoneNumber };
            State null9 = new State { Name = "Null9", ParentStateMachine = phoneNumber };
            State digit9 = new State { Name = "Digit9", ParentStateMachine = phoneNumber };
            State null10 = new State { Name = "Null10", ParentStateMachine = phoneNumber };
            State digit10 = new State { Name = "Digit10", ParentStateMachine = phoneNumber };


            phoneNumber.AddState(null1);
            phoneNumber.AddState(digit1);
            phoneNumber.AddState(null2);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(null3);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(null4);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(null5);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(null6);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(null7);
            phoneNumber.AddState(digit7);
            phoneNumber.AddState(null8);
            phoneNumber.AddState(digit8);
            phoneNumber.AddState(null9);
            phoneNumber.AddState(digit9);
            phoneNumber.AddState(null10);
            phoneNumber.AddState(digit10);

            phoneNumber.StartingStates.Add(null1);
            phoneNumber.EndingStates.Add(digit10);

            AddTransition(null1, digit1, 1d);
            AddTransition(digit1, null2, 1d);
            AddTransition(null2, digit2, 1d);
            AddTransition(digit2, null3, 1d);
            AddTransition(null3, digit3, 1d);
            AddTransition(digit3, null4, 1d);
            AddTransition(null4, digit4, 1d);
            AddTransition(digit4, null5, 1d);
            AddTransition(null5, digit5, 1d);
            AddTransition(digit5, null6, 1d);
            AddTransition(null6, digit6, 1d);
            AddTransition(digit6, null7, 1d);
            AddTransition(null7, digit7, 1d);
            AddTransition(digit7, null8, 1d);
            AddTransition(null8, digit8, 1d);
            AddTransition(digit8, null9, 1d);
            AddTransition(null9, digit9, 1d);
            AddTransition(digit9, null10, 1d);
            AddTransition(null10, digit10, 1d);

            null1.PossibleValueProbabilities[0x00] = 1d;
            null2.PossibleValueProbabilities[0x00] = 1d;
            null3.PossibleValueProbabilities[0x00] = 1d;
            null4.PossibleValueProbabilities[0x00] = 1d;
            null5.PossibleValueProbabilities[0x00] = 1d;
            null6.PossibleValueProbabilities[0x00] = 1d;
            null7.PossibleValueProbabilities[0x00] = 1d;
            null8.PossibleValueProbabilities[0x00] = 1d;
            null9.PossibleValueProbabilities[0x00] = 1d;
            null10.PossibleValueProbabilities[0x00] = 1d;


            //Digits taking values 2-9
            for (byte i = 0x32; i <= 0x39; i++)
            {

                digit1.PossibleValueProbabilities[i] = 1 / 8d;
                digit4.PossibleValueProbabilities[i] = 1 / 8d;
            }

            //Digits taking values 0-9
            for (byte i = 0x30; i <= 0x39; i++)
            {
                digit3.PossibleValueProbabilities[i] = 0.01d;
                digit5.PossibleValueProbabilities[i] = 0.01d;
                digit6.PossibleValueProbabilities[i] = 0.01d;
                digit7.PossibleValueProbabilities[i] = 0.01d;
                digit8.PossibleValueProbabilities[i] = 0.01d;
                digit9.PossibleValueProbabilities[i] = 0.01d;
                digit10.PossibleValueProbabilities[i] = 0.01d;
                digit2.PossibleValueProbabilities[i] = 0.01d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a seven digit Motorola phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_MotoSevenDigit()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_MotoSevenDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State type = new State { Name = "Type", ParentStateMachine = phoneNumber };
            State digit21 = new State { Name = "Digit2_1", ParentStateMachine = phoneNumber };
            State digit43 = new State { Name = "Digit4_3", ParentStateMachine = phoneNumber };
            State digit65 = new State { Name = "Digit6_5", ParentStateMachine = phoneNumber };
            State digitF7 = new State { Name = "DigitF_7", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(type);
            phoneNumber.AddState(digit21);
            phoneNumber.AddState(digit43);
            phoneNumber.AddState(digit65);
            phoneNumber.AddState(digitF7);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digitF7);

            AddTransition(length, type, 1d);
            AddTransition(type, digit21, 1d);
            AddTransition(digit21, digit43, 1d);
            AddTransition(digit43, digit65, 1d);
            AddTransition(digit65, digitF7, 1d);

            length.PossibleValueProbabilities[0x05] = 1d;

            type.PossibleValueProbabilities[0x91] = 1d;
            type.PossibleValueProbabilities[0x81] = 1d;

            for (byte i = 0x02; i <= 0x92; i += 0x10)
            {
                for (byte j = i; j <= i + 0x07; j++)
                {
                    digit21.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x00; i <= 0x90; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digit65.PossibleValueProbabilities[j] = 0.01d;
                    digit43.PossibleValueProbabilities[j] = 0.01d;

                }
            }

            for (byte i = 0xF0; i <= 0xF9; i++)
            {
                digitF7.PossibleValueProbabilities[i] = 0.1d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a ten digit Motorola phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_MotoTenDigit()
        {

            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_MotoTenDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State type = new State { Name = "Type", ParentStateMachine = phoneNumber };
            State digit21 = new State { Name = "Digit2_1", ParentStateMachine = phoneNumber };
            State digit43 = new State { Name = "Digit4_3", ParentStateMachine = phoneNumber };
            State digit65 = new State { Name = "Digit6_5", ParentStateMachine = phoneNumber };
            State digit87 = new State { Name = "Digit8_7", ParentStateMachine = phoneNumber };
            State digit109 = new State { Name = "Digit10_9", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(type);
            phoneNumber.AddState(digit21);
            phoneNumber.AddState(digit43);
            phoneNumber.AddState(digit65);
            phoneNumber.AddState(digit87);
            phoneNumber.AddState(digit109);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digit109);

            AddTransition(length, type, 1d);
            AddTransition(type, digit21, 1d);
            AddTransition(digit21, digit43, 1d);
            AddTransition(digit43, digit65, 1d);
            AddTransition(digit65, digit87, 1d);
            AddTransition(digit87, digit109, 1d);

            length.PossibleValueProbabilities[0x06] = 1d;

            type.PossibleValueProbabilities[0x91] = 1d;
            type.PossibleValueProbabilities[0x81] = 1d;

            for (byte i = 0x02; i <= 0x92; i += 0x10)
            {
                for (byte j = i; j <= i + 0x07; j++)
                {
                    digit21.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x20; i <= 0x90; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digit43.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x00; i <= 0x90; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digit65.PossibleValueProbabilities[j] = 0.01d;
                    digit87.PossibleValueProbabilities[j] = 0.01d;
                    digit109.PossibleValueProbabilities[j] = 0.01d;
                }
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for an eleven digit Motorola phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_MotoElevenDigit()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_MotoElevenDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State type = new State { Name = "Type", ParentStateMachine = phoneNumber };
            State digit21 = new State { Name = "Digit2_1", ParentStateMachine = phoneNumber };
            State digit43 = new State { Name = "Digit4_3", ParentStateMachine = phoneNumber };
            State digit65 = new State { Name = "Digit6_5", ParentStateMachine = phoneNumber };
            State digit87 = new State { Name = "Digit8_7", ParentStateMachine = phoneNumber };
            State digit109 = new State { Name = "Digit10_9", ParentStateMachine = phoneNumber };
            State digitF11 = new State { Name = "DigitF_11", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(type);
            phoneNumber.AddState(digit21);
            phoneNumber.AddState(digit43);
            phoneNumber.AddState(digit65);
            phoneNumber.AddState(digit87);
            phoneNumber.AddState(digit109);
            phoneNumber.AddState(digitF11);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digitF11);

            AddTransition(length, type, 1d);
            AddTransition(type, digit21, 1d);
            AddTransition(digit21, digit43, 1d);
            AddTransition(digit43, digit65, 1d);
            AddTransition(digit65, digit87, 1d);
            AddTransition(digit87, digit109, 1d);
            AddTransition(digit109, digitF11, 1d);

            length.PossibleValueProbabilities[0x07] = 1d;

            type.PossibleValueProbabilities[0x91] = 1d;
            type.PossibleValueProbabilities[0x81] = 1d;

            for (byte i = 0x02; i <= 0x92; i += 0x10)
            {
                for (byte j = i; j <= i + 0x07; j++)
                {
                    digit65.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x20; i <= 0x90; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digit21.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x00; i <= 0x90; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digit43.PossibleValueProbabilities[j] = 0.01d;
                    digit87.PossibleValueProbabilities[j] = 0.01d;
                    digit109.PossibleValueProbabilities[j] = 0.01d;
                }
            }

            for (byte i = 0xF0; i <= 0xF9; i++)
            {
                digitF11.PossibleValueProbabilities[i] = 0.1d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a ten digit Samsung ASCII phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_SamsungTenDigitAscii()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_SamsungTenDigitAscii };

            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State hiphen1 = new State { Name = "Hiphen1", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State hiphen2 = new State { Name = "Hiphen2", ParentStateMachine = phoneNumber };
            State digit8 = new State { Name = "Digit8", ParentStateMachine = phoneNumber };
            State digit9 = new State { Name = "Digit9", ParentStateMachine = phoneNumber };
            State digit10 = new State { Name = "Digit10", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(hiphen1);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(digit7);
            phoneNumber.AddState(hiphen2);
            phoneNumber.AddState(digit8);
            phoneNumber.AddState(digit9);
            phoneNumber.AddState(digit10);

            phoneNumber.StartingStates.Add(digit1);
            phoneNumber.EndingStates.Add(digit10);

            AddTransition(digit1, digit2, 1d);
            AddTransition(digit2, digit3, 1d);
            AddTransition(digit3, digit4, 1 / 2d);
            AddTransition(digit3, hiphen1, 1 / 2d);
            AddTransition(hiphen1, digit4, 1d);
            AddTransition(digit4, digit5, 1d);
            AddTransition(digit5, digit6, 1d);
            AddTransition(digit6, hiphen2, 1 / 2d);
            AddTransition(hiphen2, digit7, 1d);
            AddTransition(digit6, digit7, 1 / 2d);
            AddTransition(digit7, digit8, 1d);
            AddTransition(digit8, digit9, 1d);
            AddTransition(digit9, digit10, 1d);

            digit1.PossibleValueProbabilities[0x31] = 1d;

            for (byte i = 0x32; i <= 0x39; i++)
            {
                digit1.PossibleValueProbabilities[i] = 1 / 8d;
                digit4.PossibleValueProbabilities[i] = 1 / 8d;
            }

            for (byte i = 0x30; i <= 0x39; i++)
            {
                digit2.PossibleValueProbabilities[i] = 0.1d;
                digit3.PossibleValueProbabilities[i] = 0.1d;
                digit5.PossibleValueProbabilities[i] = 0.1d;
                digit6.PossibleValueProbabilities[i] = 0.1d;
                digit7.PossibleValueProbabilities[i] = 0.1d;
                digit8.PossibleValueProbabilities[i] = 0.1d;
                digit9.PossibleValueProbabilities[i] = 0.1d;
                digit10.PossibleValueProbabilities[i] = 0.1d;
            }

            hiphen1.PossibleValueProbabilities[0x2D] = 1d;
            hiphen2.PossibleValueProbabilities[0x2D] = 1d;

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a seven digit Samsung ASCII phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_SamsungSevenDigitAscii()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_SamsungSevenDigitAscii };

            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State hiphen = new State { Name = "Hiphen", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(hiphen);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(digit7);

            phoneNumber.StartingStates.Add(digit1);
            phoneNumber.EndingStates.Add(digit7);

            AddTransition(digit1, digit2, 1d);
            AddTransition(digit2, digit3, 1d);
            AddTransition(digit3, digit4, 1 / 2d);
            AddTransition(digit3, hiphen, 1 / 2d);
            AddTransition(hiphen, digit4, 1d);
            AddTransition(digit4, digit5, 1d);
            AddTransition(digit5, digit6, 1d);
            AddTransition(digit6, digit7, 1d);

            for (byte i = 0x32; i <= 0x39; i++)
            {
                digit1.PossibleValueProbabilities[i] = 1 / 8d;
            }

            for (byte i = 0x30; i <= 0x39; i++)
            {
                digit2.PossibleValueProbabilities[i] = 0.1d;
                digit3.PossibleValueProbabilities[i] = 0.1d;
                digit5.PossibleValueProbabilities[i] = 0.1d;
                digit6.PossibleValueProbabilities[i] = 0.1d;
                digit7.PossibleValueProbabilities[i] = 0.1d;
                digit4.PossibleValueProbabilities[i] = 0.1d;
            }

            hiphen.PossibleValueProbabilities[0x2D] = 1d;

            digit1.NormalizeProbabilities();
            digit2.NormalizeProbabilities();
            digit3.NormalizeProbabilities();
            digit4.NormalizeProbabilities();
            digit5.NormalizeProbabilities();
            digit6.NormalizeProbabilities();
            digit7.NormalizeProbabilities();
            hiphen.NormalizeProbabilities();

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for an eleven digit Samsung ASCII phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_SamsungElevenDigitAscii()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_SamsungElevenDigitAscii };

            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State digit8 = new State { Name = "Digit8", ParentStateMachine = phoneNumber };
            State digit9 = new State { Name = "Digit9", ParentStateMachine = phoneNumber };
            State digit10 = new State { Name = "Digit10", ParentStateMachine = phoneNumber };
            State digit11 = new State { Name = "Digit11", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(digit7);
            phoneNumber.AddState(digit8);
            phoneNumber.AddState(digit9);
            phoneNumber.AddState(digit10);
            phoneNumber.AddState(digit11);

            phoneNumber.StartingStates.Add(digit1);
            phoneNumber.EndingStates.Add(digit11);

            AddTransition(digit1, digit2, 1d);
            AddTransition(digit2, digit3, 1d);
            AddTransition(digit3, digit4, 1d);
            AddTransition(digit4, digit5, 1d);
            AddTransition(digit5, digit6, 1d);
            AddTransition(digit6, digit7, 1d);
            AddTransition(digit7, digit8, 1d);
            AddTransition(digit8, digit9, 1d);
            AddTransition(digit9, digit10, 1d);
            AddTransition(digit10, digit11, 1d);

            digit1.PossibleValueProbabilities[0x31] = 1d;

            for (byte i = 0x32; i <= 0x39; i++)
            {
                digit2.PossibleValueProbabilities[i] = 1 / 8d;
                digit5.PossibleValueProbabilities[i] = 1 / 8d;
            }

            for (byte i = 0x30; i <= 0x39; i++)
            {
                digit3.PossibleValueProbabilities[i] = 0.1d;
                digit4.PossibleValueProbabilities[i] = 0.1d;
                digit6.PossibleValueProbabilities[i] = 0.1d;
                digit7.PossibleValueProbabilities[i] = 0.1d;
                digit8.PossibleValueProbabilities[i] = 0.1d;
                digit9.PossibleValueProbabilities[i] = 0.1d;
                digit10.PossibleValueProbabilities[i] = 0.1d;
                digit11.PossibleValueProbabilities[i] = 0.1d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a seven digit Nokia phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_NokiaSevenDigit()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_NokiaSevenDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State digits12 = new State { Name = "Digit1_2", ParentStateMachine = phoneNumber };
            State digits34 = new State { Name = "Digit3_4", ParentStateMachine = phoneNumber };
            State digits56 = new State { Name = "Digit5_6", ParentStateMachine = phoneNumber };
            State digits70 = new State { Name = "Digit7_0", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(digits12);
            phoneNumber.AddState(digits34);
            phoneNumber.AddState(digits56);
            phoneNumber.AddState(digits70);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digits70);

            AddTransition(length, digits12, 1d);
            AddTransition(digits12, digits34, 1d);
            AddTransition(digits34, digits56, 1d);
            AddTransition(digits56, digits70, 1d);

            length.PossibleValueProbabilities[0x07] = 1d;

            for (byte i = 0x21; i <= 0x91; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits12.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x11; i <= 0xA1; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits34.PossibleValueProbabilities[j] = 0.01d;
                    digits56.PossibleValueProbabilities[j] = 0.01d;
                }
            }

            for (byte i = 0x10; i <= 0xA0; i += 0x10)
            {
                digits70.PossibleValueProbabilities[i] = 0.1d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for an eight digit Nokia phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_NokiaEightDigit()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_NokiaEightDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State digitsf1 = new State { Name = "DigitF_1", ParentStateMachine = phoneNumber };
            State digits23 = new State { Name = "Digit2_3", ParentStateMachine = phoneNumber };
            State digits45 = new State { Name = "Digit4_5", ParentStateMachine = phoneNumber };
            State digits67 = new State { Name = "Digit6_7", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(digitsf1);
            phoneNumber.AddState(digits23);
            phoneNumber.AddState(digits45);
            phoneNumber.AddState(digits67);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digits67);

            AddTransition(length, digitsf1, 1d);
            AddTransition(digitsf1, digits23, 1d);
            AddTransition(digits23, digits45, 1d);
            AddTransition(digits45, digits67, 1d);

            length.PossibleValueProbabilities[0x08] = 1d;

            for (byte i = 0xf2; i <= 0xf9; i++)
            {
                digitsf1.PossibleValueProbabilities[i] = 1 / 8d;
            }

            for (byte i = 0x11; i <= 0xA1; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits23.PossibleValueProbabilities[j] = 0.01d;
                    digits45.PossibleValueProbabilities[j] = 0.01d;
                    digits67.PossibleValueProbabilities[j] = 0.01d;
                }
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a ten digit Nokia phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_NokiaTenDigit()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_NokiaTenDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State digits12 = new State { Name = "Digit1_2", ParentStateMachine = phoneNumber };
            State digits34 = new State { Name = "Digit3_4", ParentStateMachine = phoneNumber };
            State digits56 = new State { Name = "Digit5_6", ParentStateMachine = phoneNumber };
            State digits78 = new State { Name = "Digit7_8", ParentStateMachine = phoneNumber };
            State digits910 = new State { Name = "Digit9_10", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(digits12);
            phoneNumber.AddState(digits34);
            phoneNumber.AddState(digits56);
            phoneNumber.AddState(digits78);
            phoneNumber.AddState(digits910);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digits910);

            AddTransition(length, digits12, 1d);
            AddTransition(digits12, digits34, 1d);
            AddTransition(digits34, digits56, 1d);
            AddTransition(digits56, digits78, 1d);
            AddTransition(digits78, digits910, 1d);

            length.PossibleValueProbabilities[0x0A] = 1d;

            for (byte i = 0x21; i <= 0x91; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits12.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x12; i <= 0xa2; i += 0x10)
            {
                for (byte j = i; j <= i + 0x07; j++)
                {
                    digits34.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x11; i <= 0xa1; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits56.PossibleValueProbabilities[j] = 0.01d;
                    digits78.PossibleValueProbabilities[j] = 0.01d;
                    digits910.PossibleValueProbabilities[j] = 0.01d;
                }
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for an eleven digit Nokia phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_NokiaElevenDigit()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_NokiaElevenDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State digits12 = new State { Name = "Digit1_2", ParentStateMachine = phoneNumber };
            State digits34 = new State { Name = "Digit3_4", ParentStateMachine = phoneNumber };
            State digits56 = new State { Name = "Digit5_6", ParentStateMachine = phoneNumber };
            State digits78 = new State { Name = "Digit7_8", ParentStateMachine = phoneNumber };
            State digits910 = new State { Name = "Digit9_10", ParentStateMachine = phoneNumber };
            State digits110 = new State { Name = "Digit11_0", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(digits12);
            phoneNumber.AddState(digits34);
            phoneNumber.AddState(digits56);
            phoneNumber.AddState(digits78);
            phoneNumber.AddState(digits910);
            phoneNumber.AddState(digits110);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digits110);

            AddTransition(length, digits12, 1d);
            AddTransition(digits12, digits34, 1d);
            AddTransition(digits34, digits56, 1d);
            AddTransition(digits56, digits78, 1d);
            AddTransition(digits78, digits910, 1d);
            AddTransition(digits910, digits110, 1d);

            length.PossibleValueProbabilities[0x0b] = 1d;
            length.PossibleValueProbabilities[0x11] = 1d;

            length.NormalizeProbabilities();

            for (byte i = 0x12; i <= 0x19; i++)
            {
                digits12.PossibleValueProbabilities[i] = 1 / 8d;
            }

            for (byte i = 0x11; i <= 0xA1; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits34.PossibleValueProbabilities[j] = 0.01d;
                    digits78.PossibleValueProbabilities[j] = 0.01d;
                    digits910.PossibleValueProbabilities[j] = 0.01d;
                }
            }

            for (byte i = 0x21; i <= 0x91; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits56.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x10; i <= 0xA0; i += 0x10)
            {
                digits110.PossibleValueProbabilities[i] = 0.1d;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a twelve digit Nokia phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_NokiaTwelveDigit()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_NokiaTwelveDigit };

            State length = new State { Name = "Length", ParentStateMachine = phoneNumber };
            State digitsF1 = new State { Name = "DigitF_1", ParentStateMachine = phoneNumber };
            State digits23 = new State { Name = "Digit2_3", ParentStateMachine = phoneNumber };
            State digits45 = new State { Name = "Digit4_5", ParentStateMachine = phoneNumber };
            State digits67 = new State { Name = "Digit6_7", ParentStateMachine = phoneNumber };
            State digits89 = new State { Name = "Digit8_9", ParentStateMachine = phoneNumber };
            State digits1011 = new State { Name = "Digit10_11", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(length);
            phoneNumber.AddState(digitsF1);
            phoneNumber.AddState(digits23);
            phoneNumber.AddState(digits45);
            phoneNumber.AddState(digits67);
            phoneNumber.AddState(digits89);
            phoneNumber.AddState(digits1011);

            phoneNumber.StartingStates.Add(length);
            phoneNumber.EndingStates.Add(digits1011);

            AddTransition(length, digitsF1, 1d);
            AddTransition(digitsF1, digits23, 1d);
            AddTransition(digits23, digits45, 1d);
            AddTransition(digits45, digits67, 1d);
            AddTransition(digits67, digits89, 1d);
            AddTransition(digits89, digits1011, 1d);

            length.PossibleValueProbabilities[0x0C] = 1d;
            length.PossibleValueProbabilities[0x11] = 1d;

            length.NormalizeProbabilities();

            digitsF1.PossibleValueProbabilities[0xf1] = 1d;

            for (byte i = 0x21; i <= 0x91; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits23.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x12; i <= 0xa2; i += 0x10)
            {
                for (byte j = i; j <= i + 0x07; j++)
                {
                    digits45.PossibleValueProbabilities[j] = 1 / 80d;
                }
            }

            for (byte i = 0x11; i <= 0xa1; i += 0x10)
            {
                for (byte j = i; j <= i + 0x09; j++)
                {
                    digits67.PossibleValueProbabilities[j] = 0.01d;
                    digits89.PossibleValueProbabilities[j] = 0.01d;
                    digits1011.PossibleValueProbabilities[j] = 0.01d;
                }
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a ten digit international format phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_InternationalFormatTenDigit()
        {
            #region Prepend
            StateMachine prepend = new StateMachine { Name = MachineList.Prepend_InternationalTenDigit };

            State length = new State { Name = "Length", ParentStateMachine = prepend };
            State type = new State { Name = "Type", ParentStateMachine = prepend };


            prepend.AddState(length);
            prepend.AddState(type);


            prepend.StartingStates.Add(length);
            prepend.StartingStates.Add(type);
            prepend.EndingStates.Add(type);

            AddTransition(length, type, 1d);

            length.PossibleValueProbabilities[0x0a] = 0.5f;
            length.PossibleValueProbabilities[0x07] = 0.5f;
            type.PossibleValueProbabilities[0x81] = 0.5f;
            type.PossibleValueProbabilities[0x91] = 0.5f;
            #endregion

            #region PhoneNumber

            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_InternationalFormatTenDigit };

            State digit21 = new State { Name = "Digit2_1", ParentStateMachine = phoneNumber };
            State digit43 = new State { Name = "Digit4_3", ParentStateMachine = phoneNumber };
            State digit65 = new State { Name = "Digit6_5", ParentStateMachine = phoneNumber };
            State digit87 = new State { Name = "Digit8_7", ParentStateMachine = phoneNumber };
            State digit109 = new State { Name = "Digit10_9", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(digit21);
            phoneNumber.AddState(digit43);
            phoneNumber.AddState(digit65);
            phoneNumber.AddState(digit87);
            phoneNumber.AddState(digit109);

            phoneNumber.StartingStates.Add(digit21);
            phoneNumber.EndingStates.Add(digit109);

            AddTransition(digit21, digit43, 1d);
            AddTransition(digit43, digit65, 1d);
            AddTransition(digit65, digit87, 1d);
            AddTransition(digit87, digit109, 1d);

            //both nibbles can take 0-9
            for (byte j = 0x00; j <= 0x90; j += 0x10)
            {
                for (byte i = j; i <= j + 0x09; i++)
                {
                    digit65.PossibleValueProbabilities[i] = 1d;
                    digit87.PossibleValueProbabilities[i] = 1d;
                    digit109.PossibleValueProbabilities[i] = 1d;
                }
            }

            //leading nibble can take 0-9 and the other can take 2-9. 
            for (byte j = 0x02; j <= 0x92; j += 0x10)
            {
                for (byte i = j; i <= j + 0x07; i++)
                {
                    digit21.PossibleValueProbabilities[i] = 1d;
                }
            }

            //leading nibble can take 2-9 and the other can take 0-9
            for (byte j = 0x20; j <= 0x90; j += 0x10)
            {
                for (byte i = j; i <= j + 0x09; i++)
                {
                    digit43.PossibleValueProbabilities[i] = 1d;
                }
            }

            digit21.NormalizeProbabilities();
            digit43.NormalizeProbabilities();
            digit65.NormalizeProbabilities();
            digit87.NormalizeProbabilities();
            digit109.NormalizeProbabilities();

            #endregion

            StateMachine phoneWithPrepend = new StateMachine { Name = MachineList.PhoneNumber_InternaltionalFormatWithPrepend };

            phoneWithPrepend.AddState(prepend._states);
            phoneWithPrepend.AddState(phoneNumber._states);

            phoneWithPrepend.StartingStates.AddRange(prepend.StartingStates);
            phoneWithPrepend.EndingStates = phoneNumber.EndingStates;

            AddTransitionToStateMachine(prepend, phoneNumber, 1d);

            return phoneWithPrepend;
        }

        /// <summary>
        /// Gets the state machine for an eleven digit international format phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_InternationalFormatElevenDigit()
        {
            #region Prepend
            StateMachine prepend = new StateMachine { Name = MachineList.Prepend_InternationalElevenDigit };

            State length = new State { Name = "Length", ParentStateMachine = prepend };
            State type = new State { Name = "Type", ParentStateMachine = prepend };


            prepend.AddState(length);
            prepend.AddState(type);


            prepend.StartingStates.Add(length);
            prepend.StartingStates.Add(type);
            prepend.EndingStates.Add(type);

            AddTransition(length, type, 1d);

            length.PossibleValueProbabilities[0x0b] = 1f;
            length.PossibleValueProbabilities[0x08] = 1f;
            type.PossibleValueProbabilities[0x81] = 0.5f;
            type.PossibleValueProbabilities[0x91] = 0.5f;

            length.NormalizeProbabilities();
            type.NormalizeProbabilities();
            #endregion

            #region Phone Number

            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_InternationalFormatElevenDigit };

            State digit21 = new State { Name = "Digit2_1", ParentStateMachine = phoneNumber };
            State digit43 = new State { Name = "Digit4_3", ParentStateMachine = phoneNumber };
            State digit65 = new State { Name = "Digit6_5", ParentStateMachine = phoneNumber };
            State digit87 = new State { Name = "Digit8_7", ParentStateMachine = phoneNumber };
            State digit109 = new State { Name = "Digit10_9", ParentStateMachine = phoneNumber };
            State digitF11 = new State { Name = "DigitF_11", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(digit21);
            phoneNumber.AddState(digit43);
            phoneNumber.AddState(digit65);
            phoneNumber.AddState(digit87);
            phoneNumber.AddState(digit109);
            phoneNumber.AddState(digitF11);

            phoneNumber.StartingStates.Add(digit21);
            phoneNumber.EndingStates.Add(digitF11);

            AddTransition(digit21, digit43, 1d);
            AddTransition(digit43, digit65, 1d);
            AddTransition(digit65, digit87, 1d);
            AddTransition(digit87, digit109, 1d);
            AddTransition(digit109, digitF11, 1d);

            //Digit1 0x21, 0x31, ..., 0x91
            for (byte i = 0x21; i <= 0x91; i += 0x10)
            {
                digit21.PossibleValueProbabilities[i] = 1f;
            }

            for (byte j = 0x00; j <= 0x90; j += 0x10)
            {
                for (byte i = j; i <= j + 0x09; i++)
                {
                    digit43.PossibleValueProbabilities[i] = 1f;
                    digit87.PossibleValueProbabilities[i] = 1f;
                    digit109.PossibleValueProbabilities[i] = 1f;
                }
            }

            for (byte j = 0x02; j <= 0x92; j += 0x10)
            {
                for (byte i = j; i <= j + 0x07; i++)
                {
                    digit65.PossibleValueProbabilities[i] = 1f;
                }
            }

            for (byte i = 0xf0; i <= 0xf9; i++)
            {
                digitF11.PossibleValueProbabilities[i] = 1f;
            }

            digit21.NormalizeProbabilities();
            digit43.NormalizeProbabilities();
            digit65.NormalizeProbabilities();
            digit87.NormalizeProbabilities();
            digitF11.NormalizeProbabilities();

            #endregion

            StateMachine phoneWithPrepend = new StateMachine { Name = MachineList.PhoneNumber_InternaltionalFormatWithPrepend };

            phoneWithPrepend.AddState(prepend._states);
            phoneWithPrepend.AddState(phoneNumber._states);

            phoneWithPrepend.StartingStates.AddRange(prepend.StartingStates);
            phoneWithPrepend.EndingStates = phoneNumber.EndingStates;

            AddTransitionToStateMachine(prepend, phoneNumber, 1d);

            return phoneWithPrepend;
        }

        /// <summary>
        /// Gets the state machine for a seven digit international format phone number.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetPhoneNumber_InternationalFormatSevenDigit()
        {
            #region Prepend
            StateMachine prepend = new StateMachine { Name = MachineList.Prepend_InternationalSevenDigit };

            State length = new State { Name = "Length", ParentStateMachine = prepend };
            State type = new State { Name = "Type", ParentStateMachine = prepend };


            prepend.AddState(length);
            prepend.AddState(type);


            prepend.StartingStates.Add(length);
            prepend.StartingStates.Add(type);
            prepend.EndingStates.Add(type);

            AddTransition(length, type, 1d);

            length.PossibleValueProbabilities[0x07] = 0.5f;
            length.PossibleValueProbabilities[0x04] = 0.5f;
            type.PossibleValueProbabilities[0x81] = 0.5f;
            type.PossibleValueProbabilities[0x91] = 0.5f;
            #endregion

            #region Phone Number

            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_InternationalFormatSevenDigit };

            State digit21 = new State { Name = "Digit2_1", ParentStateMachine = phoneNumber };
            State digit43 = new State { Name = "Digit4_3", ParentStateMachine = phoneNumber };
            State digit65 = new State { Name = "Digit6_5", ParentStateMachine = phoneNumber };
            State digitF7 = new State { Name = "DigitF_7", ParentStateMachine = phoneNumber };

            phoneNumber.AddState(digit21);
            phoneNumber.AddState(digit43);
            phoneNumber.AddState(digit65);
            phoneNumber.AddState(digitF7);

            phoneNumber.StartingStates.Add(digit21);
            phoneNumber.EndingStates.Add(digitF7);

            AddTransition(digit21, digit43, 1d);
            AddTransition(digit43, digit65, 1d);
            AddTransition(digit65, digitF7, 1d);

            //both nibbles can take 0-9
            for (byte j = 0x00; j <= 0x90; j += 0x10)
            {
                for (byte i = j; i <= j + 0x09; i++)
                {
                    digit65.PossibleValueProbabilities[i] = 1f;
                    digit43.PossibleValueProbabilities[i] = 1f;
                }
            }

            //leading nibble can take 0-9 and the other can take 2-9. 
            for (byte j = 0x02; j <= 0x92; j += 0x10)
            {
                for (byte i = j; i <= j + 0x07; i++)
                {
                    digit21.PossibleValueProbabilities[i] = 1f;
                }
            }

            //leading nibble can only take f, the rest can take 2-9
            for (byte i = 0xf0; i <= 0xf9; i++)
            {
                digitF7.PossibleValueProbabilities[i] = 1f;
            }

            digit21.NormalizeProbabilities();
            digit43.NormalizeProbabilities();
            digit65.NormalizeProbabilities();
            digitF7.NormalizeProbabilities();

            #endregion

            StateMachine phoneWithPrepend = new StateMachine { Name = MachineList.PhoneNumber_InternaltionalFormatWithPrepend };

            phoneWithPrepend.AddState(prepend._states);
            phoneWithPrepend.AddState(phoneNumber._states);

            phoneWithPrepend.StartingStates.AddRange(prepend.StartingStates);
            phoneWithPrepend.EndingStates = phoneNumber.EndingStates;

            AddTransitionToStateMachine(prepend, phoneNumber, 1d);

            return phoneWithPrepend;
        }

        public static StateMachine GetPhoneNumber_NokiaNumberIndex(int weight)
        {
            var indexMachine = new StateMachine { Name = MachineList.PhoneNumberIndex_Nokia, _weight = weight };

            State index = new State { Name = "Number index", ParentStateMachine = indexMachine };

            indexMachine.AddState(index);
            indexMachine.StartingStates.Add(index);
            indexMachine.EndingStates.Add(index);

            for (byte i = 0x01; i < 0x10; i++)
            {
                index.PossibleValueProbabilities[i] = 1d;
            }

            index.NormalizeProbabilities();

            return indexMachine;
        }

        public static StateMachine GetPhoneNumber_UKAsciiTrunk0()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_UKAsciiTrunk0 };
            State trunk0 = new State { Name = "Trunk0", ParentStateMachine = phoneNumber };
            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State digit8 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State digit9 = new State { Name = "Digit9", ParentStateMachine = phoneNumber };
            State digit10 = new State { Name = "Digit10", ParentStateMachine = phoneNumber };

            trunk0.PossibleValueProbabilities[48] = 1d; // '0'
            for (int n = 48; n <= 57; n++)
            {
                if (n != 48) digit1.PossibleValueProbabilities[n] = 1d / 9d;
                digit2.PossibleValueProbabilities[n] = 1d / 10d;
                digit3.PossibleValueProbabilities[n] = 1d / 10d;
                digit4.PossibleValueProbabilities[n] = 1d / 10d;
                digit5.PossibleValueProbabilities[n] = 1d / 10d;
                digit6.PossibleValueProbabilities[n] = 1d / 10d;
                digit7.PossibleValueProbabilities[n] = 1d / 10d;
                digit8.PossibleValueProbabilities[n] = 1d / 10d;
                digit9.PossibleValueProbabilities[n] = 1d / 10d;
                digit10.PossibleValueProbabilities[n] = 1d / 10d;
            }

            phoneNumber.AddState(trunk0);
            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(digit7);
            phoneNumber.AddState(digit8);
            phoneNumber.AddState(digit9);
            phoneNumber.AddState(digit10);

            phoneNumber.StartingStates.Add(trunk0);
            phoneNumber.EndingStates.Add(digit9);
            phoneNumber.EndingStates.Add(digit10);

            AddTransition(trunk0, digit1, 1d);
            AddTransition(digit1, digit2, 1d);
            AddTransition(digit2, digit3, 1d);
            AddTransition(digit3, digit4, 1d);
            AddTransition(digit4, digit5, 1d);
            AddTransition(digit5, digit6, 1d);
            AddTransition(digit6, digit7, 1d);
            AddTransition(digit7, digit8, 1d);
            AddTransition(digit8, digit9, 1d);
            AddTransition(digit9, digit10, 1d);

            return phoneNumber;
        }

        public static StateMachine GetPhoneNumber_UKAsciiCountryCode()
        {
            StateMachine phoneNumber = new StateMachine { Name = MachineList.PhoneNumber_UKAsciiCountryCode };

            State plus = new State { Name = "Plus", ParentStateMachine = phoneNumber };
            State ccode1 = new State { Name = "Ccode1", ParentStateMachine = phoneNumber };
            State ccode2 = new State { Name = "Ccode2", ParentStateMachine = phoneNumber };
            State digit1 = new State { Name = "Digit1", ParentStateMachine = phoneNumber };
            State digit2 = new State { Name = "Digit2", ParentStateMachine = phoneNumber };
            State digit3 = new State { Name = "Digit3", ParentStateMachine = phoneNumber };
            State digit4 = new State { Name = "Digit4", ParentStateMachine = phoneNumber };
            State digit5 = new State { Name = "Digit5", ParentStateMachine = phoneNumber };
            State digit6 = new State { Name = "Digit6", ParentStateMachine = phoneNumber };
            State digit7 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State digit8 = new State { Name = "Digit7", ParentStateMachine = phoneNumber };
            State digit9 = new State { Name = "Digit9", ParentStateMachine = phoneNumber };
            State digit10 = new State { Name = "Digit10", ParentStateMachine = phoneNumber };

            plus.PossibleValueProbabilities[43] = 1d; // '+'
            ccode1.PossibleValueProbabilities[52] = 1d; // '4'
            ccode2.PossibleValueProbabilities[52] = 1d; // '4'

            for (int n = 48; n <= 57; n++)
            {
                if (n != 48) digit1.PossibleValueProbabilities[n] = 1d / 9d;
                digit2.PossibleValueProbabilities[n] = 1d / 10d;
                digit3.PossibleValueProbabilities[n] = 1d / 10d;
                digit4.PossibleValueProbabilities[n] = 1d / 10d;
                digit5.PossibleValueProbabilities[n] = 1d / 10d;
                digit6.PossibleValueProbabilities[n] = 1d / 10d;
                digit7.PossibleValueProbabilities[n] = 1d / 10d;
                digit8.PossibleValueProbabilities[n] = 1d / 10d;
                digit9.PossibleValueProbabilities[n] = 1d / 10d;
                digit10.PossibleValueProbabilities[n] = 1d / 10d;
            }

            phoneNumber.AddState(plus);
            phoneNumber.AddState(ccode1);
            phoneNumber.AddState(ccode2);
            phoneNumber.AddState(digit1);
            phoneNumber.AddState(digit2);
            phoneNumber.AddState(digit3);
            phoneNumber.AddState(digit4);
            phoneNumber.AddState(digit5);
            phoneNumber.AddState(digit6);
            phoneNumber.AddState(digit7);
            phoneNumber.AddState(digit8);
            phoneNumber.AddState(digit9);
            phoneNumber.AddState(digit10);

            phoneNumber.StartingStates.Add(plus);
            phoneNumber.EndingStates.Add(digit9);
            phoneNumber.EndingStates.Add(digit10);

            AddTransition(plus, ccode1, 1d);
            AddTransition(ccode1, ccode2, 1d);
            AddTransition(ccode2, digit1, 1d);
            AddTransition(digit1, digit2, 1d);
            AddTransition(digit2, digit3, 1d);
            AddTransition(digit3, digit4, 1d);
            AddTransition(digit4, digit5, 1d);
            AddTransition(digit5, digit6, 1d);
            AddTransition(digit6, digit7, 1d);
            AddTransition(digit7, digit8, 1d);
            AddTransition(digit8, digit9, 1d);
            AddTransition(digit9, digit10, 1d);

            return phoneNumber;
        }

        /// <summary>
        /// Gets the state machine for a user-defined phone number.
        /// </summary>
        /// <param name="userState">Object representing the user-defined state machine.</param>
        /// <param name="weight"></param>
        /// <returns>The state machine.</returns>
        public static StateMachine GetPhoneNumber_UserDefined(UserState userState, int weight)
        {
            StateMachine phoneNumber = new StateMachine { Name = userState.MachineType, _weight = weight };
            State prevState = null;
            // Have a minimum of 2 bytes.
            for (int n = 0; n < userState.Bytes.Count - 1; n++)
            {
                string nm = String.Format("UserPhoneNumberByte{0}", n);
                State state = new State { Name = nm, ParentStateMachine = phoneNumber };
                phoneNumber.AddState(state);
                if (n == 0)
                {
                    phoneNumber.StartingStates.Add(state);
                }
                else
                {
                    AddTransition(prevState, state, 1d);
                }
                prevState = state;
                UserDefinedByteProbabilities(state, userState.Bytes[n]);
            }
            UserDefinedState endState = new UserDefinedState(userState) { Name = "EndUserPhoneNumberByte", ParentStateMachine = phoneNumber };
            phoneNumber.AddState(endState);
            UserDefinedByteProbabilities(endState, userState.Bytes[userState.Bytes.Count - 1]);
            AddTransition(prevState, endState, 1d);
            phoneNumber.EndingStates.Add(endState);
            return phoneNumber;
        }

        #endregion

        #region CallLog

        /// <summary>
        /// Call log type 0-2, inclusive as a little-endian integer.
        /// </summary>
        /// <param name="weight">Weight of CallLogType_SimpleLE state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogType_SimpleLE state machine.</param>
        /// <returns></returns>
        public static StateMachine GetCallLogType_SimpleLE(int weight)
        {
            var status = new StateMachine { Name = MachineList.CallLogType_SimpleLE, _weight = 1 };

            State type1 = new State { Name = "type1", ParentStateMachine = status };
            State type2 = new State { Name = "type2", ParentStateMachine = status };
            State type3 = new State { Name = "type3", ParentStateMachine = status };
            State type4 = new State { Name = "type4", ParentStateMachine = status };

            status.AddState(type1);
            status.AddState(type2);
            status.AddState(type3);
            status.AddState(type4);

            status.StartingStates.Add(type1);
            status.EndingStates.Add(type4);

            AddTransition(type1, type2, 1d);
            AddTransition(type2, type3, 1d);
            AddTransition(type3, type4, 1d);

            for (byte i = 0x00; i <= 0x02; i++)
            {
                type1.PossibleValueProbabilities[i] = 1 / 3d;
            }
            type2.PossibleValueProbabilities[0x00] = 1d;
            type3.PossibleValueProbabilities[0x00] = 1d;
            type4.PossibleValueProbabilities[0x00] = 1d;

            return status;
        }

        /// <summary>
        /// Gets the state machine for the timestamp in a call log record on a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of CallLogTimeStamp_Nokia state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to CallLogTimeStamp_Nokia state machine.</param>
        /// <returns></returns>
        public static StateMachine GetCallLogTimeStamp_Nokia(int weight)
        {
            var timeStamp = GetTimestamp_NokiaAll(1);
            var index = GetPhoneNumber_NokiaNumberIndex(1);

            var timeStampAndIndex = new StateMachine { Name = MachineList.CallLogTimeStamp_Nokia, _weight = weight };

            timeStampAndIndex.AddState(timeStamp._states);
            timeStampAndIndex.AddState(index._states);

            timeStampAndIndex.StartingStates.AddRange(timeStamp.StartingStates);
            timeStampAndIndex.EndingStates.AddRange(index.EndingStates);

            AddTransitionToStateMachine(timeStamp, index, 1d);

            return timeStampAndIndex;
        }

        public static StateMachine GetCallLog_NokiaNumberIndexAndNumber(int weight)
        {
            var numberIndexAndNumber = new StateMachine { Name = MachineList.CallLogNumberIndexAndNumber_Nokia, _weight = weight };
            var machine = new StateMachine { Name = MachineList.Binary, _weight = 1 };

            State binary1 = new State { Name = "Binary1", ParentStateMachine = machine, AllValuesPossible = true, IsBinary = true };
            State binary2 = new State { Name = "Binary2", ParentStateMachine = machine, AllValuesPossible = true, IsBinary = true };
            State binary3 = new State { Name = "Binary3", ParentStateMachine = machine, AllValuesPossible = true, IsBinary = true };
            State binary4 = new State { Name = "Binary4", ParentStateMachine = machine, AllValuesPossible = true, IsBinary = true };

            machine.AddState(binary1);
            machine.AddState(binary2);
            machine.AddState(binary3);
            machine.AddState(binary4);

            //Set the transition probability to be lower so that this machine does not dominate others. e.g. the international format
            AddTransition(binary1, binary2, 0.1f);
            AddTransition(binary2, binary3, 0.1f);
            AddTransition(binary3, binary4, 0.1f);

            machine.StartingStates.Add(binary1);
            machine.EndingStates.Add(binary4);



            var index = GetPhoneNumber_NokiaNumberIndex(1);
            var number = GetPhoneNumber_NokiaAll(1);

            AddTransitionToStateMachine(index, machine, 1d);
            AddTransitionToStateMachine(machine, number, 1d);

            numberIndexAndNumber.AddState(index._states);
            numberIndexAndNumber.AddState(machine._states);
            numberIndexAndNumber.AddState(number._states);

            numberIndexAndNumber.StartingStates.AddRange(index.StartingStates);
            numberIndexAndNumber.EndingStates.AddRange(number.EndingStates);

            return numberIndexAndNumber;
        }

        public static StateMachine GetCallLog_MotoTypeAndTime(int weight)
        {
            var callLogType = GetCallLogType_Moto(1);
            var timeStamp = GetTimestamp_Unix(1);

            var typeAndTime = new StateMachine { Name = MachineList.CallLogTypeAndTimeStamp_Moto, _weight = weight };

            typeAndTime._states.AddRange(callLogType._states);
            typeAndTime._states.AddRange(timeStamp._states);

            typeAndTime.StartingStates.AddRange(callLogType.StartingStates);
            typeAndTime.EndingStates.AddRange(timeStamp.EndingStates);

            AddTransitionToStateMachine(callLogType, timeStamp, 1d);

            return typeAndTime;
        }


        /// <summary>
        /// Not really used anymore. The CallLog state machines have been shifted into the
        /// meta state machines.
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        public static StateMachine GetCallLogType_Moto(int weight)
        {
            var status = new StateMachine { Name = MachineList.CallLogType_Moto, _weight = 1 };

            State type1 = new State { Name = "type1", ParentStateMachine = status };
            State type2 = new State { Name = "type2", ParentStateMachine = status };

            status.AddState(type1);
            status.AddState(type2); ;

            status.StartingStates.Add(type1);
            status.EndingStates.Add(type2);

            AddTransition(type1, type2, 1d);

            type1.PossibleValueProbabilities[0x00] = 1d;

            for (byte i = 0x00; i <= 0x05; i++)
            {
                type2.PossibleValueProbabilities[i] = 1 / 6d;
            }


            var prepend = new StateMachine { Name = MachineList.CallLogTypePrepend_Moto, _weight = 1 };

            State ff1 = new State { Name = "FF1", ParentStateMachine = prepend };
            State ff2 = new State { Name = "FF2", ParentStateMachine = prepend };
            State ff3 = new State { Name = "FF3", ParentStateMachine = prepend };
            State ff4 = new State { Name = "FF4", ParentStateMachine = prepend };

            prepend.AddState(ff1);
            prepend.AddState(ff2);
            prepend.AddState(ff3);
            prepend.AddState(ff4);

            prepend.StartingStates.Add(ff1);
            prepend.EndingStates.Add(ff4);

            AddTransition(ff1, ff2, 1d);
            AddTransition(ff2, ff3, 1d);
            AddTransition(ff3, ff4, 1d);

            ff1.PossibleValueProbabilities[0xff] = 1d;
            ff2.PossibleValueProbabilities[0xff] = 1d;
            ff3.PossibleValueProbabilities[0xff] = 1d;
            ff4.PossibleValueProbabilities[0xff] = 1d;

            var statusWithPrepend = new StateMachine { Name = MachineList.CallLogTypeWithPrepend_Moto, _weight = weight };

            statusWithPrepend.StartingStates.AddRange(prepend.StartingStates);
            statusWithPrepend.EndingStates.AddRange(status.EndingStates);
            statusWithPrepend.AddState(status._states);
            statusWithPrepend.AddState(prepend._states);

            AddTransitionToStateMachine(prepend, status, 1d);

            return statusWithPrepend;
        }

        public static StateMachine GetCallLogType_Samsung(int weight)
        {
            var status = new StateMachine { Name = MachineList.CallLogType_Samsung, _weight = weight };


            State type1 = new State { Name = "type1", ParentStateMachine = status };
            State type2 = new State { Name = "type2", ParentStateMachine = status };
            State type3 = new State { Name = "type3", ParentStateMachine = status };
            State type4 = new State { Name = "type4", ParentStateMachine = status };

            status.AddState(type1);
            status.AddState(type2);
            status.AddState(type3);
            status.AddState(type4);

            status.StartingStates.Add(type1);
            status.EndingStates.Add(type4);

            AddTransition(type1, type2, 1d);
            AddTransition(type2, type3, 1d);
            AddTransition(type3, type4, 1d);

            type1.PossibleValueProbabilities[0x00] = 1d;
            type2.PossibleValueProbabilities[0x00] = 1d;
            type3.PossibleValueProbabilities[0x00] = 1d;

            for (byte i = 0x00; i <= 0x05; i++)
            {
                type4.PossibleValueProbabilities[i] = 1 / 6d;
            }

            return status;
        }

        #endregion

        #region Misc

        public static StateMachine GetSamsungSMSMarker(int weight)
        {
            var marker = new StateMachine { Name = MachineList.Marker_SamsungSms, _weight = weight };
            var d = new State { Name = "D", ParentStateMachine = marker };
            var e = new State { Name = "E", ParentStateMachine = marker };
            var a = new State { Name = "A", ParentStateMachine = marker };
            var d1 = new State { Name = "D", ParentStateMachine = marker };
            var b = new State { Name = "B", ParentStateMachine = marker };
            var e1 = new State { Name = "E", ParentStateMachine = marker };
            var e2 = new State { Name = "E", ParentStateMachine = marker };
            var f = new State { Name = "F", ParentStateMachine = marker };


            marker.AddState(d);
            marker.AddState(e);
            marker.AddState(a);
            marker.AddState(d1);
            marker.AddState(b);
            marker.AddState(e1);
            marker.AddState(e2);
            marker.AddState(f);

            marker.StartingStates.Add(d);
            marker.EndingStates.Add(f);

            AddTransition(d, e, 1f);
            AddTransition(e, a, 1f);
            AddTransition(a, d1, 1f);
            AddTransition(d1, b, 1f);
            AddTransition(b, e1, 1f);
            AddTransition(e1, e2, 1f);
            AddTransition(e2, f, 1f);


            d.PossibleValueProbabilities[0x44] = 1f;
            e.PossibleValueProbabilities[0x45] = 1f;
            a.PossibleValueProbabilities[0x41] = 1f;
            d1.PossibleValueProbabilities[0x44] = 1f;
            b.PossibleValueProbabilities[0x42] = 1f;
            e1.PossibleValueProbabilities[0x45] = 1f;
            e2.PossibleValueProbabilities[0x45] = 1f;
            f.PossibleValueProbabilities[0x46] = 1f;


            return marker;
        }

        public static StateMachine GetSqliteRecord(int weight)
        {
            var sqlite = new StateMachine { Name = MachineList.Sql_SqliteRecord, _weight = weight };
            var length = new State { Name = "HeaderLength", ParentStateMachine = sqlite };
            var varintLastByte = new SqliteHeaderLengthState()
                                     {
                                         Name = "VarintLast",
                                         ParentStateMachine = sqlite,
                                         LengthState = length,
                                         AllValuesPossible = true
                                     };
            var recordByte = new State() { Name = "SqlRecord", ParentStateMachine = sqlite, AllValuesPossible = true };
            var recordByteEnd = new SqliteRecordLengthState()
                                    {
                                        Name = "SqlRecordEnd",
                                        ParentStateMachine = sqlite,
                                        LengthState = length,
                                        AllValuesPossible = true,
                                        IsSplitState = true
                                    };

            sqlite.AddState(length);
            sqlite.AddState(varintLastByte);
            sqlite.AddState(recordByte);
            sqlite.AddState(recordByteEnd);

            sqlite.StartingStates.Add(length);
            sqlite.EndingStates.Add(recordByteEnd);

            AddTransition(length, varintLastByte, 0.9f);
            AddTransition(varintLastByte, varintLastByte, 0.2f);
            AddTransition(varintLastByte, recordByte, 0.8f);
            //will only accept sqlite records with at least one column byte
            AddTransition(recordByte, recordByte, 0.5f);
            AddTransition(recordByte, recordByteEnd, 0.5f);

            var stateWeight = 128f;

            //Let's only consider sql records with at least 1 column,
            //but no more than 127. This way the header length varint
            //will only be a single byte
            for (byte i = 0x02; i <= 0x7f; i++)
            {
                length.PossibleValueProbabilities[i] = stateWeight;
                stateWeight--;
            }

            length.NormalizeProbabilities();

            return sqlite;
        }

        public static StateMachine Get7BitString_WithLength(int weight)
        {
            var sevenBit = new StateMachine { Name = MachineList.Text_SevenBitWithLength, _weight = weight };
            var length = new State { Name = "Length", ParentStateMachine = sevenBit };
            var asciiChar = new SevenBitState() { Name = "SevenBit", LengthState = length, ParentStateMachine = sevenBit, AllValuesPossible = true };
            var endChar = new SevenBitState() { Name = "SevenBitEnd", LengthState = length, ParentStateMachine = sevenBit, AllValuesPossible = true, IsEnd = true };


            sevenBit.AddState(length);
            sevenBit.AddState(asciiChar);
            sevenBit.AddState(endChar);

            sevenBit.StartingStates.Add(length);
            sevenBit.EndingStates.Add(endChar);

            AddTransition(length, asciiChar, 1f);
            AddTransition(asciiChar, asciiChar, 0.4f);
            AddTransition(asciiChar, endChar, 0.6f);


            for (byte i = 0x02; i <= 0xa0; i++)
            {
                length.PossibleValueProbabilities[i] = 1f;
            }

            length.NormalizeProbabilities();



            return sevenBit;
        }

        public static StateMachine GetAsciiString_WithLength(int weight)
        {
            var ascii = new StateMachine { Name = MachineList.Text_AsciiStringWithLength, _weight = weight };
            var length = new State { Name = "Length", ParentStateMachine = ascii, AllValuesPossible = true, IsBinary = true };
            var asciiChar = new AsciiLengthState { Name = "AsciiLengthChar", LengthState = length, ParentStateMachine = ascii };

            ascii.AddState(length);
            ascii.AddState(asciiChar);

            ascii.StartingStates.Add(length);
            ascii.EndingStates.Add(asciiChar);

            AddTransition(length, asciiChar, 1f);
            AddTransition(asciiChar, asciiChar, 0.99f);
            asciiChar.RemainingProbability = 0.01f;

            asciiChar.PossibleValueProbabilities[0x61] = 1f;

            return ascii;
        }

        /// <summary>
        /// Gets the state machine for a user-defined text field.
        /// </summary>
        /// <param name="userState">Object representing the user-defined state machine.</param>
        /// <param name="weight"></param>
        /// <returns>The state machine.</returns>
        public static StateMachine GetText_UserDefined(UserState userState, int weight)
        {
            StateMachine textState = new StateMachine { Name = userState.MachineType, _weight = weight };
            State prevState = null;
            // Have a minimum of 2 bytes.
            for (int n = 0; n < userState.Bytes.Count - 1; n++)
            {
                State state = new State { Name = String.Format("UserTextByte{0}", n), ParentStateMachine = textState };
                textState.AddState(state);
                if (n == 0)
                {
                    textState.StartingStates.Add(state);
                }
                else
                {
                    AddTransition(prevState, state, 1d);
                }
                prevState = state;
                UserDefinedByteProbabilities(state, userState.Bytes[n]);
            }
            UserDefinedState endState = new UserDefinedState(userState) { Name = "EndUserTextByte", ParentStateMachine = textState };
            textState.AddState(endState);
            UserDefinedByteProbabilities(endState, userState.Bytes[userState.Bytes.Count - 1]);
            AddTransition(prevState, endState, 1d);
            textState.EndingStates.Add(endState);
            return textState;
        }

        /// <summary>
        /// Gets the state machine representing all possible phone number formats, with states coming from state machines corresponding to national/international seven, eight, ten, eleven digit Nokia, Motorola, Samsung formats
        /// </summary>
        /// <param name="weight">Weight of AnchorFields state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to AnchorFields state machine.</param>
        /// <returns></returns>
        public static StateMachine GetAnchorFields(int weight)
        {
            StateMachine phone = new StateMachine { Name = MachineList.AnchorFields, _weight = weight };

            List<StateMachine> machines = new List<StateMachine>()
                                              {
                                                    GetPhoneNumber_NokiaElevenDigit(),
                                                    GetPhoneNumber_NokiaSevenDigit(),
                                                    GetPhoneNumber_NokiaEightDigit(),
                                                    GetPhoneNumber_NokiaTenDigit(),
                                                    GetPhoneNumber_NokiaTwelveDigit(),
                                                    //GetPhoneNumber_BCD(),
                                                    GetPhoneNumber_BCDWithPrepend(),
                                                    /*
                                                    GetPhoneNumber_InternationalFormatSevenDigit(),
                                                    GetPhoneNumber_InternationalFormatTenDigit(),
                                                    GetPhoneNumber_InternationalFormatElevenDigit(),
                                                    */
                                                    GetPhoneNumber_MotoElevenUnicode(),
                                                    GetPhoneNumber_SamsungElevenDigitAscii(),
                                                    GetPhoneNumber_SamsungTenDigitAscii(),
                                                    GetPhoneNumber_SamsungSevenDigitAscii(),
                                                    GetPhoneNumber_MotoElevenDigit(),
                                                    GetPhoneNumber_MotoSevenDigit(),
                                                    GetPhoneNumber_MotoTenDigit(),
                                                    GetPhoneNumber_MotoSevenUnicode(),
                                                    GetPhoneNumber_MotoTenUnicode(),
                                              };


            for (int i = 0; i < machines.Count; i++)
            {
                phone.StartingStates.AddRange(machines[i].StartingStates);
                phone.EndingStates.AddRange(machines[i].EndingStates);
                phone.AddState(machines[i]._states);
            }

            return phone;
        }

        /// <summary>
        /// Gets the state machine corresponding to a combination of any possible phone number format and text.
        /// </summary>
        /// <param name="weight">Weight of PhoneNumberAndText state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to PhoneNumberAndText state machine.</param>
        /// <returns></returns>
        public static StateMachine GetPhoneNumberAndText(int weight)
        {
            StateMachine phoneNumberAndText = new StateMachine { Name = MachineList.Combo_PhoneNumberAndText, _weight = weight };

            StateMachine text = GetText(1);
            StateMachine phone = GetPhoneNumber_All(1);
            StateMachine binary = GetBinary();

            phoneNumberAndText.StartingStates.AddRange(text.StartingStates);
            phoneNumberAndText.EndingStates.AddRange(phone.EndingStates);

            phoneNumberAndText.AddState(text._states);
            phoneNumberAndText.AddState(phone._states);
            phoneNumberAndText.AddState(binary._states);

            AddTransitionToStateMachine(text, binary, 0.99d);
            AddTransitionToStateMachine(text, phone, 0.01d);
            AddTransitionToStateMachine(binary, phone, 0.01d);
            AddTransitionToStateMachine(binary, binary, 0.99d);

            return phoneNumberAndText;
        }

        /// <summary>
        /// Gets the state machine representing the start of a record.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetStart()
        {
            StateMachine start = new StateMachine() { Name = MachineList.Start };
            State startState = new State() { Name = "StartState" };
            start.StartingState = startState;
            start.StartingStates.Add(startState);
            start.EndingStates.Add(startState);

            return start;
        }

        /// <summary>
        /// Gets the state machine for a binary byte.
        /// </summary>        
        /// <returns></returns>
        public static StateMachine GetBinary()
        {
            StateMachine binary = new StateMachine { Name = MachineList.Binary };
            State binaryByte = new State { Name = "BinaryByte", ParentStateMachine = binary, AllValuesPossible = true, IsBinary = true };
            binary.AddState(binaryByte);
            binary.StartingStates.Add(binaryByte);
            binary.EndingStates.Add(binaryByte);

            AddTransition(binaryByte, binaryByte, 0.1f);
            binaryByte.RemainingProbability = 0.9f;

            return binary;
        }

        /// <summary>
        /// Gets the state machine representing the end of a record on a Nokia phone.
        /// </summary>
        /// <param name="weight">Weight of NokiaRecordEnd state machine, among the set of state machines to be aggregated, governing a prior probability that the inferred sequence of states corresponds to NokiaRecordEnd state machine.</param>
        /// <returns></returns>
        public static StateMachine GetNokiaRecordEnd(int weight)
        {
            StateMachine recordEnd = new StateMachine { Name = MachineList.RecordEnd_Nokia, _weight = weight };

            State unknown1 = new State { Name = "Unknown1", ParentStateMachine = recordEnd, IsBinary = true };
            State unknown2 = new State { Name = "Unknown2", ParentStateMachine = recordEnd, IsBinary = true };
            State end1 = new State { Name = "End1", ParentStateMachine = recordEnd, IsBinary = false };
            State end2 = new State { Name = "End2", ParentStateMachine = recordEnd, IsBinary = false };

            recordEnd.AddState(unknown1);
            recordEnd.AddState(unknown2);
            recordEnd.AddState(end1);
            recordEnd.AddState(end2);

            recordEnd.StartingStates.Add(unknown1);
            recordEnd.EndingStates.Add(end2);

            AddTransition(unknown1, unknown2, 1f);
            AddTransition(unknown2, end1, 1f);
            AddTransition(end1, end2, 1f);

            end1.PossibleValueProbabilities[0xff] = 1f;
            end2.PossibleValueProbabilities[0xff] = 1f;

            for (int i = 0x00; i <= 0xfe; i++)
            {
                unknown1.PossibleValueProbabilities[i] = 1f;
                unknown2.PossibleValueProbabilities[i] = 1f;
            }

            unknown1.NormalizeProbabilities();
            unknown2.NormalizeProbabilities();

            return recordEnd;
        }

        public static StateMachine GetBinaryFF()
        {
            StateMachine binaryFF = new StateMachine { Name = MachineList.BinaryFF };
            State ffByte1 = new State { Name = "BinaryFF_1", ParentStateMachine = binaryFF, IsBinary = true };
            State ffByte2 = new State { Name = "BinaryFF_2", ParentStateMachine = binaryFF, IsBinary = true };
            State ffByte3 = new State { Name = "BinaryFF_3", ParentStateMachine = binaryFF, IsBinary = true };
            State ffByte4 = new State { Name = "BinaryFF_4", ParentStateMachine = binaryFF, IsBinary = true };
            State ffByte5 = new State { Name = "BinaryFF_5", ParentStateMachine = binaryFF, IsBinary = true };

            binaryFF.AddState(ffByte1);
            binaryFF.AddState(ffByte2);
            binaryFF.AddState(ffByte3);
            binaryFF.AddState(ffByte4);
            binaryFF.AddState(ffByte5);

            ffByte1.PossibleValueProbabilities[0xff] = 1d;
            ffByte2.PossibleValueProbabilities[0xff] = 1d;
            ffByte3.PossibleValueProbabilities[0xff] = 1d;
            ffByte4.PossibleValueProbabilities[0xff] = 1d;
            ffByte5.PossibleValueProbabilities[0xff] = 1d;

            AddTransition(ffByte1, ffByte2, 1d);
            AddTransition(ffByte2, ffByte3, 1d);
            AddTransition(ffByte3, ffByte4, 1d);
            AddTransition(ffByte4, ffByte5, 0.99d);
            ffByte4.RemainingProbability = 0.01d;
            AddTransition(ffByte5, ffByte4, 0.99d);
            ffByte5.RemainingProbability = 0.01d;

            binaryFF.StartingStates.Add(ffByte1);
            binaryFF.EndingStates.Add(ffByte4);
            binaryFF.EndingStates.Add(ffByte5);

            return binaryFF;
        }

        #endregion

        #endregion

        #region Private Methods

        private void AddState(List<State> newStates)
        {
            for (int i = 0; i < newStates.Count; i++)
            {
                AddState(newStates[i]);
            }
        }

        private void AddState(State newState)
        {
            if (!_states.Contains(newState))
                _states.Add(newState);
            else
            {
                throw new ArgumentException("This state has already been added to the state machine");
            }
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return Convert.ToString(Name);
        }

        #endregion
    }
}
