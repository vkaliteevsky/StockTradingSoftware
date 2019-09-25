﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class HYDR_CTrend : ContrTrendStrat
    {
        protected override double AlphaSpread()
        {
            return 2.4;
        }
        protected override int AmountOfSkipBars() { return 20; }
        protected override int AmountOfVolatEstim() { return 18; }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_15Min;
        }
        public HYDR_CTrend(int contractsToTrade, double step) : base("HYDR_CTrend", "HYDR-12.18_FT", contractsToTrade, step)
        {

        }
    }
}
