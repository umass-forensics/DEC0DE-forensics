/**
 * Copyright (C) 2013 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Dec0de.Bll.UserStates
{
    public static class Loader
    {
        /// <summary>
        /// Called to load and parse the user-defined states.
        /// </summary>
        /// <param name="enabled">Whether or not the option is enabled. If not enabled
        /// then an empty list is returned.</param>
        /// <param name="path">Path of the XML file defining the states.</param>
        /// <returns>List of user-defined states as represented by the UserState class.</returns>
        public static List<UserState> LoadUserStates(bool enabled, string path)
        {
            // Return empty list if not enabled.
            if (!enabled) {
                return new List<UserState>();
            }
            try {
                XDocument xDoc = XDocument.Load(path);
                return Parse(xDoc);
            } catch (UserStatesException ex) {
                throw;
            } catch (Exception ex) {
                throw new UserStatesException("Failed to load XML file: " + ex.Message);
            }
        }

        private static List<UserState> Parse(XDocument xDoc)
        {
            try {
                List<UserState> states = new List<UserState>();
                XElement xRoot = xDoc.Element("dec0destates");
                if (xRoot != null) {
                    foreach (XElement xState in xRoot.Elements("statemachine")) {
                        UserState state = ParseStateMachine(xState);
                        if (state != null) {
                            states.Add(state);
                        }
                    }
                }
                return states;
            } catch (UserStatesException ex) {
                throw;
            } catch (Exception ex) {
                throw new UserStatesException("Error: " + ex.Message);
            }
        }

        private static UserState ParseStateMachine(XElement xState)
        {
            string name = RequiredAttribute(xState, "name");
            Viterbi.MachineList type = UserState.GetMachineType(RequiredAttribute(xState, "type"));
            string lib = RequiredAttribute(xState, "lib");
            string classname = RequiredAttribute(xState, "class");
            string format = RequiredAttribute(xState, "format");
            string validate = RequiredAttribute(xState, "validate");
            string datetime = null;
            if (type == Viterbi.MachineList.TimeStamp_User) {
                datetime = RequiredAttribute(xState, "datetime");
            }
            List<UserByte> bytes = new List<UserByte>();
            foreach (XElement xByte in xState.Elements("byte")) {
                UserByte ub = new UserByte();
                foreach (XElement xVal in xByte.Elements()) {
                    ub.AddElement(xVal);
                }
                if (!ub.IsValid()) {
                    throw new UserStatesException("Invalid byte in state machine");
                }
                bytes.Add(ub);
            }
            if (bytes.Count < 2) {
                throw new UserStatesException("State machine has last than two bytes defined");
            }
            UserState uState = new UserState(type, name, bytes, lib, classname, format, validate, datetime);
            uState.LoadMethods();
            return uState;
        }

        private static string RequiredAttribute(XElement xEl, string ename)
        {
            XAttribute xa = xEl.Attribute(ename);
            if (xa == null) {
                throw new UserStatesException("Missing required attribute: " + ename);
            }
            if (String.IsNullOrWhiteSpace(xa.Value)) {
                throw new UserStatesException(String.Format("Required attribute {0} may not be empty or whitespace",
                                                            ename));
            }
            return xa.Value.Trim();
        }
    }

}
