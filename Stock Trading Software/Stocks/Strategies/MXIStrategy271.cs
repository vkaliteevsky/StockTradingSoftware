using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class MXIStrategy271 : FirstTypeStratAbst
    {
        public MXIStrategy271(int contracts, int barLength)
            : base("MXIStrategy271", "MXI-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 0.25, 1, barLength)
        {
        }

        protected override double RoundToStep(double price)
        {
            return Math.Round(price * 20.0 / 20.0, 2);
        }

        protected override double DeltaPrice { get { return (4.90); } }
    }
}
