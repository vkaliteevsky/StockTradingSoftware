using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public enum PortfolioConsistencyCode
    {
        CONSISTENT = 0,
        SUM_NOT_ONE = 1,
        EXISTS_NEGATIVE_WEIGHT = 2,
        EXISTS_WEIGHT_MORE_THAN_ONE = 3,
        SUM_TO_TRADE_LESS_THAN_ZERO = 4
    }
    class PortfolioManager
    {
        private static PortfolioManager Instance;
        public double[] TargetWs { get; set; }
        public double SumToTrade { get; set; }
        public static PortfolioManager GetInstance()
        {
            if (Instance == null)
            {
                Instance = new PortfolioManager();
            }
            return Instance;
        }
        public PortfolioManager(double[] targetWs, double sumToTrade)
        {
            TargetWs = targetWs;
            SumToTrade = sumToTrade;
        }
        public PortfolioManager()
        {
            SumToTrade = 0;
        }
        protected PortfolioConsistencyCode CheckConsistency()
        {
            if (Math.Abs(TargetWs.Sum() - 1) >= 0.0001) return PortfolioConsistencyCode.SUM_NOT_ONE;
            else if (TargetWs.Any(w => w < 0)) return PortfolioConsistencyCode.EXISTS_NEGATIVE_WEIGHT;
            else if (TargetWs.Any(w => w > 1)) return PortfolioConsistencyCode.EXISTS_WEIGHT_MORE_THAN_ONE;
            else if (SumToTrade < 0) return PortfolioConsistencyCode.SUM_TO_TRADE_LESS_THAN_ZERO;
            else return PortfolioConsistencyCode.CONSISTENT;
        }
        public int[] CalcContractsToTrade(double[] GOs)
        {
            int n = TargetWs.Length;
            PortfolioConsistencyCode code = CheckConsistency();
            if (code != PortfolioConsistencyCode.CONSISTENT || GOs.Any(go => go <= 0))
            {
                int[] ws = new int[n];
                for (int i = 0; i < n; i++)
                {
                    ws[i] = 0;
                }
                return ws;
            }
            int[] iters = Enumerable.Range(0, TargetWs.Length).ToArray();
            int[] currentCs = iters.Select(i => (int)Math.Floor(TargetWs[i] * SumToTrade / GOs[i])).ToArray();
            List<int[]> res = new List<int[]>();
            FindBestWs(TargetWs, currentCs, SumToTrade, 0, GOs, ref res);
            double[] dists = res.Select(cs => Edist(WeightsByContracts(cs, GOs), TargetWs)).ToArray();
            int index = 0;
            double min = dists[0];
            for (int i = 1; i < dists.Length; i++)
            {
                if (dists[i] < min) { min = dists[i]; index = i; }
            }
            return res[index];
        }
        private void FindBestWs(double[] targetWs, int[] currentCs, double sumToTrade, int k, double[] gos, ref List<int[]> res)
        {
            int n = currentCs.Length;
            if (k == n - 1)
            {
                FindBestWs(targetWs, currentCs, sumToTrade, k + 1, gos, ref res);
            }
            else if (k > n - 1)
            {
                if (TradingMoney(currentCs, gos) <= sumToTrade)
                {
                    res.Add(currentCs);
                }
            }
            else
            {
                int[] cs1 = (int [])currentCs.Clone();
                int[] cs2 = (int [])currentCs.Clone();
                int[] cs0 = (int[])currentCs.Clone();
                cs1[k] = (int)(currentCs[k] - 1);
                cs2[k] = (int)(currentCs[k] + 1);
                if (cs1[k] >= 0)
                    FindBestWs(targetWs, cs1, sumToTrade, k + 1, gos, ref res);
                if (cs2[k] >= 0)
                    FindBestWs(targetWs, cs2, sumToTrade, k + 1, gos, ref res);
                if (cs0[k] >= 0)
                    FindBestWs(targetWs, cs0, sumToTrade, k + 1, gos, ref res);
            }
        }
        private double Edist(double[] xs, double[] ys)
        {
            double sum = 0.0;
            int n = xs.Length;
            for (int i = 0; i < n; i++)
            {
                sum += Math.Pow((xs[i] - ys[i]), 2);
            }
            return Math.Sqrt(sum);
        }
        private double TradingMoney(int[] cs, double[] gos)
        {
            int n = cs.Length;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                sum += cs[i] * gos[i];
            }
            return sum;
        }
        private double[] WeightsByContracts(int[] cs, double[] gos)
        {
            int n = cs.Length;
            int[] seqs = new int[n];
            double sumToTrade = TradingMoney(cs, gos);
            for (int i = 0; i < n; i++)
                seqs[i] = i;
            double[] wts = seqs.Select(i => cs[i] * gos[i] / sumToTrade).ToArray();
            return wts;
        }
    }
}
