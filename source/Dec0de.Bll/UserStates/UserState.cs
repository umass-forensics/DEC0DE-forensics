/**
 * Copyright (C) 2013 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dec0de.Bll.Viterbi;
using System.Xml.Linq;

namespace Dec0de.Bll.UserStates
{
    /// <summary>
    /// This is the exception we through while building the states.
    /// </summary>
    public class UserStatesException : Exception
    {
        public UserStatesException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// This represents a byte in a user defined state (prior to interpreting it as
    /// a StateMachine).
    /// </summary>
    public class UserByte
    {
        public bool All;
        public List<byte> Values = null;

        public bool IsValid()
        {
            return (All || (Values != null));
        }

        public void AddAll()
        {
            All = true;
            Values = null;
        }

        public void AddValue(byte b)
        {
            if (All) return;
            InitValues();
            Values.Add(b);
        }

        public void AddRange(byte low, byte high)
        {
            if (All) return;
            if (high < low) {
                throw new UserStatesException("Invalid byte range");
            }
            InitValues();
            for (byte b = low; b <= high; b++) {
                Values.Add(b);
            }
        }

        private void InitValues()
        {
            if (Values == null) {
                Values = new List<byte>();
            }
        }

        public void AddElement(XElement xEl)
        {
            if (xEl.Name == "all") {
                AddAll();
                return;
            }
            if (xEl.Name == "value") {
                if (!String.IsNullOrWhiteSpace(xEl.Value)) {
                    byte b;
                    if (Byte.TryParse(xEl.Value, out b)) {
                        AddValue(b);
                        return;
                    }
                }
                throw new UserStatesException("Invalid or missing value in byte value element");
            }
            if (xEl.Name == "range") {
                try {
                    XAttribute xLow = xEl.Attribute("low");
                    XAttribute xHigh = xEl.Attribute("high");
                    AddRange(Byte.Parse(xLow.Value), Byte.Parse(xHigh.Value));
                    return;
                } catch (UserStatesException) {
                    throw;
                } catch (Exception) {
                    throw new UserStatesException("Invalid or missing attribute in byte range element");
                }
            }
            throw new UserStatesException("Invalid byte element: " + xEl.Name);
        }
    }

    /// <summary>
    /// Each instance of this class represents a user-defined state machine.
    /// </summary>
    public class UserState
    {
        public MachineList MachineType;
        public string Name;
        public List<UserByte> Bytes;
        public MethodInfo MethodFormat = null;
        public MethodInfo MethodValidate = null;
        public MethodInfo MethodDatetime = null;

        private string strLibrary;
        private string strClass;
        private string strMethodFormat;
        private string strMethodValidate;
        private string strMethodDatetime;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Type of field.</param>
        /// <param name="name">Name user has given the state.</param>
        /// <param name="bytes">List representing each byte in the state.</param>
        /// <param name="lib">Library (DLL) containing the helper methods.</param>
        /// <param name="classname">Class (with namespace) containing the static methods.</param>
        /// <param name="format">Method to format the field as a string.</param>
        /// <param name="validate">Method to validate the bytes. Invoked when all bytes match.</param>
        /// <param name="dt">DateTime method if it's a timestamp.</param>
        public UserState(MachineList type, string name, List<UserByte> bytes, string lib,
            string classname, string format, string validate, string dt)
        {
            MachineType = type;
            Name = name;
            Bytes = bytes;
            strLibrary = lib;
            strClass = classname;
            strMethodFormat = format;
            strMethodValidate = validate;
            strMethodDatetime = (type == MachineList.TimeStamp_User) ? dt : null;
        }

        /// <summary>
        /// Loads the helper methods from the library. This is done once, rather than each
        /// time they're required.
        /// </summary>
        public void LoadMethods()
        {
            Assembly assembly;
            try {
                assembly = Assembly.LoadFrom(strLibrary);
            } catch (Exception ex) {
                throw new UserStatesException(
                    String.Format("Failed to load user defined library {0}: {1}",
                    strLibrary, ex.Message));
            }
            Type type;
            try {
                type = assembly.GetType(strClass);
            } catch (Exception ex) {
                throw new UserStatesException(
                    String.Format("Failed to load user defined class {0}: {1}",
                    strClass, ex.Message));
            }
            LoadMethod(ref MethodFormat, strMethodFormat, type);
            LoadMethod(ref MethodValidate, strMethodValidate, type);
            if (strMethodDatetime != null) {
                LoadMethod(ref MethodDatetime, strMethodDatetime, type);
            }
        }

        /// <summary>
        /// Actually loads the method once the library has been loaded and class located.
        /// </summary>
        /// <param name="meth"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        private void LoadMethod(ref MethodInfo meth, string name, Type type)
        {
            try {
                meth = type.GetMethod(name);
            } catch (Exception ex) {
                throw new UserStatesException(
                    String.Format("Failed to load user defined method {0}: {1}",
                    name, ex.Message));
            }
        }

        /// <summary>
        /// Given a string describing the user type, map it to the MachineList enum.
        /// </summary>
        /// <param name="type">String describing the type.</param>
        /// <returns>MachineList enumerator</returns>
        public static MachineList GetMachineType(string type)
        {
            string itype = type.ToLower();
            if (itype == "timestamp") {
                return MachineList.TimeStamp_User;
            } else if (itype == "phonenumber") {
                return MachineList.PhoneNumber_User;
            } else if (itype == "text") {
                return MachineList.Text_User;
            } else {
                throw new UserStatesException("Invalid state machine type: " + type);
            }
        }

    }
}
