using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.UI.HashLoader.EmbeddedDal
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
