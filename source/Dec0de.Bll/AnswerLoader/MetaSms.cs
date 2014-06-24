using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dec0de.Bll.EmbeddedDal;
using Dec0de.Bll.Viterbi;

namespace Dec0de.Bll.AnswerLoader
{
    public class MetaSms : MetaField
    {
        /// <summary>
        /// Name entry for the SMS record.
        /// </summary>
        public string Name;
        /// <summary>
        /// Phone number entry for the SMS record.
        /// </summary>
        public string Number;
        /// <summary>
        /// The last seven digits of the SMS entry.
        /// </summary>
        public string SevenDigit;
        public string Number2;
        public string SevenDigit2;
        /// <summary>
        /// The timestamp field for the SMS entry.
        /// </summary>
        public DateTime? TimeStamp;
        /// <summary>
        /// The actual textual content of the SMS entry.
        /// </summary>
        public string Message;
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

        public MetaSms() : base(MetaFieldType.Sms){}

#if _INSERT_
        protected override void Insert(int fieldId, PhoneDbDataContext dataContext, bool isParse, string source)
        {
            if(isParse)
            dataContext.usp_ParsedFields_SMS_Insert(
                fieldId,
                Name,
                Number,
                SevenDigit,
                Number2,
                SevenDigit2,
                Message,
                TimeStamp,
                Offset
                );
            else
            {
                dataContext.usp_Answers_SMS_Insert(fieldId, Name, Number, SevenDigit, TimeStamp, Message, source);
            }

        }
#endif


    }
}
