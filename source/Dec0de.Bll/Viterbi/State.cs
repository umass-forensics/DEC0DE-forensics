using System;
using System.Collections.Generic;
using System.Linq;

namespace Dec0de.Bll.Viterbi
{
    public class State
    {
        #region Declarations

        //Cannot be zero or the Logarithm will be undefined
        public const double ALMOST_ZERO = double.Epsilon;
        private const double UNIFORM_PROB = 1.0f / 256f;
        /// <summary>
        /// List of all (incoming as well as outgoing) transitions for this state.
        /// </summary>
        private readonly List<Transition> _transitions = new List<Transition>();
        /// <summary>
        /// List of outgoing transitions for this state.
        /// </summary>
        private readonly List<Transition> _transitionsOut = new List<Transition>();
        /// <summary>
        /// /// List of incoming transitions for this state.
        /// </summary>
        private readonly List<Transition> _transitionsIn = new List<Transition>();



        #endregion

        #region Constructor

        public State()
        {
            ListIndex = -1;
            IsEndingState = false;
            PossibleValueProbabilities = new double[256];
            RemainingProbability = 1d;

            for (int i = 0; i < PossibleValueProbabilities.Length; i++)
            {
                PossibleValueProbabilities[i] = ALMOST_ZERO;
            }

            IsBinary = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Normalize the emission probabilities for all output bytes, given this state.
        /// </summary>
        public void NormalizeProbabilities()
        {
            double sum = 0d;

            for (int i = 0; i < PossibleValueProbabilities.Length; i++)
            {
                if (PossibleValueProbabilities[i] > ALMOST_ZERO)
                    sum += PossibleValueProbabilities[i];
            }

            for (int i = 0; i < PossibleValueProbabilities.Length; i++)
            {
                if (PossibleValueProbabilities[i] > ALMOST_ZERO)
                    PossibleValueProbabilities[i] = PossibleValueProbabilities[i] / sum;
            }
        }

        /// <summary>
        /// Adds a transition to this state.
        /// </summary>
        /// <param name="fromState">The FROM state for the transition.</param>
        /// <param name="toState">The TO state for the transition.</param>
        /// <param name="probability">The transition probability for the transition.</param>
        public void AddTransition(State fromState, State toState, double probability)
        {
            Transition newTrans = new Transition
                                      {
                                          ToState = toState,
                                          FromState = fromState,
                                          Probability = probability
                                      };

            //We do not want to add a double transition to the same state
            if (_transitions.Where(r => r.ToState == toState && r.FromState == fromState).Count() == 0)
            {
                _transitions.Add(newTrans);
            }
            else
            {
                return;
            }
            //else
            //{
            //    throw new ArgumentException("A transition to the state has already been added.");
            //}

            if (this == toState)
            {
                _transitionsIn.Add(newTrans);
            }
            if (this == fromState)
            {
                _transitionsOut.Add(newTrans);
            }

            if (this != toState && this != fromState)
            {
                throw new ArgumentException("Transistion must include an endpoint in this state.");
            }

        }

        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from the state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given this state is required.</param>
        /// <returns>The emission probability of an output byte, given this state.</returns>
        public virtual double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            if (AllValuesPossible)
                return UNIFORM_PROB;

            return PossibleValueProbabilities[values[index]];
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion

        #region Property Accessors

        public List<Transition> Transitions
        {
            get { return _transitions; }
        }

        public List<Transition> TransitionsOut
        {
            get { return _transitionsOut; }
        }

        public List<Transition> TransitionsIn
        {
            get { return _transitionsIn; }
        }

        /// <summary>
        /// The index in the states list
        /// </summary>
        public int ListIndex { get; set; }

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (value.ToLower().StartsWith("Binary"))
                    IsBinary = true;

                _name = value;
            }
        }

        public StateMachine ParentStateMachine { get; set; }

        /// <summary>
        /// Needed to help normalize probabilities
        /// </summary>
        public double RemainingProbability { get; set; }

        /// <summary>
        /// A list of possible byte values this state can take. 
        /// </summary>
        public double[] PossibleValueProbabilities { get; set; }

        /// <summary>
        /// Denotes that all byte values are possible and assigns a uniform probability to each.
        /// This is just a way to avoid searching through a long list of possible values.
        /// </summary>
        public bool AllValuesPossible { get; set; }

        public bool IsEndingState { get; set; }

        public bool IsBinary { get; set; }

        /// <summary>
        /// Our find path method will split on this state
        /// </summary>
        public bool IsSplitState { get; set; }

        #endregion
    }
}
