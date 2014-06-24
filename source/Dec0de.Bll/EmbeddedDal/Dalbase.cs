namespace Dec0de.Bll.EmbeddedDal
{
    public static class Dalbase
    {
        public static PhoneDbDataContext GetDataContext()
        {
            PhoneDbDataContext dataContext = new PhoneDbDataContext { CommandTimeout = 900 };

            return dataContext;
        }
    }
}
