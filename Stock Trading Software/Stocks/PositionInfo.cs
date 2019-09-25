using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public struct PositionInfo
    {
        public string Side { get; set; }
        public double Quantity { get; set; }
        public double Planned { get; set; }
        public double AvgPrice { get; set; }

        public PositionInfo(string side, double quantity, double planned, double avgPrice)
        {
            Side = side;
            Quantity = quantity;
            Planned = planned;
            AvgPrice = avgPrice;
        }
    }
}
