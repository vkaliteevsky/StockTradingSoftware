using Stocks.Events;
using SmartCOM3Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Stocks.Consitency;

namespace Stocks.Strategies
{
    public abstract class StrategyAbst
    {
        protected Server ServerInstance;
        protected DBInputOutput.DBWriter DatabaseWriter;
        protected DBInputOutput.DBReader DatabaseReader;
        public StrategyState CurrentState { get; }
        /// <summary>
        /// Проверяет, является ли текущее время торговым. Если не является, отменяет все выставленные заявки
        /// </summary>
        private Timer CheckTradeTimeTimer;
        private Timer MakeDecisionTimer;
        private Timer LastUpdateTimer;
        private Timer CancelStopOrdersTimer;
        /// <summary>
        /// Отмеряет периоды времени, между которыми осуществляется запись в таблицу Info гарантийного обеспечения и ContractsToTrade
        /// </summary>
        private Timer WriteInfoTimer;
        private const int WriteInfoTimerInterval = 20 * 60 * 1000;
        /// <summary>
        /// Проверяет выставлены ли в настоящий момент времени заявки и есть ли открытая позиция
        /// </summary>
        private Timer CheckNoPositionTimer;
        private const int CheckNoPositionInterval = 5 * 1000;
        private bool IsSecondaryNoPositionCheck = false;
        /// <summary>
        /// Если установлен в false, значит произошел критический сбой. Запрещено писать "Listening" в БД
        /// </summary>
        private bool CanPingDB;
        /// <summary>
        /// Таймер, предназначенный для отсрочки старта. Запускается только один раз в начале инициализации стратегии.
        /// </summary>
        private Timer StartTimer;
        
        /// <summary>
        /// Даты, в которые торговля не осуществляется
        /// </summary>
        public List<DateTime> DayOffs;
        //private bool IsSentRequestToGetBars;
        public double GO { get; set; }
        public int SymbolId { get; }
        public OrderTypeEnum OrderType { get; }
        protected SessionTypeEnum SessionType { get; }
        protected double Step { get; }
        protected int StartDayTradeHour = 10;
        protected int FinishDayTradeHour = 18;
        protected int FinishDayTradeMinute = 45;
        protected string ExactStartTimeString = "10:00:00";
        private const int StartTimerInterval = 5 * 1000;
        private bool isSecondaryConsistencyCheck = false;
        //protected string ExactDayFinishTimeString;
        //protected int BarsPerDay;
        //protected int BarsToProcess;
        protected int BarLength { get; }
        //public bool IsAborted { get; set; }
        //rivate readonly int ConsistencyInterval = 2 * 60 * 1000;
        /// <summary>
        /// Время, в течение которого осущетсвляется получение баров от сервера
        /// </summary>
        private const int LastUpdateTimerInterval = 15 * 1000;
        //private readonly int UpdatePositionInterval = 1 * 60 * 60 * 1000;
        private const int TradeTimeCheckInterval = 2 * 67 * 1000;
        public int CookieDiv { get; set; }
        public int CookieMod { get; set; }
        public int LastCookie { get; set; }
        public bool IsPositionLocked { get; set; }
        //public double LastPrice { get; set; }
        //public delegate void UpdateHandler();
        //public event UpdateHandler PositionUpdated;

        public string Name { get; }
        public string Symbol { get; set; }
        public string ShortSymbol
        {
            get
            {
                if (Symbol == null) return null;
                int pos = Symbol.IndexOf("-");
                if (pos > 0) return (Symbol.Substring(0, pos - 1));
                else return Symbol;
            }
        }
        public int ContractsToTrade { get; }
        /// <summary>
        /// Время, с которого последний раз была запрошена текущая позиция и заявки с сервера
        /// </summary>
        public DateTime LastPositionUpdateTime { get; set; }
        
        /// <summary>
        /// Ближайшее время, в которое необходимо принять торговое решение
        /// </summary>
        protected DateTime MakeDecisionDateTime { get; set; }
        protected List<Bar> Bars;
        LogWriter LogWriter;
        /// <summary>
        /// Период времени в секундах, в течении которого принимаются инвест. решения
        /// </summary>
        protected int DecisionIntervalSecs
        {
            get
            {
                return Server.BarLenCast(BarInterval()) * AmountOfSkipBars() * 60;
            }
        }

