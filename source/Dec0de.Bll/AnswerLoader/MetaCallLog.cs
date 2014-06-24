using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dec0de.Bll.EmbeddedDal;
using Dec0de.Bll.Viterbi;

namespace Dec0de.Bll.AnswerLoader
{
    public class MetaCallLog : MetaField
    {
        /// <summary>
        /// Name entry for the call log record.
        /// </summary>
        public string Name;
        /// <summary>
        /// Phone number entry for the call log record.
        /// </summary>
        public string Number;
        /// <summary>
        /// The last seven digits of the phone number entry.
        /// </summary>
        public string SevenDigit;
        /// <summary>
        /// Represents whether the call was received, dialed or missed.
        /// </summary>
        public string Type;
        /// <summary>
        /// The timestamp field for the call log entry.
        /// </summary>
        public DateTime? TimeStamp;
        /// <summary>
        /// The starting position of the record in the memory file.
        /// </summary>
        public long Offset;
        /// <summary>
        /// Measures distance of this record from a similar record in the neighbourhood on the memory file.
        /// </summary>
        public long ProximityOffset;
        /// <summary>
        /// Name of the state machine to which the record belongs to.
        /// </summary>
        public MachineList MachineName;

        public MetaCallLog() : base(MetaFieldType.CallLog){}

#if _INSERT_
        protected override void Insert(int fieldId, PhoneDbDataContext dataContext, bool isParse, string source)
        {
            if(isParse)
                dataContext.usp_ParsedFields_CallLog_Insert(fieldId, Name, Number, SevenDigit, Type, TimeStamp, Offset);
                                                            
            else
                dataContext.usp_Answers_CallLog_Insert(fieldId, Name, Number, SevenDigit, Type, TimeStamp, source);
            
        }
#endif

    }
}
