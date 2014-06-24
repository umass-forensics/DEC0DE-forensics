using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dec0de.Bll.EmbeddedDal;

namespace Dec0de.Bll.Ranking
{
    public class AddressBookFeatureSet
    {
        private List<string> _numbers;
        private List<PhoneCrossRecordFeature.Tuple> _numbersWithType;
        private AreaCodeFeature _areaCode;
        private PhoneCrossRecordFeature _phoneCrossRecord;
        private PhoneFormFeature _phoneForm;
        private BigramPerLengthFeature _bigram;

        public AddressBookFeatureSet(int parseId)
        {
            using (var dataContext = Dalbase.GetDataContext())
            {
                _numbers = (from result in dataContext.usp_ParsedFields_GetAllRecordPhoneNumbers_ByParseId(parseId) select result.number).ToList();

                _numbersWithType =
                    (from result in dataContext.usp_ParsedFields_GetAllRecordPhoneNumbersWithRecordType_ByParseId(parseId)
                     select new PhoneCrossRecordFeature.Tuple {Number = result.number, RecordType = result.recordType}).
                        ToList();

                //Create the phone features
                _areaCode = new AreaCodeFeature(_numbers);
                _phoneCrossRecord = new PhoneCrossRecordFeature(_numbersWithType);
                _phoneForm = new PhoneFormFeature(_numbers);
                _bigram = new BigramPerLengthFeature();

                var results = (from result in dataContext.usp_Decode_AddressBook_CompareAnswersToParse(parseId) 
                               where result.name != null
                               select result).ToList();

                

                for (int i = 0; i < results.Count; i++)
                {
                    bool isCorrect = (results[i].answer_name != null);

                    CreateFeatureRecord(dataContext, isCorrect, results[i].name, results[i].number, parseId);
                }
            }
        }



        private void CreateFeatureRecord(PhoneDbDataContext dataContext, bool isCorrect, string name, string number, int parseId)
        {
            double areaCodeScore = _areaCode.GetScore(number);
            double phoneCrossScore = _phoneCrossRecord.GetScore(number);
            double phoneFormScore = _phoneForm.GetScore(number);
            double lengthScore = name.Length;
            double alphaScore = AlphaPerLengthFeature.GetScore(name);
            double bigramScore = _bigram.GetScore(name);


            try
            {
                dataContext.usp_Feature_AddressBook_Insert(parseId, name, number, areaCodeScore, phoneCrossScore,
                                                           phoneFormScore, lengthScore, alphaScore, bigramScore,
                                                           isCorrect);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