        public StrategyAbst(string name, string symbol, int contractsToTrade, double step)
        {
            ServerInstance = Server.GetInstance();
            Symbol = symbol;
            Name = name;
            Step = step;
            GO = 0;
            if (contractsToTrade < 0)
            {
                throw new SmartException(ExceptionImportanceLevel.LOW, "Constructor", "StrategyAbst", "Amount of bars = " + contractsToTrade + " < 0");
            }
            IsPositionLocked = false;
            DatabaseReader = new DBInputOutput.DBReader();
            DatabaseWriter = new DBInputOutput.DBWriter();
            DayOffs = DatabaseReader.SelectDayOffs(ServerTime.GetRealTime());
            bool isDayOff = IsDayOff(ServerTime.GetRealTime());
            if (isDayOff)
            {
                InformUser("Today is Day Off. No trading allowed");
                return;
            }
            isSecondaryConsistencyCheck = false;

            ContractsToTrade = contractsToTrade;
            LogWriter = new LogWriter("StratLogs", Symbol);
            CurrentState = new StrategyState();
            Bars = new List<Bar>();

            LastPositionUpdateTime = ServerTime.GetRealTime().AddMilliseconds(LastUpdateTimerInterval);

            MakeDecisionDateTime = DatabaseReader.SelectDecisionTimes(Symbol);
            CanPingDB = true;
            //InitializeCheckNoPositionTimer(CheckNoPositionInterval);

            //LastPrice = 0.0;
            ServerInstance.Subscribe(this);
            //ThinkOfMakingDecision();
        }
        private void InitializeConsistencyTimer(int intervalMs)
        {
            ConsistencyTimer = new Timer(intervalMs);
            ConsistencyTimer.AutoReset = false;
            ConsistencyTimer.Elapsed += HandleFinishConsistencyTimer;
            ConsistencyTimer.Start();
        }
        private void InitializeCheckNoPositionTimer(int intervalMs)
        {
            CheckNoPositionTimer = new Timer(intervalMs);
            CheckNoPositionTimer.AutoReset = false;
            CheckNoPositionTimer.Elapsed += HandleFinishCheckNoPositionTimer;
            CheckNoPositionTimer.Start();
        }
        private void HandleFinishCheckNoPositionTimer(object sender, EventArgs e)
        {
            WriteToLogDB("HandleFinishCheckNoPositionTimer", "Started with IsSecondaryNoPositionCheck = " + IsSecondaryNoPositionCheck);
            /* if (!IsPositionLocked)
            {
                if (IsSecondaryNoPositionCheck)
                {
                    if (CurrentState.HasPositionOrOrders())
                    {
                        IsSecondaryNoPositionCheck = false;
                        InitializeCheckNoPositionTimer(CheckNoPositionInterval);
                        InformUser("Check for No Position: Second Succeded - Has Position");
                    }
                    else
                    {
                        InformUser("Check for No Position: Second Failed");
                        IsSecondaryNoPositionCheck = false;
                        DateTime dTime = ServerTime.GetRealTime();
                        if ((MakeDecisionDateTime - dTime).TotalSeconds <= 60)
                        {
                            InformUser("Time till Make Decision: " + (MakeDecisionDateTime - dTime).TotalSeconds + " secs. Do nothing");
                        }
                        else
                        {
                            MakeDecisionDateTime = ServerTime.GetRealTime().AddSeconds(-30);
                            WriteToLogDB("HandleFinishCheckNoPositionTimer", "Changed Make Decision Time to " + MakeDecisionDateTime.ToString());
                            InformUser("Change Make Decision Time: " + MakeDecisionDateTime.ToString());
                            ThinkOfMakingDecision();
                        }
                        InitializeCheckNoPositionTimer(CheckNoPositionInterval);
                    }
                }
                else
                {
                    if (!CurrentState.HasPositionOrOrders())
                    {
                        IsSecondaryNoPositionCheck = true;
                        InitializeCheckNoPositionTimer(LastUpdateTimerInterval);
                        InformUser("Check for No Position: First Failed");
                    }
                    else
                    {
                        InitializeCheckNoPositionTimer(CheckNoPositionInterval);
                    }
                }
            } else
            {
                InitializeCheckNoPositionTimer(CheckNoPositionInterval);
            } */
            DateTime tm = ServerTime.GetRealTime();
            if (!IsPositionLocked && IsNowTradeTime() && tm.Hour <= 23 && tm.Minute < 49)
            {
                if (!CurrentState.HasPositionOrOrders())   // заявки не выставлены, необходимо принимать решение
                {
                    MakeDecisionTimer.Stop();
                    MakeDecisionDateTime = ServerTime.GetRealTime().AddSeconds(-5);
                    DatabaseWriter.InsertDecisionTimes(MakeDecisionDateTime, Symbol);
                    WriteToLogDB("HandleFinishCheckNoPositionTimer", "Check No Position: Changed MDT to Past - " + MakeDecisionDateTime.ToString());
                    InformUser("Check No Position: Changed MDT to Past - " + MakeDecisionDateTime.ToString());
                    ThinkOfMakingDecision();
                }
            } else
            {
                InformUser("Position Locked or Not a Trade Time - Cannot Check No Position");
                WriteToLogDB("HandleFinishCheckNoPositionTimer", "Position Locked or Not a Trade Time - Cannot Check No Position");
            }
            WriteToLogDB("HandleFinishCheckNoPositionTimer", "Finished with IsSecondaryNoPositionCheck = " + IsSecondaryNoPositionCheck);
        }
        /* public bool IsDayOff(DateTime day)
        {
            day = day.Date;
            int index = DayOffs.FindIndex(dt => dt.Date.Equals(day));
            return index != -1;
        } */
        private void InitializeMakeDecisionTimer(DateTime finishTime)
        {
            WriteToLogDB("InitializeMakeDecisionTimer", "Called: finishTime = " + finishTime);
            TimeSpan tSpan = finishTime - ServerTime.GetRealTime();
            int totalMS = (int)tSpan.TotalMilliseconds;
            if (totalMS <= 0) totalMS = 100;
            //throw new SmartException(ExceptionImportanceLevel.LOW, "InitializeMakeDecisionTimer", "StrategyAbst", "totalMS <= 0: " + totalMS);
            if (MakeDecisionTimer != null)
                MakeDecisionTimer.Stop();
            MakeDecisionTimer = new Timer(totalMS);
            MakeDecisionTimer.Elapsed += HandleMakeDecisionTimer;
            MakeDecisionTimer.AutoReset = false;
            MakeDecisionTimer.Start();
            MakeDecisionDateTime = finishTime;
            InformUser("Make Decision Time Changed: " + MakeDecisionDateTime.ToString());
        }
        protected double CalcVolat(double[] vals)
        {
            double average = vals.Average();
            double sumOfSquaresOfDifferences = vals.Select(val => Math.Pow(val - average, 2)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / (vals.Length - 1));
            return (sd);
        }
        protected DateTime ReadMakeDecisionTime()
        {
            DateTime dTime = DatabaseReader.SelectDecisionTimes(Symbol);
            return dTime;
        }
        /// <summary>
        /// Отменяет все выставленные заявки и выставляет лимитные заявки на закрытие по близким ценам
        /// </summary>
        public void CloseMarket()
        {
            CancelAllOrders();
            Bar bar1 = DatabaseReader.SelectLastPrice(Symbol);
            List<Bar> bars = DatabaseReader.SelectBars(Symbol, 30);
            double[] closes = bars.Select(bar => bar.Close).ToArray();
            double[] ys = new double[bars.Count - 1];
            for (int i = 0; i < bars.Count - 1; i++)
                ys[i] = closes[i + 1] / closes[i] - 1;
            double sdev = CalcVolat(ys);
            double price = 0.0;
            ActionEnum action;
            if (CurrentState.Position > 0)
            {
                action = ActionEnum.SELL;
                price = RoundToStep(bar1.Close * (1 + 0.3 * sdev));
            } else
            {
                action = ActionEnum.BUY;
                price = RoundToStep(bar1.Close * (1 - 0.3 * sdev));
            }
            Order order = new Order(Symbol, GenerateCookie(), "", Math.Abs(CurrentState.Position), 0, price, 0, action, OrderTypeEnum.LIMIT);
            if (order.Volume != 0)
                ServerInstance.PlaceOrder(order);
        }
        private void HandleTradeTimeCheck(object sender, EventArgs e)
        {
            WriteToLogDB("HandleTradeTimeCheck", "Called");
            if (!IsNowTradeTime())
            {
                WriteToLogDB("HandleTradeTimeCheck", "Not a trade time");
                //CancelAllOrders();
                if (IsEveningPause())   // наступление перерыва между дневной и вечерней торговой сессией
                {
                    DateTime thisTime = ServerTime.GetRealTime();
                    DateTime startTime = new DateTime(thisTime.Year, thisTime.Month, thisTime.Day, 19, 0, 5);
                    if (startTime > thisTime)
                    {
                        InitializeMakeDecisionTimer(startTime);
                    }
                }
            }
            //ServerInstance.CheckEndOfDay();
        }
        private void HandleWriteInfoTimer(object sender, EventArgs e)
        {
            DateTime dTime = ServerTime.GetRealTime();
            DatabaseWriter.InsertInfo(dTime, Symbol, "go", GO);
            DatabaseWriter.InsertInfo(dTime, Symbol, "weight", ContractsToTrade);
            //DatabaseWriter.InsertGeneral("Working", Symbol);
            //DatabaseWriter.InsertSts(dTime, "Stocks", "Working");
        }
        private void HandleStartTimer(object sender, EventArgs e)
        {
            WriteInfoTimer = new Timer(WriteInfoTimerInterval);
            WriteInfoTimer.AutoReset = true;
            WriteInfoTimer.Elapsed += HandleWriteInfoTimer;
            WriteInfoTimer.Start();

            CheckTradeTimeTimer = new Timer(TradeTimeCheckInterval);
            CheckTradeTimeTimer.AutoReset = true;
            CheckTradeTimeTimer.Elapsed += HandleTradeTimeCheck;
            CheckTradeTimeTimer.Start();

            DateTime dt = ServerTime.GetRealTime();
            DateTime cancelStopsTime = new DateTime(dt.Year, dt.Month, dt.Day, 23, 49, 0);
            double dtMs = (cancelStopsTime - dt).TotalMilliseconds;
            if (dtMs <= 0)
            {
                EmailSender.SendEmail("Warning", "Cancel Stop Time dtMs in negative");
            }
            else
            {
                CancelStopOrdersTimer = new Timer(dtMs);
                CancelStopOrdersTimer.AutoReset = false;
                CancelStopOrdersTimer.Elapsed += CancelStopOrdersTimer_Elapsed;
                CancelStopOrdersTimer.Start();
            }

            //InitializeConsistencyTimer(ConsistencyInterval);
            //IsSentRequestToGetBars = false;

            //StartWork();
            ThinkOfMakingDecision();
        }

        private void CancelStopOrdersTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CancelStopOrders();
        }

        /// <summary>
        /// Останавливает работу стратегии
        /// </summary>
        public void StopWork()
        {
            WriteToLogDB("StopWork", "Called");
            LastUpdateTimer.Stop();
            WriteInfoTimer.Stop();
            MakeDecisionTimer.Stop();
            StartTimer.Stop();
            //CancelAllOrders();
        }
        protected int GenerateCookie()
        {
            int cookie = LastCookie;
            LastCookie += CookieDiv;
            return (cookie);
        }
        public void StartWork()
        {
            Random r = new Random();
            double randVal = r.NextDouble() * 2;
            DateTime thisTime = ServerTime.GetRealTime();
            DateTime startTime = new DateTime(thisTime.Year, thisTime.Month, thisTime.Day, 10, 0, 5);
            double deltaMs = (startTime - thisTime).TotalMilliseconds;
            if ((deltaMs <= StartTimerInterval) || deltaMs <= 0)
            {
                StartTimer = new Timer(StartTimerInterval - randVal * 1000);
            }
            else
            {
                StartTimer = new Timer(deltaMs - randVal * 1000);
            }
            StartTimer.AutoReset = false;
            StartTimer.Elapsed += HandleStartTimer;
            StartTimer.Start();
        }
        private bool IsEveningPause()
        {
            return ServerInstance.IsEveningPause();
        }
        private bool IsDayOff(DateTime dTime)
        {
            return (dTime.DayOfWeek == DayOfWeek.Sunday
                || (DayOffs.FindIndex(dt => dt.Day == dTime.Day && dt.Month == dTime.Month && dt.Year == dTime.Year) != -1));
        }

