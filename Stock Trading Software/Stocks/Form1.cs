using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Stocks.Strategies;

namespace Stocks
{
    public partial class MainForm : Form
    {
        private System.Timers.Timer ReportTimer;
        public MainForm ()
        {
            InitializeComponent();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            Server server = Server.GetInstance(this);
            if (server == null)
            {
                WriteToError("Server is null");
                throw new SmartException(ExceptionImportanceLevel.HIGH, "connectButton_Click", "Form1", "Server is null");
            }
            server.StartWork();
            //PortfolioManager portManager = PortfolioManager.GetInstance();
            List<StrategyParams> pars = ConfigReader.ReadConfig();
            //portManager.TargetWs = pars.Select(par => par.StrategicWeight).ToArray();
            //int[] contractsToTrade = portManager.CalcContractsToTrade(new double[] { 100, 300 });
            foreach (StrategyParams par in pars)
            {
                if (par.ContractsToTrade > 0)
                    StrategyFactory.CreateStrategy(par);
            }
            BindingSource bs = new BindingSource();
            List<string> supportedStrats = server.GetStrategies().Select(strat => strat.Name).ToList();
            stratListBox.DataSource = supportedStrats;

            ReportTimer = new System.Timers.Timer(120 * 1000);
            ReportTimer.AutoReset = false;
            ReportTimer.Elapsed += ReportTimer_Elapsed;
            ReportTimer.Start();
            /* System.Threading.Thread.Sleep(25000);
            foreach (StrategyAbst strat in server.GetStrategies())
            {
                strat.StartWork();
            }
            */
        }

        private void ReportTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<StrategyAbst> strats = Server.GetInstance().GetStrategies();
            string heading = "Robot Started - Report";
            string text = "";
            for (int i = 0; i < strats.Count; i++)
            {
                if (strats[i].GO <= 0)
                {
                    text += "Strategy " + strats[i].Name + " is not connected! User check is strongly advised.\r\n";
                } else if (strats[i].GO > 0)
                {
                    text += "Strategy " + strats[i].Name + " is connected.\r\n";
                }
            }
            text = "Report\r\n" + text;
            DateTime thisTime = ServerTime.GetRealTime();
            if (thisTime.Hour < 11)
            {
                EmailSender.SendEmail(heading, text);
            }
        }

        delegate void WriteToStatusDelegate(string text);
        delegate void WriteToErrorDelegate(string text);
        private void getBarsButton_Click(object sender, EventArgs e)
        {
            DBInputOutput.DBReader dbReader = new DBInputOutput.DBReader();
            Server server = Server.GetInstance();
            PortfolioManager portManager = PortfolioManager.GetInstance();
            List<StrategyParams> pars = ConfigReader.ReadConfig();
            double[] ws = pars.Select(par => par.ContractsToTrade > 0 ? par.StrategicWeight : -1).Where(w => w >= 0).ToArray();
            double[] mults = pars.Select(par => par.ContractsToTrade > 0 ? par.Mult : -1).Where(m => m != -1).ToArray();
            List<string> stratNames = pars.Select(par => par.ContractsToTrade > 0 ? par.Name : "").Where(s => !s.Equals("")).ToList();
            double[] gos = stratNames.Select(name => server.GetStrategies().Find(strat => strat.Name.Equals(name)).GO).ToArray();
            double[] ps = stratNames.Select(name => dbReader.SelectLastPrice(server.GetStrategies().Find(strat => strat.Name.Equals(name)).Symbol).Close).ToArray();
            double[] steps = pars.Select(par => par.ContractsToTrade > 0 ? par.Step : -1).Where(s => s != -1).ToArray();
            for (int i = 0; i < ws.Length; i++)
            {
                ws[i] = ws[i] / (ps[i] * mults[i] / gos[i]);
            }
            double sm = ws.Sum();
            portManager.TargetWs = ws.Select(w => w / sm).ToArray();
            //portManager.TargetWs = pars.Select(par => par.ContractsToTrade > 0 ? par.StrategicWeight : -1).Where(w => w >= 0).ToArray();
            if (Math.Abs(portManager.TargetWs.Sum() - 1.0) >= 0.001)
            {
                WriteToError("Sum of TargetWs != 1. Cannot optimize");
                return;
            }

            int[] contractsToTrade = portManager.CalcContractsToTrade(gos);
            string message = "Nearest Weights:\r\n";

            double sumToTrade = 0.0;
            double goToTrade = 0.0;
            for (int i = 0; i < stratNames.Count; i++)
            {
                sumToTrade += contractsToTrade[i] * ps[i] * mults[i];
                goToTrade += contractsToTrade[i] * gos[i];
            }
                

            for (int i = 0; i < stratNames.Count; i++)
            {
                double pct = Math.Round(contractsToTrade[i] * ps[i] * mults[i] / sumToTrade, 4) * 100;
                message += stratNames[i] + ": " + contractsToTrade[i] + " * " + ps[i] * mults[i] + " = " + contractsToTrade[i] * ps[i] * mults[i] + 
                    " ₽ (" + pct + "%)\r\n";
            }
            message += "Total Sum: " + sumToTrade + " ₽ \r\n";
            message += "Total Money Used: " + goToTrade + " ₽ \r\n";
            WriteToError(message);
        }
        public void WriteToStatus(string message)
        {
            if (infoTextBox.InvokeRequired)
            {
                WriteToStatusDelegate d = new WriteToStatusDelegate(WriteToStatus);
                Invoke(d, new object[] { message });
            }
            else
            {
                infoTextBox.AppendText(ServerTime.GetRealTime() + ": " + message + "\n");
            }
        }
        public void WriteToError(string msg)
        {
            string message = msg;
            if (errorTextBox.InvokeRequired)
            {
                WriteToErrorDelegate d = new WriteToErrorDelegate(WriteToStatus);
                Invoke(d, new object[] { message });
            }
            else
            {
                errorTextBox.AppendText(message + "\n");
            }
        }

