using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stocks.Events;
using SmartCOM3Lib;

namespace Stocks
{
    public class StrategyState
    {
        private LogWriter LogWriter;
        public List<Order> BuyOrders { get; set; }
        public List<Order> SellOrders { get; set; }
        public int Position { get; set; }
        public int AmountOfOrders
        {
            get { return (BuyOrders.Count + SellOrders.Count); }
        }
        public int Planned {
            get
            {
                return (Position + BuyOrders.Select(order => order.Volume).Sum() - SellOrders.Select(order => order.Volume).Sum());
            }
        }
        public double AvgPrice { get; set; }
        private const int EmptyTimerInterval = 10 * 1000;
        private System.Timers.Timer EmptyTimer;
        public delegate void EmptyEvent();
        public event EmptyEvent Empty;
        public StrategyState(List<Order> buyOrders, List<Order> sellOrders, int position, double avgPrice)
        {
            BuyOrders = buyOrders;
            SellOrders = sellOrders;
            Position = position;
            AvgPrice = avgPrice;
            LogWriter = new LogWriter();
        }
        public StrategyState()
        {
            BuyOrders = new List<Order>();
            SellOrders = new List<Order>();
        }
        private void InitializeEmptyTimer()
        {
            EmptyTimer = new System.Timers.Timer(EmptyTimerInterval);
            EmptyTimer.AutoReset = false;
            EmptyTimer.Elapsed += HandleEmptyTimer;
            EmptyTimer.Start();
        }
        private void HandleEmptyTimer(object sender, EventArgs e)
        {
            Empty();
        }
        public List<Order> CloneBuyOrders()
        {
            return BuyOrders.Select(order => order.Clone()).ToList();
        }
        public List<Order> CloneSellOrders()
        {
            return SellOrders.Select(order => order.Clone()).ToList();
        }
        public void UpdateStateByOrderEvent(UpdateOrderEvent ev)
        {
            //if (ev.Filled <= 0 || ev.Amount <= 0) return;
            int index = FindBuyOrderIndex(ev.OrderId, ev.Cookie);
            if (index == -1) index = FindSellOrderIndex(ev.OrderId, ev.Cookie);
            if (index == -1)        // заявка отсутствует; нужно добавить новую
            {
                Order order = new Order(ev.Symbol, ev.Cookie, ev.OrderId, ev.Amount, ev.Filled, ev.Price, ev.Stop, ev.Action, ev.Type);
                AddOrder(order);
            }
            else    // заявка найдена, нужно обновить информацию
            {
                UpdateOrder(ev);
            }
        }