        public DateTime CalcMakeDicisionTime(DateTime thisTime, int intervalSeconds)
        {
            int thisYear = thisTime.Year;
            int thisMonth = thisTime.Month;
            int thisDay = thisTime.Day;
            DateTime dt1 = new DateTime(thisYear, thisMonth, thisDay, 10, 0, 0);
            DateTime dt2 = new DateTime(thisYear, thisMonth, thisDay, 18, 45, 0);
            DateTime dt3 = new DateTime(thisYear, thisMonth, thisDay, 19, 0, 0);
            DateTime dt4 = new DateTime(thisYear, thisMonth, thisDay, 23, 50, 0);
            if (thisTime < dt1) thisTime = dt1;
            else if (thisTime > dt2 && thisTime < dt3) thisTime = dt3;
            else if (thisTime > dt4) thisTime = new DateTime(thisYear, thisMonth, thisDay + 1, 10, 0, 0);
            bool wasIn = false;
            while (IsDayOff(thisTime))
            {
                thisTime = thisTime.AddDays(1);
                wasIn = true;
            }
            if (wasIn)
            {
                thisTime = new DateTime(thisTime.Year, thisTime.Month, thisTime.Day, 10, 0, 0);
            }

            while (intervalSeconds > 0)
            {
                while (IsDayOff(thisTime)) thisTime = thisTime.AddDays(1);
                thisYear = thisTime.Year;
                thisMonth = thisTime.Month;
                thisDay = thisTime.Day;
                dt1 = new DateTime(thisYear, thisMonth, thisDay, 10, 0, 0);
                dt2 = new DateTime(thisYear, thisMonth, thisDay, 18, 45, 0);
                dt3 = new DateTime(thisYear, thisMonth, thisDay, 19, 0, 0);
                dt4 = new DateTime(thisYear, thisMonth, thisDay, 23, 50, 0);
                if (thisTime >= dt1 && thisTime <= dt2)
                {
                    int diffSecs = (int)((dt2 - thisTime).TotalSeconds);
                    if (diffSecs <= intervalSeconds)
                    {
                        thisTime = new DateTime(thisTime.Year, thisTime.Month, thisTime.Day, 19, 0, 0);
                        intervalSeconds -= diffSecs;
                    } else
                    {
                        thisTime = thisTime.AddSeconds(intervalSeconds);
                        intervalSeconds = 0;
                    }
                } else if (thisTime >= dt3 && thisTime <= dt4)
                {
                    int diffSecs = (int)((dt4 - thisTime).TotalSeconds);
                    if (diffSecs <= intervalSeconds)
                    {
                        thisTime = new DateTime(thisTime.Year, thisTime.Month, thisTime.Day + 1, 10, 0, 0);
                        intervalSeconds -= diffSecs;
                    }
                    else
                    {
                        thisTime = thisTime.AddSeconds(intervalSeconds);
                        intervalSeconds = 0;
                    }
                } else
                {
                    string message = "DateTime " + thisTime + "doesn't belong to working period!";
                    throw new SmartException(ExceptionImportanceLevel.LOW, "CalcMakeDicisionTime", "StrategyAbst", message);
                }
            }
            return thisTime;
        }

        public List<Bar> GetBars(StBarInterval interval, int amountOfBars)
        {
            //InformUser("Recieving Bars from Server");
            WriteToLogDB("GetBars", "Called. Interval = " + interval + ", Amount of Bars = " + amountOfBars);
            /* DateTime dTime = ServerTime.GetRealTime();
            if (IsSentRequestToGetBars)
            {
                return;
            } else
            {
                IsSentRequestToGetBars = true;
                Bars.Clear();
                ServerInstance.GetBars(Symbol, interval, dTime, amountOfBars);
            } */
            List <Bar> bars = DatabaseReader.SelectBars(Symbol, amountOfBars);
            return bars;
        }
        protected void InformUser(string message)
        {
            ServerInstance.InformUser(Symbol + ": " + message);
        }
        /// <summary>
        /// Срабатывает в те моменты времени, когда необходимо принять какое-то решение. Несмотря на это, по итогам работы, решение может быть вообще не принято
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void HandleMakeDecisionTimer(object sender, EventArgs e)
        {
            WriteToLogDB("HandleMakeDecisionTimer", "Called");
            ThinkOfMakingDecision();
        }

        /// <summary>
        /// Округление цены до шага цены Step
        /// </summary>
        /// <param name="price">Цена, которую необходимо округлить</param>
        /// <returns></returns>
        protected double RoundToStep(double price)
        {
            return ((int)Math.Round(price / Step) * Step);
        }

        public override string ToString()
        {
            string msg = "*** Current State of " + Symbol + " ***\r\n" + CurrentState.ToString();
            /*msg += "*** Timers ***\r\n";
            if (LastUpdateTimer == null) msg += "Time Till Next Update of Current State: null\r\n";
            else msg += "Time Till Next Update of Current State: " + LastUpdateTimer.Interval + "\r\n";
            msg += "Check Consistency Timer: " + (ConsistencyTimer == null ? "null" : ConsistencyTimer.Interval.ToString()) + "\r\n";
            msg += "Make Decision Timer: " + (MakeDecisionTimer == null ? "null" : MakeDecisionTimer.Interval.ToString()) + "\r\n";
            msg += "Trade Time Check Timer: " + (CheckTradeTimeTimer == null ? "null" : CheckTradeTimeTimer.ToString()) + "\r\n";*/
            msg += "Make Decision Time: " + MakeDecisionDateTime.ToString() + "\r\n";
            msg += "Last Update Time: " + LastPositionUpdateTime.ToString() + "\r\n";
            //msg += "*** Others ***\r\n";
            msg += "GO: " + GO + "; Step: " + Step + "\r\n";
            msg += "Bars Count: " + Bars.Count + "\r\n";
            msg += "Contracts To Trade: " + ContractsToTrade + "\r\n";
            //msg += "Is Secondary Consistency Check: " + isSecondaryConsistencyCheck + "\r\n";
            //msg += "Is Secondary No Position Check: " + IsSecondaryNoPositionCheck + "\r\n";
            //msg += "Is Sent Request to get Bars: " + IsSentRequestToGetBars + "\r\n";
            return (msg);
        }

        /// <summary>
        /// Обработчик метода GetBars. Вызывается в нужный момент сервером ServerInstance
        /// </summary>
        /// <param name="bars">Бары, упорядоченные по возрастанию времени</param>
        public void HandleGetBars(List<Bar> bars)
        {
            WriteToLogDB("HandleGetBars", "Started");
            Bars = bars;
            /* foreach (Bar bar in bars)
            {
                WriteToLogDB("HandleGetBars", "HandleGetBars: " + bar.ToString());
            } */
            InformUser("Bars recieved");
            //IsSentRequestToGetBars = false;
            //ThinkOfMakingDecision();
        }
        /// <summary>
        /// Отмеряет периоды времени, между которыми запускается проверка целостности
        /// </summary>
        protected Timer ConsistencyTimer;
        /// <summary>
        /// Отмеряет периоды времени, между которыми запускается принудительное обновление позиций из Excel
        /// </summary>
        //protected Timer UpdatePositionTimer;

        public void CancelAllOrders()
        {
            WriteToLogDB("CancelAllOrders", "Started");
            CanOrder();
            WriteToLogDB("CancelAllOrders", "Finished");
        }

