using Connect.ClientBase;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Connect.DB
{
    public class SQLDB<T> : IDB<T>
    {
        private const string ConnectionString = Environment.GetEnvironmentVariable("SQL_CONNSTRING");

        private class SQLDTO
        {
            public int ID { get; set; }
            public DateTime Time { get; set; }
            public string Body { get; set; }
        }

        public IDictionary<int, T> Read()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                try
                {
                    Logger.MethodWatch.Start("querysql");

                    string sql = "SELECT * FROM Clients";
                    var query = con.Query<SQLDTO>(sql);

                    Logger.MethodWatch.Stop("querysql");

                    Logger.MethodWatch.Start("dictionary");

                    var dict = query.ToDictionary(X => X.ID,
                        X => JsonConvert.DeserializeObject<T>(X.Body));

                    Logger.MethodWatch.Stop("dictionary");

                    return dict;
                }
                catch (SqlException exc)
                {
                    Logger.Log.error(exc.Message);
                    throw;
                }
            }
        }

        public T Read(int id)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                try
                {
                    string sql = "SELECT * FROM Clients WHERE [ID] = @ID";
                    var query = con.Query<SQLDTO>(sql, new { @ID = id });
                    if (query.Any())
                    {
                        return JsonConvert.DeserializeObject<T>(query.First().Body);
                    }
                    else
                    {
                        return default;
                    }
                }
                catch (SqlException exc)
                {
                    Logger.Log.error(exc.Message);
                    throw;
                }
            }
        }

        public void Write(IDictionary<int, T> values, bool overwrite = true)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                using (var trans = con.BeginTransaction())
                {
                    try
                    {
                        string sql = string.Empty;
                        if (overwrite)
                        {
                            sql = "UPDATE Clients " +
                                "SET Body = @Body, Time = @Time " +
                                "WHERE ID = @ID\r\n" +
                                "IF @@ROWCOUNT = 0" +
                                "INSERT INTO Clients VALUES (" +
                                "@ID, @Time, @Body)";
                        }
                        else
                        {
                            sql = "INSERT INTO Clients VALUES (" +
                                "@ID, @Time, @Body)";
                        }

                        foreach (var item in values)
                        {
                            string body = JsonConvert.SerializeObject(item.Value);
                            con.Execute(sql, new { @ID = item.Key, @Time = DateTime.Now, @Body = body }, trans);
                        }

                        trans.Commit();
                    }
                    catch (SqlException exc)
                    {
                        trans.Rollback();
                        Logger.Log.error(exc.Message);
                        throw;
                    }
                }
            }
        }

        public void Write(int id, T value, bool overwrite = true)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                try
                {
                    string sql = string.Empty;
                    if (overwrite)
                    {
                        sql = "UPDATE Clients " +
                            "SET Body = @Body, Time = @Time " +
                            "WHERE ID = @ID\r\n" +
                            "IF @@ROWCOUNT = 0" +
                            "INSERT INTO Clients VALUES (" +
                            "@ID, @Time, @Body)";
                    }
                    else
                    {
                        sql = "INSERT INTO Clients VALUES (" +
                            "@ID, @Time, @Body)";
                    }

                    string body = JsonConvert.SerializeObject(value);
                    con.Execute(sql, new { @ID = id, @Time = DateTime.Now, @Body = body });
                }
                catch (SqlException exc)
                {
                    Logger.Log.error(exc.Message);
                    throw;
                }
            }
        }

        public void Delete(int id)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                try
                {
                    string sql = "DELETE FROM Clients WHERE ID = @ID";
                    con.Execute(sql, new { @ID = id });
                }
                catch (SqlException exc)
                {
                    Logger.Log.error(exc.Message);
                    throw;
                }
            }
        }
    }
}