﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM3Lib;

namespace Stocks.Strategies
{
    class Si_Trend : TrendStrat
    {
        protected override double AlphaSpread()
        {
            return 0.7;
        }
        protected override int AmountOfSkipBars() { return 75; }
        protected override int AmountOfVolatEstim() { return 30; }
        protected override double Slip()
        {
            return Step * 1;
        }
        public override StBarInterval BarInterval()
        {
            return StBarInterval.StBarInterval_5Min;
        }
        public Si_Trend(int contractsToTrade, double step) : base("Si_Trend", "Si-12.18_FT", contractsToTrade, step)
        {

        }
    }
}
