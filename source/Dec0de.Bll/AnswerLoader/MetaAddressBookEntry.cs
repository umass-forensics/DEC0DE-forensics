using Dec0de.Bll.EmbeddedDal;
using Dec0de.Bll.Viterbi;

namespace Dec0de.Bll.AnswerLoader
{
    public class MetaAddressBookEntry : MetaField
    {
        /// <summary>
        /// Name entry for the address book record.
        /// </summary>
        public string Name;
        /// <summary>
        /// Phone Number entry for the address book record.
        /// </summary>
        public string Number;
        /// <summary>
        /// The last seven digits of the phone number entry.
        /// </summary>
        public string SevenDigit;
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

        public MetaAddressBookEntry() : base(MetaFieldType.AddressBookEntry){}

#if _INSERT_
        protected override void Insert(int fieldId, PhoneDbDataContext dataContext, bool isParse, string source)
        {
            if (isParse)
                dataContext.usp_ParsedFields_AddressBook_Insert(fieldId, Name, Number, SevenDigit, Offset);
            else
                dataContext.usp_Answers_AddressBook_Insert(fieldId, Name, Number, SevenDigit, source);
                
            
        }
#endif

    }
}
