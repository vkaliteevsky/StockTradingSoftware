using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Events
{
    public class UpdatePositionEvent : EventAbst
    {
        public string Symbol { get; set; }
        public double Amount { get; set; }
        public double Planned { get; set; } 
        public double AvgPrice { get; set; }

        public UpdatePositionEvent(string symbol, double amount, double planned, double avgPrice)
        {
            Symbol = symbol;
            Amount = amount;
            Planned = planned;
            AvgPrice = avgPrice;
        }
        public override string ToString()
        {
            return ("UpdatePositionEvent: Volume = " + Amount + ", Planned = " + Planned + ", AvgPrice = " + AvgPrice);
        }
        public override void Log(LogWriter logWriter, DBInputOutput.DBWriter dbWriter = null, int assetid = -1)
        {
            DateTime dTime = ServerTime.GetRealTime();
            if (dbWriter != null)
            {
                dbWriter.InsertPosition(dTime, Symbol, assetid, Amount, Planned, AvgPrice);
            }
            logWriter.WriteLine(dTime.ToString(DateTimeFormat) + 
                " | Update position. Symbol: {0}; Amount: {1}; Planned: {2}", Symbol, Amount, Planned);
        }
    }
}
