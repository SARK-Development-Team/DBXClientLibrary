using Connect.ClientBase;
using Dapper;
using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Connect.DB
{
    public class SQLCached : IDB<Client>
    {
        private const string ConnectionString = Environment.GetEnvironmentVariable("SQL_CONNSTRING");

        private static readonly string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dropbox Integrate");
        private static string password = "adminaccess";

        private LiteDatabase cache;
        private ILiteCollection<Client> cacheCollection;

        //ReadTask is a class object to prevent overusage of query search. With this configuration. ReadTask will only run if it is free.
        private Task readTask;

        private Task readsingleTask;

        public SQLCached()
        {
            Directory.CreateDirectory(cachePath);
            string filepath = Path.Combine(cachePath, "cache.litedb");

            string cachestring = $"Filename={filepath};Password={password};";
            cache = new LiteDatabase(cachestring);
            cacheCollection = cache.GetCollection<Client>("clients");
        }

        private class SQLDTO
        {
            public int ID { get; set; }
            public DateTime Time { get; set; }
            public string Body { get; set; }
        }

        /// <summary>
        /// Reads from cache, and performs a running SQL query on the server to retrieve data. Updates cache.
        /// </summary>
        /// <returns></returns>
        public IDictionary<int, Client> Read()
        {
            try
            {
                Logger.MethodWatch.Start("querycache");

                //Retrieve data from the cache.
                var cachequery = cacheCollection.FindAll();

                Logger.MethodWatch.Stop("querycache");

                //Run a task that retrieves data from the server.
                if (readTask == null)
                {
                    readTask = new Task(() =>
                    {
                        using (SqlConnection con = new SqlConnection(ConnectionString))
                        {
                            Logger.MethodWatch.Start("querysql");

                            string sql = "SELECT * FROM Clients";
                            var result = con.Query<SQLDTO>(sql);
                            var clients = result.Select(X => JsonConvert.DeserializeObject<Client>(X.Body));

                            cacheCollection.Upsert(clients);

                            //Remove deleted clients from cache.
                            int[] resultIDs = result.Select(X => X.ID).ToArray();

                            try
                            {
                                if (resultIDs.Length != cacheCollection.Count())
                                {
                                    int amount = cacheCollection.DeleteMany(X => !resultIDs.Contains(X.ID));
                                    Logger.Log.error("Cache deleted " + amount + " IDs from cache.");
                                }
                            }
                            catch (Exception) { }


                            Logger.MethodWatch.Stop("querysql");
                        }
                    });

                    readTask.Start();
                    readTask = null;
                }

                Logger.MethodWatch.Start("dictionary");

                var dict = cachequery.ToDictionary(X => X.ID, X => X);

                Logger.MethodWatch.Stop("dictionary");

                return dict;
            }
            catch (SqlException exc)
            {
                Logger.Log.error(exc.Message);
                throw;
            }
        }

        public Client Read(int id)
        {
            try
            {
                //Retrieves from cache.
                var cacheclient = cacheCollection.FindById(id);

                if (readsingleTask == null)
                {
                    readsingleTask = new Task(() =>
                    {
                        using (SqlConnection con = new SqlConnection(ConnectionString))
                        {
                            string sql = "SELECT * FROM Clients WHERE [ID] = @ID";
                            var query = con.Query<SQLDTO>(sql, new { @ID = id });
                            if (query.Any())
                            {
                                var value = JsonConvert.DeserializeObject<Client>(query.First().Body);
                                cacheCollection.Update(value);
                            }
                        }
                    });

                    readsingleTask.Start();
                    readsingleTask = null;
                }

                return cacheclient;
            }
            catch (SqlException exc)
            {
                Logger.Log.error(exc.Message);
                throw;
            }
        }

        public void Write(IDictionary<int, Client> values, bool overwrite = true)
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

                        //Updates cache after the server is updated.
                        cacheCollection.Upsert(values.Values);
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

        public void Write(int id, Client value, bool overwrite = true)
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

                    //Updates cache after the server is updated.
                    cacheCollection.Upsert(value);
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
                    string sql = "DELETE FROM Deleted WHERE ID = @ID" +
                        "\r\n" + "INSERT INTO Deleted SELECT * FROM Clients WHERE ID = @ID";
                    con.Execute(sql, new { @ID = id });

                    sql = "DELETE FROM Clients WHERE ID = @ID";
                    con.Execute(sql, new { @ID = id });

                    cacheCollection.Delete(id);
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