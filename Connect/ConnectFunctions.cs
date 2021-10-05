using System;

using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using RingCentral;
using MimeKit;
using System.Net.Mail;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using EverSign;
using System.IO;
using System.Net.Http;
using EverSign.Models;

namespace Connect
{
    public static class CalendarFunctions
    {
        private static CalendarService calendar = CalendarConnect.Service;

        public static List<string> EventsListener(DateTime time)
        {
            var listevents = new List<string>();
            EventsResource.ListRequest request = calendar.Events.List("primary");
            request.TimeMin = time.Date;
            request.TimeMax = time.Date.AddDays(1);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    listevents.Add(string.Format("{0} ({1})", when, eventItem.Summary));
                }
            }
            else
            {
                listevents.Add("No upcoming events found.");
            }

            return listevents;
        }
        public static void EventsWriter(DateTime date, string EventName, string EventDescription, string ClientName = null, string ClientEmail = null)
        {
            // Refer to the Java quickstart on how to setup the environment:
            // https://developers.google.com/calendar/quickstart/java
            // Change the scope to CalendarScopes.CALENDAR and delete any stored
            // credentials.
            Event scheduledevent = new Event();

            scheduledevent.Start = new EventDateTime() { DateTime = date, TimeZone = "America/Los_Angeles" };
            scheduledevent.End = new EventDateTime() { DateTime = date.AddHours(0.5), TimeZone = "America/Los_Angeles" };

            scheduledevent.Summary = EventName;
            scheduledevent.Description = EventDescription;

            if (!(ClientEmail == null))
            {
                scheduledevent.Attendees = new List<EventAttendee> { new EventAttendee() {
                DisplayName = ClientName ?? "Global Tax Client",
                Email = ClientEmail } };
            }

            scheduledevent.Reminders = new Event.RemindersData() { UseDefault = true };
            scheduledevent.Visibility = "public";
            var eventadd = calendar.Events.Insert(scheduledevent, "primary");
            eventadd.Execute();
        }
    }

    public static class MailFunctions
    {
        public static void emailMessage(string recipientEmail, string subject, BodyBuilder messagebody = null, string[] attachments = null)
        {
            MailMessage rawmessage = new MailMessage();
            //MimeMessage rawmessage = new MimeMessage();
            rawmessage.From = new MailAddress("tax@taxkp.com", "TAX - GLOBAL TAX SERVICES");
            rawmessage.To.Add(new MailAddress(recipientEmail));
            rawmessage.Subject = subject;

            rawmessage.IsBodyHtml = true;
            rawmessage.Body = messagebody.HtmlBody;

            if (attachments != null)
            {
                for (int i = 0; i < attachments.Length; i++)
                {
                    rawmessage.Attachments.Add(new System.Net.Mail.Attachment(attachments[i]));
                }
                
            }

            var buffer = new MemoryStream();
            MimeKit.MimeMessage mimeMessage = MimeMessage.CreateFromMailMessage(rawmessage);
            mimeMessage.WriteTo(buffer);
            byte[] rawbytes = new byte[buffer.Length];
            rawbytes = buffer.ToArray();

            char[] padding = { '=' };
            string returnValue = System.Convert.ToBase64String(rawbytes)
            .TrimEnd(padding).Replace('+', '-').Replace('/', '_');


            if (buffer.CanRead)
            {
                buffer.Dispose();
            }

            var message = new Google.Apis.Gmail.v1.Data.Message();
            message.Raw = returnValue;

            var request = GmailConnect.Service.Users.Messages;
            
            request.Send(message, "me").Execute();
        }
    }

    public static class MessagingFunctions
    {
        public static async Task SendSMS(string phone, string message)
        {
            await RingCentralConnect.Authorize();

            var parameters = new CreateSMSMessage();
            parameters.from = new MessageStoreCallerInfoRequest() { phoneNumber = RingCentralConnect.accountphone };
            parameters.to = new MessageStoreCallerInfoRequest[] { new MessageStoreCallerInfoRequest() { phoneNumber = phone } };
            parameters.text = message;
            var resp = await RingCentralConnect.restClient.Restapi().Account().Extension().Sms().Post(parameters);
        }
    }

    public static class RebrandlyFunctions
    {
        static string APIKey = "6d391cca688b4f3b99162828b0a1532a";

        public static async Task<string> ShortenLinkAsync(string longURL)
        {
            var payload = new
            {
                destination = longURL,
                domain = new
                {
                    fullName = "rebrand.ly"
                }
                //, slashtag = "A_NEW_SLASHTAG"
                //, title = "Rebrandly YouTube channel"
            };

            using (var httpClient = new HttpClient { BaseAddress = new Uri("https://api.rebrandly.com") })
            {
                httpClient.DefaultRequestHeaders.Add("apikey", APIKey);

                var body = new StringContent(
                    JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync("/v1/links", body))
                {
                    response.EnsureSuccessStatusCode();

                    var link = JsonConvert.DeserializeObject<dynamic>(
                        await response.Content.ReadAsStringAsync());

                    return "https://" + link.shortUrl;
                }
            }
        }
    }
}