using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Events
{
    public abstract class EventAbst
    {
        //public abstract void Log(LogWriter logWriter);
        public abstract void Log(LogWriter logWriter, DBInputOutput.DBWriter dbWriter = null, int assetid = -1);
        protected static string DateTimeFormat = "G";
    }
}
