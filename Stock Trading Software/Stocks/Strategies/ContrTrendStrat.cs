using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    abstract class ContrTrendStrat : StrategyAbst
    {
        public ContrTrendStrat(string name, string symbol, int contractsToTrade, double step) : base(name, symbol, contractsToTrade, step)
        {

        }
        protected override List<Order> PreparePlaceOrders()
        {
            WriteToLogDB("PreparePlaceOrders", "Started");
            if (Bars.Count < 3)
            {
                throw new SmartException(ExceptionImportanceLevel.MEDIUM, "PreparePlaceOrders", "ContrTrendStrat", Symbol + ": Bars are empty or has less than three elements!");
            }
            if (AmountOfVolatEstim() > AmountOfSkipBars())
            {
                throw new SmartException(ExceptionImportanceLevel.MEDIUM, "PreparePlaceOrders", "ContrTrendStrat", Symbol + ": AmountOfVolatEstim > AmountOfSkipBars");
            }
            double[] closes = Bars.Select(bar => bar.Close).ToArray();
            double[] ys = new double[Bars.Count - 1];
            for (int i = 0; i < Bars.Count - 1; i++)
                ys[i] = closes[i + 1] / closes[i] - 1;
            ys = ys.Skip(Bars.Count - 1 - AmountOfVolatEstim()).ToArray();
            double sdev = CalcVolat(ys);
            double alpha = AlphaSpread() * Math.Sqrt(AmountOfSkipBars()) * sdev;
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

            double buyPrice = RoundToStep(lastPrice * (1 - alpha));
            double sellPrice = RoundToStep(lastPrice * (1 + alpha));
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
            if (buyPrice >= sellPrice || (buyPrice <= 0 && buyVol != 0) || (sellPrice <= 0 && sellVol != 0))
            {
                throw new SmartException(ExceptionImportanceLevel.HIGH, "PreparePlaceOrders", "ContrTrendStrat", "buyPrice = " + buyPrice + ", sellPrice = " + sellPrice);
            }
            WriteToLogDB("PreparePlaceOrders", "Buy: Price = " + buyPrice + ", Volume = " + buyVol + "; Sell: Price = " + sellPrice + ", Volume = " + sellVol);
            List<Order> placeOrders = new List<Order>();

            DateTime dTime = ServerTime.GetRealTime();
            DatabaseWriter.InsertDecision(dTime, Symbol, ActionEnum.BUY, buyPrice, buyVol, 0);
            DatabaseWriter.InsertDecision(dTime, Symbol, ActionEnum.SELL, sellPrice, sellVol, 0);

            if (buyVol > 0)
                placeOrders.Add(new Order(Symbol, GenerateCookie(), "", buyVol, 0, buyPrice, 0, ActionEnum.BUY, OrderTypeEnum.LIMIT));
            if (sellVol > 0)
                placeOrders.Add(new Order(Symbol, GenerateCookie(), "", sellVol, 0, sellPrice, 0, ActionEnum.SELL, OrderTypeEnum.LIMIT));

            WriteToLogDB("PreparePlaceOrders", "Finished");
            return (placeOrders);
            /* WriteToLogDB("PreparePlaceOrders", "Started");
            double cp = 100;
            double up = 1;
            double down = 1;
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
            double buyPrice = RoundToStep(cp - down);
            double sellPrice = RoundToStep(cp + up);
            WriteToLogDB("PreparePlaceOrders", "Buy: Price = " + buyPrice + ", Volume = " + buyVol + "; Sell: Price = " + sellPrice + ", Volume = " + sellVol);

            List<Order> placeOrders = new List<Order>();
            if (buyVol != 0)
                placeOrders.Add(new Order(Symbol, GenerateCookie(), "", buyVol, 0, buyPrice, 0, ActionEnum.BUY, OrderTypeEnum.LIMIT));
            if (sellVol != 0)
                placeOrders.Add(new Order(Symbol, GenerateCookie(), "", sellVol, 0, sellPrice, 0, ActionEnum.SELL, OrderTypeEnum.LIMIT));

            WriteToLogDB("PreparePlaceOrders", "Finished");
            return (placeOrders); */
            // return new List<Order>();
        }
    }
}
