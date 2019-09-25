using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class SBPRStrategy2111 : FourthTypeStratAbst
    {
        public SBPRStrategy2111(int contracts, int barLength)
            : base("SBPRStrategy2111", "SBPR-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 1, 1, barLength)
        {
        }

        protected override double DeltaPrice { get { return (31.0); } }
        protected override double DelimCC(double yield)
        {
            return (Math.Max(1, Math.Pow(yield * 100, 1 / 5)));
        }
        protected override double StopSpread { get { return 1; } }
    }
}
