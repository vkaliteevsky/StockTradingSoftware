using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public class Contract
    {
        public int TickerId { get; set; }
        public DateTime DTime { get; set; }
        public double Price { get; set; }
        public double Volume { get; set; }

        public Contract(int tickerId, DateTime dtime, double price, double volume)
        {
            TickerId = tickerId;
            DTime = dtime;
            Price = price;
            Volume = volume;
        }
    }
}
