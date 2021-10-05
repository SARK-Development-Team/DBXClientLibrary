using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect.DBX
{
    public static class Password
    {
        private static DropboxClient user = DropboxStartup.Admin;

        public static async Task<List<SharedLinkMetadata>> ListLinks()
        {
            List<SharedLinkMetadata> links = new List<SharedLinkMetadata>();
            var listquery = await user.Sharing.ListSharedLinksAsync();
            links.AddRange(listquery.Links);

            while(listquery.HasMore)
            {
                listquery = await user.Sharing.ListSharedLinksAsync(null, listquery.Cursor);
                links.AddRange(listquery.Links);
            }

            return links;
        }

        public static async Task<bool?> HasPassword(string url)
        {
            try
            {
                var metadata = await user.Sharing.GetSharedLinkMetadataAsync(url);
                return metadata.LinkPermissions.RequestedVisibility.IsPassword;
            }
            catch
            {
                return null;
            }

        }

        public static async Task SetLinkPassword(string url, string password)
        {
            var accesslevel = RequestedLinkAccessLevel.Viewer.Instance;
            var audience = LinkAudience.Password.Instance;
            var sharedlinksettings = new SharedLinkSettings(true, password, null, audience, accesslevel, null, true);

            await user.Sharing.ModifySharedLinkSettingsAsync(url, sharedlinksettings, false);
        }

        public static async Task RemoveLinkPassword(string url)
        {
            var accesslevel = RequestedLinkAccessLevel.Viewer.Instance;
            var audience = LinkAudience.Public.Instance;
            var visibility = RequestedVisibility.Public.Instance;
            var sharedlinksettings = new SharedLinkSettings(false, null, null, audience, accesslevel, visibility);

            await user.Sharing.ModifySharedLinkSettingsAsync(url, sharedlinksettings, false);
        }
    }
}
