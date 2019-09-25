﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class MGNT_CTrend : ContrTrendStrat
    {
        protected override double AlphaSpread()
        {
            return 1.7;
        }
        protected override int AmountOfSkipBars() { return 20; }
        protected override int AmountOfVolatEstim() { return 18; }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_15Min;
        }
        public MGNT_CTrend(int contractsToTrade, double step) : base("MGNT_CTrend", "MGNT-12.18_FT", contractsToTrade, step)
        {

        }
    }
}
