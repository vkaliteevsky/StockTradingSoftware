using SmartCOM3Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks.Events
{
    public class UpdateOrderEvent : EventAbst
    {
        public string Symbol { get; set; }
        public StOrder_State State { get; set; }
        public StOrder_Action Action { get; set; }
        public StOrder_Type Type { get; set; }
        public double Price { get; set; }
        /// <summary>
        /// Исходный объем ЦБ в приказе
        /// </summary>
        public double Amount { get; set; }
        public double Stop { get; set; }
        /// <summary>
        /// Объем ЦБ, оставшийся в приказе
        /// </summary>
        public double Filled { get; set; }
        public DateTime Datetime { get; set; }
        public string OrderId { get; set; }
        public int Cookie { get; set; }

        public UpdateOrderEvent(string symbol
            , StOrder_State state
            , StOrder_Action action
            , StOrder_Type type
            , double price
            , double amount
            , double stop
            , double filled
            , DateTime datetime
            , string orderId
            , int cookie)
        {
            Symbol = symbol;
            State = state;
            Action = action;
            Type = type;
            Price = price;
            Amount = amount;
            Stop = stop;
            Filled = filled;
            Datetime = datetime;
            OrderId = orderId;
            Cookie = cookie;
        }
        public override string ToString()
        {
            return ("UpdateOrderEvent: Cookie = " + Cookie + ", OrderId = " + OrderId + ", Action = " + Action + ", Price = " + Price + ", Volume = " + Amount);
        }
        public override void Log(LogWriter logWriter, DBInputOutput.DBWriter dbWriter = null, int assetid = -1)
        {
            DateTime dTime = ServerTime.GetRealTime();
            if (dbWriter != null)
            {
                dbWriter.InsertOrderLog(dTime, OrderId, "UpdateOrder", Cookie, "", assetid, (int)State, (int)Action, (int)Type, Price, Amount, Stop, Filled);
            }
            logWriter.WriteLine(dTime.ToString(DateTimeFormat) +
                " | Update order. Symbol: {0}; State: {1}; Action: {2}; Type: {3}; Price: {4}; Amount: {5}; Stop: {6}; Filled: {7}; " +
                "Datetime: {8}; OrderId: {9}; Cookie: {10}", Symbol, State, Action, Type, Price, Amount, Stop, Filled, Datetime, OrderId, Cookie);
        }
    }
}

