/**
 * Copyright (C) 2013 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dec0de.Bll.UserStates;

namespace Dec0de.Bll.Viterbi
{
    /// <summary>
    /// State for last byte of a user defined state machine. Invokes the user's method
    /// for validating the bytes.
    /// </summary>
    public class UserDefinedState : State
    {
        public UserState UserStateObj = null;

        public UserDefinedState(UserState us)
        {
            UserStateObj = us;
        }

        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            // If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO) return ALMOST_ZERO;

            try {
                // Allocate byte array for data.
                byte[] input = new byte[UserStateObj.Bytes.Count];
                for (int n = input.Length - 1, i = 0; n >= 0; n--, i++) {
                    input[n] = values[index - i];
                }
                if (Validate(UserStateObj, input)) {
                    return baseProb;
                } else {
                    return ALMOST_ZERO;
                }
            } catch {
                return ALMOST_ZERO;
            }
        }

        private bool Validate(UserState userState, byte[] input)
        {
            try {
                return (bool)userState.MethodValidate.Invoke(null, new object[] { input });
            } catch {
                return false;
            }
        }
    }


    /// <summary>
    /// Used for user defined timestamps. The base class will invoke the user's
    /// validation method, and then the timestamp is examined here. 
    /// </summary>
    public class UserDefinedTimestampState : UserDefinedState
    {

        public UserDefinedTimestampState(UserState us)
            : base(us)
        {
        }

        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            double baseProb = base.GetValueProbability(values, index, viterbi);
            // If the probability is zero, no need to do all of this logic.
            if (baseProb == ALMOST_ZERO) return ALMOST_ZERO;

            try {
                // Allocate byte array for data.
                byte[] input = new byte[UserStateObj.Bytes.Count];
                for (int n = input.Length - 1, i = 0; n >= 0; n--, i++) {
                    input[n] = values[index - i];
                }
                DateTime dateTime = GetDateTime(UserStateObj, input);
                if (dateTime.Year >= TimeConstants.START_YEAR && dateTime.Year <= TimeConstants.END_YEAR) {
                    return baseProb;
                } else {
                    return ALMOST_ZERO;
                }
            } catch {
                return ALMOST_ZERO;
            }
        }

        private DateTime GetDateTime(UserState userState, byte[] input)
        {
            try {
                return (DateTime)userState.MethodDatetime.Invoke(null, new object[] { input });
            } catch {
                // Timestamp that will be filtered out.
                return new DateTime(1800, 1, 1);
            }
        }

    }

}
