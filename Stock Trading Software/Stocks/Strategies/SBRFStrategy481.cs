using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class SBRFStrategy481 : SecondTypeStratAbst
    {
        public SBRFStrategy481(int contracts, int barLength)
            : base("SBRFStrategy481", "SBRF-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 1, 1, barLength)
        {
        }
        protected override double DelimCC(double yield)
        {
            return (Math.Max(1, Math.Pow(yield * 100, 1 / 20)));
        }
        protected override double DeltaPrice { get { return 45; } }
        //protected override double StopSpread { get { return 1; } }
    }
}
