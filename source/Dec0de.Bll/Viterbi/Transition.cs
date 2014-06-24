namespace Dec0de.Bll.Viterbi
{
    public class Transition
    {
        /// <summary>
        /// The state from which the transition would happen.
        /// </summary>
        public State FromState { get; set; }
        /// <summary>
        /// The state to which the transition would happen.
        /// </summary>
        public State ToState { get; set; }
        /// <summary>
        /// The probability of transition.
        /// </summary>
        public double Probability { get; set; }
    }
}
