using System;
using System.Collections.Generic;
using Dec0de.Bll.EmbeddedDal;

namespace Dec0de.Bll.AnswerLoader
{

    /// <summary>
    /// The base class for all of the meta fields. A meta field is just
    /// a simple representation of composite fields, e.g. call logs, SMS,
    /// and address book entries. These classes are used to upload the
    /// 'answer key' or 'results' for a specfic phone and composite field type.
    /// </summary>
    public abstract class MetaField
    {
        public const string DEFAULT_STRING = "*NONE*";
        public static readonly DateTime DEFAULT_DATE = new DateTime(1900, 1, 1, 0, 0, 0);

        /// <summary>
        /// I use this protected constructor to force myself to add
        /// new entries to the MetaFieldType enumeration everytime
        /// I create a new child class type.
        /// </summary>
        /// <param name="type"></param>
        protected MetaField(MetaFieldType type)
        {
            FieldType = type;
        }

#if _INSERT_
        /// <summary>
        /// Inserts the field into the database. The specific sproc is
        /// dependent on the type of field and whether it is an answer or a parse result.
        /// </summary>
        /// <param name="fieldId">fieldId is either the parseId or the phoneId depending on if
        /// you are loading results for a Dec0de parse or the answers (fields known to be on the phone)</param>
        /// <param name="dataContext"></param>
        /// <param name="isParse">Set to 'true' if this is a result from Dec0de's parse. Set to 'false'
        /// if this is an answer/known field value</param>
        /// <param name="source">The source of the data for the insert, e.g. xry</param>
        protected abstract void Insert(int fieldId, PhoneDbDataContext dataContext, bool isParse, string source);

        /// <summary>
        /// Inserts a collection of composite fields into the database. The specific sproc is
        /// dependent on the type of field.
        /// </summary>
        /// <param name="fieldId">fieldId is either the parseId or the phoneId depending on if
        /// you are loading results for a Dec0de parse or the answers (fields known to be on the phone)</param>
        /// <param name="fields">The collection of fields to be loaded</param>
        /// <param name="isParse">Set to 'true' if this is a result from Dec0de's parse. Set to 'false'
        /// if this is an answer/known field value</param>
        /// <param name="source">The source of the data for the insert, e.g. xry</param>
        public static void Insert(int fieldId, List<MetaField> fields, bool isParse, string source)
        {
            int insertCount = 0;

            using(var dataContext = Dalbase.GetDataContext())
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    try
                    {
                        fields[i].Insert(fieldId, dataContext, isParse, source);
                        insertCount++;
                    }
                    catch (Exception ex)
                    {
                 
                        Console.WriteLine(ex.Message + " : " + fields[i]);
                    }
                }
            }

            Console.WriteLine("Inserted {0} records into the database. Failed on {1}.", insertCount, fields.Count - insertCount);
        }
#endif

        /// <summary>
        /// Attempts to parse the string into a valid meta field type. Throw exception on
        /// failure.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static MetaFieldType GetFieldType(string typeName)
        {
                MetaFieldType fieldType;

                if(Enum.TryParse(typeName, true, out fieldType))
                {
                    return fieldType;
                }

                string message = "Failed to parse: " + typeName + ". Allowable types are: ";

                var types = Enum.GetNames(typeof (MetaFieldType));

                for (int i = 0; i < types.Length; i++)
                {
                    message += types[i] + ", ";
                }

                //Remove the last ", "
                message.Remove(message.Length - 2, 2);

                throw new ArgumentException(message);
 
        }


        public MetaFieldType FieldType { get; private set; }


        public override string ToString()
        {
            var fields = GetType().GetFields();

            string message = "";

            for (int i = 0; i < fields.Length; i++)
            {
                message += fields[i].GetValue(this) + ", ";
            }

            return message.Remove(message.Length - 2, 2);
        }
    }


    
}
