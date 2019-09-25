using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Events
{
    public class OrderMoveSucceededEvent : EventAbst
    {
        public string OrderId { get; set; }

        public OrderMoveSucceededEvent(string orderId)
        {
            OrderId = orderId;
        }
        public override string ToString()
        {
            return ("OrderMoveSucceededEvent: OrderId = " + OrderId);
        }
        public override void Log(LogWriter logWriter, DBInputOutput.DBWriter dbWriter = null, int assetid = -1)
        {
            DateTime dTime = ServerTime.GetRealTime();
            if (dbWriter != null)
            {
                dbWriter.InsertOrderLog(dTime, OrderId, "MoveSucceded", 0, "", assetid);
            }
            logWriter.WriteLine(dTime.ToString(DateTimeFormat) + " | Order move succeeded. OrderId: " + OrderId);
        }
    }
}
