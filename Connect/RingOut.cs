using Newtonsoft.Json;
using RingCentral;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace Connect
{
    public static class RingOutConnect
    {
        const string DEV_RINGCENTRAL_CLIENTID = Environment.GetEnvironmentVariable("DEV_RC_CLIENTID");
        const string DEV_RINGCENTRAL_CLIENTSECRET = Environment.GetEnvironmentVariable("DEV_RC_CLIENTSECRET");
        const bool DEV_RINGCENTRAL_PRODUCTION = false;

        const string RINGCENTRAL_CLIENTID = Environment.GetEnvironmentVariable("RC_CLIENTID");
        const string RINGCENTRAL_CLIENTSECRET = Environment.GetEnvironmentVariable("RC_CLIENTSECRET");
        const bool RINGCENTRAL_PRODUCTION = true;

        const string DEV_RINGCENTRAL_USERNAME = Environment.GetEnvironmentVariable("DEV_RC_USERNAME");
        const string RINGCENTRAL_USERNAME = Environment.GetEnvironmentVariable("RC_USERNAME");

        const string RINGCENTRAL_PASSWORD = Environment.GetEnvironmentVariable("RC_PASSWORD");
        const string RINGCENTRAL_EXTENSION = "101";

        public static RestClient RCSDK { get; private set; }
        public static string Phone
        {
            get
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    return DEV_RINGCENTRAL_USERNAME;
                }
                else
                {
                    return RINGCENTRAL_USERNAME;
                }
            }
        }

        public static async Task Authorize()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                string id = DEV_RINGCENTRAL_CLIENTID;
                string secret = DEV_RINGCENTRAL_CLIENTSECRET;
                string password = RINGCENTRAL_PASSWORD;

                RCSDK = new RestClient(id, secret, DEV_RINGCENTRAL_PRODUCTION);
                await RCSDK.Authorize(DEV_RINGCENTRAL_USERNAME, RINGCENTRAL_EXTENSION, password);
            }
            else
            {
                string id = RINGCENTRAL_CLIENTID;
                string secret = RINGCENTRAL_CLIENTSECRET;
                string password = RINGCENTRAL_PASSWORD;

                RCSDK = new RestClient(id, secret, RINGCENTRAL_PRODUCTION);
                await RCSDK.Authorize(RINGCENTRAL_USERNAME, RINGCENTRAL_EXTENSION, password);
            }
        }
    }

    public class PhoneUser
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        public string E164Phone()
        {
            if (!string.IsNullOrEmpty(Phone))
            {
                string filter = Regex.Replace(Phone, "[^0-9.]", "");
                if (!filter.StartsWith('1'))
                {
                    filter = "+1" + filter;
                }

                return filter;
            }

            return null;
        }
    }

    public static class RingOutFunctions
    {
        private static Dictionary<string, PhoneUser> _users;
        public static Dictionary<string, PhoneUser> Users
        {
            get
            {
                if (_users == null)
                {
                    string path = FilePaths.Authentication + "Phones.json";
                    string json = File.ReadAllText(path);
                    _users = JsonConvert.DeserializeObject<Dictionary<string, PhoneUser>>(json);
                }

                return _users;
            } 
        }

        public static async Task CallClient(string tophone)
        {
            //Filters everything out of the tophone.
            tophone = Regex.Replace(tophone, "[^0-9.]", "");

            //Grabs the from phone based on the user's desktop.
            string key = Environment.UserName;
            string fromphone = RingOutConnect.Phone;

            if (Users.ContainsKey(key))
            {
                string e164 = Users[key].E164Phone();
                if (e164 != null)
                {
                    fromphone = e164;
                }
            }

            if (RingOutConnect.RCSDK == null)
            {
                await RingOutConnect.Authorize();
            }

            var parameters = new MakeRingOutRequest()
            {
                to = new MakeRingOutCallerInfoRequestTo()
                {
                    phoneNumber = tophone
                },
                from = new MakeRingOutCallerInfoRequestFrom()
                {
                    phoneNumber = fromphone
                },
                callerId = new MakeRingOutCallerInfoRequestTo()
                {
                    phoneNumber = RingOutConnect.Phone
                },
                playPrompt = false
            };

            var rcclient = RingOutConnect.RCSDK;
            await rcclient.Restapi().Account().Extension().RingOut().Post(parameters);
        }

        public static async Task TestCalls(string tophone)
        {
            //Filters everything out of the tophone.
            tophone = Regex.Replace(tophone, "[^0-9.]", "");

            if (RingOutConnect.RCSDK == null)
            {
                await RingOutConnect.Authorize();
            }

            foreach (var user in Users)
            {
                
                string fromphone = user.Value.E164Phone();
                Console.WriteLine($"Calling test phone # using {fromphone} for user {user.Value.Name} | ENV: {user.Key}.");
                Console.ReadLine();

                 var parameters = new MakeRingOutRequest()
                {
                    to = new MakeRingOutCallerInfoRequestTo()
                    {
                        phoneNumber = tophone
                    },
                    from = new MakeRingOutCallerInfoRequestFrom()
                    {
                        phoneNumber = fromphone ?? RingOutConnect.Phone
                    },
                    callerId = new MakeRingOutCallerInfoRequestTo()
                    {
                        phoneNumber = RingOutConnect.Phone
                    },
                    playPrompt = false
                };


                var rcclient = RingOutConnect.RCSDK;
                await rcclient.Restapi().Account().Extension().RingOut().Post(parameters);
                
            }
        }
    }
}
