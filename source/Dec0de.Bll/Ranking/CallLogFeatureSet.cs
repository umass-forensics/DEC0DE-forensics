using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dec0de.Bll.EmbeddedDal;

namespace Dec0de.Bll.Ranking
{
    public class CallLogFeatureSet
    {
        private List<string> _numbers;
        private List<PhoneCrossRecordFeature.Tuple> _numbersWithType;
        private AreaCodeFeature _areaCode;
        private PhoneCrossRecordFeature _phoneCrossRecord;
        private PhoneFormFeature _phoneForm;
        private BigramPerLengthFeature _bigram;
        private DateTimeDistanceFeature _dateDist;

        public CallLogFeatureSet(int parseId)
        {
            using (var dataContext = Dalbase.GetDataContext())
            {
                _numbers = (from result in dataContext.usp_ParsedFields_GetAllRecordPhoneNumbers_ByParseId(parseId) select result.number).ToList();

                _numbersWithType =
                    (from result in dataContext.usp_ParsedFields_GetAllRecordPhoneNumbersWithRecordType_ByParseId(parseId)
                     select new PhoneCrossRecordFeature.Tuple { Number = result.number, RecordType = result.recordType }).
                        ToList();

                //Create the phone features
                _areaCode = new AreaCodeFeature(_numbers);
                _phoneCrossRecord = new PhoneCrossRecordFeature(_numbersWithType);
                _phoneForm = new PhoneFormFeature(_numbers);
                _bigram = new BigramPerLengthFeature();

                var results = (from result in dataContext.usp_Decode_CallLogs_CompareAnswersToParse(parseId)
                               where result.name != null
                               select result).ToList();

                var timeStamps = (from result in results select result.timestamp.Value).ToList();

                _dateDist = new DateTimeDistanceFeature(timeStamps);

                for (int i = 0; i < results.Count; i++)
                {
                    bool isCorrect = (results[i].answer_name != null);

                    CreateFeatureRecord(dataContext, isCorrect, results[i].name, results[i].number, results[i].timestamp.Value, parseId);
                }
            }
        }



        private void CreateFeatureRecord(PhoneDbDataContext dataContext, bool isCorrect, string name, string number, DateTime timestamp, int parseId)
        {
            double areaCodeScore = _areaCode.GetScore(number);
            double phoneCrossScore = _phoneCrossRecord.GetScore(number);
            double phoneFormScore = _phoneForm.GetScore(number);
            double lengthScore = name.Length;
            double alphaScore = AlphaPerLengthFeature.GetScore(name);
            double bigramScore = _bigram.GetScore(name);
            double distScore = _dateDist.GetScore(timestamp);

            try
            {
                dataContext.usp_Feature_Calllog_Insert(parseId, name, number, timestamp, areaCodeScore, phoneCrossScore,
                                                           phoneFormScore, lengthScore, alphaScore, bigramScore, distScore,
                                                           isCorrect);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
