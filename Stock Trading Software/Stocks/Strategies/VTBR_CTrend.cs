using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class VTBR_CTrend : ContrTrendStrat
    {
        protected override double AlphaSpread()
        {
            return 1.0;
        }
        protected override int AmountOfSkipBars() { return 50; }
        protected override int AmountOfVolatEstim() { return 30; }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_5Min;
        }
        public VTBR_CTrend(int contractsToTrade, double step) : base("VTBR_CTrend", "VTBR-12.18_FT", contractsToTrade, step)
        {

        }
    }
}
