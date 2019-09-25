using SmartCOM3Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stocks.Events;
using Stocks.Strategies;
using System.Threading;
using System.Timers;

namespace Stocks
{
    public sealed class Server
    {
        private static Server Instance;

        public MainForm Form { get; }

        private const string ServerIP = "213.59.8.133";//"213.247.232.236"; //"213.59.8.133";
        private const string ServerPort = "8443";
        private static readonly string Login = "77CGYM";//"LGUGJTU3"; //"13SMOH";
        private static readonly string Password = "7Y0QWD";//"GFKZNR"; //"TLQ23W";
        private readonly string Portfolio = "BP16181-RF-01";//"ST105396-RF-01"; //"BP16181-RF-01";
        private const int UpdatePositionInterval = 1 * 60 * 1000;

        private StServer SmartComServer;
        private const string SmartComParams = "logLevel=5;maxWorkerThreads=2";
        private DataCollector Collector;

        private List<StrategyAbst> Strategies;

        private LogWriter Logger;
        private DBInputOutput.DBWriter dbWriter;
        private const string LogPath = "ServerLogs";
        private const string LogName = "ServerLog";
        public const string LogDateTimeFormat = "G";

        private bool IsDisconnectedByUser = false;
        private System.Timers.Timer UpdatePositionTimer;

        private Server(string login, string password, MainForm form)
        {
            //Login = login;
            //Password = password;
            Form = form;
            Logger = new LogWriter(LogPath, LogName);
            dbWriter = new DBInputOutput.DBWriter();
            Strategies = new List<StrategyAbst>();
            Collector = new DataCollector();

            UpdatePositionTimer = new System.Timers.Timer(UpdatePositionInterval);
            UpdatePositionTimer.AutoReset = true;
            UpdatePositionTimer.Elapsed += HandleUpdatePosition;
            UpdatePositionTimer.Start();


        }

        private void HandleUpdatePosition(object sender, EventArgs e)
        {
            InformUser("Sever: Position Update Called");
            GetOrdersAndPositionFromServer();
        }
        private static Server GetInstance(string login, string password, MainForm form)
        {
            if (Instance == null)
            {
                Instance = new Server(login, password, form);
            }
            return Instance;
        }

        public static Server GetInstance(MainForm form)
        {
            if (Instance == null)
            {
                Instance = new Server(Server.Login, Server.Password, form);
            }
            return Instance;
        }
        public static Server GetInstance()
        {
            return Instance;
        }
        /// <summary>
        /// Проверяет, является ли текущий момент времени концом торгового дня
        /// </summary>
        /// <returns>Возвращает true, если торговый день завершен</returns>
        private bool IsEndOfWork()
        {
            DateTime dTime = ServerTime.GetRealTime();
            return (dTime.Hour < 10 || (dTime.Hour == 23 && dTime.Minute > 50));
        }
        /// <summary>
        /// Проверяет, конец ли торговой сессии. Если да, прерывает работу программы
        /// </summary>
        public void CheckEndOfDay()
        {
            if (IsEndOfWork())
            {
                WriteToLog("End of Day Found");
                foreach (StrategyAbst strat in GetStrategies())
                {
                    strat.StopWork();
                }
                DisconnectStockServer();
                SendReport();
                System.Windows.Forms.Application.Exit();
            }
        }
        /// <summary>
        /// Отправляет на почту отчет об успешности завершения торгового дня
        /// </summary>
        private void SendReport()
        {
            string title = "End of Trading Day - " + ServerTime.GetRealTime().ToShortDateString();
            string message = "Day successfully closed.\r\n";
            foreach (StrategyAbst strat in GetInstance().GetStrategies())
            {
                message += strat.ToString() + "***********************\r\n\r\n";
            }
            EmailSender.SendEmail(title, message);
        }
        private void WriteToLog(string message, params object[] args)
        {
            Logger.WriteLine(LogPath, LogName, ServerTime.GetRealTime().ToString(LogDateTimeFormat) + " | " + message, args);
        }

        private bool IsReady { get { return (SmartComServer != null); } }