        private int FindBuyOrderIndex(string orderId, int cookie)
        {
            int index = -1;
            if (!orderId.Equals(""))        // поиск только по orderId
            {
                index = BuyOrders.FindIndex(x => x.OrderId.Equals(orderId));
                return index;
            }
            else
            {
                index = BuyOrders.FindIndex(x => x.OrderId.Equals(orderId));
                if (index == -1 || orderId.Equals("")) index = -1;
                if (index != -1) return index;
                index = BuyOrders.FindIndex(x => x.Cookie == cookie);
            }
            return index;
        }
        private int FindSellOrderIndex(string orderId, int cookie)
        {
            int index = -1;
            if (!orderId.Equals(""))        // поиск только по orderId
            {
                index = SellOrders.FindIndex(x => x.OrderId.Equals(orderId));
                return index;
            }
            else
            {
                index = SellOrders.FindIndex(x => x.OrderId.Equals(orderId));
                if (index == -1 || orderId.Equals("")) index = -1;
                if (index != -1) return index;
                index = SellOrders.FindIndex(x => x.Cookie == cookie);
            }
            return index;
        }
        /// <summary>
        /// Находит и обновляет заявку. Известно, что заявка существует
        /// </summary>
        /// <param name="ev"></param>
        private void UpdateOrder(UpdateOrderEvent ev)
        {
            int index = FindBuyOrderIndex(ev.OrderId, ev.Cookie);
            if (index != -1)
            {
                if (ev.Filled <= 0 || ev.Amount <= 0)
                {
                    BuyOrders.RemoveAt(index);
                } else
                {
                    BuyOrders[index].Cookie = ev.Cookie;
                    BuyOrders[index].FilledVolume = (int)ev.Filled;
                    BuyOrders[index].OrderId = ev.OrderId;
                    BuyOrders[index].Price = ev.Price;
                    BuyOrders[index].StopPrice = ev.Stop;
                    BuyOrders[index].Type = Server.OrderTypeCast(ev.Type);
                    BuyOrders[index].Volume = (int)ev.Amount;
                    BuyOrders[index].Action = Server.ActionCast(ev.Action);
                }
            } else
            {
                index = FindSellOrderIndex(ev.OrderId, ev.Cookie);
                if (index != -1)
                {
                    if (ev.Filled <= 0 || ev.Amount <= 0)
                    {
                        SellOrders.RemoveAt(index);
                    }
                    else
                    {
                        SellOrders[index].Cookie = ev.Cookie;
                        SellOrders[index].FilledVolume = (int)ev.Filled;
                        SellOrders[index].OrderId = ev.OrderId;
                        SellOrders[index].Price = ev.Price;
                        SellOrders[index].StopPrice = ev.Stop;
                        SellOrders[index].Type = Server.OrderTypeCast(ev.Type);
                        SellOrders[index].Volume = (int)ev.Amount;
                        SellOrders[index].Action = Server.ActionCast(ev.Action);
                    }
                }
            }
        }
        private Order GetBuyOrderById(string orderId)
        {
            return BuyOrders.Find(x => x.OrderId.Equals(orderId));
        }
        public void Clear()
        {
            BuyOrders.Clear();
            SellOrders.Clear();
            Position = 0;
            AvgPrice = 0;
        }
        public override string ToString()
        {
            string message = "Position = " + Position + ", Planned = " + Planned + ", Avg Price = " + AvgPrice + "\r\n";
            //message += "Buy Orders: \r\n";
            foreach (Order order in BuyOrders)
            {
                message += order.ToString() + "\r\n";
            }
            //message += "Sell Orders: \r\n";
            foreach (Order order in SellOrders)
            {
                message += order.ToString() + "\r\n";
            }
            return message;
        }
        private Order GetBuyOrderByCookie(int cookie)
        {
            return BuyOrders.Find(x => x.Cookie == cookie);
        }

        public Order FindBuyOrder(string orderId, int cookie)
        {
            Order order = null;
            if (!string.IsNullOrEmpty(orderId)) order = GetBuyOrderById(orderId);
            if (order == null) order = GetBuyOrderByCookie(cookie);
            return (order);
        }
        private void WriteToLogDB(string funcName, string comment, string symbol)
        {
            LogWriter.WriteLine(symbol, ": StrategyState." + funcName + ": " + comment);
        }
        public void UpdateWithExcel(List<Order> excelOrders, PositionInfo positionInfo, string symbol)
        {
            WriteToLogDB("UpdateWithExcel", "Started", symbol);
            // Updating CurrentState's Position
            if (!AvgPrice.Equals(positionInfo.AvgPrice))
            {
                WriteToLogDB("UpdateWithExcel", "Changing AvgPrice from " + AvgPrice + " to " + positionInfo.AvgPrice, symbol);
                AvgPrice = positionInfo.AvgPrice;
            }
            int sideCoeff = (positionInfo.Side == "Long" ? 1 : -1);
            /*if (Planned != (int)positionInfo.Planned * sideCoeff)
            {
                WriteToLogDB("UpdateWithExcel", "Changing Planned from " + Planned + " to " + (int)positionInfo.Planned, symbol);
                //Planned = ((int)positionInfo.Planned) * sideCoeff;
            } */
            int newPosition = ((int)positionInfo.Quantity) * sideCoeff;
            if (Position != newPosition)
            {
                WriteToLogDB("UpdateWithExcel", "Changing Position from " + Position + " to " + newPosition, symbol);
                Position = newPosition;
            }

            // Updating buy and sell orders

            List<int> indexesToDeleteFromCurrentState = new List<int>();
            List<string> orderIds = BuyOrders.Select(order => order.OrderId).ToList();
            List<int> cookies = BuyOrders.Select(order => order.Cookie).ToList();
            orderIds.AddRange(SellOrders.Select(order => order.OrderId).ToList());
            cookies.AddRange(SellOrders.Select(order => order.Cookie).ToList());

            BuyOrders.Clear();
            BuyOrders = excelOrders.Where(order => order.Action == ActionEnum.BUY).ToList();
            SellOrders.Clear();
            SellOrders = excelOrders.Where(order => order.Action == ActionEnum.SELL).ToList();
            WriteToLogDB("UpdateWithExcel", "Buy and Sell orders from Current State Cleared. Updating from Excel", symbol);
            for (int i = 0; i < BuyOrders.Count; i++)
            {
                int index = orderIds.IndexOf(BuyOrders[i].OrderId);
                if (index >= 0)
                {
                    BuyOrders[i].Cookie = cookies[index];
                }
                else
                {
                    WriteToLogDB("UpdateWithExcel", BuyOrders[i].OrderId + " not found. Order added to CurrentState from Excel", symbol);
                }
            }
            for (int i = 0; i < SellOrders.Count; i++)
            {
                int index = orderIds.IndexOf(SellOrders[i].OrderId);
                if (index >= 0)
                {
                    SellOrders[i].Cookie = cookies[index];
                }
                else
                {
                    WriteToLogDB("UpdateWithExcel", SellOrders[i].OrderId + " not found. Order added to CurrentState from Excel", symbol);
                }
            }
            WriteToLogDB("UpdateWithExcel", "Finished", symbol);
        }
        public Order FindSellOrder(string orderId, int cookie)
        {
            Order order = null;
            if (!string.IsNullOrEmpty(orderId)) order = GetSellOrderById(orderId);
            if (order == null) order = GetSellOrderByCookie(cookie);
            return (order);
        }

