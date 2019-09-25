using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class VTBRStrategy242 : ThirdTypeStratAbst
    {
        public VTBRStrategy242(int contracts, int barLength)
            : base("VTBRStrategy242", "VTBR-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 1, 1, barLength)
        {
        }
        protected override double DeltaPrice { get { return (30.0); } }
        /* protected override double DelimCC(double yield)
        {
            return (Math.Max(1, Math.Pow(yield * 100, 1 / 10)));
        } */
        protected override double StopSpread { get { return 1; } }
    }
}
