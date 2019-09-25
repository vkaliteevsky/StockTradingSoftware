using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class ROSNStrategy1121 : FirstTypeStratAbst
    {
        public ROSNStrategy1121(int contracts, int barLength)
            : base("ROSNStrategy1121", "ROSN-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 1, contracts, 1, 1, barLength)
        {
        }

        protected override double DeltaPrice { get { return (118.0); } }
    }
}
