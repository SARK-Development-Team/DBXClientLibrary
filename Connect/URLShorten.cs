using System;
using System.Threading.Tasks;

namespace Connect
{
    public static class URLShorten
    {
        const string functionkey = "WBQ4cxvsCuqsdUYa0W9IGT0qTjPmAQwV6CeTrSUXUVXjZSNNQlCsaw==";
        public static int MaxClicks { get; set; } = 1000;
        public static DateTime Expiration { get; set; } = DateTime.Now.AddDays(365);

        public static async Task<string> Shorten(string longURL, string vanity)
        {
            var client = new AzureShortener.ShortenClient(functionkey);
            var link = await client.Shorten(longURL, vanity, MaxClicks, Expiration);
            return "https://tax-kp.com/" + link.ShortURL;
        }
    }
}
