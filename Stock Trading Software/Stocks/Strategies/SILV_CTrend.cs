using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class SILV_CTrend : StrategyAbst
    {
        public SILV_CTrend(int contractsToTrade, double step) : base("SILV_CTrend", "SILV-12.18_FT", contractsToTrade, step)
        {

        }
        protected override double AlphaSpread()
        {
            return 1.0;
        }
        protected override int AmountOfSkipBars() { return 3; }
        protected override int AmountOfVolatEstim() { return 2; }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_5Min;
        }
        protected override List<Order> PreparePlaceOrders()
        {
            WriteToLogDB("PreparePlaceOrders", "Started");
            Bar bar = DatabaseReader.SelectLastPrice(Symbol);
            double lastPrice = bar.Close;

            double buyPrice = RoundToStep(lastPrice - 0.02);
            double sellPrice = RoundToStep(lastPrice + 0.02);
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
        }
    }
}
