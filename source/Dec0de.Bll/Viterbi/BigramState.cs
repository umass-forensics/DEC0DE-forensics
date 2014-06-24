using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dec0de.Bll.Viterbi
{
    public class BigramState : State
    {
        public const byte SPACE = 0x20;
        private readonly static double[,] Bigrams = new double[27, 27];
        private List<StateValue> PossibleValues_Old = new List<StateValue>();

        private readonly static double[,] PossibleValues = new double[256, 256];

        static BigramState()
        {
            Bigrams = ParseBigramProbabilityFile();
            //ALMOST_ZERO;
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    PossibleValues[i, j] = ALMOST_ZERO;
                }
            }

            for (int i = 0; i < 27; i++)
            {
                for (int j = 0; j < 27; j++)
                {
                    byte value = GetValueByIndex(j, false);
                    byte prevValue = GetValueByIndex(i, false);

                    PossibleValues[prevValue, value] = Bigrams[i, j];
                }
            }
        }

        public BigramState()
        {
            //Set the possible state values
            var result = GetValueByIndex(1, false);
            result = GetValueByIndex(1, true);

            for (int i = 0; i < 27; i++)
            {
                for (int j = 0; j < 27; j++)
                {
                    byte value = GetValueByIndex(j, false);
                    byte prevValue = GetValueByIndex(i, false);

                    var stateValueLower = new BigramStateValue() { PreviousValue = prevValue, Value = value, Probability = Bigrams[i, j] };

                    PossibleValues_Old.Add(stateValueLower);
                }
            }
        }

        private static byte GetValueByIndex(int index, bool isUpper)
        {
            byte value = 0x00;

            #region Switch
            switch (index)
            {
                case 0:
                    value = (isUpper) ? Convert.ToByte('A') : Convert.ToByte('a');
                    break;
                case 1:
                    value = (isUpper) ? Convert.ToByte('B') : Convert.ToByte('b');
                    break;
                case 2:
                    value = (isUpper) ? Convert.ToByte('C') : Convert.ToByte('c');
                    break;
                case 3:
                    value = (isUpper) ? Convert.ToByte('D') : Convert.ToByte('d');
                    break;
                case 4:
                    value = (isUpper) ? Convert.ToByte('E') : Convert.ToByte('e');
                    break;
                case 5:
                    value = (isUpper) ? Convert.ToByte('F') : Convert.ToByte('f');
                    break;
                case 6:
                    value = (isUpper) ? Convert.ToByte('G') : Convert.ToByte('g');
                    break;
                case 7:
                    value = (isUpper) ? Convert.ToByte('H') : Convert.ToByte('h');
                    break;
                case 8:
                    value = (isUpper) ? Convert.ToByte('I') : Convert.ToByte('i');
                    break;
                case 9:
                    value = (isUpper) ? Convert.ToByte('J') : Convert.ToByte('j');
                    break;
                case 10:
                    value = (isUpper) ? Convert.ToByte('K') : Convert.ToByte('k');
                    break;
                case 11:
                    value = (isUpper) ? Convert.ToByte('L') : Convert.ToByte('l');
                    break;
                case 12:
                    value = (isUpper) ? Convert.ToByte('M') : Convert.ToByte('m');
                    break;
                case 13:
                    value = (isUpper) ? Convert.ToByte('N') : Convert.ToByte('n');
                    break;
                case 14:
                    value = (isUpper) ? Convert.ToByte('O') : Convert.ToByte('o');
                    break;
                case 15:
                    value = (isUpper) ? Convert.ToByte('P') : Convert.ToByte('p');
                    break;
                case 16:
                    value = (isUpper) ? Convert.ToByte('Q') : Convert.ToByte('q');
                    break;
                case 17:
                    value = (isUpper) ? Convert.ToByte('R') : Convert.ToByte('r');
                    break;
                case 18:
                    value = (isUpper) ? Convert.ToByte('S') : Convert.ToByte('s');
                    break;
                case 19:
                    value = (isUpper) ? Convert.ToByte('T') : Convert.ToByte('t');
                    break;
                case 20:
                    value = (isUpper) ? Convert.ToByte('U') : Convert.ToByte('u');
                    break;
                case 21:
                    value = (isUpper) ? Convert.ToByte('V') : Convert.ToByte('v');
                    break;
                case 22:
                    value = (isUpper) ? Convert.ToByte('W') : Convert.ToByte('w');
                    break;
                case 23:
                    value = (isUpper) ? Convert.ToByte('X') : Convert.ToByte('x');
                    break;
                case 24:
                    value = (isUpper) ? Convert.ToByte('Y') : Convert.ToByte('y');
                    break;
                case 25:
                    value = (isUpper) ? Convert.ToByte('Z') : Convert.ToByte('z');
                    break;
                case 26:
                    value = Convert.ToByte(' ');
                    break;
            }
            #endregion

            return value;
        }

        /// <summary>
        /// Returns the emission probability of a particular byte. If the probability is explicitly defined it returns
        /// that value. Otherwise it returns ALMOST_ZERO.
        /// </summary>
        /// <param name="value">List of byte outputs from the Bigram state.</param>
        /// <param name="index">Index of the byte in values, whose emission probability, given the Bigram state is required.</param>
        /// <returns>The emission probability of an output byte, given the Bigram state.</returns>
        public override double GetValueProbability(byte[] values, int index, Viterbi viterbi)
        {
            //Make sure there is a previous value. If not, assume it is a space
            byte prevValue = (index > 0) ? values[index - 1] : SPACE;
            byte currentValue = values[index];

            if (PossibleValues[prevValue, currentValue] == 0.0f)
                return double.Epsilon;

            //If the previous byte was a lower case character, and the current byte is an upper case character
            if (IsLower(prevValue) && IsUpper(currentValue))
                return double.Epsilon;

            return PossibleValues[ConvertToLowerIfPossible(prevValue), ConvertToLowerIfPossible(currentValue)];
        }

        public double GetValueProbability_Old(byte[] values, int index)
        {
            //Make sure there is a previous value. If not, assume it is a space
            byte prevValue = (index > 0) ? ConvertToLowerIfPossible(values[index - 1]) : SPACE;
            byte currentValue = ConvertToLowerIfPossible(values[index]);

            double returnValue;

            //Check to see if the previous value is text or a space, if not then we can assume that this is the first letter
            //if it is the first letter than we will prepend a SPACE.
            if (PossibleValues_Old.Where(r => ((BigramStateValue)r).PreviousValue == prevValue).Count() == 0)
                prevValue = SPACE;

            var bigramMatches =
                PossibleValues_Old.Where(r => ((BigramStateValue)r).PreviousValue == prevValue && r.Value == currentValue);

            if (bigramMatches.Count() > 0)
                returnValue = bigramMatches.First().Probability;
            else
                returnValue = ALMOST_ZERO;

            return returnValue;
        }

        private static bool IsUpper(byte value)
        {
            if (value >= 0x41 && value <= 0x5a)
                return true;

            return false;
        }

        private static bool IsLower(byte value)
        {
            if (value >= 0x61 && value <= 0x7a)
                return true;

            return false;
        }

        private static byte ConvertToLowerIfPossible(byte value)
        {
            if (value >= 0x41 && value <= 0x5a)
                value += 32;
            //Convert '-' to SPACE
            else if (value == 0x2D)
                value = SPACE;

            return value;
        }

        private static double[,] ParseBigramProbabilityFile()
        {
            // We look for the bigrams file in the program's executable directory.
            const string BIGRAMFILE = "bigramsWithSpace.txt";
            string dir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            string probFile = Path.Combine(dir, BIGRAMFILE);
            if (!File.Exists(probFile)) {
                // Throw a meaningful exception so that someone without the
                // source can figure out what the problem is.
                throw new Exception(String.Format("File {0} does not exist in executable folder {1}",
                                                  BIGRAMFILE, dir));
            }

            string[] lines = File.ReadAllLines(probFile);

            //Includes space
            double[,] bigrams = new double[27, 27];

            for (int i = 0; i < lines.Length; i++)
            {
                string[] entries = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j < entries.Length; j++)
                {
                    bigrams[i, j] = ReadEntry(entries[j]);
                }
            }

            return bigrams;
        }

        private static double ReadEntry(string entry)
        {
            //Format 6.5125e-05  or 0.025815
            string[] entryParts = entry.Split(new[] { 'e' }, StringSplitOptions.RemoveEmptyEntries);

            double value = Convert.ToDouble(entryParts[0]);

            if (entryParts.Length > 1)
            {
                double power = Convert.ToInt32(entryParts[1]);

                value *= Math.Pow(10, power);
            }

            return value;

        }
    }
}
