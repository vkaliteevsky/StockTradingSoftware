using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    class DBInputOutput
    {
        private const string ConnectionString = "datasource=localhost;port=3306;database=mdata;username=gkraychik;password=Stock.exchange";
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private static string[] EveryTableName = { "trades", "deals", "decisions", "orders", "risks", "info" };
        private static string CheckTableString = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'mdata' AND table_name = ?tableName";

        private static object LockObject = new object();

        public static string[] GetEveryTableName()
        {
            return EveryTableName;
        }

        public static string GetConnectionString()
        {
            return ConnectionString;
        }

        public static DBWriter dbWriter = new DBWriter();
        public static DBReader dbReader = new DBReader();

        public static bool IsTableExist(MySqlConnection conn, string tableName)
        {
            MySqlCommand command = conn.CreateCommand();
            command.CommandText = CheckTableString;
            command.Parameters.AddWithValue("?tableName", tableName);
            bool res = false;
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetInt32(0) == 1)
                    {
                        res = true;
                    }
                }
            }
            return res;
        }

        public class DBWriter
        {
            private MySqlConnection DatabaseConnection;

            private const string InsertTradeQuery = "insert into trades(assetid, dtime, price, volume) values " +
                "(?tTickerId, ?tDateTime, ?tPrice, ?tVolume)";

            private const string InsertDealQuery = "insert into deals(assetid, dtime, price, volume) values " +
                "(?dTickerId, ?dDateTime, ?dPrice, ?dVolume)";

            private const string InsertConsistencyQuery = "insert into consistency (dtime, ticker, tickerid, errorid, comment) " +
                "(?dTime, ?dTicker, ?dTickerId, ?dErrorId, ?dComment)";

            private const string InsertOrderGeneralQuery = "insert into general (dtime, comment, ticker) values (?dTime, ?dComment, ?dTicker)";

            private const string InsertOrderLogQuery = "insert into orderlog (dtime, assetid, orderid, evnt, cookie, addcomment, state, act, typ, price, volume, stop, filled) values " +
                "(?dTime, ?dAssetId, ?dOrderId, ?dEvnt, ?dCookie, ?dAddComment, ?dState, ?dAct, ?dTyp, ?dPrice, ?dVolume, ?dStop, ?dFilled)";

            private const string InsertPositionQuery = "insert into position (dtime, ticker, tickerid, amount, planned, avgprice) values " +
                "(?dTime, ?dTicker, ?dTickerId, ?dAmount, ?dPlanned, ?dAvgPrice)";

            private const string InsertDecisionQuery = "insert into decisions(assetid, dtime, buy_price, buy_stopprice, buy_volume, sell_price, sell_stopprice, sell_volume) values " +
                "(?dAssetid, ?dDtime, ?dBuy_price, ?dBuy_stopprice, ?dBuy_volume, ?dSell_price, ?dSell_stopprice, ?dSell_volume)";

            private const string InsertDecsQuery = "insert into decs (dtime, ticker, buysell, price, volume, stopprice) values " + 
                "(?dTime, ?dTicker, ?dBuySell, ?dPrice, ?dVolume, ?dStopPrice)";
            private const string InsertOrderQuery = "insert into orders(assetid, dtime, price, volume) values " +
                "(?oTickerId, ?oDateTime, ?oPrice, ?oVolume)";

            private const string InsertRisksQuery = "insert into risks(dtime, asset, status_id) values " +
                "(?dDateTime, ?dTicker, ?dStatusID)";

            private const string InsertSymbolQuery = "insert into info(date, symbol, prop_name, val) values " +
                "(?iDate, ?dSymbol, ?iProp, ?iVal)";

            public DBWriter()
            {
                DatabaseConnection = new MySqlConnection(ConnectionString);
                try
                {
                    DatabaseConnection.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "\n****************\n" + e.StackTrace);
                    throw e;
                }
            }

            public void InsertTrade(int tickerId, DateTime dateTime, double price, double volume)
            {
                lock (LockObject)
                {
                    MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                    insertCommand.CommandText = InsertTradeQuery;
                    insertCommand.Parameters.AddWithValue("?tTickerId", tickerId);
                    insertCommand.Parameters.AddWithValue("?tDateTime", dateTime.ToString(DateTimeFormat));
                    insertCommand.Parameters.AddWithValue("?tPrice", price);
                    insertCommand.Parameters.AddWithValue("?tVolume", volume);
                    insertCommand.ExecuteNonQuery();
                }
            }

            public void InsertRisk(DateTime dateTime, string ticker, int statusId)
            {
                lock (LockObject)
                {
                    MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                    insertCommand.CommandText = InsertRisksQuery;
                    insertCommand.Parameters.AddWithValue("?dDateTime", dateTime.ToString(DateTimeFormat));
                    insertCommand.Parameters.AddWithValue("?dTicker", ticker);
                    insertCommand.Parameters.AddWithValue("?dStatusID", statusId);
                    insertCommand.ExecuteNonQuery();
                }
            }

            public void InsertDeal(int tickerId, DateTime dateTime, double price, double volume)
            {
                lock (LockObject)
                {
                    MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                    insertCommand.CommandText = InsertDealQuery;
                    insertCommand.Parameters.AddWithValue("?dTickerId", tickerId);
                    insertCommand.Parameters.AddWithValue("?dDateTime", dateTime.ToString(DateTimeFormat));
                    insertCommand.Parameters.AddWithValue("?dPrice", price);
                    insertCommand.Parameters.AddWithValue("?dVolume", volume);
                    insertCommand.ExecuteNonQuery();
                }
            }

            public void InsertPosition(DateTime dTime, string ticker, int tickerid, double amount, double planned, double avgPrice)
            {
                lock (LockObject)
                {
                    MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                    insertCommand.CommandText = InsertPositionQuery;
                    insertCommand.Parameters.AddWithValue("?dTime", dTime.ToString(DateTimeFormat));
                    insertCommand.Parameters.AddWithValue("?dTicker", ticker);
                    insertCommand.Parameters.AddWithValue("?dTickerId", tickerid);
                    insertCommand.Parameters.AddWithValue("?dAmount", amount);
                    insertCommand.Parameters.AddWithValue("?dPlanned", planned);
                    insertCommand.Parameters.AddWithValue("?dAvgPrice", avgPrice);
                    insertCommand.ExecuteNonQuery();
                }
            }

            public void InsertOrderLog(DateTime dTime,string orderId, string evnt, int cookie = 0, string addComment = "", int assetid = -1,
                int state = -10, int action = -10, int type = -10, double price = 0.0, double volume = 0.0, double stop = 0.0, double filled = 0.0)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = InsertOrderLogQuery;
                        insertCommand.Parameters.AddWithValue("?dTime", dTime.ToString(DateTimeFormat));
                        insertCommand.Parameters.AddWithValue("?dAssetId", assetid);
                        insertCommand.Parameters.AddWithValue("?dOrderId", orderId);
                        insertCommand.Parameters.AddWithValue("?dEvnt", evnt);
                        insertCommand.Parameters.AddWithValue("?dCookie", cookie);
                        insertCommand.Parameters.AddWithValue("?dAddComment", addComment);
                        insertCommand.Parameters.AddWithValue("?dState", state);
                        insertCommand.Parameters.AddWithValue("?dAct", action);
                        insertCommand.Parameters.AddWithValue("?dTyp", type);
                        insertCommand.Parameters.AddWithValue("?dPrice", price);
                        insertCommand.Parameters.AddWithValue("?dVolume", volume);
                        insertCommand.Parameters.AddWithValue("?dStop", stop);
                        insertCommand.Parameters.AddWithValue("?dFilled", filled);
                        insertCommand.ExecuteNonQuery();
                    }
                } catch (Exception e)
                {
                    throw new SmartException(e);
                }

            }
            public void InsertDecision(int tickerId, DateTime dateTime, double[] decision)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = InsertDecisionQuery;
                        insertCommand.Parameters.AddWithValue("?dAssetid", tickerId);
                        insertCommand.Parameters.AddWithValue("?dDtime", dateTime.ToString(DateTimeFormat));
                        insertCommand.Parameters.AddWithValue("?dBuy_price", decision[0]);
                        insertCommand.Parameters.AddWithValue("?dBuy_stopprice", 0);
                        insertCommand.Parameters.AddWithValue("?dBuy_volume", decision[1]);
                        insertCommand.Parameters.AddWithValue("?dSell_price", decision[2]);
                        insertCommand.Parameters.AddWithValue("?dSell_stopprice", 0);
                        insertCommand.Parameters.AddWithValue("?dSell_volume", decision[3]);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }

            public void InsertConsistency(DateTime dTime, string ticker, int tickerId, int errorId, string comment)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = InsertConsistencyQuery;
                        insertCommand.Parameters.AddWithValue("?dTime", dTime.ToString(DateTimeFormat));
                        insertCommand.Parameters.AddWithValue("?dTicker", ticker);
                        insertCommand.Parameters.AddWithValue("?dTickerId", tickerId);
                        insertCommand.Parameters.AddWithValue("?dErrorId", errorId);
                        insertCommand.Parameters.AddWithValue("?dComment", comment);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }

            public void InsertDecision(DateTime dateTime, string ticker, ActionEnum buysell, double price, int volume, double stopprice)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = InsertDecsQuery;
                        insertCommand.Parameters.AddWithValue("?dTime", dateTime.ToString(DateTimeFormat));
                        insertCommand.Parameters.AddWithValue("?dTicker", ticker);
                        insertCommand.Parameters.AddWithValue("?dBuySell", ((int)(buysell)) + 1);
                        insertCommand.Parameters.AddWithValue("?dPrice", price);
                        insertCommand.Parameters.AddWithValue("?dVolume", volume);
                        insertCommand.Parameters.AddWithValue("?dStopPrice", stopprice);
                        insertCommand.ExecuteNonQuery();
                    }
                } catch (Exception e)
                {
                    throw new SmartException(e);
                }

            }

            public void InsertDecisionTimes(DateTime dateTime, string symbol)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = "insert into decisiontimes (symbol, dtime) values (?dSymbol, ?dTime)";
                        insertCommand.Parameters.AddWithValue("?dSymbol", symbol);
                        insertCommand.Parameters.AddWithValue("?dTime", dateTime.ToString(DateTimeFormat));
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }

            public void InsertOrder(int tickerId, DateTime dateTime, double price, double volume)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = InsertOrderQuery;
                        insertCommand.Parameters.AddWithValue("?oTickerId", tickerId);
                        insertCommand.Parameters.AddWithValue("?oDateTime", dateTime.ToString(DateTimeFormat));
                        insertCommand.Parameters.AddWithValue("?oPrice", price);
                        insertCommand.Parameters.AddWithValue("?oVolume", volume);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }

            public void InsertGeneral(DateTime dTime, string comment, string ticker)
            {
                try
                {
                    lock (LockObject)
                    {
                        //if (ticker == null) ticker = "";
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = InsertOrderGeneralQuery;
                        insertCommand.Parameters.AddWithValue("?dTime", dTime.ToString(DateTimeFormat));
                        insertCommand.Parameters.AddWithValue("?dComment", comment);
                        insertCommand.Parameters.AddWithValue("?dTicker", ticker);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch
                {
                    throw new SmartException(ExceptionImportanceLevel.HIGH, "InsertGeneral", "DBInputOutput", "Unexpected Exception");
                }
            }
            public void InsertGeneral(string comment, string ticker)
            {
                InsertGeneral(ServerTime.GetRealTime(), comment, ticker);
            }

            public void InsertInfo(DateTime date, string symbol, string prop, double val)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand insertCommand = DatabaseConnection.CreateCommand();
                        insertCommand.CommandText = InsertSymbolQuery;
                        insertCommand.Parameters.AddWithValue("?iDate", date);
                        insertCommand.Parameters.AddWithValue("?dSymbol", symbol);
                        insertCommand.Parameters.AddWithValue("?iProp", prop);
                        insertCommand.Parameters.AddWithValue("?iVal", val);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }
        }

        public class DBReader
        {
            private MySqlConnection DatabaseConnection;

            private const string SelectTradesFromToMomentQuery = "select assetid, dtime, price, volume from trades where (assetid = ?tickerId and dtime >= ?momentFrom and dtime < ?momentTo) order by dtime;";
            private const string SelectDecisionByTickerId = "select buy_price, buy_volume, sell_price, sell_volume from decisions where (assetid = ?tickerId) order by dtime desc limit 1";
            private const string GetDisicionByDecQuery = "SELECT * FROM decs WHERE ticker = '?dSymbol' and dtime = (SELECT MAX(dtime) FROM decs WHERE ticker = '?dSymbol')";
            //private const string GetDisicionByDecQuery = "SELECT dtime, ticker, buysell, price, volume, stopprice FROM decs WHERE ticker = '?dSymbol'";
            private const string SelectInfoDate = "select date from info where (asset_id = ?assetid) orderby date desc limit 1";
            private const string SelectGOBuyByTickerId = "select asset_id, date, prop_name, val from info where (asset_id = ?assetid and date = ?dtime and prop_name = 'gobuy') order by date desc limit 1";
            private const string SelectGOSellByTickerId = "select asset_id, date, prop_name, val from info where (asset_id = ?assetid and date = ?dtime and prop_name = 'gosell') order by date desc limit 1";

            public DBReader()
            {
                DatabaseConnection = new MySqlConnection(ConnectionString);
                try
                {
                    DatabaseConnection.Open();
                }
                catch (Exception e)
                {
                    EmailSender.SendEmail(e);
                    Console.WriteLine(e.Message + "\n****************\n" + e.StackTrace);
                    throw e;
                }
            }

            public List<Contract> SelectTradesFromToMoment(int tickerId, DateTime momentFrom, DateTime momentTo)
            {
                lock(LockObject)
                {
                    List<Contract> selection = new List<Contract>();

                    MySqlCommand selectCommand = DatabaseConnection.CreateCommand();
                    selectCommand.CommandText = SelectTradesFromToMomentQuery;
                    selectCommand.Parameters.AddWithValue("?tickerId", tickerId);
                    selectCommand.Parameters.AddWithValue("?momentFrom", momentFrom.ToString(DateTimeFormat));
                    selectCommand.Parameters.AddWithValue("?momentTo", momentTo.ToString(DateTimeFormat));
                    using (MySqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32("assetid");
                            DateTime dtime = reader.GetDateTime("dtime");
                            double price = reader.GetDouble("price");
                            double volume = reader.GetDouble("volume");
                            selection.Add(new Contract(id, dtime, price, volume));
                        }
                        reader.Close();
                    }
                    return selection;
                }
            }

            public double[] SelectLastDecision(int tickerId)
            {
                try
                {
                    double[] res = new double[4];
                    MySqlCommand selectCommand = DatabaseConnection.CreateCommand();
                    selectCommand.CommandText = SelectDecisionByTickerId;
                    selectCommand.Parameters.AddWithValue("?tickerId", tickerId);
                    using (MySqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            res[0] = reader.GetDouble("buy_price");
                            res[1] = reader.GetDouble("buy_volume");
                            res[2] = reader.GetDouble("sell_price");
                            res[3] = reader.GetDouble("sell_volume");
                        }
                        reader.Close();
                    }
                    return res;
                } catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }

            public List<Order> GetLastDec(string symbol)
            {
                try
                {
                    List<Order> lastDecision = new List<Order>();
                    if (string.IsNullOrEmpty(symbol)) return lastDecision;
                    MySqlCommand selectCommand = DatabaseConnection.CreateCommand();
                    string query = GetDisicionByDecQuery.Replace("?dSymbol", symbol);
                    selectCommand.CommandText = query;
                    //selectCommand.Parameters.AddWithValue("?dSymbol", symbol);
                    using (MySqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int volume = reader.GetInt32("volume");
                            double price = reader.GetDouble("price");
                            double stopprice = reader.GetDouble("stopprice");
                            string actionString = reader.GetString("buysell");
                            ActionEnum action = actionString == "BUY" ? ActionEnum.BUY : ActionEnum.SELL;
                            OrderTypeEnum type = Math.Abs(stopprice) <= 0.0001 ? OrderTypeEnum.LIMIT : OrderTypeEnum.STOP;
                            lastDecision.Add(new Order(symbol, 0, "", volume, 0, price, stopprice, action, type));
                        }
                        reader.Close();
                    }
                    return (lastDecision);
                } catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }
            public double SelectGOById(int tickerId)
            {
                try
                {
                    MySqlCommand selectCommand = DatabaseConnection.CreateCommand();
                    selectCommand.CommandText = SelectGOBuyByTickerId;
                    selectCommand.Parameters.AddWithValue("?assetid", tickerId);
                    selectCommand.Parameters.AddWithValue("?dtime", ServerTime.GetRealTime().ToString("yyyy-MM-dd"));
                    double goBuy = -1;
                    using (MySqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            goBuy = reader.GetDouble("val");
                        }
                        reader.Close();
                    }
                    double goSell = -1;
                    if (goBuy > 0)
                    {
                        selectCommand = DatabaseConnection.CreateCommand();
                        selectCommand.CommandText = SelectGOSellByTickerId;
                        selectCommand.Parameters.AddWithValue("?assetid", tickerId);
                        selectCommand.Parameters.AddWithValue("?dtime", ServerTime.GetRealTime().ToString("yyyy-MM-dd"));
                        goSell = -1;
                        using (MySqlDataReader reader = selectCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                goSell = reader.GetDouble("val");
                            }
                            reader.Close();
                        }
                    }
                    return (goBuy + goSell) / 2;
                }
                catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }

            public DateTime SelectDecisionTimes(string symbol)
            {
                try
                {
                    lock (LockObject)
                    {
                        MySqlCommand selectCommand = DatabaseConnection.CreateCommand();
                        //selectCommand.CommandText = "insert into decisiontimes (symbol, dtime) values (?dSymbol, ?dTime)";
                        selectCommand.CommandText = "select dtime from decisiontimes where symbol = ?dSymbol ORDER BY id DESC LIMIT 1";
                        selectCommand.Parameters.AddWithValue("?dSymbol", symbol);
                        MySqlDataReader reader = selectCommand.ExecuteReader();
                        bool b = reader.Read();
                        if (!b) return new DateTime(1900, 1, 1);
                        DateTime dTime = reader.GetDateTime("dtime");
                        reader.Close();
                        return (dTime);
                    }
                } catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }
            public List<DateTime> SelectDayOffs(DateTime since)
            {
                try
                {
                    lock (LockObject)
                    {
                        List<DateTime> res = new List<DateTime>();
                        MySqlCommand selectCommand = DatabaseConnection.CreateCommand();
                        selectCommand.CommandText = "select date from dayoffs where date >= ?dSinceDate order by date";
                        selectCommand.Parameters.AddWithValue("?dSinceDate", since.ToString("yyy-MM-dd"));
                        using (MySqlDataReader reader = selectCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime dTime = reader.GetDateTime("date");
                                res.Add(dTime);
                            }
                            reader.Close();
                        }
                        return res;
                    }
                } catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }

            public bool IsInfoNeedsToBeInsertedToday(int tickerId)
            {
                try
                {
                    MySqlCommand selectCommand = DatabaseConnection.CreateCommand();
                    selectCommand.CommandText = SelectInfoDate;
                    selectCommand.Parameters.AddWithValue("?assetid", tickerId);
                    DateTime res = DateTime.MinValue;
                    using (MySqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            res = reader.GetDateTime("date");
                        }
                        reader.Close();
                    }
                    DateTime today = ServerTime.GetRealTime();
                    return !((res.Day == today.Day) && (res.Month == today.Month) && (res.Year == today.Year));
                }
                catch (Exception e)
                {
                    throw new SmartException(e);
                }
            }
        }
    }
}
