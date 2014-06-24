using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Dec0de.Bll.Filter;
using Dec0de.Bll;
using Dec0de.Bll.AnswerLoader;
using Dec0de.Bll.Viterbi;
using Plossum.CommandLine;

namespace Dec0de.con
{
    public class Program
    {
        internal const string COMMANDS_ID = "commands";

        [CommandLineManager(ApplicationName = "Dec0de",
         EnabledOptionStyles = OptionStyles.ShortUnix | OptionStyles.LongUnix,
            Copyright = "Robert J. Walls 2014", IsCaseSensitive = false)]
        [CommandLineOptionGroup(COMMANDS_ID, Name = "Commands",
            Require = OptionGroupRequirement.AtMostOne)]
        public class Options
        {
            #region Commands

            [CommandLineOption(Name = "h", Aliases = "help", GroupId = COMMANDS_ID,
                Description = "Displays this help text")]
            public bool Help { get; set; }

            [CommandLineOption(Name = "test", Aliases = "testMachines", GroupId = COMMANDS_ID,
                Description = "Runs a series of tests to make sure the machines are decoding corrrectly")]
            public bool Test { get; set; }

            [CommandLineOption(Name = "teststr", Aliases = "testString", GroupId = COMMANDS_ID,
                Description = "Run DEC0DE on a byte string and return the results.")]
            public string TestString { get; set; }

            [CommandLineOption(Name = "run", GroupId = COMMANDS_ID,
                Description = "Runs DEC0DE using the specified run type")]
            public string Run { get; set; }

            #endregion

            //[CommandLineOption(Name = "debug",
            //    Description = "Pause the program for a short period before running so the user can attach a debugger")]
            //public bool Debug { get; set; }

            [CommandLineOption(Description = "The input image", MinOccurs = 1)]
            public string Image { get; set; }
        }

        private static Options _options;

        static int Main(string[] args)
        {
            _options = new Options();
            CommandLineParser parser = new CommandLineParser(_options);
            parser.Parse();

            if (_options.Help)
            {
                Console.WriteLine(parser.UsageInfo.ToString(78, false));
                return 0;
            }
            
            if (parser.HasErrors)
            {
                Console.WriteLine(parser.UsageInfo.ToString(78, true));
                return -1;
            }

            if(_options.Test)
                Test();
            if (_options.TestString != null)
                TestString();    
            else
                RunDec0de();



            return 0;
       }

        private static void TestString()
        {
            ViterbiTest viterbiTest = new ViterbiTest();
            viterbiTest.TestString(_options.TestString);
        }

        private static void Test()
        {
            ViterbiTest viterbiTest = new ViterbiTest();
            viterbiTest.TestAll();
        }

        private static void RunDec0de()
        {
            RunType runType;

            try
            {
                if(String.IsNullOrEmpty(_options.Run))
                    runType = RunType.GeneralParse;
                else
                    runType = (RunType)Enum.Parse(typeof(RunType), _options.Run, true);
            }
            catch (Exception)
            {
                Console.WriteLine("Unrecognized RunType.");
                return;
            }


            var blocklist = (new NoFilter(_options.Image, 32768)).Filter().UnfilteredBlocks;

            var viterbi = new ThreadedViterbi(blocklist, runType, null, _options.Image, null);
            var results = viterbi.RunThreadedViterbi();

            for (int i = 0; i < results.Fields.Count; i++)
            {
                Console.WriteLine("{0} ({2}): {1}", 
                    results.Fields[i].MachineName,
                    results.Fields[i].FieldString,
                    results.Fields[i].Length);
            }
        }
    }
}
