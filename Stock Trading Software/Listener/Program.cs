using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listener
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Listener started ...");
            List<Query> quers = new List<Query>();
            quers.Add(new Query("Si-12.18_FT", 5, 75));
            quers.Add(new Query("VTBR-12.18_FT", 5, 50));
            quers.Add(new Query("SBRF-12.18_FT", 5, 125));
            quers.Add(new Query("SBPR-12.18_FT", 5, 50));
            quers.Add(new Query("GAZR-12.18_FT", 5, 50));
            quers.Add(new Query("ROSN-12.18_FT", 5, 50));
            quers.Add(new Query("MOEX-12.18_FT", 5, 75));
            quers.Add(new Query("GOLD-12.18_FT", 5, 150));
            quers.Add(new Query("HYDR-12.18_FT", 15, 20));
            quers.Add(new Query("GMKR-12.18_FT", 15, 20));
            quers.Add(new Query("TATN-12.18_FT", 15, 20));
            quers.Add(new Query("MGNT-12.18_FT", 15, 40));
            quers.Add(new Query("RTKM-12.18_FT", 15, 20));
            quers.Add(new Query("UJPY-12.18_FT", 15, 20));
            quers.Add(new Query("MXI-12.18_FT", 5, 75));
            quers.Add(new Query("SILV-12.18_FT", 5, 3));
            Listener listener = new Listener(quers);
            listener.StartWork();

            while (true)
            {
                string cmd = Console.ReadLine();
                if (cmd.Equals("exit"))
                    return;
                else
                    Console.WriteLine("Unknown command");
            }
        }
    }
}
