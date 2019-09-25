using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            Monitor monitor = new Monitor();
            Console.WriteLine("Monitor Started ...");
            while (true)
            {
                string cmd = Console.ReadLine();
                if (cmd.Equals("check stocks"))
                {
                    StocksConsistency csy = monitor.CheckStocksConsistency();
                    Console.WriteLine(DateTime.Now.ToString() + ": Check Result - " + csy);
                    if (csy != StocksConsistency.CONSISTENT)
                    {
                        Monitor.RestartProcess("Stocks");
                    }
                }
                    
                else if (cmd.Equals("check listener"))
                {
                    ListenerConsistency csy = monitor.CheckListenerConsistency();
                    Console.WriteLine(DateTime.Now.ToString() + ": Check Result - " + csy);
                    if (csy != ListenerConsistency.CONSISTENT)
                    {
                        Monitor.RestartProcess("Listener");
                    }
                }
                    
                else if (cmd.Equals("restart listener"))
                {
                    Monitor.RestartProcess("Listener");
                }
                else if (cmd.Equals("restart stocks"))
                {
                    Monitor.RestartProcess("Stocks");
                } else if (cmd.Equals("clear"))
                {
                    monitor.ClearDB();
                }
                else if (cmd.Equals("exit"))
                    return;
                else
                    Console.WriteLine("Unknown command");
            }
        }
    }
}
