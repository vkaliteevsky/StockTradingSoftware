using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Events
{
    public class OrderFailedEvent : EventAbst
    {
        public int Cookie { get; set; }
        public string OrderId { get; set; }
        public string Reason { get; set; }

        public OrderFailedEvent(int cookie, string orderId, string reason)
        {
            Cookie = cookie;
            OrderId = orderId;
            Reason = reason;
        }

        public override string ToString()
        {
            return ("OrderFailedEvent: OrderId = " + OrderId + ", Cookie = " + Cookie + ", Reason = " + Reason);
        }
        public override void Log(LogWriter logWriter, DBInputOutput.DBWriter dbWriter = null, int assetid = -1)
        {
            DateTime dTime = ServerTime.GetRealTime();
            if (dbWriter != null)
            {
                dbWriter.InsertOrderLog(dTime, OrderId, "Failed", Cookie, Reason, assetid);
            }
            logWriter.WriteLine(dTime.ToString(DateTimeFormat) + " | Order failed. Cookie: {0}; OrderId: {1}; Reason: {2}", Cookie, OrderId, Reason);
        }
    }
}