        private bool CanOrder()
        {
            bool wasExceptBuy = false;
            bool wasExceptSell = false;
            foreach (Order order in CurrentState.BuyOrders)
            {
                if (order.OrderId.Equals("")) wasExceptBuy = true;
                else ServerInstance.CancelOrder(Symbol, order.OrderId);
            }
            if (!wasExceptBuy)
                CurrentState.BuyOrders.Clear();
            foreach (Order order in CurrentState.SellOrders)
            {
                if (order.OrderId.Equals("")) wasExceptSell = true;
                else ServerInstance.CancelOrder(Symbol, order.OrderId);
            }
            if (!wasExceptSell)
                CurrentState.SellOrders.Clear();
            return wasExceptBuy || wasExceptSell;
        }
        public void ClearCurrentPosition()
        {
            WriteToLogDB("ClearCurrentPosition", "Called");
            LastPositionUpdateTime = new DateTime(2000, 1, 1, 0, 0, 0);
            CurrentState.Clear();
        }
        public void UpdatePositionFromServer()
        {
            WriteToLogDB("UpdatePositionFromServer", "Called");
            DateTime thisTime = ServerTime.GetRealTime();
            if (IsNowTradeTime() && ServerInstance.IsConnected)
            {
                InformUser("Update of Position Initiated");
                ServerInstance.GetOrdersAndPositionFromServer();
                InitializeLastUpdateTimer();
            } else
            {
                InformUser("Update of Position Not Initiated - Not a Trade Time");
            }

        }
        public void CancelStopOrders()
        {
            WriteToLogDB("CancelStopOrders", "Called");
            foreach (Order stopOrder in CurrentState.BuyOrders)
            {
                if (stopOrder.Type == OrderTypeEnum.STOP)
                    ServerInstance.CancelOrder(Symbol, stopOrder.OrderId);
            }
            foreach (Order stopOrder in CurrentState.SellOrders)
            {
                if (stopOrder.Type == OrderTypeEnum.STOP)
                    ServerInstance.CancelOrder(Symbol, stopOrder.OrderId);
            }
        }
        public void InitializeLastUpdateTimer()
        {
            LastUpdateTimer = new Timer(LastUpdateTimerInterval);
            LastUpdateTimer.AutoReset = false;
            LastUpdateTimer.Elapsed += HandleLastUpdateTimer;
            LastUpdateTimer.Start();
        }
        private void HandleLastUpdateTimer(object sender, EventArgs e)
        {
            IsPositionLocked = false;
            DateTime thisTime = ServerTime.GetRealTime();
            //InitializeConsistencyTimer(10 * 1000);
            if (IsNowTradeTime())
            {
                if (IsEveningPause())
                {
                    //CancelAllOrders();
                }
                HandleFinishConsistencyTimer(new object(), null);
                InitializeCheckNoPositionTimer(CheckNoPositionInterval);
            }
            LastPositionUpdateTime = thisTime;
            //PositionUpdated();
        }
        protected double CalcIntervalFromLastUpdateSecs()
        {
            if (LastPositionUpdateTime == null || LastPositionUpdateTime.Year < 2018)
            {
                throw new SmartException(ExceptionImportanceLevel.LOW, "IntervalFromLastUpdateSecs", "StrategyAbst", "LastPositionUpdateTime is empty: " + LastPositionUpdateTime.ToString());
            }
            DateTime dTime = ServerTime.GetRealTime();
            TimeSpan tSpan = dTime - LastPositionUpdateTime;
            double secs = tSpan.TotalSeconds;
            if (secs < 0)
            {
                string message = Symbol + ": Total Seconds is less than zero. Secs = " + secs.ToString();
                throw new SmartException(ExceptionImportanceLevel.LOW, "IntervalFromLastUpdateSecs", "StrategyAbst", message);
            }
            return secs;
        }
        protected virtual void WriteToLogDB(string funcName, string comment)
        {
            DatabaseWriter.InsertGeneral(ServerTime.GetRealTime(), "StrategyAbst." + funcName + ": " + comment, Symbol);
        }
        protected bool IsNowTradeTime()
        {
            return ServerInstance.IsNowTradeTime();
        }
        /* Обработчики позиции и выставленных заявок */
        public void HandleEvent(EventAbst ev)
        {
            //WriteToLogDB("HandleEvent", "Called. " + ev.ToString());
            if (ev is UpdatePositionEvent)
            {
                HandleUpdatePositionEvent((UpdatePositionEvent)ev);
            }
            else if (ev is UpdateOrderEvent)
            {
                //UpdateOrderEvent evnt = (UpdateOrderEvent)ev;
                //ServerInstance.InformUser("GMKR: " + evnt.ToString());
                HandleUpdateOrderEvent((UpdateOrderEvent)ev);
            }
            else if (ev is OrderSucceededEvent)
            {
                HandleOrderSucceededEvent((OrderSucceededEvent)ev);
            }
            else if (ev is OrderFailedEvent)
            {
                HandleOrderFailedEvent((OrderFailedEvent)ev);
            }
            else if (ev is OrderMoveSucceededEvent)
            {
                HandleOrderMoveSucceededEvent((OrderMoveSucceededEvent)ev);
            }
            else if (ev is OrderMoveFailedEvent)
            {
                HandleOrderMoveFailedEvent((OrderMoveFailedEvent)ev);
            }
            else if (ev is OrderCancelSucceededEvent)
            {
                HandleOrderCancelSucceededEvent((OrderCancelSucceededEvent)ev);
            }
            else if (ev is OrderCancelFailedEvent)
            {
                HandleOrderCancelFailedEvent((OrderCancelFailedEvent)ev);
            }
            else if (ev is SetMyOrderEvent)
            {
                HandleSetMyOrderEvent((SetMyOrderEvent)ev);
            }
        }

        protected void HandleUpdatePositionEvent(UpdatePositionEvent ev)
        {
            if (ev.Symbol.Equals(Symbol))
            {
                int lastPos = CurrentState.Position;
                WriteToLogDB("HandleUpdatePositionEvent", "Called. " + ev.ToString());
                CurrentState.Position = (int)ev.Amount;
                //CurrentState.Planned = (int)ev.Planned + (int)ev.Amount;
                CurrentState.AvgPrice = ev.AvgPrice;
            }
        }
        protected void HandleUpdateOrderEvent(UpdateOrderEvent ev)
        {
            if (ev.Symbol.Equals(Symbol))
            {
                WriteToLogDB("HandleUpdateOrderEvent", "Called. " + ev.ToString());
                StOrder_State state = ev.State;
                if (state.Equals(StOrder_State.StOrder_State_ContragentCancel) || state.Equals(StOrder_State.StOrder_State_SystemReject)
                    || state.Equals(StOrder_State.StOrder_State_SystemCancel))
                {
                    Order foundOrder = CurrentState.FindOrder(ev.OrderId, ev.Cookie);
                    if (foundOrder != null)
                    {
                        CurrentState.RemoveOrder(foundOrder.OrderId, foundOrder.Cookie);
                    }
                    HandleSystemOrderCancel(ev.OrderId, ev.Cookie);
                }
                else if (state.Equals(StOrder_State.StOrder_State_Cancel) || state.Equals(StOrder_State.StOrder_State_Expired))
                { // Отменен пользователем или отменен по причине окончания торгового дня
                    Order foundOrder = CurrentState.FindOrder(ev.OrderId, ev.Cookie);
                    if (foundOrder != null)
                    {
                        CurrentState.RemoveOrder(foundOrder.OrderId, foundOrder.Cookie);
                    }
                }
                else if (state.Equals(StOrder_State.StOrder_State_Partial))
                {
                    Order foundOrder = CurrentState.FindOrder(ev.OrderId, ev.Cookie);
                    if (foundOrder != null)
                    {
                        //foundOrder.FilledVolume = (int)ev.Amount - (int)ev.Filled;
                        foundOrder.FilledVolume = (int)ev.Filled;
                        foundOrder.Volume = (int)ev.Filled;
                    }
                    else
                    {
                        //CurrentState.AddOrder(ev);
                        //CurrentState.AddOrderByUpdateOrderEvent(ev);
                        CurrentState.UpdateStateByOrderEvent(ev);
                    }
                    /*Order foundOrder = CurrentState.GetBuyOrderById(ev.OrderId);
                    if (foundOrder == null)
                    {
                        foundOrder = CurrentState.GetSellOrderById(ev.OrderId);
                    }
                    if (foundOrder != null)
                    {
                        foundOrder.FilledVolume = (int)ev.Amount - (int)ev.Filled;
                        foundOrder.Volume = (int)ev.Filled;
                        if (((int)ev.Amount - (int)ev.Filled) == 0)
                        {
                            WriteToLogDB("HandleUpdateOrderEvent", "Calling CurrentState.RemoveOrder: OrderId = " + ev.OrderId + ", Cookie = " + ev.Cookie);
                            CurrentState.RemoveOrder(ev.OrderId, ev.Cookie);
                        }
                    }
                    */
                }
                else if (state.Equals(StOrder_State.StOrder_State_Filled))
                {
                    Order foundOrder = CurrentState.FindOrder(ev.OrderId, ev.Cookie);
                    if (foundOrder != null)
                    {
                        CurrentState.RemoveOrder(foundOrder.OrderId, foundOrder.Cookie);
                    }
                }
                else if (state.Equals(StOrder_State.StOrder_State_Open) || (state.Equals(StOrder_State.StOrder_State_Pending)))
                {
                    if (ev.Action.Equals(StOrder_Action.StOrder_Action_Buy) || ev.Action.Equals(StOrder_Action.StOrder_Action_Sell))
                    {
                        /*bool isOrderAdded = CurrentState.AddOrderByUpdateOrderEvent(ev);
                        if (!isOrderAdded)
                        {
                            CurrentState.UpdateOrdersByUpdateOrderEvent(ev);
                            //CurrentState.UpdateStateByOrderEvent(ev);
                        }*/
                        CurrentState.UpdateStateByOrderEvent(ev);
                    }
                    else
                    {
                        throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleUpdateOrderEvent", "StrategyAbst", "No other options. Ev = " + ev.ToString());
                    }
                }
            }
        }

        protected virtual void HandleSystemOrderCancel(string orderId, int cookie)
        {
            //WriteToLogDB("HandleSystemOrderCancel", "Called: OrderId = " + orderId + ", Cookie = " + cookie);
            /* if (!IsNowTradeTime())
            {
                ServerInstance.CancelQuotes(Symbol);
            } */
            //Order foundOrder = CurrentState.FindOrder(orderId, cookie);

        }

        protected void HandleOrderSucceededEvent(OrderSucceededEvent ev)
        {
            Order foundOrder = CurrentState.FindOrder(ev.OrderId, ev.Cookie);
            if (foundOrder != null)
            {
                WriteToLogDB("HandleOrderSucceededEvent", "Called. Event: " + ev.ToString());
                foundOrder.OrderId = ev.OrderId;
            }
        }

        protected void HandleOrderFailedEvent(OrderFailedEvent ev)
        {
            Order foundOrder = CurrentState.FindOrder(ev.OrderId, ev.Cookie);
            if (foundOrder != null)
            {
                WriteToLogDB("HandleOrderFailedEvent", "Called. " + ev.ToString());
                CurrentState.RemoveOrder(foundOrder.OrderId, foundOrder.Cookie);
            }
            //EmailSender.SendEmail("Warning - Order Failed Event", ": " + Symbol + ": " + ev.ToString());
        }

