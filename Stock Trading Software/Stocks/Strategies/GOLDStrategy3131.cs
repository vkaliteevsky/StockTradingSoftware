using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterTrade.Consitency;

namespace BetterTrade.Strategies
{
    class GOLDStrategy3131 : FourthTypeStratAbst
    {
        public GOLDStrategy3131(int contracts, int barLength)
            : base("GOLDStrategy3131", "GOLD-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 1, 1, barLength)
        {
        }
        protected override double DelimCC(double yield)
        {
            return (Math.Max(1, Math.Pow(yield * 100, 1 / 8)));
        }
        protected override double DeltaPrice { get { return 2.60; } }
        protected override double StopSpread { get { return (0.1); } }
        protected override double RoundToStep(double price)
        {
            return Math.Round(price * 100.0, 2) / 100.0;
        }
    }
}
