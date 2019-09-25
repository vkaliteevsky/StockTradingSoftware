using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public struct OpenOrdersInfo
    {
        public List<string> BuyOrdersIds { get; set; }
        public List<double> BuyPrices { get; set; }
        public List<double> BuyStopPrices { get; set; }
        public List<double> BuyVolumes { get; set; }
        public List<double> BuyFilledVolumes { get; set; }

        public List<string> SellOrdersIds { get; set; }
        public List<double> SellPrices { get; set; }
        public List<double> SellStopPrices { get; set; }
        public List<double> SellVolumes { get; set; }
        public List<double> SellFilledVolumes { get; set; }

        public string Symbol { get; set; }

        public OpenOrdersInfo(List<string> buyOrdersIds, List<double> buyPrices, List<double> buyStopPrices, List<double> buyVolumes, List<double> buyFilledVolumes
            , List<string> sellOrdersIds, List<double> sellPrices, List<double> sellStopPrices, List<double> sellVolumes, List<double> sellFilledVolumes, string symbol)
        {
            BuyOrdersIds = buyOrdersIds;
            BuyPrices = buyPrices;
            BuyStopPrices = buyStopPrices;
            BuyVolumes = buyVolumes;
            BuyFilledVolumes = buyFilledVolumes;
            SellOrdersIds = sellOrdersIds;
            SellPrices = sellPrices;
            SellStopPrices = sellStopPrices;
            SellVolumes = sellVolumes;
            SellFilledVolumes = sellFilledVolumes;
            Symbol = symbol;
        }
        public List<Order> ToOrders()
        {
            List<Order> orders = new List<Order>();
            for (int i = 0; i < BuyOrdersIds.Count; i++)
                orders.Add(ToOrder(i, true));
            for (int i = 0; i < SellOrdersIds.Count; i++)
                orders.Add(ToOrder(i, false));
            return (orders);
        }
        /// <summary>
        /// Преобразовывает i-ые значения в объект класса Order
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private Order ToOrder(int i, bool isBuy)
        {
            Order order;
            if (isBuy)
            {
                if (i < 0 || i >= BuyOrdersIds.Count) return (null);
                double stopPrice = BuyStopPrices[i];
                OrderTypeEnum type = (Math.Abs(stopPrice) <= 0.0001 ? OrderTypeEnum.LIMIT : OrderTypeEnum.STOP);
                order = new Order(Symbol, 0, BuyOrdersIds[i], (int)BuyVolumes[i], (int)BuyFilledVolumes[i], BuyPrices[i], stopPrice, ActionEnum.BUY, type);
            }
            else
            {
                if (i < 0 || i >= SellOrdersIds.Count) return (null);
                double stopPrice = SellStopPrices[i];
                OrderTypeEnum type = (Math.Abs(stopPrice) <= 0.0001 ? OrderTypeEnum.LIMIT : OrderTypeEnum.STOP);
                order = new Order(Symbol, 0, SellOrdersIds[i], (int)SellVolumes[i], (int)SellFilledVolumes[i], SellPrices[i], stopPrice, ActionEnum.SELL, type);
            }
            return (order);
        }
    }
}
