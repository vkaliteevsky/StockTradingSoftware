using Stocks.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    class StrategyFactory
    {
        public static List<String> SupportedStrategies = new List<string>(new string[] {
            "Si_Trend", "VTBR_CTrend", "SBRF_Trend", "SBPR_CTrend", "GAZR_CTrend", "ROSN_CTrend", "MOEX_CTrend",
            "GOLD_CTrend", "HYDR_CTrend", "GMKR_Trend", "TATN_CTrend", "MGNT_CTrend", "RTKM_CTrend", "UJPY_Trend", "MXI_Trend", "SILV_CTrend"
        });

        public static StrategyAbst CreateStrategy(StrategyParams strategyParams)
        {
            switch (strategyParams.Name)
            {
                case "Si_Trend":
                    return new Si_Trend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "VTBR_CTrend":
                    return new VTBR_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "SBRF_Trend":
                    return new SBRF_Trend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "SBPR_CTrend":
                    return new SBPR_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "GAZR_CTrend":
                    return new GAZR_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "ROSN_CTrend":
                    return new ROSN_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "MOEX_CTrend":
                    return new MOEX_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "GOLD_CTrend":
                    return new GOLD_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "HYDR_CTrend":
                    return new HYDR_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "GMKR_Trend":
                    return new GMKR_Trend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "TATN_CTrend":
                    return new TATN_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "MGNT_CTrend":
                    return new MGNT_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "RTKM_CTrend":
                    return new RTKM_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "UJPY_Trend":
                    return new UJPY_Trend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "MXI_Trend":
                    return new MXI_Trend(strategyParams.ContractsToTrade, strategyParams.Step);
                case "SILV_CTrend":
                    return new SILV_CTrend(strategyParams.ContractsToTrade, strategyParams.Step);
                default:
                    return null;
            }
        }
    }
}
