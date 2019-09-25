using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Events
{
    public class OrderSucceededEvent : EventAbst
    {
        public int Cookie { get; set; }
        public string OrderId { get; set; }

        public OrderSucceededEvent(int cookie, string orderId)
        {
            Cookie = cookie;
            OrderId = orderId;
        }
        public override string ToString()
        {
            return ("OrderSucceededEvent: Cookie = " + Cookie + ", OrderId = " + OrderId);
        }
        public override void Log(LogWriter logWriter, DBInputOutput.DBWriter dbWriter = null, int assetid = -1)
        {
            DateTime dTime = ServerTime.GetRealTime();
            if (dbWriter != null)
            {
                dbWriter.InsertOrderLog(dTime, OrderId, "Succeded", Cookie, "", assetid);
            }
            logWriter.WriteLine(dTime.ToString(DateTimeFormat) + " | Order succeeded. Cookie: {0}; OrderId: {1}", Cookie, OrderId);
        }
    }
}
