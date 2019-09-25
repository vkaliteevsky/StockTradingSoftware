using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class MGNTStrategy291 : SecondTypeStratAbst
    {
        public MGNTStrategy291(int contracts, int barLength)
            : base("MGNTStrategy291", "MGNT-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 1, 1, barLength)
        {
        }

        protected override double DeltaPrice { get { return (49.0); } }

    }
}
