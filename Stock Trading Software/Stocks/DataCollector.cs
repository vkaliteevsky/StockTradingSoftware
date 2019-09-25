using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks
{
    public class DataCollector
    {
        private List<DataRequest> DataRequests;
        public delegate void FinishHandler(string symbol, StBarInterval interval, List<Bar> bars);
        public event FinishHandler BarsCollected;
        public DataCollector()
        {
            DataRequests = new List<DataRequest>();
        }
        public void InitDataRequest(string symbol, StBarInterval interval, System.DateTime since, int count)
        {
            if (DataRequestExists(symbol, interval, since, count))
                throw new SmartException(ExceptionImportanceLevel.HIGH, "InitRequest", "DataCollector", "Already exists!");
            DataRequests.Add(new DataRequest(symbol, interval, since, count));
        }
        public void AddBar(int row, int nrows, string symbol, StBarInterval interval, System.DateTime datetime,
            double open, double high, double low, double close, double volume, double open_int)
        {
            int index = FindIndexOfDataRequest(symbol, interval, nrows);
            if (index == -1)
            {
                string message = "Can't find: symbol = " + symbol + ", interval = " + interval + ", nrows = " + nrows;
                throw new SmartException(ExceptionImportanceLevel.LOW, "AddBar", "DataCollector", message);
            } 
            Bar bar = new Bar(open, close, low, high, volume, datetime);
            DataRequests[index].AddBar(bar);
            if (Math.Abs(row) == DataRequests[index].TotalCount - 1)
            {
                DataRequests[index].UserSort();
                //Server server = Server.GetInstance();
                //server.NotifyBarsCollected(DataRequests[index].Bars, symbol);
                BarsCollected(DataRequests[index].Symbol, DataRequests[index].Interval, DataRequests[index].Bars);
                DataRequests.RemoveAt(index);
            }
        }
        public bool DataRequestExists(string symbol, StBarInterval interval, System.DateTime since, int count)
        {
            return FindIndexOfDataRequest(symbol, interval, count) != -1;
        }
        public int FindIndexOfDataRequest(string symbol, StBarInterval interval, int count)
        {
            //int index = DataRequests.FindIndex(req => (req.Symbol == symbol) && (req.Interval == interval) && (req.TotalCount == Math.Abs(count)));
            int index = DataRequests.FindIndex(req => req.Symbol == symbol);
            return (index);
        }
    }
    class DataRequest
    {
        public int TotalCount { get; }
        public DateTime SinceTime { get; }
        public StBarInterval Interval {get;}
        public string Symbol { get; set; }
        public List<Bar> Bars;
        public DataRequest(string symbol, StBarInterval interval, System.DateTime since, int count)
        {
            TotalCount = Math.Abs(count);
            Interval = interval;
            SinceTime = since;
            Symbol = symbol;
            Bars = new List<Bar>();
        }
        public void AddBar(Bar bar)
        {
            if (Bars.Count > TotalCount)
                throw new SmartException(ExceptionImportanceLevel.LOW, "AddBar", "DataRequest", "Bars.Count > TotalCount");
            Bars.Add(bar);
        }
        public void UserSort()
        {
            Bars.Sort(delegate(Bar x, Bar y) {
                if (x.StartTime < y.StartTime) return (-1);
                else if (x.StartTime == y.StartTime) return 0;
                else return 1;
            });
        }
    }
}
