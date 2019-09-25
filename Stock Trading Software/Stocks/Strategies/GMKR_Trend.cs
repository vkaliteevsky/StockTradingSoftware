using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class GMKR_Trend : TrendStrat
    {
        protected override double AlphaSpread()
        {
            return 0.8;
        }
        protected override int AmountOfSkipBars() { return 20; }
        protected override int AmountOfVolatEstim() { return 18; }
        protected override double Slip()
        {
            return Step * 2;
        }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_15Min;
        }
        public GMKR_Trend(int contractsToTrade, double step) : base("GMKR_Trend", "GMKR-12.18_FT", contractsToTrade, step)
        {

        }
    }
}
