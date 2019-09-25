using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stocks;
using SmartCOM3Lib;
using System.Timers;

namespace Listener
{
    class MyTimer : System.Timers.Timer
    {
        public MyTimer(string symbol, int ms) : base(ms)
        {
            Symbol = symbol;
        }
        public string Symbol { get; set; }
    }
    class ListenerException : SmartException
    {
        public ListenerException(Exception e) : base(e)
        {
            Environment.Exit(0);
        }
        public ListenerException(ExceptionImportanceLevel level, string methodName, string className, string message, int code)
            : base(level, methodName, className, message, code)
        {
            Environment.Exit(0);
        }
        public ListenerException(ExceptionImportanceLevel level, string methodName, string className, string message)
            : base(level, methodName, className, message)
        {
            Environment.Exit(0);
        }
    }
    class Listener
    {
        private const string ServerIP = "213.59.8.133";//"213.247.232.236"; //"213.59.8.133";
        //private const string ServerIP = "89.249.21.170";
        private const string ServerPort = "8443";
        private static readonly string Login = "37PDCL";//"NSD2R32P";//"LGUGJTU3"; //"13SMOH";
        private static readonly string Password = "YGZ23dQW4x94";//"HkxTPSArre";//"GFKZNR"; //"TLQ23W";
        private readonly string Portfolio = "BP16181-RF-01";//"ST105396-RF-01"; //"BP16181-RF-01";

        private StServer SmartComServer;
        private const string SmartComParams = "logLevel=5;maxWorkerThreads=2";
        private bool IsDisconnectedByUser = false;
        public string[] Symbols { get; }
        public int[] TypeMins { get; }
        public int[] Amounts { get; }
        public int[] TimerIntervals { get; }
        private MyTimer[] Timers;
        /// <summary>
        /// Отмеряет периоды времени, между которыми осуществляет запись в БД general
        /// </summary>
        private Timer InformTimer;
        private const int InformTimerInterval = 2 * 60 * 1000;
        private bool WasConnected;

        private List<DateTime> DayOffs;

        private DataCollector Collector;
        private DBInputOutput.DBReader dbReader;
        private DBInputOutput.DBWriter dbWriter;

        public Listener()
        {
            Collector = new DataCollector();
            dbWriter = new DBInputOutput.DBWriter();
            dbReader = new DBInputOutput.DBReader();
            dbWriter.InsertSts(ServerTime.GetRealTime(), "Listener", "Work Started");
            Collector.BarsCollected += BarsCollectedHandler;

            InformTimer = new Timer(InformTimerInterval);
            InformTimer.AutoReset = true;
            InformTimer.Elapsed += HandleInformTimer;
            InformTimer.Start();

            dbWriter.InsertSts(ServerTime.GetRealTime(), "Listener", "Listening");

            DayOffs = dbReader.SelectDayOffs(ServerTime.GetRealTime());
            bool isDayOff = IsDayOff(ServerTime.GetRealTime());
            if (isDayOff || IsEndOfWork())
            {
                Environment.Exit(0);
            }
            WasConnected = false;
        }

        private bool IsDayOff(DateTime dTime)
        {
            return (dTime.DayOfWeek == DayOfWeek.Sunday
                || (DayOffs.FindIndex(dt => dt.Day == dTime.Day && dt.Month == dTime.Month && dt.Year == dTime.Year) != -1));
        }
        private void HandleInformTimer(object sender, ElapsedEventArgs e)
        {
            //dbWriter.InsertGeneral("Listening", "Listener");
            dbWriter.InsertSts(ServerTime.GetRealTime(), "Listener", "Listening");
            if (IsEndOfWork())
            {
                //dbWriter.InsertGeneral("Work Ended", "Listener");
                dbWriter.InsertSts(Stocks.ServerTime.GetRealTime(), "Listener", "Work Ended");
                DisconnectStockServer();
                Environment.Exit(0);
            }
        }

