using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class SBRF_Trend : TrendStrat
    {
        protected override double AlphaSpread()
        {
            return 2.6;
        }
        protected override int AmountOfSkipBars() { return 125; }
        protected override int AmountOfVolatEstim() { return 30; }
        protected override double Slip()
        {
            return Step * 1;
        }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_5Min;
        }
        public SBRF_Trend(int contractsToTrade, double step) : base("SBRF_Trend", "SBRF-12.18_FT", contractsToTrade, step)
        {

        }
    }
}
