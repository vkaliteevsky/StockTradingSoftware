using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    abstract class TrendStrat : StrategyAbst
    {
        public TrendStrat(string name, string symbol, int contractsToTrade, double step) : base(name, symbol, contractsToTrade, step)
        {

        }
        protected abstract double Slip();
        protected override List<Order> PreparePlaceOrders()
        {
            WriteToLogDB("PreparePlaceOrders", "Started");
            if (Bars.Count < 3)
            {
                throw new SmartException(ExceptionImportanceLevel.MEDIUM, "PreparePlaceOrders", "TrendStrat", Symbol + ": Bars are empty or has less than three elements!");
            }
            if (AmountOfVolatEstim() > AmountOfSkipBars())
            {
                throw new SmartException(ExceptionImportanceLevel.MEDIUM, "PreparePlaceOrders", "TrendStrat", Symbol + ": AmountOfVolatEstim > AmountOfSkipBars");
            }
            double[] closes = Bars.Select(bar => bar.Close).ToArray();
            double[] ys = new double[Bars.Count - 1];
            for (int i = 0; i < Bars.Count - 1; i++)
                ys[i] = closes[i + 1] / closes[i] - 1;
            ys = ys.Skip(Bars.Count - 1 - AmountOfVolatEstim()).ToArray();
            double sdev = CalcVolat(ys);
            double alpha = AlphaSpread() * Math.Sqrt(AmountOfSkipBars()) * sdev;
            //double lastPrice = Bars.Last().Close;
            double lastPrice = 0.0;
            try
            {
                Bar bar = DatabaseReader.SelectLastPrice(Symbol);
                lastPrice = bar.Close;
            }
            catch (Exception e)
            {
                lastPrice = Bars.Last().Close;
            }
            double buyStopPrice = RoundToStep(lastPrice * (1 + alpha));
            double sellStopPrice = RoundToStep(lastPrice * (1 - alpha));
            int buyVol = 0;
            int sellVol = 0;
            if (CurrentState.Position == 0)
            {
                buyVol = ContractsToTrade;
                sellVol = ContractsToTrade;
            }
            else if (CurrentState.Position < 0)
            {
                buyVol = ContractsToTrade;
                sellVol = ContractsToTrade - Math.Abs(CurrentState.Position);
            }
            else
            {
                buyVol = ContractsToTrade - Math.Abs(CurrentState.Position);
                sellVol = ContractsToTrade;
            }
            double buyPrice = buyStopPrice + Slip();
            double sellPrice = sellStopPrice - Slip();

            if (buyStopPrice <= sellStopPrice || (buyStopPrice <= 0 && buyVol != 0) || (sellStopPrice <= 0 && sellVol != 0))
            {
                throw new SmartException(ExceptionImportanceLevel.HIGH, "PreparePlaceOrders", "TrendStrat", "buyPrice = " + buyStopPrice + ", sellPrice = " + sellStopPrice);
            }
            WriteToLogDB("PreparePlaceOrders", "Buy: Price = " + buyStopPrice + ", Volume = " + buyVol + "; Sell: Price = " + sellStopPrice + ", Volume = " + sellVol);
            List<Order> placeOrders = new List<Order>();

            DateTime dTime = ServerTime.GetRealTime();
            DatabaseWriter.InsertDecision(dTime, Symbol, ActionEnum.BUY, buyPrice, buyVol, buyStopPrice);
            DatabaseWriter.InsertDecision(dTime, Symbol, ActionEnum.SELL, sellPrice, sellVol, sellStopPrice);

            if (buyVol > 0)
                placeOrders.Add(new Order(Symbol, GenerateCookie(), "", buyVol, 0, buyPrice, buyStopPrice, ActionEnum.BUY, OrderTypeEnum.STOP));
            if (sellVol > 0)
                placeOrders.Add(new Order(Symbol, GenerateCookie(), "", sellVol, 0, sellPrice, sellStopPrice, ActionEnum.SELL, OrderTypeEnum.STOP));

            WriteToLogDB("PreparePlaceOrders", "Finished");
            return (placeOrders);
        }
    }
}