        public Listener(List<Query> qs) : this()
        {
            Symbols = qs.Select(q => q.Symbol).ToArray();
            TypeMins = qs.Select(q => q.TypeMins).ToArray();
            Amounts = qs.Select(q => q.Amount + 1).ToArray();
            //TimerIntervals = qs.Select(q => q.TimerInterval).ToArray();
            int n = Symbols.Length;
            Timers = new MyTimer[n];

        }
        private void InitializeTimers()
        {
            for (int i = 0; i < Symbols.Length; i++)
            {
                Timers[i] = new MyTimer(Symbols[i], TypeMins[i] * 60 * 1000);
                Timers[i].Elapsed += HandleMyTimer;
                Timers[i].AutoReset = true;
                Timers[i].Start();
            }
            for (int i = 0; i < Symbols.Length; i++)
            {
                GetBars(Symbols[i], Server.BarLenCast(TypeMins[i]), Amounts[i]);
            }
        }
        private void HandleMyTimer(object sender, EventArgs eventArgs)
        {
            try
            {
                MyTimer thisTimer = sender as MyTimer;
                string symbol = thisTimer.Symbol;
                InformUser("Timer Elapsed: " + symbol);
                int index = -1;
                int n = Symbols.Length;
                for (int i = 0; i < n; i++)
                {
                    if (Symbols[i] == symbol)
                    {
                        index = i; break;
                    }
                }
                if (index == -1)
                {
                    throw new ListenerException(ExceptionImportanceLevel.HIGH, "HandleMyTimer", "Listener", "Could't find symbol " + symbol);
                }
                if (IsNowTradeTime())
                {
                    GetBars(symbol, Server.BarLenCast(TypeMins[index]), Amounts[index]);
                }
            }
            catch (SmartException e)
            {
                Abort();
            }
        }
        public void Abort()
        {
            // dbWriter.InsertGeneral("Failed", "Listener");
            dbWriter.InsertSts(Stocks.ServerTime.GetRealTime(), "Listener", "Failed");
            Environment.Exit(0);
        }
        private void Listener_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Проверяет, является ли текущий момент времени концом торгового дня
        /// </summary>
        /// <returns>Возвращает true, если торговый день завершен</returns>
        private bool IsEndOfWork()
        {
            DateTime dTime = ServerTime.GetRealTime();
            return (dTime.Hour < 9 || (dTime.Hour == 23 && dTime.Minute >= 50));
        }
        public bool IsEveningPause()
        {
            DateTime dTime = ServerTime.GetRealTime();
            return (dTime.Hour == 18 && dTime.Minute >= 45 || (dTime.Hour == 19 && dTime.Minute == 0));
        }
        /// <summary>
        /// Проверяет, конец ли торговой сессии. Если да, прерывает работу программы
        /// </summary>
        public void CheckEndOfDay()
        {
            if (IsEndOfWork())
            {
                WriteToLog("End of Day Found");
                DisconnectStockServer();
                Environment.Exit(0);
            }
        }
        private void WriteToLog(string message, params object[] args)
        {
            //Logger.WriteLine(LogPath, LogName, ServerTime.GetRealTime().ToString(LogDateTimeFormat) + " | " + message, args);
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
                        throw new ListenerException(e);
                    }
                }
                return isConnectedResponse;
            }
        }
        public void InformUser(string message)
        {
            Console.WriteLine(message);
        }
        public void GetBars(string symbol, StBarInterval interval, int count)
        {
            try
            {
                if (count <= 0) throw new ListenerException(ExceptionImportanceLevel.MEDIUM, "GetBars", "Listener", "No other choice for count: " + count);
                DateTime since = DateTime.Now;
                Collector.InitDataRequest(symbol, interval, since, count);
                InformUser(symbol + " - Geting Bars ...");
                SmartComServer.GetBars(symbol, interval, since, count);
            } catch (SmartException e)
            {
                //dbWriter.InsertGeneral("Failed", "Listener");
                dbWriter.InsertSts(Stocks.ServerTime.GetRealTime(), "Listener", "Failed");
                Environment.Exit(0);
            }

        }

        private void InitializeServer()
        {
            SmartComServer = new StServer();
            SmartComServer.Connected += new _IStClient_ConnectedEventHandler(SmartComServerConnected);
            SmartComServer.Disconnected += new _IStClient_DisconnectedEventHandler(SmartComServerDisconnected);
            SmartComServer.AddTick += new _IStClient_AddTickEventHandler(SmartComServerTickAdded);
            //SmartComServer.UpdateQuote += new _IStClient_UpdateQuoteEventHandler(SmartComServerUpdateQuote);
            SmartComServer.AddBar += new _IStClient_AddBarEventHandler(SmartComServer_AddBar);
            SmartComServer.ConfigureClient(SmartComParams);
        }
        void SmartComServer_AddBar(int row, int nrows, string symbol, StBarInterval interval, System.DateTime datetime,
            double open, double high, double low, double close, double volume, double open_int)
        {
            Collector.AddBar(row, nrows, symbol, interval, datetime, open, high, low, close, volume, open_int);
        }

        private void ConnectStockServer()
        {
            if (!IsConnected)
            {
                try
                {
                    WriteToLog("Trying to connect");
                    InformUser("Trying to connect");
                    if (WasConnected)
                    {
                        EmailSender.SendEmail("Listener Stopped", "Listener Stopped because the connection was lost. Will be reconnected by Monitor");
                        Environment.Exit(0);
                    }
                    else
                    {
                        WasConnected = true;
                    }
                    SmartComServer.connect(ServerIP, ushort.Parse(ServerPort), Login, Password);
                }
                catch (Exception e)
                {
                    EmailSender.SendEmail(e);
                    WriteToLog(e.Message);
                    throw new ListenerException(e);
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
        private void SmartComServerDisconnected(string reason)
        {
            WriteToLog("Disconnected from server. Reason: " + reason);
            InformUser("Disconnected from server. Reason: " + reason);
            if (!IsDisconnectedByUser)
            {
                ConnectStockServer();
            }
        }
        private void SmartComServerConnected()
        {
            WriteToLog("Server connected");
            InformUser("Server connected");
            IsDisconnectedByUser = false;
            SmartComServer.ListenPortfolio(Portfolio);
            foreach (string symbol in Symbols)
            {
                ListenSymbol(symbol);
            }
            InitializeTimers();
        }

        public void ListenSymbol(string symbol)
        {
            //SmartComServer.ListenQuotes(symbol);
            SmartComServer.ListenTicks(symbol);
        }

        private void SmartComServerUpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit, int trading_status, double volat, double theor_price)
        {
            //InformUser(datetime + ": " + symbol + ": " + last + " | " + volume);
            //dbWriter.InsertTrade(symbol, datetime, last, volume);
        }
        private void SmartComServerTickAdded(string symbol, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action)
        {
            //InformUser(datetime + ": " + symbol + ": " + price + " | " + volume);
            dbWriter.InsertTrade(symbol, datetime, price, volume);
        }

        public void BarsCollectedHandler(string symbol, StBarInterval interval, List<Bar> bars)
        {
            InformUser(symbol + " - Bars Collected");
            bars.RemoveAt(bars.Count - 1);
            foreach (Bar bar in bars)
            {
                dbWriter.InsertOrderBook(bar.StartTime, symbol, interval, bar.Open, bar.High, bar.Low, bar.Close, (int)bar.Volume);
                Console.Write(symbol + " - " + bar.ToString());
            }
            Console.WriteLine("");
        }
        
        private void Print(string text)
        {
            Console.WriteLine(DateTime.Now.ToString() + ": " + text);
        }
        private void Print(List<Bar> bars)
        {
            Print("Bars collected");
            foreach (Bar bar in bars)
            {
                Console.WriteLine(bar.ToString());
            }
        }
    }

    class Query
    {
        public string Symbol { get; }
        public int TypeMins { get; }
        //public int TimerInterval { get; }
        public int Amount { get; }
        public Query(string symbol, int typeMins, int amount)
        {
            Symbol = symbol;
            TypeMins = typeMins;
            //sTimerInterval = timerInterval;
            Amount = amount;
        }
    }
}