        private void testButton_Click(object sender, EventArgs e)
        {
            //WriteToStatus("Test called.");
            /* foreach (StrategyAbst strat in Server.GetInstance().GetStrategies())
            {
                WriteToStatus(strat.CurrentState.ToString());
            } */
            /* DBInputOutput.DBReader dbReader = new DBInputOutput.DBReader();
            DateTime dTime = dbReader.SelectDecisionTimes("GAZR-12.18_FT");
            WriteToStatus("Found time: " + dTime);
            List<DateTime> dTimes = new List<DateTime>();
            dTimes.Add(new DateTime(2018, 1, 26, 11, 00, 00));
            dTimes.Add(new DateTime(2018, 1, 26, 09, 50, 00));
            dTimes.Add(new DateTime(2018, 1, 26, 18, 40, 00));
            dTimes.Add(new DateTime(2018, 1, 26, 18, 50, 00));
            dTimes.Add(new DateTime(2018, 1, 26, 20, 0, 0));
            dTimes.Add(new DateTime(2018, 1, 26, 23, 54, 00));
            foreach (DateTime dTime in dTimes)
            {
                DateTime res = Server.GetInstance().GetStrategies()[0].CalcMakeDicisionTime(dTime, 16 * 60 * 60);
                WriteToStatus(res.ToString());
            }*/
            Server server = Server.GetInstance();
            string stratName = stratListBox.Text;
            StrategyAbst stratFound = server.GetStrategies().Find(strat => strat.Name == stratName);
            if (stratFound == null)
            {
                WriteToStatus("Strategy not found\r\n");
            }
            else
            {
                WriteToStatus("\r\n" + stratFound.ToString());
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Show();
            connectButton_Click(new object(), null);
        }

        private void clearPositionButton_Click(object sender, EventArgs e)
        {
            Server server = Server.GetInstance();
            foreach (StrategyAbst strat in server.GetStrategies())
            {
                strat.CancelAllOrders();
            }
        }

        private void getPositionButton_Click(object sender, EventArgs e)
        {
            Server server = Server.GetInstance();
            List<StrategyAbst> strats = server.GetStrategies();
            if (strats.Count > 0)
            {
                strats[0].UpdatePositionFromServer();
            }
            /* foreach (StrategyAbst strat in server.GetStrategies())
            {
                strat.UpdatePositionFromServer();
            } */
        }

        private void ClearTextButton_Click(object sender, EventArgs e)
        {
            Server server = Server.GetInstance();
            foreach (StrategyAbst strat in server.GetStrategies())
            {
                strat.CloseMarket();
            }
        }

        private void clearStratButton_Click(object sender, EventArgs e)
        {
            Server server = Server.GetInstance();
            string stratName = stratListBox.Text;
            StrategyAbst stratFound = server.GetStrategies().Find(strat => strat.Name == stratName);
            if (stratFound != null)
            {
                stratFound.ClearCurrentPosition();
                WriteToStatus(stratFound.Symbol + ": Position Cleared");
            } else
            {
                WriteToStatus(stratFound.Symbol + ": Strategy Not Found");
            }
        }

        private void updateStrat_Click(object sender, EventArgs e)
        {
            /*Server server = Server.GetInstance();
            string stratName = stratListBox.Text;
            StrategyAbst stratFound = server.GetStrategies().Find(strat => strat.Name == stratName);
            if (stratFound != null)
            {
                stratFound.UpdatePositionFromServer();
                WriteToStatus(stratFound.Symbol + ": Position Updated");
            } else
            {
                WriteToStatus(stratFound.Symbol + ": Strategy Not Found");
            }
            */
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {
            string message = "***********Report All***********\r\n";
            foreach (StrategyAbst strat in Server.GetInstance().GetStrategies())
            {
                message += strat.Symbol + ": Position: " + strat.CurrentState.Position + " | Planned: " + strat.CurrentState.Planned;
                message += " | Contracts to Trade: " + strat.ContractsToTrade + "\r\n";
            }
            message += "\r\n";
            WriteToStatus(message);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
