using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class HYDRStrategy2141 : FirstTypeStratAbst
    {
        public HYDRStrategy2141(int contracts, int barLength)
            : base("HYDRStrategy2141", "HYDR-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 1, 1, barLength)
        {
        }

        protected override double DeltaPrice { get { return (50.0); } }
    }
}
