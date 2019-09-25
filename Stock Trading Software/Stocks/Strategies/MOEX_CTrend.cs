using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class MOEX_CTrend : ContrTrendStrat
    {
        protected override double AlphaSpread()
        {
            return 1.5;
        }
        protected override int AmountOfSkipBars() { return 75; }
        protected override int AmountOfVolatEstim() { return 30; }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_5Min;
        }
        public MOEX_CTrend(int contractsToTrade, double step) : base("MOEX_CTrend", "MOEX-12.18_FT", contractsToTrade, step)
        {

        }
    }
}