        protected void HandleOrderMoveSucceededEvent(OrderMoveSucceededEvent ev)
        {
            Order foundOrder = CurrentState.FindOrder(ev.OrderId, -1000);
            if (foundOrder != null)
            {
                int coeff = foundOrder.Action == ActionEnum.BUY ? 1 : -1;
                WriteToLogDB("HandleOrderMoveSucceededEvent", "Called. " + ev.ToString());
                //DatabaseWriter.InsertOrder(SymbolId, ServerTime.GetRealTime(), foundOrder.Price, foundOrder.Volume * coeff);
            }
        }

        protected void HandleOrderMoveFailedEvent(OrderMoveFailedEvent ev)
        {
            Order foundOrder = CurrentState.FindOrder(ev.OrderId, -1000);
            if (foundOrder != null)
            {
                WriteToLogDB("HandleOrderMoveFailedEvent", "Called. " + ev.ToString());
            }
            EmailSender.SendEmail("Move Failed Event", "Failed to Move Order at " + Symbol + ": OrderId = " + ev.OrderId);
        }

        protected void HandleOrderCancelSucceededEvent(OrderCancelSucceededEvent ev)
        {
            Order foundOrder = CurrentState.FindOrder(ev.OrderId, -1000);
            if (foundOrder != null)
            {
                CurrentState.RemoveOrder(foundOrder.OrderId, foundOrder.Cookie);
                WriteToLogDB("HandleOrderCancelSucceededEvent", "Called. " + ev.ToString());
            }
        }

        protected void HandleOrderCancelFailedEvent(OrderCancelFailedEvent ev)
        {
            Order foundOrder = CurrentState.FindOrder(ev.OrderId, -1000);
            if (foundOrder != null)
            {
                WriteToLogDB("HandleOrderCancelFailedEvent", "Called. " + ev.ToString());
            }
            //EmailSender.SendEmail("Warning - Failed to Cancel Order", Symbol);
        }

        protected void HandleSetMyOrderEvent(SetMyOrderEvent ev)
        {
            if (ev.Symbol.Equals(Symbol))
            {
                Order foundOrder = CurrentState.FindOrder(ev.OrderId, ev.Cookie);
                if (foundOrder != null)
                {
                    //ev.Log(Logger, DatabaseWriter, SymbolId);
                    foundOrder.Price = ev.Price;
                }
                WriteToLogDB("HandleSetMyOrderEvent", "Called. " + ev.ToString());
            }
        }

