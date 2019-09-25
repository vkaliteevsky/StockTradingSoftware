using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Consitency
{
    public enum ConsistencyError
    {
        CONSISTENT = 0,
        BIG_AMT_OF_ORDERS = 1,
        BIG_AMT_OF_BUY_ORDERS = 2,
        BIG_AMT_OF_SELL_ORDERS = 3,
        BIG_OPENED_POS = 4,
        BIG_PLANNED = 5,
        BIG_BUY_TOTAL_VOLUME = 6,
        BIG_SELL_TOTAL_VOLUME = 7,
        PLANNED_NOT_ZERO = 8,
        HUGE_BUY_VOLUME = 9,
        HUGE_SELL_VOLUME = 10
    }
    public enum ConsistencyStatus
    {
        NOT_VERIFIED = 1,
        READY_FOR_HANDLING = 2
    }
    public class ConsistencyEvent
    {
        public ConsistencyError ErrorCode { get; set; }
        public ConsistencyStatus Status { get; set; }
        public string Symbol { get; set; }
        public ConsistencyEvent(ConsistencyError errorCode, string symbol, bool toSendEmail = true, StrategyState state = null)
        {
            ErrorCode = errorCode;
            Symbol = symbol;
            Status = ConsistencyStatus.NOT_VERIFIED;
            if (errorCode != ConsistencyError.CONSISTENT && toSendEmail)
            {
                if (state != null)
                {
                    EmailSender.SendEmail("Consistency Error", ToString() + "\r\n" + state.ToString());
                } else
                {
                    EmailSender.SendEmail("Consistency Error", ToString());
                }
            }
        }
        public ConsistencyEvent(ConsistencyError errorCode, ConsistencyStatus status)
        {
            ErrorCode = errorCode;
            Status = status;
        }
        public override string ToString()
        {
            return ("Symbol: " + Symbol + ", " + "Error: " + ErrorCode);
        }
    }
}
