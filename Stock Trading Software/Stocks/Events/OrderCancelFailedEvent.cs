using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Events
{
    public class OrderCancelFailedEvent : EventAbst
    {
        public string OrderId { get; set; }

        public OrderCancelFailedEvent(string orderId)
        {
            OrderId = orderId;
        }
        public override string ToString()
        {
            return ("OrderCancelFailedEvent: OrderId = " + OrderId);
        }
        public override void Log(LogWriter logWriter, DBInputOutput.DBWriter dbWriter = null, int assetid = -1)
        {
            DateTime dTime = ServerTime.GetRealTime();
            if (dbWriter != null)
            {
                dbWriter.InsertOrderLog(dTime, OrderId, "CancelFailed", 0, "", assetid);
            }
            logWriter.WriteLine(dTime.ToString(DateTimeFormat) + " | Order cancel failed. OrderId: " + OrderId);
        }
    }
}