        /// <summary>
        /// Используемый в стратегии размер бара
        /// </summary>
        public virtual StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_5Min;
        }

        /// <summary>
        /// Количество баров, через которые инициируется принятие решения
        /// </summary>
        protected abstract int AmountOfSkipBars();

        /// <summary>
        /// Количество точек (баров), по которым вычисляется волатильность
        /// </summary>
        protected abstract int AmountOfVolatEstim();
        /// <summary>
        /// Отклонение выставления заявки относительно sigma * sqrt(n)
        /// </summary>
        /// <returns></returns>
        protected abstract double AlphaSpread();

        public void ThinkOfMakingDecision()
        {
            WriteToLogDB("ThinkOfMakingDecision", "Started");
            DateTime thisTime = ServerTime.GetRealTime();
            if (!(thisTime.Hour == 23 && thisTime.Minute >= 49))
            {
                if (!IsNowTradeTime())
                {
                    InformUser("Cannot Make Decision: Not a Trade Time");
                    WriteToLogDB("ThinkOfMakingDecision", "Cannot Make Decision: Not a Trade Time");
                    InitializeMakeDecisionTimer(thisTime.AddSeconds(20));
                }
                else if (IsPositionLocked)
                {
                    DateTime nextTime = MakeDecisionDateTime.AddMilliseconds(LastUpdateTimerInterval);
                    if (nextTime < thisTime.AddMilliseconds(LastUpdateTimerInterval / 2))
                    {
                        nextTime = thisTime.AddMilliseconds(LastUpdateTimerInterval);
                    }
                    InformUser("Position Locked. DTM changed to " + nextTime.ToString());
                    WriteToLogDB("ThinkOfMakingDecision", "Position Locked. nextTime = " + nextTime.ToString());
                    InitializeMakeDecisionTimer(nextTime);
                }
                else
                {
                    MakeDecisionDateTime = DatabaseReader.SelectDecisionTimes(Symbol);
                    WriteToLogDB("ThinkOfMakingDecision", "Make Decision DateTime from DB: " + MakeDecisionDateTime.ToString());
                    if (MakeDecisionDateTime > thisTime.AddSeconds(5))      // время принятия решения еще не наступило
                    {
                        InformUser("Making Decision: Time is not ready yet: " + MakeDecisionDateTime.ToString());
                        WriteToLogDB("ThinkOfMakingDecision", "Make Decision Time = " + MakeDecisionDateTime + " > " + "thisTime = " + thisTime);
                        //CancelAllOrders();
                        List<Order> decisionOrders = DatabaseReader.GetLastDec(Symbol);
                        List<Order> placeOrders = RestorePlacedOrders(decisionOrders);
                        WriteToLogDB("ThinkOfMakingDecision", "Amount of Orders To Be Restored: " + placeOrders.Count);
                        if (placeOrders.Count == 0)     // восстановить состояние не удалось
                        {
                            InitializeMakeDecisionTimer(ServerTime.GetRealTime().AddSeconds(10));
                            DatabaseWriter.InsertDecisionTimes(ServerTime.GetRealTime().AddSeconds(10), Symbol);
                            EmailSender.SendEmail("Stocks Warning", "Cannot Restore State in ThinkOfMakingDecision\r\n" + ToString());
                        }
                        else   // состояние восстановить удалось
                        {
                            /* CancelAllOrders();
                            foreach (Order order in placeOrders)
                            {
                                ServerInstance.PlaceOrder(order);
                            } */
                            List<Order> cancelOrders = CurrentState.CloneBuyOrders();
                            cancelOrders.AddRange(CurrentState.CloneSellOrders());
                            ExecDecision(cancelOrders, placeOrders);
                            InitializeMakeDecisionTimer(MakeDecisionDateTime);
                        }
                        WriteToLogDB("ThinkOfMakingDecision", "Position = " + CurrentState.Position);
                    }
                    else    // время принятия решения пропущено, либо программа запущена в первый раз
                    {
                        // требуется принять новое решение
                        InformUser("Making Decision: Initializing to place orders");
                        WriteToLogDB("ThinkOfMakingDecision", "Make Decision Time = " + MakeDecisionDateTime + " <= " + "thisTime = " + thisTime);
                        WriteToLogDB("ThinkOfMakingDecision", "Bars.Count = " + Bars.Count);
                        Bars = GetBars(BarInterval(), AmountOfSkipBars());
                        if (Bars.Count != AmountOfSkipBars())
                        {
                            Bars = GetBars(BarInterval(), AmountOfSkipBars());
                        }
                        if (Bars.Count != AmountOfSkipBars())
                        {
                            EmailSender.SendEmail("Stocks - Error", "Bars Count is Incorrect. Not enough bars to make decision. Program continues to work, but user actions are strongly admired");
                            DateTime nextTime = MakeDecisionDateTime.AddSeconds(30);
                            DatabaseWriter.InsertDecisionTimes(nextTime, Symbol);
                            InitializeMakeDecisionTimer(nextTime);
                        }
                        else
                        {
                            MakeDecision();
                            DateTime nextDecisionTime = CalcMakeDicisionTime(ServerTime.GetRealTime(), DecisionIntervalSecs);
                            DatabaseWriter.InsertDecisionTimes(nextDecisionTime, Symbol);
                            InitializeMakeDecisionTimer(nextDecisionTime);
                        }
                    }
                }
            }
            WriteToLogDB("ThinkOfMakingDecision", "Finished");
        }

        /* Блок исполнения заявок и оптимизации */
        public void MakeDecision()
        {
            WriteToLogDB("MakeDecision", "Started");
            List<Order> cancelOrders = PrepareCancelOrders();
            List<Order> placeOrders = new List<Order>();
            try
            {
                placeOrders = PreparePlaceOrders();     // Decision
            } catch (SmartException se)
            {
                EmailSender.SendEmail("Prep Place Order - Error", CurrentState.ToString() + "Bars Count: " + Bars.Count);
                throw se;
            }
            
            /* DateTime dTime = ServerTime.GetRealTime();
            foreach (Order order in placeOrders)
            {
                DatabaseWriter.InsertDecision(dTime, order.Symbol, order.Action, order.Price, order.Volume, order.StopPrice);
            } */
            ExecDecision(cancelOrders, placeOrders);
            Bars.Clear();
            WriteToLogDB("MakeDecision", "Finished");
        }

        /// <summary>
        /// Формирует список заявок, которые необходимо выставить на биржу для восстановления нормального состояния
        /// </summary>
        /// <param name="decisionOrders"></param>
        /// <returns>Список заявок, готовых к выставлению на биржу</returns>
        protected virtual List<Order> RestorePlacedOrders(List<Order> decisionOrders)
        {
            WriteToLogDB("RestorePlacedOrders", "Started");
            if (decisionOrders.Count == 0)
            {
                return new List<Order>();
            }
            if (decisionOrders.Count != 2)
            {
                throw new SmartException(ExceptionImportanceLevel.MEDIUM, "RestorePlacedOrders", "StrategyAbst", "Number of orders in DB: " + decisionOrders.Count);
            }
            //CancelAllOrders();
            int pos = CurrentState.Position;
            double buyPrice, sellPrice, stopBuyPrice, stopSellPrice;
            int buyVol, sellVol;
            OrderTypeEnum buyType, sellType;
            if (decisionOrders[0].Action == ActionEnum.BUY)
            {
                buyPrice = decisionOrders[0].Price;
                sellPrice = decisionOrders[1].Price;
                stopBuyPrice = decisionOrders[0].StopPrice;
                stopSellPrice = decisionOrders[1].StopPrice;
            } else
            {
                buyPrice = decisionOrders[1].Price;
                sellPrice = decisionOrders[0].Price;
                stopBuyPrice = decisionOrders[1].StopPrice;
                stopSellPrice = decisionOrders[0].StopPrice;
            }
            buyType = stopBuyPrice == 0 ? OrderTypeEnum.LIMIT : OrderTypeEnum.STOP;
            sellType = stopSellPrice == 0 ? OrderTypeEnum.LIMIT : OrderTypeEnum.STOP;
            if (pos >= 0)
            {
                buyVol = ContractsToTrade - pos;
                sellVol = ContractsToTrade;
            } else
            {
                buyVol = ContractsToTrade;
                sellVol = ContractsToTrade + pos;
            }
            List<Order> restoreOrders = new List<Order>();
            if (buyVol > 0)
                restoreOrders.Add(new Order(Symbol, GenerateCookie(), "", buyVol, 0, buyPrice, stopBuyPrice, ActionEnum.BUY, buyType));
            if (sellVol > 0)
                restoreOrders.Add(new Order(Symbol, GenerateCookie(), "", sellVol, 0, sellPrice, stopSellPrice, ActionEnum.SELL, sellType));
            WriteToLogDB("RestorePlacedOrders", "Finished");
            return restoreOrders;
        }
        /// <summary>
        /// Подготавливает список заявок, которые необходимо выставить
        /// </summary>
        /// <returns>Список заявок Order</returns>
        protected abstract List<Order> PreparePlaceOrders();

        
        /// <summary>
        /// Подготавливает список заявок, которые необходимо отменить
        /// </summary>
        /// <returns>Список заявок Order</returns>
        protected List<Order> PrepareCancelOrders()
        {
            WriteToLogDB("PrepareCancelOrders", "Started");
            List<Order> cancelOrders = new List<Order>();
            for (int i = 0; i < CurrentState.BuyOrders.Count; i++)
            {
                cancelOrders.Add(new Order(Symbol, 0, CurrentState.BuyOrders[i].OrderId, CurrentState.BuyOrders[i].Volume, 0,
                    CurrentState.BuyOrders[i].Price, CurrentState.BuyOrders[i].StopPrice, ActionEnum.BUY, OrderTypeEnum.LIMIT));
            }
            for (int i = 0; i < CurrentState.SellOrders.Count; i++)
            {
                cancelOrders.Add(new Order(Symbol, 0, CurrentState.SellOrders[i].OrderId, CurrentState.SellOrders[i].Volume, 0,
                    CurrentState.SellOrders[i].Price, CurrentState.SellOrders[i].StopPrice, ActionEnum.SELL, OrderTypeEnum.LIMIT));
            }
            WriteToLogDB("PrepareCancelOrders", "Finished");
            return (cancelOrders);
        }

        protected void ExecDecision(List<Order> cancelOrders, List<Order> placeOrders)
        {
            WriteToLogDB("ExecDecision", "Started");
            List<Order> copyCancel = cancelOrders.Select(order => order.Clone()).ToList();
            List<Order> copyPlace = placeOrders.Select(order => order.Clone()).ToList();
            List<Order> moveOrders;
            try
            {
                moveOrders = OptimizeDecision(ref cancelOrders, ref placeOrders);
            } catch (Exception e)
            {
                cancelOrders = copyCancel;
                placeOrders = copyPlace;
                moveOrders = new List<Order>();
                EmailSender.SendEmail("Stocks Warning: Error in ExecDecision", e.Message);
            }
            
            string message = "Canceling orders " + Symbol + ": ";
            foreach (Order order in cancelOrders)
            {
                message += (order.OrderId + ", ");
            }
            if (cancelOrders.Count > 0)
                WriteToLogDB("ExecDecision", message);
            else
                WriteToLogDB("ExecDecision", "Canceled nothing");
            foreach (Order order in cancelOrders)
            {
                if (!string.IsNullOrEmpty(order.OrderId))
                {
                    ServerInstance.CancelOrder(order.Symbol, order.OrderId);
                }
                else
                {
                    WriteToLogDB("ExecDecision", "Error: cannot cancel order, no OrderId: Cookie = " + order.Cookie + ", Price = " + order.Price +
                        ", Volume = " + order.Volume);
                    InformUser("Cannot Cancel Order: Cookie = " + order.Cookie + "; ID = " + order.OrderId);
                }
                CurrentState.RemoveOrder(order.OrderId, order.Cookie);
            }
            message = "Placing orders: " + Symbol + ": Cookies: ";
            foreach (Order order in placeOrders)
            {
                if (order.Action == ActionEnum.BUY)
                {
                    CurrentState.BuyOrders.Add(order);
                }
                else if (order.Action == ActionEnum.SELL)
                {
                    CurrentState.SellOrders.Add(order);
                }
                else { throw new Exception("ExecDecision: " + Symbol + ": No other choices"); }
                message += (order.Cookie + ", ");
                ServerInstance.PlaceOrder(order);
            }
            if (placeOrders.Count > 0)
                WriteToLogDB("ExecDecision", message);
            else
                WriteToLogDB("ExecDecision", "Placed nothing");

            message = "Moving orders: " + Symbol + ": OrderIds: ";
            foreach (Order order in moveOrders)
            {
                message += (order.OrderId + ", ");
                ServerInstance.MoveOrder(order.OrderId, order.Price);
            }
            if (moveOrders.Count > 0)
                WriteToLogDB("ExecDecision", message);
            else
                WriteToLogDB("ExecDecision", "Moved nothing");

            WriteToLogDB("ExecDecision", "Finished");
        }

        /// <summary>
        /// Оптимизирует принятое решение с точки зрения процедуры отмены и выставления заявок. Изменяет cancelOrders и placeOrders
        /// </summary>
        /// <param name="cancelOrders"></param>
        /// <param name="placeOrders"></param>
        /// <returns>Список заявок, подлежащих переставлению (Move)</returns>
        protected List<Order> OptimizeDecision(ref List<Order> cancelOrders, ref List<Order> placeOrders)
        {
            /* Tuple<List<int>, List<int>> rms = RemoveSameOrders(cancelOrders, placeOrders);
            List<int> cancelIds = rms.Item1;
            List<int> placeIds = rms.Item2;
            cancelIds.Sort();
            cancelIds.Reverse();
            placeIds.Sort();
            placeIds.Reverse();
            if (cancelIds.Count != placeIds.Count)
                throw new SmartException(ExceptionImportanceLevel.MEDIUM, "OptimizeDecision", "StrategyAbst", "Impossible return of Remove Same Orders. Lengths differ.");
            foreach (int index in cancelIds)
            {
                cancelOrders.RemoveAt(index);
            }
            foreach (int index in placeIds)
            {
                placeIds.RemoveAt(index);
            } */
            RemoveSameOrders(ref cancelOrders, ref placeOrders);
            return CancelAndPlaceToMoveOrder(ref cancelOrders, ref placeOrders);
        }

        /// <summary>
        /// Удаляет все заявки, которые необходимо отменить и впоследствии выставить точно такую же
        /// </summary>
        /// <param name="cancelOrders">Заявки, подлежащие отмене</param>
        /// <param name="placeOrder">Заявки, подлежащие выставлению</param>
        private void RemoveSameOrders(ref List<Order> cancelOrders, ref List<Order> placeOrders)
        {
            WriteToLogDB("RemoveSameOrders", "Started");
            bool wasDeleted = true;
            while (wasDeleted)
            {
                wasDeleted = false;
                for (int i = 0; i < cancelOrders.Count; i++)
                {
                    for (int j = 0; j < placeOrders.Count; j++)
                    {
                        if ((Math.Abs(placeOrders[j].Price - cancelOrders[i].Price) <= 0.0001) &&
                            (placeOrders[j].Volume == cancelOrders[i].Volume) && (placeOrders[j].Action == cancelOrders[i].Action) &&
                            (Math.Abs(placeOrders[j].StopPrice - cancelOrders[i].StopPrice) <= 0.0001))
                        {
                            WriteToLogDB("RemoveSameOrders", "Removing From List OrderId = " + cancelOrders[i].OrderId + ": Price = " + cancelOrders[i].Price + ": Volume = " + cancelOrders[i].Volume);
                            WriteToLogDB("RemoveSameOrders", "Removing From List Cookie = " + placeOrders[j].Cookie + ": Price = " + placeOrders[j].Price + ": Volume = " + placeOrders[j].Volume);
                            wasDeleted = true;
                            cancelOrders.RemoveAt(i);
                            placeOrders.RemoveAt(j);
                            break;
                        }
                    }
                    if (wasDeleted) break;
                }
            }
            /* List<int> cancelIds = new List<int>();
            List<int> placeIds = new List<int>();
            for (int i = 0; i < cancelOrders.Count; i++)
            {
                for (int j = 0; j < placeOrders.Count; j++)
                {
                    if ((cancelIds.FindIndex(ind => ind == i) == -1) && (placeIds.FindIndex(ind => ind == j) == -1) && (AreSame(cancelOrders[i], placeOrders[j]))) {
                        cancelIds.Add(i);
                        placeIds.Add(j);
                    }
                }
            }*/ 
            WriteToLogDB("RemoveSameOrders", "Finished");
            //return new Tuple<List<int>, List<int>>(cancelIds, placeIds);
        }
        private bool AreSame(Order order1, Order order2)
        {
            return (Math.Abs(order1.Price - order2.Price) <= 0.0001) && (order1.Volume == order2.Volume) &&
                (order1.Action == order2.Action) && (Math.Abs(order1.StopPrice - order2.StopPrice) <= 0.0001);
        }
        /// <summary>
        /// Оптимизирует отмену с последующим выставлением заявки в изменение ее цены (если есть возможность)
        /// </summary>
        /// <param name="cancelOrders"></param>
        /// <param name="placeOrders"></param>
        /// <returns>Список заявок, подлежащих переставлению цены</returns>
        protected List<Order> CancelAndPlaceToMoveOrder(ref List<Order> cancelOrders, ref List<Order> placeOrders)
        {
            WriteToLogDB("CancelAndPlaceToMoveOrder", "Started");
            List<Order> moveOrders = new List<Order>();
            bool wasFound = true;
            while (wasFound)
            {
                wasFound = false;
                for (int i = 0; i < cancelOrders.Count; i++)
                {
                    for (int j = 0; j < placeOrders.Count; j++)
                    {
                        if ((placeOrders[j].Volume == cancelOrders[i].Volume) && (placeOrders[j].Action == cancelOrders[i].Action) &&
                            (Math.Abs(cancelOrders[i].StopPrice) <= 0.0001) && (Math.Abs(placeOrders[j].StopPrice) <= 0.0001))
                        {
                            WriteToLogDB("CancelAndPlaceToMoveOrder", "Removing OrderId = " + cancelOrders[i].OrderId + ": Price = " + cancelOrders[i].Price + ": Volume = " + cancelOrders[i].Volume);
                            WriteToLogDB("CancelAndPlaceToMoveOrder", "Removing Cookie = " + placeOrders[j].Cookie + ": Price = " + placeOrders[j].Price + ": Volume = " + placeOrders[j].Volume);
                            WriteToLogDB("CancelAndPlaceToMoveOrder", "Moving OrderId = " + cancelOrders[i].OrderId + ": Price = " + cancelOrders[i].Price + ": Volume = " + cancelOrders[i].Volume + ": To Price = " + placeOrders[j].Price);
                            wasFound = true;
                            moveOrders.Add(new Order(cancelOrders[i].Symbol, 0, cancelOrders[i].OrderId, cancelOrders[i].Volume,
                                cancelOrders[i].FilledVolume, placeOrders[j].Price, cancelOrders[i].StopPrice,
                                cancelOrders[i].Action, cancelOrders[i].Type));
                            cancelOrders.RemoveAt(i);
                            placeOrders.RemoveAt(j);
                            break;
                        }
                    }
                    if (wasFound) break;
                }
            }
            WriteToLogDB("CancelAndPlaceToMoveOrder", "Finished");
            return (moveOrders);
        }

        /// <summary>
        /// Отменяет все заявки и восстанавливает их из истории
        /// </summary>
        /// <returns>Возвращет true, если состояние удалось успешно восстановить</returns>
        protected bool DropAndRestoreOrders()
        {
            List<Order> lastDecision = DatabaseReader.GetLastDec(Symbol);
            if (lastDecision.Count == 0)
            {
                return false;
            }
            if (lastDecision.Count != 2)
            {
                throw new SmartException(ExceptionImportanceLevel.HIGH, "DropAndRestoreOrders", "StrategyAbst", "last Decision Count != 2: " + lastDecision.Count);
            }
            List<Order> placeOrders = RestorePlacedOrders(lastDecision);
            CancelAllOrders();
            foreach (Order order in placeOrders)
            {
                ServerInstance.PlaceOrder(order);
                if (order.Action == ActionEnum.BUY) CurrentState.BuyOrders.Add(order);
                else if (order.Action == ActionEnum.SELL) CurrentState.SellOrders.Add(order);
            }
            /* List<Order> cancelOrders = CurrentState.CloneBuyOrders();
            cancelOrders.AddRange(CurrentState.CloneSellOrders());
            ExecDecision(cancelOrders, placeOrders); */
            return true;
        }

        /* Блок проверок и поддержания целостности */
        protected virtual ConsistencyEvent CheckConsistency(bool toSendEmail)
        {
            ConsistencyEvent consistencyEvent;
            if (IsHugeBuyVolume()) consistencyEvent = new ConsistencyEvent(ConsistencyError.HUGE_BUY_VOLUME, Symbol, toSendEmail, CurrentState);
            else if (IsHugeSellVolume()) consistencyEvent = new ConsistencyEvent(ConsistencyError.HUGE_SELL_VOLUME, Symbol, toSendEmail, CurrentState);
            else if (IsAmountOfOrdersTooBig()) consistencyEvent = new ConsistencyEvent(ConsistencyError.BIG_AMT_OF_ORDERS, Symbol, toSendEmail, CurrentState);
            else if (IsPositionTooBig()) consistencyEvent = new ConsistencyEvent(ConsistencyError.BIG_OPENED_POS, Symbol, toSendEmail, CurrentState);
            else if (IsPlannedTooBig()) consistencyEvent = new ConsistencyEvent(ConsistencyError.BIG_PLANNED, Symbol, toSendEmail, CurrentState);
            else if (IsAmountOfBuyOrdersTooBig()) consistencyEvent = new ConsistencyEvent(ConsistencyError.BIG_AMT_OF_BUY_ORDERS, Symbol, toSendEmail, CurrentState);
            else if (IsAmountOfSellOrdersTooBig()) consistencyEvent = new ConsistencyEvent(ConsistencyError.BIG_AMT_OF_SELL_ORDERS, Symbol, toSendEmail, CurrentState);
            else if (IsTotalBuyVolumeTooBig()) consistencyEvent = new ConsistencyEvent(ConsistencyError.BIG_BUY_TOTAL_VOLUME, Symbol, toSendEmail, CurrentState);
            else if (IsTotalSellVolumeTooBig()) consistencyEvent = new ConsistencyEvent(ConsistencyError.BIG_SELL_TOTAL_VOLUME, Symbol, toSendEmail, CurrentState);
            else if (IsPlannedNotZero()) consistencyEvent = new ConsistencyEvent(ConsistencyError.PLANNED_NOT_ZERO, Symbol, toSendEmail, CurrentState);
            else consistencyEvent = new ConsistencyEvent(ConsistencyError.CONSISTENT, Symbol, toSendEmail, CurrentState);
            return (consistencyEvent);
        }
        protected virtual bool IsHugeBuyVolume()
        {
            return CurrentState.Position + CurrentState.GetTotalBuyVolume() >= ContractsToTrade * 2.5;
        }
        protected virtual bool IsHugeSellVolume()
        {
            return Math.Abs(CurrentState.Position - CurrentState.GetTotalSellVolume()) >= ContractsToTrade * 2.5;
        }
        protected virtual bool IsPlannedNotZero()
        {
            return (CurrentState.Planned != 0);
        }
        protected virtual bool IsAmountOfOrdersTooBig()
        {
            return (CurrentState.BuyOrders.Count + CurrentState.SellOrders.Count) > 2;
        }
        protected virtual bool IsAmountOfBuyOrdersTooBig()
        {
            return CurrentState.BuyOrders.Count > 1;
        }
        protected virtual bool IsAmountOfSellOrdersTooBig()
        {
            return CurrentState.SellOrders.Count > 1;
        }
        protected virtual bool IsPositionTooBig()
        {
            return (Math.Abs(CurrentState.Position) > ContractsToTrade);
        }
        protected virtual bool IsPlannedTooBig()
        {
            return (Math.Abs(CurrentState.Planned) > ContractsToTrade);
        }
        protected virtual bool IsTotalBuyVolumeTooBig()
        {
            return (Math.Abs(CurrentState.GetTotalBuyVolume() + CurrentState.Position) > ContractsToTrade ||
                Math.Abs(CurrentState.GetTotalBuyVolume()) > ContractsToTrade);
        }
        protected virtual bool IsTotalSellVolumeTooBig()
        {
            return (Math.Abs(-CurrentState.GetTotalSellVolume() + CurrentState.Position) > ContractsToTrade ||
                Math.Abs(-CurrentState.GetTotalSellVolume()) > ContractsToTrade);
        }
        public void HandleFinishConsistencyTimer(object sender, EventArgs e)
        {
            WriteToLogDB("HandleFinishConsistencyTimer", "Started");
            /*
             * bool toSendEmail = isSecondaryConsistencyCheck;
            ConsistencyEvent consistencyEvent = CheckConsistency(toSendEmail);
            if (consistencyEvent.ErrorCode != ConsistencyError.CONSISTENT)
            {
                InformUser("Inconsistent");
                WriteToLogDB("HandleFinishConsistencyTimer", "Found inconsistency. Error Code = " + consistencyEvent.ErrorCode + ". Calling Update CurrentState");
                WriteToLogDB("HandleFinishConsistencyTimer", "Current State: " + CurrentState.ToString());
                if (!isSecondaryConsistencyCheck)       // первичная проверка целостности
                {
                    WriteToLogDB("HandleFinishConsistencyTimer", "Current State is old. Needs an update");
                    // позиция давно не обновлялась, необходимо обновить
                    UpdatePositionFromServer();
                    isSecondaryConsistencyCheck = true;
                    InitializeConsistencyTimer(LastUpdateTimerInterval + 3000);
                } else      // вторичная проверка целостности
                {
                    // позиция достаточно новая, необходимо обрабатывать ошибку сервера
                    WriteToLogDB("HandleFinishConsistencyTimer", "Current State is new: " + LastPositionUpdateTime.ToString());
                    HandleConsistencyEvent(consistencyEvent);
                    InitializeConsistencyTimer(ConsistencyInterval);
                    isSecondaryConsistencyCheck = false;
                }
            }
            else
            {
                InformUser("Consistent");
                WriteToLogDB("HandleFinishConsistencyTimer", "Consistent");
                InitializeConsistencyTimer(ConsistencyInterval);
                isSecondaryConsistencyCheck = false;
            }
            */
            // на данном моменте позиция уже обновлена
            DateTime tm = ServerTime.GetRealTime();
            if (IsNowTradeTime() && (tm.Hour <= 23 && tm.Minute < 49))
            {
                ConsistencyEvent consistencyEvent = CheckConsistency(true);
                if (consistencyEvent.ErrorCode != ConsistencyError.CONSISTENT)
                {
                    WriteToLogDB("HandleFinishConsistencyTimer", "Inconsistency Found: " + consistencyEvent.ErrorCode);
                    InformUser("Inconsistency Found: " + consistencyEvent.ErrorCode);
                    HandleConsistencyEvent(consistencyEvent);
                }
                else
                {
                    WriteToLogDB("HandleFinishConsistencyTimer", "State is Consistent");
                    InformUser("State is Consistent");
                }
                //InitializeCheckNoPositionTimer(CheckNoPositionInterval);
            }
            WriteToLogDB("HandleFinishConsistencyTimer", "Finished");
        }
        protected void HandleConsistencyEvent(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleConsistencyEvent", "Started. Event = " + ev.ToString());
            if (IsNowTradeTime())
            {
                if (ev.ErrorCode == ConsistencyError.HUGE_BUY_VOLUME)
                {
                    HandleHugeBuyVolume(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.HUGE_SELL_VOLUME)
                {
                    HandleHugeSellVolume(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.BIG_AMT_OF_ORDERS)
                {
                    HandleBigAmountOfOrders(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.BIG_OPENED_POS)
                {
                    HandleBigOpenedPosition(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.BIG_PLANNED)
                {
                    HandleBigPlanned(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.BIG_BUY_TOTAL_VOLUME)
                {
                    HandleBigBuyTotalVolume(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.BIG_SELL_TOTAL_VOLUME)
                {
                    HandleBigSellTotalVolume(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.BIG_AMT_OF_BUY_ORDERS)
                {
                    HandleBigAmountOfBuyOrders(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.BIG_AMT_OF_SELL_ORDERS)
                {
                    HandleBigAmountOfSellOrders(ev);
                }
                else if (ev.ErrorCode == ConsistencyError.PLANNED_NOT_ZERO)
                {
                    HandlePlannedNotZero(ev);
                }
                else
                {
                    throw new SmartException(ExceptionImportanceLevel.MEDIUM, "HandleConsistencyEvent", "StrategyAbst", "No other choice for ev.ErrorCode: " + ev.ErrorCode);
                }
            }
            WriteToLogDB("HandleConsistencyEvent", "Finished");
        }

        protected virtual void HandleBigAmountOfOrders(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleBigAmountOfOrders", "Started");
            bool isSuccess = DropAndRestoreOrders();
            if (!isSuccess)
            {
                string message = "Cannot restore consistency";
                throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigAmountOfOrders", "StrategyAbst", message);
            }
            WriteToLogDB("HandleBigAmountOfOrders", "Finished");
        }
        protected virtual void HandleHugeBuyVolume(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleHugeBuyVolume", "Started");
            CancelAllOrders();
            string message = "Huge Buy Volume found. Program interrupted. User actions are strongly needed!";
            WriteToLogDB("HandleHugeBuyVolume", "Finished");
            throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleHugeBuyVolume", "StrategyAbst", message);
        }
        protected virtual void HandleHugeSellVolume(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleHugeSellVolume", "Started");
            CancelAllOrders();
            string message = "Huge Sell Volume found. Program interrupted. User actions are strongly needed!";
            WriteToLogDB("HandleHugeSellVolume", "Finished");
            throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleHugeSellVolume", "StrategyAbst", message);
        }
        protected virtual void HandleBigPlanned(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleBigPlanned", "Started");
            bool isSuccess = DropAndRestoreOrders();
            if (!isSuccess)
            {
                string message = "Cannot restore consistency";
                throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigAmountOfOrders", "StrategyAbst", message);
            }
            WriteToLogDB("HandleBigPlanned", "Started");
        }

        protected virtual void HandleBigAmountOfBuyOrders(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleBigAmountOfBuyOrders", "Started");
            bool isSuccess = DropAndRestoreOrders();
            if (!isSuccess)
            {
                string message = "Cannot restore consistency";
                throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigAmountOfOrders", "StrategyAbst", message);
            }
            WriteToLogDB("HandleBigAmountOfBuyOrders", "Finished");
        }

        protected virtual void HandleBigAmountOfSellOrders(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleBigAmountOfSellOrders", "Started");
            bool isSuccess = DropAndRestoreOrders();
            if (!isSuccess)
            {
                string message = "Cannot restore consistency";
                throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigAmountOfOrders", "StrategyAbst", message);
            }
            WriteToLogDB("HandleBigAmountOfSellOrders", "Finished");
        }

        protected virtual void HandleBigBuyTotalVolume(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleBigBuyTotalVolume", "Started");
            bool isSuccess = DropAndRestoreOrders();
            if (!isSuccess)
            {
                string message = "Cannot restore consistency";
                throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigAmountOfOrders", "StrategyAbst", message);
            }
            WriteToLogDB("HandleBigBuyTotalVolume", "Finished");
        }

        protected virtual void HandleBigSellTotalVolume(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleBigSellTotalVolume", "Started");
            bool isSuccess = DropAndRestoreOrders();
            if (!isSuccess)
            {
                string message = "Cannot restore consistency";
                throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigAmountOfOrders", "StrategyAbst", message);
            }
            WriteToLogDB("HandleBigSellTotalVolume", "Finished");
        }
        protected virtual void HandlePlannedNotZero(ConsistencyEvent ev)
        {
            WriteToLogDB("HandlePlannedNotZero", "Started");
            bool isSuccess = DropAndRestoreOrders();
            if (!isSuccess)
            {
                string message = "Cannot restore consistency";
                throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigAmountOfOrders", "StrategyAbst", message);
            }
            WriteToLogDB("HandleBigSellTotalVolume", "Finished");
        }
        protected virtual void HandleBigOpenedPosition(ConsistencyEvent ev)
        {
            WriteToLogDB("HandleBigOpenedPosition", "Started");
            string message = "Opened big position on " + Symbol + ". Have no permission to handle it. All orders canceled. User actions are strongly needed.";
            CancelAllOrders();
            WriteToLogDB("HandleBigOpenedPosition", "Finished");
            throw new SmartException(ExceptionImportanceLevel.HIGH, "HandleBigOpenedPosition", "StrategyAbst", message);
            //WriteToLogDB("HandleBigOpenedPosition", "Finished");
        }
    }
}
