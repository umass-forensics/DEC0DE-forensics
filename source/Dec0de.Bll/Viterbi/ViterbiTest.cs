using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll.Viterbi
{
    public class ViterbiTest
    {
        private class Test
        {
            public string Name;
            public byte[] RawBytes;
            public MachineList[] Machines = new MachineList[]{};
            public string[] Answers = new string[]{};
            public string[] WrongAnswers = new string[]{};
        }

        private enum TestResult
        {
            Passed,
            Partial,
            Failed
        }

        #region Test Observations
        //XXXXXXX
        //0x58, 0x58,0x58,0x58,0x58,0x58,0x58,0x58
        private static byte[] Test_XXXXXXX = new byte[] { 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58 };

        private static byte[] Test_XXXXXXX_Unicode = new byte[] { 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58, 0x00, 0x58 };

        private static byte[] Test_MotoUnicode_bad = new byte[] { 0x00, 0x31, 0x00, 0x35, 0x00, 0x30, 0x00, 0x20, 0x00, 0x33, 0x00, 0x41, 0x00, 0x37, 0x00, 0x34, 0x00, 0x31, 0x00, 0x35, 0x00, 0x30 };

        private static byte[] Test_SamsungPhone11DigitAscii_bad = new byte[] { 0x0f, 0x33, 0x32, 0x33, 0x38, 0x33, 0x36, 0x38, 0x39, 0x38, 0x31 };

        private static byte[] Test_SamsungPhone11DigitAscii = new byte[] { 0x31, 0x33, 0x32, 0x33, 0x38, 0x33, 0x36, 0x38, 0x39, 0x38, 0x31 };

        private static byte[] Test_SamsungPhone10DigitAscii = new byte[] { 0x33, 0x32, 0x33, 0x38, 0x33, 0x36, 0x38, 0x39, 0x38, 0x31 };


        private static byte[] Test_SamsungPhone7DigitAscii = new byte[] {  0x38, 0x33, 0x36, 0x38, 0x39, 0x38, 0x31 };


        private static byte[] Test_NokiaPhone11Digit = new byte[] { 0x0B, 0x17, 0xA2, 0x37, 0x27, 0x91, 0x60 };

        private static byte[] Test_NokiaPhone12Digit = new byte[] { 0x0C, 0xF1, 0x91, 0x62, 0xA8, 0x29, 0x79 };

        private static byte[] Test_NokiaPhone10Digit = new byte[] { 0x0A, 0x91, 0x65, 0x49, 0x58, 0xA4 };

        private static byte[] Test_NokiaPhone7Digit = new byte[] { 0x07, 0x54, 0x95, 0x8A, 0x40 };

        private static byte[] Test_NokiaPhone8Digit = new byte[] { 0x08, 0xF7, 0x17, 0x71, 0x72 };

        private static byte[] Test_SmsPhone = new byte[] { 0x0B, 0x91, 0x61, 0x63, 0x83, 0x84, 0x08, 0xF1 };

        private static byte[] Test_ShortAscii = new byte[] { 0x52, 0x6F, 0x62 };

        //Bro magnum Entry
        private static byte[] Test_BroMagnum = new byte[]
                                    {
                                        0x42, 0x72, 0x6F, 0x20, 0x4D, 0x61, 0x67, 0x6E, 0x75, 0x6D, 0xFF, 0x06, 0x81, 0x79,
                                        0x12, 0x11, 0x11, 0x11, 0xFF, 0xFF, 0xFF
                                    };

        //Moto Unicode Phone
        private static byte[] Test_MotoUnicodePhone = new byte[]
                                                     {
                                                         0x00, 0x31, 0x00, 0x34, 0x00, 0x30, 0x00, 0x35, 0x00, 0x34, 
                                                         0x00, 0x32, 0x00, 0x30, 0x00, 0x32, 0x00, 0x32, 0x00, 0x38, 
                                                         0x00, 0x33
                                                     };

        private static byte[] Test_MotoUnicodePhoneTen = new byte[]
                                                     {
                                                         0x00, 0x34, 0x00, 0x30, 0x00, 0x35, 0x00, 0x34, 
                                                         0x00, 0x32, 0x00, 0x30, 0x00, 0x32, 0x00, 0x32, 0x00, 0x38, 
                                                         0x00, 0x33
                                                     };

        private static byte[] Test_MotoUnicodePhoneSeven = new byte[]
                                                     {
                                                         0x00, 0x34, 
                                                         0x00, 0x32, 0x00, 0x30, 0x00, 0x32, 0x00, 0x32, 0x00, 0x38, 
                                                         0x00, 0x33
                                                     };

        private static byte[] Test_Moto7Digit = new byte[] { 0x05, 0x81, 0x36, 0x76, 0x41, 0xF4 };

        private static byte[] Test_Moto10Digit = new byte[] { 0x06, 0x81, 0x16, 0x96, 0x08, 0x42, 0x50 };
        private static byte[] Test_Moto11Digit = new byte[] { 0x07, 0x81, 0x81, 0x00, 0x45, 0x37, 0x00, 0xF0 };

        private static byte[] Test_NokiaTimeStamp = new byte[] { 0x07, 0xD6, 0x02, 0x0E, 0x11, 0x29, 0x18 };

        private static byte[] Test_MotoTimeStamp = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                            0xFF, 0xFF, 0x00, 0x02, 0x47, 0x01, 0x34, 0x54 };

        private static byte[] Test_NokiaCallLog = new byte[]{0x06, 0x00, 0x10, 0x07, 0x06, 0x00, 0x44, 0x00, 0x65, 0x00, 0x72, 0x00, 0x65, 0x00, 0x6B, 0x00
                                                            , 0x20, 0x00, 0x4C, 0x00, 0x2E, 0x00, 0x0B, 0x0B, 0x01, 0x00, 0x0A, 0x00, 0x00, 0x0C, 0xF1, 0x91
                                                            , 0x62, 0xA8, 0x29, 0x79, 0x00, 0x08, 0x13, 0x02, 0x07, 0xD6, 0x02, 0x12, 0x16, 0x02, 0x0B, 0x01
                                                            , 0x00, 0x08, 0x13, 0x03, 0x07, 0xD6, 0x02, 0x11, 0x13, 0x08, 0x07, 0x01, 0x00, 0x08, 0x13, 0x04
                                                            , 0x07, 0xD6, 0x02, 0x0F, 0x13, 0x18, 0x2E, 0x01, 0x00, 0x08, 0x13, 0x05, 0x07, 0xD6, 0x02, 0x0F
                                                            , 0x12, 0x19, 0x2C, 0x01};

        private static byte[] Test_SamsungCallLog = new byte[] {0x31, 0x33, 0x32, 0x33, 0x38, 0x33, 0x36, 0x38, 0x39, 0x38, 0x31, 0x00, 0x00, 0x00, 0x00, 0x00
                                                                , 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                                                                , 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5C, 0x3D, 0x7C, 0x7D, 0x00, 0x00
                                                                , 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};


        private static byte[] Test_MotoCallLog = new byte[] { 0xE6, 0x4A, 0x4F, 0x53, 0x45, 0x50, 0x48, 0x49, 0x4E, 0x45, 0xFF, 0xFF, 0xFF,
                                                            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                            0x06, 0x81, 0x17, 0x24, 0x69, 0x30, 0x73, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                            0xFF, 0xFF, 0x00, 0x00, 0x46, 0x46, 0xE6, 0xDB, 0x00, 0x00, 0x01, 0x2D, 0x02, 0x00, 0x0F, 0xDB };

        private static byte[] Test_SamsungTimeStamp = new byte[] { 0x5c, 0x3d, 0x7c, 0x7d };

        private static byte[] Test_SmsTimeStamp = new byte[] { 0x00, 0x20, 0x21, 0x50, 0x75, 0x03, 0x21 };

        private static byte[] Test_Josephine = new byte[] { 0x4A, 0x4F, 0x53, 0x45, 0x50, 0x48, 0x49, 0x4E, 0x45 };

        private static byte[] Test_Ed_Dash_Cell = new byte[] { 0x45, 0x44, 0x2d, 0x43, 0x45, 0x4c, 0x4c };

        private static byte[] Test_LowerToUpper = Utilities.GetBytes("6a4550");


        private static byte[] Test_UnicodeShort = new byte[] { 0x00, 0x45 };

        private static byte[] Test_UnicodeThree = new byte[] { 0x00, 0x45, 0x00, 0x45, 0x00, 0x45 };

        private static byte[] Test_NokiaAddressBook = new byte[]{0x04, 0x00, 0x0A, 0x07, 0x01, 0x00, 0x4D, 0x00, 0x6F, 0x00, 0x6D, 0x00, 0x6D, 0x00, 0x79, 0x00
                                                                , 0x0B, 0x0B, 0x02, 0x00, 0x00, 0x00, 0x00, 0x0C, 0xF1, 0x91, 0x67, 0x17, 0x71, 0x72, 0x00, 0x08
                                                                , 0x33, 0x03, 0x00, 0x01, 0x00, 0x4E, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x1E, 0x04, 0x01};

        private static byte[] Test_MotoSms = Utilities.GetBytes("008A0091000006C6A8A0F4A470A2F830" +
                                                                "000090000001010001240C18142C0698" +
                                                                "00030000000B916163838408F1000B81" +
                                                                "3112124321F90400060000840000045F" +
                                                                "06000000045800424D65727279204368" +
                                                                "726973746D61732066726F6D20466C6F" +
                                                                "72696461212054686973206973206D79" +
                                                                "206E6577202320202020202020202020");

        private static byte[] Test_HiPunct = Utilities.GetBytes("486921");
        private static byte[] Test_HiPunctUnicode = Utilities.GetBytes("004800690021");

        private static byte[] Test_NokiaSentSms = Utilities.GetBytes("0203000105010238010000000004820C"
                                                               + "01080B919161357296F8820C02080791"
                                                               + "9107739603F1800C070841B63C7D46D3"
                                                               + "5D00840C07D60205112E090000");

        private static byte[] Test_AsciiShort = Utilities.GetBytes("6c4f");

        private static byte[] Test_NokiaPhoneWithIndex = Utilities.GetBytes("01000300000B197879A86660");

        private static byte[] Test_NokiaTimeStampWithIndex = Utilities.GetBytes("07D6021415041E01");

                                                                                        //080102010308010308010308
        private static byte[] Test_Meta_NokiaCallLogMulti = Utilities.GetBytes("040108010201080102010308010308010308");

        private static byte[] Test_Meta_NokiaAddressBookSingle = Utilities.GetBytes("040108010201");

        private static byte[] Test_Meta_NokiaAddressBookDouble = Utilities.GetBytes("040108010201040108010201");

        private static byte[] Test_NokiaTimeStamp1 = Utilities.GetBytes("07D603070F1A17");

        private static byte[] Test_Meta_MotoCallLogWithName = Utilities.GetBytes("04010201090603");

        private static byte[] Test_Meta_MotoCallLogWithoutName = Utilities.GetBytes("0201090603");

        private static byte[] Test_International10Digit = Utilities.GetBytes("07811244779173");

        private static byte[] Test_International7Digit = Utilities.GetBytes("0481741739f7");

        private static byte[] Test_Meta_SamsungAddressBook = Utilities.GetBytes("040104010102");

        private static byte[] Test_Meta_SamsungSms = Utilities.GetBytes("0a0107020103");

        private static byte[] Test_UnicodeEndian = Utilities.GetBytes("48006F006D006500");

        private static byte[] Test_UnicodeShort1 = Utilities.GetBytes("00520079");

        private static byte[] Test_UnicodeTooShort = Utilities.GetBytes("5100");

        private static byte[] Test_AsciiStringWithLength = Utilities.GetBytes("03616161");

        private static byte[] Test_NokiaTimeStamp2 = Utilities.GetBytes("D8070B050B352A");

        private static byte[] Test_Meta_NokiaCallLogNoName = Utilities.GetBytes("02010308");

        private static byte[] Test_Meta_GeneralNokia = Utilities.GetBytes("020104010308010308");

        private static byte[] Test_Meta_Sms_NoText = Utilities.GetBytes("07020107020103");

        private static byte[] Test_Meta_Sms_NoTime = Utilities.GetBytes("07020107020104");

        private static byte[] Test_Meta_Sms = Utilities.GetBytes("070201070201030104");

        private static byte[] Test_SamsungSevenDigitNot = Utilities.GetBytes("3337333733373337333733373337");

        private static byte[] Test_SamsungTimeStamp1 = Utilities.GetBytes("D2747C7D");

        private static byte[] Test_Meta_AddressBook = Utilities.GetBytes("0402");

        private static byte[] Test_Meta_AddressBook1 = Utilities.GetBytes("020104");

        private static byte[] Test_Number_Lg = Utilities.GetBytes("810641240027");

        private static byte[] Test_Number_Lg7 = Utilities.GetBytes("81440522F5");

        private static byte[] Test_Number_Inter = Utilities.GetBytes("0000000003820C01070A81374495339900820C020807919170389103F1840C07D40417120E17000000FF30008F0000FFFF00000002000000120503FF30003A0022FFFF000000210000003202000E0701004F006E00200053007400610072000A0B02000000");

        private static byte[] Test_MotoSmsTimestamp = Utilities.GetBytes("240C190C0513");

        private static byte[] Test_MotoSmsTimestamp2 = Utilities.GetBytes("00010100012408080E120F940003000000");

        private static byte[] Test_7bitEncoding = Utilities.GetBytes("0AE8329BFD4697D9EC37");

        private static byte[] Test_7bitEncoding2 = Utilities.GetBytes("0841B63C7D46D35D");

        private static byte[] Test_MarkerSamsungSms = Utilities.GetBytes("4445414442454546");

        private static byte[] Test_MetaMotoSms = Utilities.GetBytes("0301070201020104");

        private static byte[] Test_MetaMotoSms1 = Utilities.GetBytes("0301020104");

        #endregion

        #region Test Values

        #region Meta

        private Test _metaSmsMoto = new Test { Name = "Meta Sms Moto", Machines = new MachineList[] { MachineList.Meta_Sms }, RawBytes = Test_MetaMotoSms };

        private Test _metaSmsMoto1 = new Test { Name = "Meta Sms Moto1", Machines = new MachineList[] { MachineList.Meta_Sms }, RawBytes = Test_MetaMotoSms1 };



        private Test _metaSmsNoText = new Test { Name = "Meta Sms No Text", Machines = new MachineList[] { MachineList.Meta_Sms }, RawBytes = Test_Meta_Sms_NoText };

        private Test _metaSmsNoTime = new Test { Name = "Meta Sms No Time", Machines = new MachineList[] { MachineList.Meta_Sms }, RawBytes = Test_Meta_Sms_NoTime };

        private Test _metaSmsSamsung = new Test { Name = "Meta Sms Samsung", Machines = new MachineList[] { MachineList.Meta_Sms }, RawBytes = Test_Meta_SamsungSms };

        private Test _metaSms = new Test { Name = "Meta Sms", Machines = new MachineList[] { MachineList.Meta_Sms }, RawBytes = Test_Meta_Sms };

        private Test _metaAddressBook = new Test { Name = "Meta Addressbook Simple", Machines = new MachineList[] { MachineList.Meta_AddressBook }, RawBytes = Test_Meta_AddressBook };

        private Test _metaAddressBook1 = new Test { Name = "Meta Addressbook Simple 1", Machines = new MachineList[] { MachineList.Meta_AddressBook }, RawBytes = Test_Meta_AddressBook1 };


        private Test _metaMotoCallLogWithoutName = new Test { Name = "Moto CallLog Without Name", Machines = new MachineList[] { MachineList.Meta_CallLogMoto }, RawBytes = Test_Meta_MotoCallLogWithoutName };


        private Test _metaMotoCallLogWithName = new Test { Name = "Moto CallLog With Name", Machines = new MachineList[] { MachineList.Meta_CallLogMoto }, RawBytes = Test_Meta_MotoCallLogWithName };


        private Test _metaNokiaCallLogNoName = new Test { Name = "Meta Nokia CallLog No Name", Machines = new MachineList[] { MachineList.Meta_CallLogGeneric }, RawBytes = Test_Meta_NokiaCallLogNoName };

        private Test _metaNokiaCallLogGeneral = new Test { Name = "Meta Nokia CallLog General", Machines = new MachineList[] { MachineList.Meta_CallLogGeneric }, RawBytes = Test_Meta_GeneralNokia };


        private Test _metaNokiaCallLogMulti = new Test { Name = "Meta Nokia CallLog Multi", Machines = new MachineList[]{MachineList.Meta_CallLogNokiaMulti_v2}, RawBytes = Test_Meta_NokiaCallLogMulti };

        private Test _metaSamsungAddressBook = new Test { Name = "Samsung Address book", Machines = new MachineList[] { MachineList.Meta_AddressBook }, RawBytes = Test_Meta_SamsungAddressBook };

        private Test _metaNokiaAddressBookSingle= new Test { Name = "Meta Nokia Address book (1 entry)", Machines = new MachineList[] { MachineList.Meta_AddressBookNokia }, RawBytes = Test_Meta_NokiaAddressBookSingle };

        private Test _metaNokiaAddressBookDouble = new Test { Name = "Meta Nokia Address book (2 entry)", Machines = new MachineList[] { MachineList.Meta_AddressBookNokia }, RawBytes = Test_Meta_NokiaAddressBookDouble };

        

        #endregion

        #region SMS

        private Test _motoSMS = new Test
        {
            Name = "Motorola SMS",
            Answers = new[] 
                        { 
                            "12/24/2006 8:44:06 PM", 
                            "16363848801",
                            "13212134129",
                            "Merry Christmas from Florida! This is my new #          "
                        },
            RawBytes = Test_MotoSms
        };

        private Test _nokiaSentSMS = new Test
        {
            Name = "Nokia Sent SMS",
            Answers = new[] 
                        { 
                            "19165327698", 
                            "19703769301",
                            "2/5/2006 5:46:09 PM",
                            "Alright."
                        },
            RawBytes = Test_NokiaSentSms
        };

        #endregion

        #region AddressBook

        private Test _nokiaAddressBook = new Test { Name = "Nokia Address Book", Answers = new[] { "Mommy", "+19167177172" }, RawBytes = Test_NokiaAddressBook };

        #endregion

        #region CallLogs

        private Test _nokiaCallLog = new Test
        {
            Name = "Nokia CallLog",
            Answers = new[] { "Derek L.", "+19162082979", "2/18/2006 10:02:11 PM", "2/17/2006 7:08:07 PM", "2/15/2006 7:24:46 PM", "2/15/2006 6:25:44 PM" },
            RawBytes = Test_NokiaCallLog
        };

        private Test _motoCallLog = new Test { Name = "Motorola CallLog", Answers = new[] { "JOSEPHINE", "7142960337", "5/13/2007 10:22:19 AM" }, RawBytes = Test_MotoCallLog };

        private Test _samsungCallLog = new Test { Name = "Samsung CallLog", Answers = new[] { "13238368981", "12/7/2007 9:28:00 PM" }, RawBytes = Test_SamsungCallLog };

        #endregion

        #region Text

        private Test _textMarkerSamsungSms = new Test { Name = "Test Samsung DEADBEEF", Answers = new[] { "DEADBEEF" }, RawBytes = Test_MarkerSamsungSms, Machines = new[]{MachineList.Marker_SamsungSms}};


        private Test _text7BitEncoding2 = new Test { Name = "Test 7 Bit encoding with Length 2", Answers = new[] { "Alright." }, RawBytes = Test_7bitEncoding2 };


        private Test _text7BitEncoding = new Test { Name = "Test 7 Bit encoding with Length", Answers = new[] { "hellohello" }, RawBytes = Test_7bitEncoding };

        private Test _textHiPunctUnicode = new Test { Name = "Text Hi! Unicode", Answers = new[] { "Hi!" }, RawBytes = Test_HiPunctUnicode };

        private Test _textHiPunct = new Test { Name = "Text Hi!", Answers = new[] { "Hi!" }, RawBytes = Test_HiPunct };

        private Test _textLowerToUpper = new Test { Name = "Text jEP", WrongAnswers = new[] { "jEP" }, RawBytes = Test_LowerToUpper };

        private Test _textAsciiShort = new Test { Name = "Text AciiShort", WrongAnswers = new [] { "lO" }, RawBytes = Test_AsciiShort };

        private Test _textAsciiWithLength = new Test { Name = "Text Ascii with length", Answers = new[] { "aaa" }, RawBytes = Test_AsciiStringWithLength };


        private Test _textUnicodeEndian = new Test { Name = "Text Unicode Endian", Answers = new[] { "Home" }, RawBytes = Test_UnicodeEndian };


        private Test _textUnicodeTooShort = new Test { Name = "Text Unicode Too Short Q", WrongAnswers = new[] { "Q" }, RawBytes = Test_UnicodeTooShort };


        private Test _textUnicodeShort = new Test { Name = "Text Unicode Ry", Answers = new[] { "Ry" }, RawBytes = Test_UnicodeShort1 };


        private Test _textUnicode = new Test { Name = "Text Unicode", Answers = new[] { "XXXXXXXX" }, RawBytes = Test_XXXXXXX_Unicode };

        private Test _textEd = new Test { Name = "Text Ed-Cell", Answers = new[] { "ED-CELL" }, RawBytes = Test_Ed_Dash_Cell };

        private Test _textJosephine = new Test { Name = "Text Josephine", Answers = new[] { "JOSEPHINE" }, RawBytes = Test_Josephine };

        #endregion

        #region PhoneNumbers

        private Test _lgPhone = new Test { Name = "LG", Answers = new[] { "6014420072" }, RawBytes = Test_Number_Lg };

        private Test _lgPhone7 = new Test { Name = "LG 7 Digit", Answers = new[] { "4450225" }, RawBytes = Test_Number_Lg7 };


        private Test _internationalPhone7 = new Test { Name = "International (SMS) Phone 7", Answers = new[] { "4771937" }, RawBytes = Test_International7Digit };

        private Test _internationalPhone10 = new Test { Name = "International (SMS) Phone 10", Answers = new[] { "2144771937" }, RawBytes = Test_International10Digit };

        private Test _internationalPhone10_mixup = new Test { Name = "International (SMS) Phone 10 Digit Mixup", Answers = new[] { "7344593399" }, RawBytes = Test_Number_Inter };


        private Test _internationalPhone11 = new Test { Name = "International (SMS) Phone 11 Digit", Answers = new[] { "16363848801" }, RawBytes = Test_SmsPhone };

        private Test _samsung7Digit = new Test { Name = "Samsung 7 Digit", Answers = new[] { "8368981" }, RawBytes = Test_SamsungPhone7DigitAscii };

        private Test _samsung7DigitNot = new Test { Name = "Samsung 7 Digit Bad", WrongAnswers = new[] { "37373737373737" }, RawBytes = Test_SamsungSevenDigitNot };



        private Test _samsung10Digit = new Test { Name = "Samsung 10 Digit", Answers = new[] { "3238368981" }, RawBytes = Test_SamsungPhone10DigitAscii };

        private Test _samsung11Digit = new Test { Name = "Samsung 11 Digit", Answers = new[] { "13238368981" }, RawBytes = Test_SamsungPhone11DigitAscii };

        private Test _motoUnicodePhone7 = new Test { Name = "Motorola Unicode Phone 7", Answers = new[] { "4202283" }, RawBytes = Test_MotoUnicodePhoneSeven };


        private Test _motoUnicodePhone10 = new Test { Name = "Motorola Unicode Phone 10", Answers = new[] { "4054202283" }, RawBytes = Test_MotoUnicodePhoneTen };


        private Test _motoUnicodePhone = new Test { Name = "Motorola Unicode Phone 11", Answers = new[] { "14054202283" }, RawBytes = Test_MotoUnicodePhone };

        private Test _moto7Digit = new Test { Name = "Motorola 7 Digit", Answers = new[] { "6367144" }, RawBytes = Test_Moto7Digit };

        private Test _moto10Digit = new Test { Name = "Motorola 10 Digit", Answers = new[] { "6169802405" }, RawBytes = Test_Moto10Digit };

        private Test _moto11Digit = new Test { Name = "Motorola 11 Digit", Answers = new[] { "18005473000" }, RawBytes = Test_Moto11Digit };

        private Test _nokia7Digit = new Test { Name = "Nokia 7 Digit", Answers = new[] { "5495804" }, RawBytes = Test_NokiaPhone7Digit };

        private Test _nokia8Digit = new Test { Name = "Nokia 8 Digit", Answers = new[] { "+7177172" }, RawBytes = Test_NokiaPhone8Digit };

        private Test _nokia10Digit = new Test { Name = "Nokia 10 Digit", Answers = new[] { "9165495804" }, RawBytes = Test_NokiaPhone10Digit };

        private Test _nokia11Digit = new Test { Name = "Nokia 11 Digit", Answers = new[] { "17023727916" }, RawBytes = Test_NokiaPhone11Digit };

        private Test _nokia12Digit = new Test { Name = "Nokia 12 Digit", Answers = new[] { "+19162082979" }, RawBytes = Test_NokiaPhone12Digit };

        private Test _nokiaNumberWithIndex = new Test { Name = "Nokia Number with CallLog Index", Answers = new[] { "19787908666", "1" }, RawBytes = Test_NokiaPhoneWithIndex };


        #endregion

        #region TimeStamps

        private Test _motoTimeStamp = new Test { Name = "Motorola TimeStamp", Answers = new[] { "10/1/2007 5:54:28 PM" }, RawBytes = Test_MotoTimeStamp };

        private Test _motoSmsTimeStamp = new Test { Name = "Motorola SMS TimeStamp", Answers = new[] { "12/25/2006 12:05:19 PM" }, RawBytes = Test_MotoSmsTimestamp };

        private Test _motoSmsTimeStamp2 = new Test { Name = "Motorola SMS TimeStamp 2", Answers = new[] { "8/8/2006 2:18:15 PM" }, RawBytes = Test_MotoSmsTimestamp2 };


        private Test _nokiaTimeStamp1 = new Test { Name = "Nokia TimeStamp 1", Answers = new[] { "3/7/2006 3:26:23 PM" }, RawBytes = Test_NokiaTimeStamp1 };


        private Test _nokiaTimeStamp2 = new Test { Name = "Nokia TimeStamp Endian", Answers = new[] { "11/5/2008 11:53:42 AM" }, RawBytes = Test_NokiaTimeStamp2 };


        private Test _nokiaTimeStamp = new Test { Name = "Nokia TimeStamp", Answers = new[] { "2/14/2006 5:41:24 PM" }, RawBytes = Test_NokiaTimeStamp };

        private Test _nokiaTimeStampWithIndex = new Test { Name = "Nokia TimeStamp With Index", Answers = new[] { "2/20/2006 9:04:30 PM", "1" }, RawBytes = Test_NokiaTimeStampWithIndex };

        private Test _samsungTimeStamp1 = new Test { Name = "Samsung TimeStamp 1", Answers = new[] { "12/14/2007 7:18:00 PM"}, RawBytes = Test_SamsungTimeStamp1 };


        private Test _samsungTimeStamp = new Test { Name = "Samsung TimeStamp", Answers = new[] { "12/7/2007 9:28:00 PM" }, RawBytes = Test_SamsungTimeStamp };

        private Test _smsTimeStamp = new Test { Name = "Sms TimeStamp", Answers = new[] { "2/12/2000 5:57:30 AM" }, RawBytes = Test_SmsTimeStamp };

        #endregion

        #endregion

        #region Declarations

        private List<StateMachine> _generalMachines = new List<StateMachine>();
        private List<State> _generalStates = new List<State>();
        private State _generalStartState = null;
        private Viterbi _generalViterbi;

        private List<StateMachine> _metaMachines = new List<StateMachine>();
        private List<State> _metaStates = new List<State>();
        private State _metaStartState = null;
        private Viterbi _metaViterbi;

        #endregion

        public ViterbiTest()
        {
            StateMachine.GeneralParse(ref _generalMachines, ref _generalStates, ref _generalStartState);


            //var testMachines = new List<StateMachine>
            //                       {
            //                           StateMachine.Get7BitString_WithLength(1),
            //                           //StateMachine.GetPhoneNumber_All(6),
            //                           //StateMachine.GetTimeStamp_All(1),
            //                       };


            //StateMachine.TestStateMachines(testMachines, ref _generalMachines, ref _generalStates, ref _generalStartState);

            StateMachine.TestMeta(ref _metaMachines, ref _metaStates, ref _metaStartState);

            _metaViterbi = new Viterbi(_metaMachines, _metaStates, _metaStartState);

            _generalViterbi = new Viterbi(_generalMachines, _generalStates, _generalStartState);
        }

        public void TestString(string input)
        {
            Console.WriteLine("Testing input: {0}", input);

            byte[] raw = Utilities.GetBytes(input);

            _generalViterbi.Run(raw, 0);


            for (int i = 0; i < _generalViterbi.FieldStrings.Count(); i++)
            {
                Console.WriteLine("{0} : {1}", _generalViterbi.Fields[i].FieldString, 
                    _generalViterbi.Fields[i].MachineName);
            }

            _generalViterbi.FieldStrings.Clear();
            _generalViterbi.Fields.Clear();
        }

        public void TestTimeStamps()
        {
            Console.WriteLine("Testing timestamps...");

            var timeStampTests = new List<Test>()
                            {
                                _motoSmsTimeStamp2,
                                _motoSmsTimeStamp,
                                _samsungTimeStamp1,
                                _nokiaTimeStamp2,
                                _nokiaTimeStamp1,
                                _nokiaTimeStampWithIndex,
                                _motoTimeStamp,
                                _nokiaTimeStamp,
                                _motoTimeStamp,
                                _samsungTimeStamp,
                                _smsTimeStamp
                            };

            TestAll(_generalViterbi, timeStampTests);
        }

        public void TestPhoneNumbers()
        {
            Console.WriteLine("Testing phone numbers...");

            var phoneTests = new List<Test>()
                                 {
                                     _internationalPhone10_mixup,
                                     _lgPhone,
                                     _lgPhone7,
                                     _samsung7DigitNot,
                                     _internationalPhone7,
                                     _internationalPhone10,
                                     _internationalPhone11,
                                     _nokiaNumberWithIndex,
                                     _nokia7Digit,
                                     _nokia8Digit,
                                     _nokia10Digit,
                                     _nokia11Digit,
                                     _nokia12Digit,
                                     _moto7Digit,
                                     _moto10Digit,
                                     _moto11Digit,
                                     _motoUnicodePhone,
                                     _motoUnicodePhone7,
                                     _motoUnicodePhone10,
                                     _samsung11Digit,
                                     _samsung10Digit,
                                     _samsung7Digit
                                 };

            TestAll(_generalViterbi, phoneTests);
        }

        public void TestText()
        {
            Console.WriteLine("Testing text...");

            var textTests = new List<Test>()
                                {
                                    
                                    _text7BitEncoding2,
                                    _textMarkerSamsungSms,
                                    _text7BitEncoding,
                                    _textUnicode,
                                    _textAsciiWithLength,
                                    _textUnicodeTooShort,
                                    _textUnicodeShort,
                                    _textUnicodeEndian,
                                    _textLowerToUpper,
                                    _textAsciiShort,
                                    _textHiPunct,
                                    _textHiPunctUnicode,
                                    _textJosephine,
                                    _textEd

                                };

            TestAll(_generalViterbi, textTests);
        }

        public void TestCallLogs()
        {
            Console.WriteLine("Testing call logs...");

            var callLogTests = new List<Test>()
                                   {
                                       _nokiaCallLog,
                                       _samsungCallLog,
                                       _motoCallLog
                                   };

            TestAll(_generalViterbi, callLogTests);
        }

        public void TestAddressBook()
        {
            Console.WriteLine("Testing address book entries...");

            var addressBookTests = new List<Test>()
                                       {
                                           _nokiaAddressBook
                                       };

            TestAll(_generalViterbi, addressBookTests);
        }

        public void TestSms()
        {
            Console.WriteLine("Testing SMS...");

            var smsTests = new List<Test>()
                               {
                                   _nokiaSentSMS,
                                   _motoSMS
                               };

            TestAll(_generalViterbi, smsTests);
        }

        public void TestMeta()
        {
            Console.WriteLine("Testing Meta...");

            var metaTests = new List<Test>
                                {
                                    _metaSmsMoto,
                                    _metaSmsMoto1,
                                    _metaSmsSamsung,
                                    _metaSmsNoTime,
                                    _metaSms,
                                    _metaSmsNoText,
                                    _metaAddressBook1,
                                    _metaAddressBook,
                                    _metaNokiaCallLogGeneral,
                                    _metaNokiaCallLogNoName,
                                    _metaSamsungAddressBook,
                                    _metaMotoCallLogWithName,
                                    _metaMotoCallLogWithoutName,
                                    _metaNokiaAddressBookSingle,
                                    _metaNokiaAddressBookDouble,
                                    _metaNokiaCallLogMulti
                                    
                                };
            TestAll(_metaViterbi, metaTests);
        }

        public void TestAll()
        {
            TestMeta();
            TestTimeStamps();
            TestPhoneNumbers();
            TestText();
            TestCallLogs();
            TestAddressBook();
            TestSms();
        }

        private void TestAll(Viterbi viterbi, List<Test> tests)
        {
            for (int i = 0; i < tests.Count; i++)
            {
                viterbi.Run(tests[i].RawBytes, 0);
                PrintResult(viterbi, tests[i]);

                viterbi.FieldStrings.Clear();
                viterbi.Fields.Clear();
            }
        }

        private void PrintResult(Viterbi viterbi, Test test)
        {
            TestResult result = TestResult.Failed;

            int count = 0;
            int countWrong = 0;
            int countMachines = 0;



            if (test.Answers.Length == 0 && test.WrongAnswers.Length == 0 && test.Machines.Length == 0)
            {
                Console.WriteLine("No answers defined for {0}!", test.Name);
                return;
            }

            MachineList[] machineList = (from v in viterbi.Fields select v.MachineName).ToArray();

            for (int i = 0; i < test.Machines.Length; i++)
            {
                if (machineList.Contains(test.Machines[i]))
                    countMachines++;
            }

            for (int i = 0; i < test.WrongAnswers.Length; i++)
            {
                if (viterbi.FieldStrings.Contains(test.WrongAnswers[i]))
                    countWrong++;
            }

            for (int i = 0; i < test.Answers.Length; i++)
            {
                if (viterbi.FieldStrings.Contains(test.Answers[i]))
                    count++;
            }

            var prevColor = Console.ForegroundColor;

            //test Answers length could be zero
            if (count == test.Answers.Length && countWrong == 0 && countMachines == test.Machines.Length)
            {
                result = TestResult.Passed;
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (count != test.Answers.Length && count > 0 && countWrong == 0 &&
                countMachines != test.Machines.Length && countMachines > 0)
            {
                result = TestResult.Partial;
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else
            {
                result = TestResult.Failed;
                Console.ForegroundColor = ConsoleColor.Red;
            }




            Console.WriteLine("Test \'{0}\': {1}", test.Name, result);

            Console.ForegroundColor = prevColor;
        }
    }
}
