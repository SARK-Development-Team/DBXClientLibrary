using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZoomNet;

namespace Connect
{
    public class Zoom
    {
        const string devapiKey = Environment.GetEnvironmentVariable("DEV_ZOOM_APIKEY");
        const string devapiSecret = Environment.GetEnvironmentVariable("DEV_ZOOM_APISECRET");
        const string devIMToken = Environment.GetEnvironmentVariable("DEV_ZOOM_APITOKEN");

        const string apiKey = Environment.GetEnvironmentVariable("ZOOM_APIKEY");
        const string apiSecret = Environment.GetEnvironmentVariable("ZOOM_APISECRET");
        const string IMToken = Environment.GetEnvironmentVariable("ZOOM_APITOKEN");

        const string kaumapiKey = Environment.GetEnvironmentVariable("KAUM_ZOOM_APIKEY");;
        const string kaumapiSecret = Environment.GetEnvironmentVariable("KAUM_ZOOM_APISECRET");

        ZoomClient Client;

        public ZoomNet.Models.User MainUser { get; private set; } = null;

        public Zoom()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Client = new ZoomClient(new JwtConnectionInfo(devapiKey, devapiSecret));
            }
            else
            {
                if (Environment.UserName == "Kaumudi Walinjkar")
                {
                    Client = new ZoomClient(new JwtConnectionInfo(kaumapiKey, kaumapiSecret));
                }
                else
                {
                    Client = new ZoomClient(new JwtConnectionInfo(apiKey, apiSecret));
                }
            }
    
        }

        public async Task RegisterUser()
        {
            var userquery = await Client.Users.GetAllAsync();
            MainUser = userquery.Records[0];
        }

        public async Task<ZoomNet.Models.ScheduledMeeting> ScheduleMeetingAsync(string meetingTitle, string meetingAgenda, DateTime time, int minutes = 30)
        {
            return await Client.Meetings.CreateScheduledMeetingAsync(MainUser.Id, meetingTitle, meetingAgenda, time, minutes, null);
        }

        public async Task DeleteMeetingAsync(ZoomNet.Models.ScheduledMeeting meeting)
        {
            await Client.Meetings.DeleteAsync(meeting.Id);
        }
    }
}
