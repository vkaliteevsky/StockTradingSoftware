using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace Stocks
{
    class ExcelReader
    {
        private static DBInputOutput.DBWriter dbWriter = new DBInputOutput.DBWriter();
        public static string ExcelPositionFileName;// = @"Position.xlsm";
        public static string ExcelPositionSheetName;// = "All open Positions #1"; // "Все откр. позиции #1";
        public static string ExcelOrdersSheetName;// = "All orders #1"; //"Все заявки #1";
        public static string PositionTickerColumn;
        public static string QtyColumn;
        public static string PlannedColumn;
        public static string PositionSideColumn;
        public static string AvgPriceColumn;
        public static string OrderTickerColumn;
        public static string StatusColumn;
        public static string OrderSideColumn;
        public static string OrderIdColumn;
        public static string PriceColumn;
        public static string StopPriceColumn;
        public static string VolumeColumn;
        public static string BalanceColumn;

        public static PositionInfo ReadPosition(string ticker)
        {
            dbWriter.InsertGeneral("ExcelReader.ReadPosition: Started", ticker);
            Application xlApp = (Application)Marshal.GetActiveObject("Excel.Application");
            Workbook xlWorkbook = xlApp.Workbooks[ExcelPositionFileName];
            Worksheet xlWorksheet = (Worksheet)xlWorkbook.Sheets[ExcelPositionSheetName];
            Range xlRange = xlWorksheet.UsedRange;
            Range xlCells = xlRange.Cells;
            int tickerRow = 1;
            while ((xlCells[tickerRow, PositionTickerColumn] as Range).Value2 != null)
            {
                if ((xlCells[tickerRow, PositionTickerColumn] as Range).Value2.ToString().Equals(ticker))
                {
                    break;
                }
                else
                {
                    tickerRow++;
                }
            }
            double qty = 0;
            string side = null;
            double planned = 0;
            double avgPrice = 0;
            if (tickerRow > 1)
            {
                var cellQty = (xlCells[tickerRow, QtyColumn] as Range).Value2;
                if (cellQty != null)
                {
                    qty = (double)cellQty;
                }
                var cellPlanned = (xlCells[tickerRow, PlannedColumn] as Range).Value2;
                if (cellPlanned != null)
                {
                    planned = (double)cellPlanned;
                }
                var cellSide = (xlCells[tickerRow, PositionSideColumn] as Range).Value2;
                if (cellSide != null)
                {
                    side = cellSide;
                }
                var cellAvg = (xlCells[tickerRow, AvgPriceColumn] as Range).Value2;
                if (cellAvg != null)
                {
                    avgPrice = cellAvg;
                }
            }
            dbWriter.InsertGeneral("ExcelReader.ReadPosition: Finished", ticker);
            return new PositionInfo(side, qty, planned, avgPrice);
        }

        public static OpenOrdersInfo ReadOpenOrders(string ticker)
        {
            dbWriter.InsertGeneral("ExcelReader.ReadOpenOrders: Started", ticker);
            Application xlApp = (Microsoft.Office.Interop.Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
            Workbook xlWorkbook = xlApp.Workbooks[ExcelPositionFileName];
            Worksheet xlWorksheet = (Worksheet)xlWorkbook.Sheets[ExcelOrdersSheetName];
            Range xlRange = xlWorksheet.UsedRange;
            Range xlCells = xlRange.Cells;

            string buyOrderId = null;
            double buyPrice = 0;
            double buyStopPrice = 0;
            double buyVolume = 0;
            double buyFilledVolume = 0;

            string sellOrderId = null;
            double sellPrice = 0;
            double sellStopPrice = 0;
            double sellVolume = 0;
            double sellFilledVolume = 0;

            var ordersInfoRow = 1;
            int buyOrders = 0;
            int sellOrders = 0;

            List<string> buyOrdersIds = new List<string>();
            List<double> buyPrices = new List<double>();
            List<double> buyStopPrices = new List<double>();
            List<double> buyVolumes = new List<double>();
            List<double> buyFilledVolumes = new List<double>();

            List<string> sellOrdersIds = new List<string>();
            List<double> sellPrices = new List<double>();
            List<double> sellStopPrices = new List<double>();
            List<double> sellVolumes = new List<double>();
            List<double> sellFilledVolumes = new List<double>();

            while ((xlCells[ordersInfoRow, OrderTickerColumn] as Range).Value2 != null)
            {
                if ((xlCells[ordersInfoRow, OrderTickerColumn] as Range).Value2.ToString().Equals(ticker))
                {
                    string status = (xlCells[ordersInfoRow, StatusColumn] as Range).Value2.ToString();
                    if (status.Equals("Open") || status.Equals("Partial") || status.Equals("Pending"))
                    {
                        string side = (xlCells[ordersInfoRow, OrderSideColumn] as Range).Value2.ToString();
                        if (side.Equals("Buy"))
                        {
                            buyOrders++;
                            buyOrderId = (xlCells[ordersInfoRow, OrderIdColumn] as Range).Value2.ToString().Substring(2);
                            buyOrdersIds.Add(buyOrderId);
                            buyPrice = (double)(xlCells[ordersInfoRow, PriceColumn] as Range).Value2;
                            buyPrices.Add(buyPrice);
                            buyStopPrice = (double)(xlCells[ordersInfoRow, StopPriceColumn] as Range).Value2;
                            buyStopPrices.Add(buyStopPrice);
                            buyVolume = (double)(xlCells[ordersInfoRow, BalanceColumn] as Range).Value2;
                            buyVolumes.Add(buyVolume);
                            buyFilledVolume = (double)(xlCells[ordersInfoRow, VolumeColumn] as Range).Value2 - buyVolume;
                            buyFilledVolumes.Add(buyFilledVolume);
                        }
                        else
                        {
                            sellOrders++;
                            sellOrderId = (xlCells[ordersInfoRow, OrderIdColumn] as Range).Value2.ToString().Substring(2);
                            sellOrdersIds.Add(sellOrderId);
                            sellPrice = (double)(xlCells[ordersInfoRow, PriceColumn] as Range).Value2;
                            sellPrices.Add(sellPrice);
                            sellStopPrice = (double)(xlCells[ordersInfoRow, StopPriceColumn] as Range).Value2;
                            sellStopPrices.Add(sellStopPrice);
                            sellVolume = (double)(xlCells[ordersInfoRow, BalanceColumn] as Range).Value2;
                            sellVolumes.Add(sellVolume);
                            sellFilledVolume = (double)(xlCells[ordersInfoRow, VolumeColumn] as Range).Value2 - sellVolume;
                            sellFilledVolumes.Add(sellFilledVolume);
                        }
                    }
                }
                ordersInfoRow++;
            }
            dbWriter.InsertGeneral("ExcelReader.OpenOrdersInfo: Finished", ticker);
            return new OpenOrdersInfo(buyOrdersIds, buyPrices, buyStopPrices, buyVolumes, buyFilledVolumes
                , sellOrdersIds, sellPrices, sellStopPrices, sellVolumes, sellFilledVolumes, ticker);
        }
    }
}
