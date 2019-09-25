using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    static class ConfigReader
    {
        static string ConfigPath = @"config.cfg";

        public static List<StrategyParams> ReadConfig()
        {
            using (StreamReader r = new StreamReader(ConfigPath))
            {
                string json = r.ReadToEnd();
                CommonParams commonParams = JsonConvert.DeserializeObject<CommonParams>(json);
                ExcelReader.ExcelPositionFileName = commonParams.ExcelPositionFileName;
                ExcelReader.ExcelPositionSheetName = commonParams.ExcelPositionSheetName;
                ExcelReader.ExcelOrdersSheetName = commonParams.ExcelOrdersSheetName;
                ExcelReader.PositionTickerColumn = commonParams.PositionTickerColumn;
                ExcelReader.QtyColumn = commonParams.QtyColumn;
                ExcelReader.PlannedColumn = commonParams.PlannedColumn;
                ExcelReader.PositionSideColumn = commonParams.PositionSideColumn;
                ExcelReader.AvgPriceColumn = commonParams.AvgPriceColumn;
                ExcelReader.OrderTickerColumn = commonParams.OrderTickerColumn;
                ExcelReader.StatusColumn = commonParams.StatusColumn;
                ExcelReader.OrderSideColumn = commonParams.OrderSideColumn;
                ExcelReader.OrderIdColumn = commonParams.OrderIdColumn;
                ExcelReader.PriceColumn = commonParams.PriceColumn;
                ExcelReader.StopPriceColumn = commonParams.StopPriceColumn;
                ExcelReader.VolumeColumn = commonParams.VolumeColumn;
                ExcelReader.BalanceColumn = commonParams.BalanceColumn;
                PortfolioManager.GetInstance().SumToTrade = commonParams.SumToTrade;
                return commonParams.Strategies;
            }
        }
    }

    public class CommonParams
    {
        public string ExcelPositionFileName { get; set; }
        public string ExcelPositionSheetName { get; set; }
        public string ExcelOrdersSheetName { get; set; }
        public string PositionTickerColumn { get; set; }
        public string QtyColumn { get; set; }
        public string PlannedColumn { get; set; }
        public string PositionSideColumn { get; set; }
        public string AvgPriceColumn { get; set; }
        public string OrderTickerColumn { get; set; }
        public string StatusColumn { get; set; }
        public string OrderSideColumn { get; set; }
        public string OrderIdColumn { get; set; }
        public string PriceColumn { get; set; }
        public string StopPriceColumn { get; set; }
        public string VolumeColumn { get; set; }
        public string BalanceColumn { get; set; }
        public double SumToTrade { get; set; }
        public List<StrategyParams> Strategies { get; set; }
    }

    public class StrategyParams
    {
        public string Name { get; set; }
        public int ContractsToTrade { get; set; }
        //public int BarsMillisecondsLength { get; set; }
        public double StrategicWeight { get; set; }
        public double Step { get; set; }
        public double Mult { get; set; }
        public StrategyParams(string name, int contracts, double strategicWeight, double step, double mult)
        {
            Name = name;
            ContractsToTrade = contracts;
            StrategicWeight = strategicWeight;
            Step = step;
            Mult = mult;
        }
    }
}
