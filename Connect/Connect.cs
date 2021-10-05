using Dropbox.Api;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Gmail.v1;
using Newtonsoft.Json.Linq;
using RingCentral;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Connect
{
    /// <summary>
    /// DropboxStartup syncs with the admin@sarkinsurance.com Dropbox account through DropboxAppKeys.json in order to provide client folder functionality.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">JSON parsing did not find all the required variables.</exception>
    /// <exception cref="OAuth2Exception">There was an issue with authentication.</exception>
    /// <exception cref="DirectoryNotFoundException">Directory could not be found.</exception>
    public static class DropboxStartup
    {
        private static string AppKey = "";
        private static string AppSecret = "";
        private static string AccessToken = "";
        //private static OAuth2Response AuthFlowTry; Use with Business API functions.
       
        /// <summary>
        /// Admin become the DropboxClient by static constructor. If BusinessAPI is activated, use TeamtoAdmin to get administrative access to Dropbox.
        /// </summary>
        public static DropboxClient Admin { get; private set; }
        private static DropboxTeamClient teamClient;
        public static string AuthorizationCode { get; set; }
        private static JObject read;

        /// <summary>
        /// Static constructor that parses DropboxAppKeys.json in order to get AppKeys, AppSecret, and AccessToken. Then, it tests the Oauth2 authentication with listing Paper Docs.
        /// </summary>
        static DropboxStartup()
        {
            //DropboxClient.Properties.Resources.path links to the csproj's Resources.resx, shortcut to changing filepaths everywhere.
            string path = FilePaths.DropboxAppKeys;

            var reader = JObject.Parse(System.IO.File.ReadAllText(path));
            int count = 0;
            foreach (var prop in reader.Properties())
            {
                
                if (prop.Name == "AppKey")
                {
                    AppKey = prop.Value.ToString();
                    ++count;
                }
                else if (prop.Name == "AppSecret")
                {
                    AppSecret = prop.Value.ToString();
                    ++count;
                }
                else if (prop.Name == "AccessToken")
                {
                    AccessToken = prop.Value.ToString();
                    ++count;
                }
                else
                {
                    if (count < 2)
                    {
                        throw new ArgumentOutOfRangeException("Missing Json property.");
                    }
                    
                }
            }
            

            read = reader;
            teamClient = new DropboxTeamClient(AccessToken);
            Admin = new DropboxClient(AccessToken);

            try
            {
                Admin.Paper.DocsListAsync().Wait();
            }
            catch (DropboxException)
            {
                Logger.Log.error("ERROR: FAILURE TO LOGIN TO DROPBOX ACCOUNTS USING KEYS. PLEASE TRY AGAIN."
                    + "AppInfo Exception Error, 76");
                throw new OAuth2Exception("Could not validate login. Please sync.");
            }
            

        }

        #region //These functions are based on the Business API, and allow the user to connect to Business Dropbox Accounts, acting as the admin. Do not use them for individual accounts.
        //internal async static Task Authorize()
        //{
        //    Dropbox.Api.Team.TeamGetInfoResult teamInfo;
        //    try
        //    {
        //        teamInfo = await teamClient.Team.GetInfoAsync();
        //        return;
        //    }
        //    catch (DropboxException)
        //    {
        //        var msg = MessageBox.Show("ASYNC error, would you like to get a new access code?", "ASYNC ERROR", MessageBoxButtons.OKCancel);
        //        if (msg == DialogResult.OK)
        //        {
        //            goto Authorize;
        //        }
        //        else
        //        {
        //            return;
        //        }

        //    }

        //Authorize:
        //    Uri redirect = new Uri("https://www.dropbox.com/oauth2/");
        //    var getURL = DropboxOAuth2Helper.GetAuthorizeUri(AppKey);
        //    System.Diagnostics.Process.Start(getURL.ToString());

        //    var token = new GetAuthToken();
        //    token.BringToFront();
        //    Application.Run(token);

        //    bool check = true;
        //    int counter = 0;

        //    while (check)
        //    {
        //        while (string.IsNullOrEmpty(AuthorizationCode))
        //        {
        //            MessageBox.Show("BEEP! NO PARAMETERS GIVEN", "Null Parameter");
        //            goto FinishLoop;
        //        }

        //        try
        //        {
        //            AuthFlowTry = await DropboxOAuth2Helper.ProcessCodeFlowAsync(AuthorizationCode, AppKey, AppSecret);
        //            check = false;

        //        }
        //        catch (OAuth2Exception)
        //        {
        //            MessageBox.Show("BEEP! INCORRECT CODE GIVEN.", "Wrong Authorization Code");
        //            goto FinishLoop;
        //        }


        //    FinishLoop:
        //        if (check)
        //        {
        //            token = new GetAuthToken();
        //            token.BringToFront();
        //            Application.Run(token);
        //            counter++;
        //        }

        //        if (counter >= 3)
        //        {
        //            check = false;
        //            throw new Exception("Too many false entries. Please try again.");
        //        }
        //    }

        //    var AuthFlow = AuthFlowTry;
        //    AccessToken = AuthFlow.AccessToken;


        //    if (read.Property("AccessToken").Value.ToString() != AccessToken)
        //    {
        //        read.Property("AccessToken").Value = new JValue(AccessToken);
        //    }

        //    teamClient = new DropboxTeamClient(AccessToken);
        //    admin = new Dropbox.Api.DropboxClient(AccessToken);
        //    System.IO.File.WriteAllText(@"..\..\Functions\AppKeys.json", Newtonsoft.Json.JsonConvert.SerializeObject(read, Newtonsoft.Json.Formatting.Indented));

        //    //MessageBox.Show(string.Format("The access token is {0}.", AccessToken), "SUCCESS!");
        //    //Console.WriteLine(AccessToken);

        //}

        //public async static Task TeamtoAdmin()
        //{
        //    try
        //    {
        //        var msg = await teamClient.Team.GetInfoAsync();
        //        Console.WriteLine(msg.Name + " " + msg.TeamId);
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //    var GetMembers = await teamClient.Team.MembersListAsync();

        //    foreach (var member in GetMembers.Members)
        //    {
        //        var GetAdmin = member;

        //        if (GetAdmin.Role.IsTeamAdmin)
        //        {
        //            var GetAdminProfile = GetAdmin.Profile;
        //            string AdminID = GetAdminProfile.TeamMemberId;
        //            admin = teamClient.AsAdmin(AdminID);
        //            //MessageBox.Show(string.Format("{0} is the admin with an id of {1}.", GetAdminProfile.Name.DisplayName,AdminID));
        //        }
        //    }

        //    var adminacc = await admin.Users.GetCurrentAccountAsync();
        //    var pathroot = adminacc.RootInfo.RootNamespaceId;
        //    admin = admin.WithPathRoot(new Dropbox.Api.Common.PathRoot.Root(pathroot));
        //}

        #endregion
    }

    /// <summary>
    /// SQLConnect has a static constructor that looks in DropboxAppKeys.json for key name ConnectionString to find connection string to office SQL server.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">DropboxAppKeys.json could not be found.</exception>
    public static class SqlConnect
    {
        static string path = FilePaths.DropboxAppKeys;
        /// <summary>
        /// ConnectionString for SQL Server, provided by static constructor.
        /// </summary>
        public static string ConnectionString { get; private set; }

        static SqlConnect()
        {
            var reader = JObject.Parse(System.IO.File.ReadAllText(path));
            foreach (var prop in reader.Properties())
            {
                if (prop.Name == "ConnectionString")
                {
                    ConnectionString = prop.Value.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Connects to Google's Calendar service under sark@sarkinsurance.com. Static constructor authenticates with OAUTH2, provides purpose to public CalendarService Service.
    /// </summary>
    /// <exception cref="OAuth2Exception"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static class CalendarConnect
    {
        private static string path = FilePaths.CalendarAppKeys;

        private static string[] Scopes = { CalendarService.Scope.CalendarEvents };
        private static string ApplicationName = "Client Appointment Manager";
        public static CalendarService Service { get; private set; }

        static CalendarConnect()
        {
            UserCredential credential;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = FilePaths.CalendarToken;
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    System.Threading.CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            Service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }
    }

    /// <summary>
    /// Connects to Gmail service under tax@taxkp.com. Static constructor authenticates with OAUTH2, provides purpose to public GmailService Service.
    /// </summary>
    /// <exception cref="OAuth2Exception"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static class GmailConnect
    {
        private static string path = FilePaths.MailAppKeys;
        //private static string tokenfilepath = @"..\..\..\Data\GoogleMailToken\Google.Apis.Auth.OAuth2.Responses.TokenResponse-user"; Gives filepath for Gmail token.
        public static string AccessToken { get; private set; }

        static string[] Scopes = { GmailService.Scope.GmailCompose };
        static string ApplicationName = "GmailClientManager";
        public static GmailService Service { get; private set; }

        static GmailConnect()
        {
            UserCredential credential;

            using (var stream =
                new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = FilePaths.MailToken;
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    System.Threading.CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Gmail API service.
            Service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

        }
    }

    /// <summary>
    /// Connects to RingCentral service under 4082629190. Static constructor parses through RingCentralToken.json, but does not provide OAUTH2 authentication. async Authorize() is needed before any
    /// SMS can be commanded.
    /// </summary>
    /// <exception cref="OAuth2Exception"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static class RingCentralConnect 
    {
        private static string path = FilePaths.RingCentralToken;
        private static string clientid;
        private static string clientsecret;
        private static string accountpassword;
        private static string extension;
        /// <summary>
        /// Accountphone for current RingCentralApplication, used in several other functions.
        /// </summary>
        public static string accountphone { get; private set; }
        public static RestClient restClient { get; private set; }
        
        static RingCentralConnect()
        {
            if (!File.Exists(path))
            {
                throw new RingCentral.JsonDeserializeException("JSON properties could not be found. Please move file to correct directory.", new Newtonsoft.Json.JsonReaderException());
            }

            using (var reader = new System.IO.StreamReader(path))
            {
                var jsonstring = reader.ReadToEnd();
                JObject json = JObject.Parse(jsonstring);
                clientid = (string)json.Property("ClientID").Value;
                clientsecret = (string)json.Property("ClientSecret").Value;
                accountphone = (string)json.Property("AccountPhone").Value;
                accountpassword = (string)json.Property("Password").Value;
                extension = (string)json.Property("Ext").Value;
            }
        }

        public static async Task Authorize()
        {
            restClient = new RestClient(clientid, clientsecret, true, "SMS ClientManager");
            await restClient.Authorize(accountphone, extension, accountpassword);
        }
    }
}






