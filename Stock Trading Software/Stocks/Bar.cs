using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public class Bar
    {
        public DateTime StartTime { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double Low { get; set; }
        public double High { get; set; }
        public double Volume { get; set; }
        public double WeightedSum
        {
            get
            {
                double sum = 0;
                double volume = 0;
                foreach (Contract tick in Ticks)
                {
                    sum += tick.Price * tick.Volume;
                    volume += tick.Volume;
                }
                return sum / volume;
            }
        }

        public List<Contract> Ticks { get; set; }

        public const String DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public Bar()
        {
            StartTime = ServerTime.GetRealTime();
            Ticks = new List<Contract>();
        }

        public Bar(double open, double close, double low, double high, double volume, String startTime, List<Contract> ticks)
        {
            Open = open;
            Close = close;
            Low = low;
            High = high;
            Volume = volume;
            StartTime = DateTime.ParseExact(startTime, DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
            Ticks = ticks;
        }

        public Bar(double open, double close, double low, double high, double volume, DateTime startTime, List<Contract> ticks)
        {
            Open = open;
            Close = close;
            Low = low;
            High = high;
            Volume = volume;
            StartTime = startTime;
            Ticks = ticks;
        }
        public Bar(double open, double close, double low, double high, double volume, DateTime startTime)
        {
            Open = open;
            Close = close;
            Low = low;
            High = high;
            Volume = volume;
            StartTime = startTime;
            Ticks = new List<Contract>();
        }
        public Bar(Bar bar)
        {
            Open = bar.Open;
            Close = bar.Close;
            Low = bar.Low;
            High = bar.High;
            Volume = bar.Volume;
            StartTime = bar.StartTime;
            Ticks = bar.Ticks;
        }

        public void AddTick(Contract tick)
        {
            Ticks.Add(tick);
        }
        public override string ToString()
        {
            return StartTime + ": " + Open + " " + High + " " + Low + " " + Close + "\r\n";
        }
    }
}
