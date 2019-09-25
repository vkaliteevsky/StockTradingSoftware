using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Monitor
{
    public enum StocksConsistency
    {
        CONSISTENT = 0,
        INCONSISTENT = 1
    }
    public enum ListenerConsistency
    {
        CONSISTENT = 0, 
        NO_PING = 1,
        NO_TICKS = 2,
        NO_BARS = 3
    }
    class Monitor
    {
        private List<DateTime> DayOffs;

        private Timer CheckTimer;
        private const int CheckInterval = 3 * 60 * 1000;
        private Timer CheckEnd;
        private const int CheckEndInterval = 1 * 45 * 1000;

        private Stocks.DBInputOutput.DBReader dbReader;
        public Stocks.DBInputOutput.DBWriter dbWriter;
        
        public Monitor()
        {
            dbReader = new Stocks.DBInputOutput.DBReader();
            dbWriter = new Stocks.DBInputOutput.DBWriter();

            CheckTimer = new Timer(CheckInterval);
            CheckTimer.Elapsed += HandleCheckTimer;
            CheckTimer.AutoReset = true;
            CheckTimer.Start();

            CheckEnd = new Timer(CheckEndInterval);
            CheckEnd.Elapsed += CheckEnd_Elapsed;
            CheckEnd.AutoReset = true;
            CheckEnd.Start();

            DayOffs = dbReader.SelectDayOffs(Stocks.ServerTime.GetRealTime());
            DateTime thisTime = Stocks.ServerTime.GetRealTime();
            if (IsDayOff(thisTime) || IsEndOfWork())
            {
                Environment.Exit(0);
            }
        }

        private void CheckEnd_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsEndOfWork())
            {
                KillProcess("Listener");
                //ClearDB();
                Environment.Exit(0);
            }
        }
        public void ClearDB()
        {
            DateTime thisTime = Stocks.ServerTime.GetRealTime().Date;
            DateTime beforeTime = thisTime.AddDays(-10);
            dbWriter.TruncateTrades(beforeTime);
            /* try
            {
                dbWriter.TruncateTrades(beforeTime);
            } catch (Exception e)
            {
                Stocks.EmailSender.SendEmail(e);
            }
            try
            {
                dbWriter.TruncateOrderBook(beforeTime);
            } catch (Exception e)
            {
                Stocks.EmailSender.SendEmail(e);
            }
            try
            {
                dbWriter.TruncateGeneral(beforeTime);
            } catch (Exception e)
            {
                Stocks.EmailSender.SendEmail(e);
            }*/

        }
        private bool IsDayOff(DateTime dTime)
        {
            return (dTime.DayOfWeek == DayOfWeek.Sunday
                || (DayOffs.FindIndex(dt => dt.Day == dTime.Day && dt.Month == dTime.Month && dt.Year == dTime.Year) != -1));
        }
        private static void KillProcess(string name)
        {
            if (name != "Listener" && name != "Stocks")
            {
                throw new Stocks.SmartException(Stocks.ExceptionImportanceLevel.MEDIUM, "KillProcess", "Monitor", "Don't know name = " + name);
            }
            else
            {
                foreach (System.Diagnostics.Process proc in System.Diagnostics.Process.GetProcessesByName(name))
                {
                    proc.Kill();
                }
                Console.WriteLine("Process " + name + " killed.");
            }
        }

        private static void StartProcess(string name)
        {
            if (name != "Listener" && name != "Stocks")
            {
                throw new Stocks.SmartException(Stocks.ExceptionImportanceLevel.MEDIUM, "StartProcess", "Monitor", "Don't know name = " + name);
            }
            else
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("..\\..\\..\\" + name + "\\bin\\Debug\\" + name + ".exe");
                startInfo.WorkingDirectory = "..\\..\\..\\" + name + "\\bin\\Debug";
                System.Diagnostics.Process.Start(startInfo);
            }
        }

        public static void RestartProcess(string name)
        {
            if (name != "Listener" && name != "Stocks")
            {
                throw new Stocks.SmartException(Stocks.ExceptionImportanceLevel.MEDIUM, "RestartProcess", "Monitor", "Don't know name = " + name);
            } else
            {
                KillProcess(name);
                StartProcess(name);
            }
        }

        private bool IsEndOfWork()
        {
            DateTime dTime = Stocks.ServerTime.GetRealTime();
            return (dTime.Hour < 9 || (dTime.Hour == 23 && dTime.Minute >= 50));
        }

        private void HandleCheckTimer(object sender, EventArgs e)
        {
            DateTime thisTime = Stocks.ServerTime.GetRealTime();
            if (thisTime.Hour < 10 || (thisTime.Hour == 18 && thisTime.Minute >= 45))
            {
                Console.WriteLine("No Check is Made - Not a Trade Time");
                return;
            }
            StocksConsistency consistency;
            try
            {
                consistency = CheckStocksConsistency();
                Console.WriteLine(Stocks.ServerTime.GetRealTime().ToString() + ": Robot: Check Result - " + consistency);
                if (consistency == StocksConsistency.INCONSISTENT)
                {
                    InformManager("Monitor: Stocks Interrupted", "Program stopped working. Will be restarted by Monitor");
                    RestartProcess("Stocks");
                }

                ListenerConsistency listenerConsistency = CheckListenerConsistency();
                Console.WriteLine(DateTime.Now.ToString() + ": Listener: Check Result - " + listenerConsistency);
                if (listenerConsistency != ListenerConsistency.CONSISTENT)
                {
                    InformManager("Monitor: Listener Interrupted", "Cannot ping Listener. Will be restarted by Monitor");
                    RestartProcess("Listener");
                }
            } catch (Exception)
            {
                InformManager("Monitor: Something went wrong", "Database Request Failed. User actions needed.");
            }

        }
        public StocksConsistency CheckStocksConsistency()
        {
            List<Tuple<DateTime, string, string>> sts = dbReader.SelectSts("Stocks", 1, "Working");
            DateTime lastTimePing = sts[0].Item1;
            DateTime thisTime = Stocks.ServerTime.GetRealTime();
            if ((thisTime - lastTimePing).TotalSeconds >= 6 * 60)
            {
                return StocksConsistency.INCONSISTENT;
            }
            return StocksConsistency.CONSISTENT;
            /* DateTime sinceTime = DateTime.Now.AddMinutes(-10);
            //Tuple<List<DateTime>, List<double>> res = dbReader.ReadInfo("weight", sinceTime);
            List<DateTime> res = dbReader.ReadGeneral(sinceTime);
            return res.Count > 0 ? StocksConsistency.CONSISTENT : StocksConsistency.INCONSISTENT; */
        }
        public ListenerConsistency CheckListenerConsistency()
        {
            //List<Tuple<DateTime, string, string>> general = dbReader.SelectGeneral("Listener", 1);
            List<Tuple<DateTime, string, string>> sts = dbReader.SelectSts("Listener", 1, "Listening");
            DateTime lastTimePing = sts[0].Item1;
            DateTime thisTime = Stocks.ServerTime.GetRealTime();
            if ((thisTime - lastTimePing).TotalSeconds >= 3 * 60)
            {
                return ListenerConsistency.NO_PING;
            }
            return ListenerConsistency.CONSISTENT;
        }
        public void InformManager(string heading, string text)
        {
            Stocks.EmailSender.SendEmail(heading, text);
        }
    }
}
