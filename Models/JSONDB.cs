using System.Collections.Generic;
using System.Data.SqlClient;
using Logger;
using Connect.ClientBase;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Connect.DB
{
    /// <summary>
    /// Temporarily deprecate to replace everything with ShardDB.
    /// </summary>
    public static class JSONDB
    {
        //Simple query function that returns ClientData from JSON in current client object.
        public static List<Client> QueryfromJSON()
        {
            List<Client> clientlist = new List<Client>();
            string path = FilePaths.ClientData;
            string unencryptedjson = File.ReadAllText(path);
            string json = EncryptJSON.Decrypt(unencryptedjson, "agreentejada");
            //string json = unencryptedjson;
            clientlist = JsonConvert.DeserializeObject<List<Client>>(json);
            return clientlist;
        }

        //Pushes a ClientList of current Client object into ClientData.json. Useful if ClientList is edited.
        public static void PushListtoJSON(List<Client> clientlist)
        {
            string path = FilePaths.ClientData;
            if (!File.Exists(path))
            {
                var filecreated = File.Create(path);
                filecreated.Close();
            }

            string unencryptedjson = JsonConvert.SerializeObject(clientlist, Formatting.Indented, new JsonSerializerSettings { DateFormatString = "MM'/'dd'/'yyyy" });
            string json = EncryptJSON.Encrypt(unencryptedjson, "agreentejada");
            File.WriteAllText(path, json);
        }

        //Pushes new client into current ClientData JSON file after querying JSON file. New client ID is one higher than last ID on list.
        public static void UpdateClientinJSON(Client client)
        {
            string path = FilePaths.ClientData;
            if (!File.Exists(path))
            {
                var filecreated = File.Create(path);
                filecreated.Close();
            }
            var clientlist = QueryfromJSON();
            for (int i = 0; i < clientlist.Count; i++)
            {
                if (clientlist[i].ID == client.ID)
                {
                    clientlist[i] = client;
                    break;
                }
            }
            PushListtoJSON(clientlist);
        }

        public static void RemoveClientinJSON(Client client)
        {
            var clientlist = QueryfromJSON();

            if (clientlist.Remove(clientlist.Where(cli => cli.ID == client.ID).Single()))
            {
                PushListtoJSON(clientlist);
            }
            else
            {
                throw new System.InvalidOperationException("The ID requested could not be found to be deleted.");
            }
        }

        public static void AddClientinJSON(Client client)
        {
            var clientlist = QueryfromJSON();
            if (!clientlist.Where(cli => cli.ID == client.ID).Any())
            {
                clientlist.Add(client);
                PushListtoJSON(clientlist);
            }
            else
            {
                throw new System.InvalidOperationException("The client asked to be added already exists in the DB. Please consider another ID.");
            }
        }
    }

    public static class DeletedJSONDB
    {
        //Simple query function that returns ClientData from JSON in current client object.
        public static List<Client> QueryfromJSON()
        {
            List<Client> clientlist = new List<Client>();
            string path = FilePaths.Deleted;
            string unencryptedjson = File.ReadAllText(path);
            string json = EncryptJSON.Decrypt(unencryptedjson, "deleted");
            //string json = unencryptedjson;
            clientlist = JsonConvert.DeserializeObject<List<Client>>(json);
            return clientlist;
        }

        //Pushes a ClientList of current Client object into ClientData.json. Useful if ClientList is edited.
        public static void PushListtoJSON(List<Client> clientlist)
        {
            string path = FilePaths.Deleted;
            if (!File.Exists(path))
            {
                var filecreated = File.Create(path);
                filecreated.Close();
            }

            string unencryptedjson = JsonConvert.SerializeObject(clientlist, Formatting.Indented, new JsonSerializerSettings { DateFormatString = "MM'/'dd'/'yyyy" });
            string json = EncryptJSON.Encrypt(unencryptedjson, "agreentejada");
            File.WriteAllText(path, json);
        }

        //Pushes new client into current ClientData JSON file after querying JSON file. New client ID is one higher than last ID on list.
        public static void UpdateClientinJSON(Client client)
        {
            string path = FilePaths.Deleted;
            if (!File.Exists(path))
            {
                var filecreated = File.Create(path);
                filecreated.Close();
            }
            var clientlist = QueryfromJSON();
            for (int i = 0; i < clientlist.Count; i++)
            {
                if (clientlist[i].ID == client.ID)
                {
                    clientlist[i] = client;
                    break;
                }
            }
            PushListtoJSON(clientlist);
        }

        public static void RemoveClientinJSON(Client client)
        {
            var clientlist = QueryfromJSON();

            if (clientlist.Remove(clientlist.Where(cli => cli.ID == client.ID).Single()))
            {
                PushListtoJSON(clientlist);
            }
            else
            {
                throw new System.InvalidOperationException("The ID requested could not be found to be deleted.");
            }
        }

        public static void AddClientinJSON(Client client)
        {
            var clientlist = QueryfromJSON() ?? new List<Client>();
            if (!clientlist.Where(cli => cli.ID == client.ID).Any())
            {
                clientlist.Add(client);
                PushListtoJSON(clientlist);
            }
            else
            {
                throw new System.InvalidOperationException("The client asked to be added already exists in the DB. Please consider another ID.");
            }
        }
    }

}
