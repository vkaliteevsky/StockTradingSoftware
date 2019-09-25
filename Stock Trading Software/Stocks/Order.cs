using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks
{
    public class Order
    {
        public string Symbol { get; set; }
        public int Cookie { get; set; }
        public string OrderId { get; set; }
        /// <summary>
        /// Исходный объем ЦБ в приказе
        /// </summary>
        public int Volume { get; set; }
        /// <summary>
        /// Объем ЦБ, оставшийся в приказе
        /// </summary>
        public int FilledVolume {get; set; }
        public double Price { get; set; }
        public double StopPrice { get; set; }
        public ActionEnum Action { get; set; }
        public OrderTypeEnum Type { get; set; }
        public Order(string symbol, int cookie, string orderId, int volume, int filledVolume, double price, double stopPrice, ActionEnum action, OrderTypeEnum type)
        {
            Symbol = symbol;
            Cookie = cookie;
            OrderId = orderId;
            Volume = volume;
            FilledVolume = filledVolume;
            Price = price;
            StopPrice = stopPrice;
            Action = action;
            Type = type;
        }
        public Order(string symbol, int cookie, string orderId, double volume, double filledVolume, double price, double stopPrice, StOrder_Action action, StOrder_Type type)
        {
            Symbol = symbol;
            Cookie = cookie;
            OrderId = orderId;
            Volume = (int)volume;
            FilledVolume = (int)filledVolume;
            Price = price;
            StopPrice = stopPrice;
            Action = Server.ActionCast(action);
            Type = Server.OrderTypeCast(type);
        }
        public override string ToString()
        {
            //string action = Action == ActionEnum.BUY ? "Buy order" : "Sell order";
            return ("Price = " + Price + ": Volume = " + Volume + ": OrderID = " + OrderId + ": Cookie = " + 
                Cookie + ": FilledVolume = " + FilledVolume + ": Action = " + Action + ": Type = " + Type);
        }
        public Order Clone()
        {
            return new Order(Symbol, Cookie, OrderId, Volume, FilledVolume, Price, StopPrice, Action, Type);
        }
    }
}
