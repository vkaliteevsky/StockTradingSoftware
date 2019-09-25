using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrade.Strategies
{
    class SILVStrategy183 : FirstTypeStratAbst
    {
        public SILVStrategy183(int contracts, int barLength)
            : base("SILVStrategy183", "SILV-3.18_FT", OrderTypeEnum.LIMIT, SessionTypeEnum.FULL, 2, contracts, 0.01, 1, barLength)
        {
        }

        protected override double RoundToStep(double price)
        {
            return Math.Round(price * 100.0 / 100.0, 2);
        }

        protected override double DeltaPrice { get { return (0.02); } }

    }
}