        /// <summary>
        /// Находит заявку с заданным orderId или cookie
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="cookie"></param>
        /// <returns>Возвращает ссылку на заявку</returns>
        public Order FindOrder(string orderId, int cookie)
        {
            /* Order order = FindBuyOrder(orderId, cookie);
            if (order == null) order = FindSellOrder(orderId, cookie);
            return (order); */
            int index = FindBuyOrderIndex(orderId, cookie);
            if (index != -1) return BuyOrders[index];
            index = FindSellOrderIndex(orderId, cookie);
            if (index != -1) return SellOrders[index];
            else return null;
        }
        
        public void UpdateOrders(OpenOrdersInfo openOrders)
        {
            
        }
        /// <summary>
        /// Находит и обновляет информацию о заявке по событию ev. Не делает никаких проверок адекватности
        /// </summary>
        /// <param name="ev"></param>
        /* public void UpdateOrdersByUpdateOrderEvent(UpdateOrderEvent ev)
        {
            Order foundOrder = FindOrder(ev.OrderId, ev.Cookie);
            if (foundOrder != null)
            {
                foundOrder.Action = Server.ActionCast(ev.Action);
                foundOrder.Cookie = ev.Cookie;
                foundOrder.OrderId = ev.OrderId;
                foundOrder.Price = ev.Price;
                foundOrder.Type = Server.OrderTypeCast(ev.Type);
                foundOrder.Volume = (int)ev.Amount;
                foundOrder.StopPrice = ev.Stop;
                foundOrder.FilledVolume = (int)ev.Amount - (int)ev.Filled;
            }
        } */
        /// <summary>
        /// Проверяет на возможность добавления заявки в объект и возвращает true / false в случае успеха / неуспеха
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        /*public bool AddOrderByUpdateOrderEvent(UpdateOrderEvent ev)
        {
            Order foundOrderId = FindOrderById(ev.OrderId);
            Order foundOrderCookie = FindOrderByCookie(ev.Cookie);
            Order newOrder = new Order(ev.Symbol, ev.Cookie, ev.OrderId, (int)ev.Amount, (int)ev.Amount - (int)ev.Filled, ev.Price, ev.Stop, Server.ActionCast(ev.Action), Server.OrderTypeCast(ev.Type));
            bool isOrderIdFound = (foundOrderId != null);
            bool isCookieFound = (foundOrderCookie != null);
            if ((!isOrderIdFound && !isCookieFound) || (!isOrderIdFound && isCookieFound && ev.Cookie != 0)
                || (!isOrderIdFound) && isCookieFound && ev.Cookie == 0 && !AreOrdersSimilar(newOrder, foundOrderCookie)) 
            {
                AddOrder(newOrder);
                return (true);
            }
            return (false);
        }*/
        private bool AreOrdersSimilar(Order order1, Order order2)
        {
            return ((order1.Price.Equals(order2.Price)) && (order1.Volume == order2.Volume) && (order1.Action == order2.Action) && (order1.StopPrice.Equals(order2.StopPrice)));
        }
        private void AddOrder(Order newOrder)
        {
            if (newOrder.Action == ActionEnum.BUY)
                BuyOrders.Add(newOrder);
            else if (newOrder.Action == ActionEnum.SELL)
                SellOrders.Add(newOrder);
            else
                throw new SmartException(ExceptionImportanceLevel.HIGH, "AddOrder", "StrategyState", "No other options. New Order = " + newOrder.ToString());
            MakeConsistent();
        }
        private Order FindOrderById(string orderId)
        {
            if (orderId.Equals("")) return (null);
            Order order = GetBuyOrderById(orderId);
            if (order == null) order = GetSellOrderById(orderId);
            return (order);
        }
        private Order FindOrderByCookie(int cookie)
        {
            Order order = GetBuyOrderByCookie(cookie);
            if (order == null) order = GetSellOrderByCookie(cookie);
            return (order);
        }
        public void MakeConsistent()
        {
            BuyOrders = RemoveEmptyOrders(BuyOrders);
            SellOrders = RemoveEmptyOrders(SellOrders);
        }
        public bool HasPositionOrOrders()
        {
            return (BuyOrders.Count + SellOrders.Count + Math.Abs(Position) > 0);
        }
        private List<Order> RemoveEmptyOrders(List<Order> orderList)
        {
            int i = 0;
            while (i < orderList.Count)
            {
                Order order = orderList[i];
                if (order.Volume == 0)
                {
                    orderList.RemoveAt(i);
                    i = -1;
                }
                i++;
            }
            return (orderList);
        }
        public void RemoveOrder(string orderId, int cookie)
        {
            /* int buyIndex = BuyOrders.FindIndex(order => (order.OrderId == orderId));
            int sellIndex = SellOrders.FindIndex(order => (order.OrderId == orderId));
            if (buyIndex >= 0)
                BuyOrders.RemoveAt(buyIndex);
            else if (sellIndex >= 0)
                SellOrders.RemoveAt(sellIndex);
            else
            {
                buyIndex = BuyOrders.FindIndex(order => (order.Cookie == cookie));
                sellIndex = SellOrders.FindIndex(order => (order.Cookie == cookie));
                if (buyIndex >= 0)
                    BuyOrders.RemoveAt(buyIndex);
                else if (sellIndex >= 0)
                    SellOrders.RemoveAt(sellIndex);
            } */
            Order foundOrder = FindOrder(orderId, cookie);
            if (foundOrder != null)
            {
                if (foundOrder.Action == ActionEnum.BUY)
                {
                    BuyOrders.Remove(foundOrder);
                }
                else if (foundOrder.Action == ActionEnum.SELL)
                {
                    SellOrders.Remove(foundOrder);
                } else
                {
                    throw new SmartException(ExceptionImportanceLevel.LOW, "RemoveOrder", "StrategyState", "No other choices in foundOrder.Action");
                }
            }
        }

        public Order GetSellOrderById(string orderId)
        {
            return SellOrders.Find(x => x.OrderId.Equals(orderId));
        }

        public Order GetSellOrderByCookie(int cookie)
        {
            return SellOrders.Find(x => x.Cookie == cookie);
        }

        public int GetTotalBuyVolume()
        {
            if (BuyOrders.Count <= 0) return (0);
            return (BuyOrders.Select(order => order.Volume).Sum());
        }

        public int GetTotalSellVolume()
        {
            if (SellOrders.Count <= 0) return (0);
            return SellOrders.Select(order => order.Volume).Sum();
        }
    }
}