        public bool IsConnected
        {
            get
            {
                bool isConnectedResponse = false;
                if (IsReady)
                {
                    try
                    {
                        isConnectedResponse = SmartComServer.IsConnected();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                return isConnectedResponse;
            }
        }

        public List<StrategyAbst> GetStrategies()
        {
            return Strategies;
        }

        public void Subscribe(StrategyAbst strat)
        {
            Strategies.Add(strat);
            GiveCookies(strat);
            Form.WriteToStatus(strat.Name + " subscribed");
        }
        
        public void InformUser(string message)
        {
            Form.WriteToStatus(message);
        }

        public void Unsubscribe(StrategyAbst strat)
        {
            Strategies.Remove(strat);
        }

        private void NotifyStrategies(EventAbst ev)
        {
            foreach (StrategyAbst strat in Strategies.ToList())
            {
                strat.HandleEvent(ev);
            }
        }

        public static ActionEnum ActionCast(StOrder_Action action)
        {
            if (action == StOrder_Action.StOrder_Action_Buy) return (ActionEnum.BUY);
            else if (action == StOrder_Action.StOrder_Action_Sell) return (ActionEnum.SELL);
            else
            {
                string message = "ActionCast: Cannot convert StOrder_Action = " + action;
                throw new SmartException(ExceptionImportanceLevel.MEDIUM, "ActionCast", "Server", message);
            }
        }

        public static StOrder_Action ActionCast(ActionEnum action)
        {
            if (action == ActionEnum.BUY) return (StOrder_Action.StOrder_Action_Buy);
            else if (action == ActionEnum.SELL) return (StOrder_Action.StOrder_Action_Sell);
            else throw new SmartException(ExceptionImportanceLevel.MEDIUM, "ActionCast", "Server", "Cannot convert ActionEnum = " + action);
        }

        public static StOrder_Type OrderTypeCast (OrderTypeEnum type)
        {
            if (type == OrderTypeEnum.LIMIT) return (StOrder_Type.StOrder_Type_Limit);
            else if (type == OrderTypeEnum.MARKET) return (StOrder_Type.StOrder_Type_Market);
            else if (type == OrderTypeEnum.STOP) return (StOrder_Type.StOrder_Type_StopLimit);
            else throw new SmartException(ExceptionImportanceLevel.MEDIUM, "OrderTypeCast", "Server", "Cannot convert OrderTypeEnum = " + type);
        }

        public static OrderTypeEnum OrderTypeCast(StOrder_Type type)
        {
            if (type == StOrder_Type.StOrder_Type_Limit) return (OrderTypeEnum.LIMIT);
            else if (type == StOrder_Type.StOrder_Type_Market) return (OrderTypeEnum.MARKET);
            else if (type == StOrder_Type.StOrder_Type_StopLimit) return (OrderTypeEnum.STOP);
            else throw new SmartException(ExceptionImportanceLevel.MEDIUM, "OrderTypeCast", "Server", "Cannot convert StOrder_Type = " + type);
        }

        public static int BarLenCast(StBarInterval interval)
        {
            switch (interval)
            {
                case StBarInterval.StBarInterval_1Min:
                    return 1;
                case StBarInterval.StBarInterval_5Min:
                    return 5;
                case StBarInterval.StBarInterval_10Min:
                    return 10;
                case StBarInterval.StBarInterval_15Min:
                    return 15;
                case StBarInterval.StBarInterval_30Min:
                    return 30;
                case StBarInterval.StBarInterval_60Min:
                    return 60;
                default:
                    throw new SmartException(ExceptionImportanceLevel.LOW, "BarLenCast", "Server", "StBarInterval not defined!");
            }
        }

        public static StBarInterval BarLenCast(int minutes)
        {
            switch (minutes)
            {
                case 1:
                    return StBarInterval.StBarInterval_1Min;
                case 5:
                    return StBarInterval.StBarInterval_5Min;
                case 10:
                    return StBarInterval.StBarInterval_10Min;
                case 15:
                    return StBarInterval.StBarInterval_15Min;
                case 30:
                    return StBarInterval.StBarInterval_30Min;
                case 60:
                    return StBarInterval.StBarInterval_60Min;
                default:
                    throw new SmartException(ExceptionImportanceLevel.LOW, "BarLenCast", "Server", "minutes = " + minutes + ": not found");
            }
        }
        private void InitializeServer()
        {
            SmartComServer = new StServer();

            SmartComServer.Connected += new _IStClient_ConnectedEventHandler(SmartComServerConnected);
            SmartComServer.Disconnected += new _IStClient_DisconnectedEventHandler(SmartComServerDisconnected);
            SmartComServer.UpdatePosition += new _IStClient_UpdatePositionEventHandler(SmartComServerUpdatePosition);
            SmartComServer.UpdateOrder += new _IStClient_UpdateOrderEventHandler(SmartComServerUpdateOrder);
            SmartComServer.SetMyOrder += new _IStClient_SetMyOrderEventHandler(SmartComServerSetMyOrder);
            SmartComServer.OrderSucceeded += new _IStClient_OrderSucceededEventHandler(SmartComServerOrderSucceeded);
            SmartComServer.OrderFailed += new _IStClient_OrderFailedEventHandler(SmartComServerOrderFailed);
            SmartComServer.OrderCancelFailed += new _IStClient_OrderCancelFailedEventHandler(SmartComServerOrderCancelFailed);
            SmartComServer.OrderCancelSucceeded += new _IStClient_OrderCancelSucceededEventHandler(SmartComServerOrderCancelSucceeded);
            SmartComServer.OrderMoveSucceeded += new _IStClient_OrderMoveSucceededEventHandler(SmartComServerOrderMoveSucceded);
            SmartComServer.OrderMoveFailed += new _IStClient_OrderMoveFailedEventHandler(SmartComServerOrderMoveFailed);
            SmartComServer.UpdateQuote += new _IStClient_UpdateQuoteEventHandler(SmartComServerUpdateQuote);
            SmartComServer.AddBar += new _IStClient_AddBarEventHandler(SmartComServer_AddBar);
            SmartComServer.ConfigureClient(SmartComParams);
        }
        
        /* public static OrderTypeEnum CastEnumType(StOrder_Type type)
        {
            if (type == StOrder_Type.StOrder_Type_Limit) return OrderTypeEnum.LIMIT;
            else if (type == StOrder_Type.StOrder_Type_Stop) return OrderTypeEnum.STOP;
            else throw new SmartException(ExceptionImportanceLevel.MEDIUM, "OrderTypeEnum", "Server", "No other choice type: " + type);
        }
        public static ActionEnum CastEnumAction(StOrder_Action action)
        {
            if (action == StOrder_Action.StOrder_Action_Buy) return ActionEnum.BUY;
            else if (action == StOrder_Action.StOrder_Action_Sell) return ActionEnum.SELL;
            else throw new SmartException(ExceptionImportanceLevel.MEDIUM, "CastEnumAction", "Server", "No other choice: " + action);
        } */
        private void ConnectStockServer()
        {
            if (!IsConnected)
            {
                try
                {
                    WriteToLog("Trying to connect");
                    Form.WriteToStatus("Trying to connect");
                    SmartComServer.connect(ServerIP, ushort.Parse(ServerPort), Login, Password);
                }
                catch (Exception e)
                {
                    EmailSender.SendEmail(e);
                    WriteToLog(e.Message);
                    throw e;
                }
            }
        }
        public bool IsNowTradeTime()
        {
            DateTime dTime = ServerTime.GetRealTime();
            return IsNowDayTradeTime() || IsNowEveningTradeTime();
        }
        public bool IsNowDayTradeTime()
        {
            DateTime dTime = ServerTime.GetRealTime();
            return (dTime.Hour >= 10 && dTime.Hour < 18) || (dTime.Hour == 18 && dTime.Minute < 45);
        }
        public bool IsNowEveningTradeTime()
        {
            DateTime dTime = ServerTime.GetRealTime();
            return (dTime.Hour >= 19 && dTime.Hour < 23) || (dTime.Hour == 23 && dTime.Minute < 50);
        }
        public void GetBars(string symbol, StBarInterval interval, System.DateTime since, int count)
        {
            Collector.InitDataRequest(symbol, interval, since, count);
            try
            {
                SmartComServer.GetBars(symbol, interval, since, count);
            } catch (Exception e)
            {
                DisconnectStockServer();
                ConnectStockServer();
                System.Threading.Thread.Sleep(7000);
                SmartComServer.GetBars(symbol, interval, since, count);
                EmailSender.SendEmail("Server Disconnected", "In GetBars: Server was disconnected. Tryied to reconect.\r\n" + e.Message);
            }
            
            //System.Threading.Thread.Sleep(500);
        }
        void SmartComServer_AddBar(int row, int nrows, string symbol, StBarInterval interval, System.DateTime datetime,
            double open, double high, double low, double close, double volume, double open_int)
        {
            //string message = row + ":" + nrows + ":" + symbol+":"+interval+":"+datetime+":"+open+":"+high+":"+low+":"+close+":"+volume+":"+open_int;
            //Form.WriteToStatus(message);
            Collector.AddBar(row, nrows, symbol, interval, datetime, open, high, low, close, volume, open_int);
            //WriteToLog(message);
        }
        public void NotifyBarsCollected(List<Bar> bars, string symbol)
        {
            foreach (StrategyAbst strat in Strategies)
            {
                if (strat.Symbol == symbol)
                {
                    strat.HandleGetBars(bars);
                }
            }
        }
        public void StartWork()
        {
            InitializeServer();
            ConnectStockServer();
            var connectionTimer = new System.Timers.Timer(1000);
            connectionTimer.Elapsed += (src, args) =>
            {
                if (IsConnected)
                {
                    connectionTimer.Stop();
                }
            };
            connectionTimer.AutoReset = true;
            connectionTimer.Start();
        }

        public void DisconnectStockServer()
        {
            if (IsConnected)
            {
                InformUser("Disconnecting ...");
                IsDisconnectedByUser = true;
                SmartComServer.CancelPortfolio(Portfolio);
                SmartComServer.disconnect();
            }
        }

        public void CancelQuotes(string symbol)
        {
            if (IsConnected)
            {
                SmartComServer.CancelQuotes(symbol);
            }
        }

        private void SmartComServerConnected()
        {
            WriteToLog("Server connected");
            Form.WriteToStatus("Server connected");
            IsDisconnectedByUser = false;
            SmartComServer.ListenPortfolio(Portfolio);
            foreach (StrategyAbst strat in Strategies.ToList())
            {
                ListenSymbol(strat.Symbol);
            }
        }

        public void ListenSymbol(string symbol)
        {
            SmartComServer.ListenQuotes(symbol);
        }

        private void SmartComServerDisconnected(string reason)
        {
            WriteToLog("Disconnected from server. Reason: " + reason);
            InformUser("Disconnected from server. Reason: " + reason);
            if (!IsDisconnectedByUser)
            {
                ConnectStockServer();
            }
        }

        private void SmartComServerUpdatePosition(string portfolio, string symbol, double avprice, double amount, double planned)
        {
            if (portfolio.Equals(Portfolio))
            {
                UpdatePositionEvent ev = new UpdatePositionEvent(symbol, amount, planned, avprice);
                NotifyStrategies(ev);
                ev.Log(Logger);
            }
        }

        private void SmartComServerUpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit, int trading_status, double volat, double theor_price)
        {
            StrategyAbst foundStrat = Strategies.Find(strat => strat.Symbol == symbol);
            if (foundStrat != null)
            {
                foundStrat.GO = go_buy;
                foundStrat.LastPrice = close;
            }
        }

        private void SmartComServerUpdateOrder(string portfolio, string symbol, SmartCOM3Lib.StOrder_State state, SmartCOM3Lib.StOrder_Action action, SmartCOM3Lib.StOrder_Type type, SmartCOM3Lib.StOrder_Validity validity, double price, double amount, double stop, double filled, System.DateTime datetime, string orderid, string orderno, int status_mask, int cookie)
        {
            if (portfolio.Equals(Portfolio))
            {
                UpdateOrderEvent ev = new UpdateOrderEvent(symbol, state, action, type, price, amount, stop, filled, datetime, orderid, cookie);
                NotifyStrategies(ev);
                ev.Log(Logger);
            }
        }

        private void SmartComServerSetMyOrder(int row, int nrows, string portfolio, string symbol, SmartCOM3Lib.StOrder_State state, SmartCOM3Lib.StOrder_Action action, SmartCOM3Lib.StOrder_Type type, SmartCOM3Lib.StOrder_Validity validity, double price, double amount, double stop, double filled, System.DateTime datetime, string orderid, string no, int cookie)
        {
            SetMyOrderEvent ev = new SetMyOrderEvent(symbol, state, action, type, price, amount, stop, filled, datetime, orderid, cookie);
            NotifyStrategies(ev);
            ev.Log(Logger);
        }

        private void SmartComServerOrderSucceeded(int cookie, string orderid)
        {
            OrderSucceededEvent ev = new OrderSucceededEvent(cookie, orderid);
            NotifyStrategies(ev);
            ev.Log(Logger);
        }

        private void SmartComServerOrderFailed(int cookie, string orderid, string reason)
        {
            OrderFailedEvent ev = new OrderFailedEvent(cookie, orderid, reason);
            NotifyStrategies(ev);
            ev.Log(Logger);
        }

        private void SmartComServerOrderCancelFailed(string orderid)
        {
            OrderCancelFailedEvent ev = new OrderCancelFailedEvent(orderid);
            NotifyStrategies(ev);
            ev.Log(Logger);
        }

        private void SmartComServerOrderCancelSucceeded(string orderid)
        {
            OrderCancelSucceededEvent ev = new OrderCancelSucceededEvent(orderid);
            NotifyStrategies(ev);
            ev.Log(Logger);
        }

        private void SmartComServerOrderMoveSucceded(string orderid)
        {
            OrderMoveSucceededEvent ev = new OrderMoveSucceededEvent(orderid);
            NotifyStrategies(ev);
            ev.Log(Logger);
        }

        private void SmartComServerOrderMoveFailed(string orderid)
        {
            OrderMoveFailedEvent ev = new OrderMoveFailedEvent(orderid);
            NotifyStrategies(ev);
            ev.Log(Logger);
        }

        private void GiveCookies(StrategyAbst strat)
        {
            strat.CookieDiv = 100;
            strat.CookieMod = Strategies.IndexOf(strat) + 1;
            strat.LastCookie = strat.CookieMod;
        }

        public void PlaceOrder(string symbol, ActionEnum action, OrderTypeEnum type, double price, double volume, double stopPrice, int cookie)
        {
            if (IsConnected)
            {
                WriteToLog("Place order. Symbol: {0}; Action: {1}; Price: {2}; Volume: {3}",
                    symbol, action, price, volume);
                SmartComServer.PlaceOrder(Portfolio, symbol, action == ActionEnum.BUY ? StOrder_Action.StOrder_Action_Buy : StOrder_Action.StOrder_Action_Sell
                    , type == OrderTypeEnum.LIMIT ? StOrder_Type.StOrder_Type_Limit : StOrder_Type.StOrder_Type_StopLimit, StOrder_Validity.StOrder_Validity_Day
                    , price, volume, stopPrice, cookie);
            }
        }

        public void PlaceOrder(Order order)
        {
            if (IsConnected)
            {
                PlaceOrder(order.Symbol, order.Action, order.Type, order.Price, order.Volume, order.StopPrice, order.Cookie);
            }
        }

        public void CancelOrder(string symbol, string orderId)
        {
            if (IsConnected)
            {
                WriteToLog("Cancel order. Symbol: {0}; OrderId: {1}", symbol, orderId);
                SmartComServer.CancelOrder(Portfolio, symbol, orderId);
            }
        }

        public void MoveOrder(string orderId, double price)
        {
            if (IsConnected)
            {
                WriteToLog("Move order. OrderId: " + orderId);
                SmartComServer.MoveOrder(Portfolio, orderId, price);
            }
        }

        public void GetOrdersAndPositionFromServer()
        {
            WriteToLog("GetOrdersAndPositionFromServer Started");
            foreach (StrategyAbst strat in Server.GetInstance().GetStrategies())
            {
                strat.ClearCurrentPosition();
            }
            try
            {
                SmartComServer.CancelPortfolio(Portfolio);
                SmartComServer.UpdatePosition += new _IStClient_UpdatePositionEventHandler(SmartComServerUpdatePosition);
                SmartComServer.UpdateOrder += new _IStClient_UpdateOrderEventHandler(SmartComServerUpdateOrder);
                SmartComServer.ListenPortfolio(Portfolio);
            } catch (Exception e)
            {
                DisconnectStockServer();
                ConnectStockServer();
                //SmartComServer.disconnect();
                //throw new SmartException(e);
                EmailSender.SendEmail("Stock Server Disconnected", e.Message);
            }
            foreach (StrategyAbst strat in Server.GetInstance().GetStrategies())
            {
                strat.InitializeLastUpdateTimer();
                //strat.HandleFinishConsistencyTimer(new object(), null);
            }
        }
    }
}
