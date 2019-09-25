using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class ALRSStrategy2 : SecondTypeStratAbst
    {
        public ALRSStrategy2(int contracts, int barLength)
            : base("ALRSStrategy2", "ALRS-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 1, 1, barLength)
        {
        }
        protected override double DeltaPrice { get { return (56.0); } }
        protected override double DelimCC(double yield)
        {
            return (Math.Max(1, Math.Pow(yield * 100, 1 / 3)));
        }
    }
}
