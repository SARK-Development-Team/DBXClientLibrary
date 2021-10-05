using Dropbox.Api.Files;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Connect.ClientBase
{
    public class Client
    {
        [LiteDB.BsonId]
        public int ID { get; set; }
        public string Name { get 
            { 
                if (ClientType == ClientType.Business)
                {
                    return InternalData.CompanyName;
                }
                else if (InternalData.Married != MarriedStatus.Single)
                {
                    return SingleName + " & " + InternalData.SpouseName;
                }
                else
                {
                    return SingleName;
                }
            } }

        public string SingleName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public ClientType ClientType { get; set; } = ClientType.Personal;

        public string DropboxSharedURL { get; set; }
        public string ShortenedURL { get; set; }

        [Newtonsoft.Json.JsonProperty(Order = 1)]
        public ClientInternal InternalData { get; set; } = new ClientInternal();
        [Newtonsoft.Json.JsonProperty(Order = 2)]
        public ClientContract ContractData { get; set; } = new ClientContract();
    }

    public class OldClient
    {
        //Proprties for the client class, with JSON attributes included.
        public int ID { get; set; }
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value?.Trim(); }
        }
        public string Email { get; set; }
        public string Phone { get; set; }

        public ClientType clientType { get; set; } = ClientType.Personal;
        public string DropboxSharedUrl { get; set; }
        public string ShortenedUrl { get; set; }

        [Newtonsoft.Json.JsonProperty(Order = 1)]
        public ClientInternal internalData { get; set; } = new ClientInternal();
        [Newtonsoft.Json.JsonProperty(Order = 2)]
        public ClientContract contractData { get; set; } = new ClientContract();
    }

    /// <summary>
    /// Used to find the directory and documents for the client.
    /// </summary>
    public enum ClientType
    {
        Personal,
        Business
    }

    /// <summary>
    /// Used to find the married status of the client.
    /// </summary>
    public enum MarriedStatus
    {
        Single = 1,
        Married,
        FilingSeparately
    }

    public class ClientInternal
    {
        //App-related.
        public bool Active { get; set; } = true;
        public string comments { get; set; }

        //Client-related. Splits into married and business categories.
        public MarriedStatus Married { get; set; } = MarriedStatus.Single;
        public string SpouseName { get; set; }
        public string SpouseEmail { get; set; }
        public string SpousePhone { get; set; }

        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyType { get; set; }

        //Dropbox-related.
        public string FileRequestURL { get; set; }
        public string DropboxFolderPath { get; set; }
        public string LocalFolderName { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [LiteDB.BsonIgnore]
        public FolderMetadata DropboxFolder { get; set; }

        //Metadata, while not strictly used, useful for logging purposes.
        public DateTime FileCreated { get; set; }
    }

    public class ClientContract
    {
        public string total { get; set; } = "0";
        public Service[] services { get; set; }

        public struct Service
        {
            public string servicetype { get; set; }
            public string description { get; set; }
            public string cost { get; set; }

            public Service(string serviceType, string descrip, string costed)
            {
                servicetype = serviceType;
                description = descrip;
                cost = costed;
            }
        }
    }
}


