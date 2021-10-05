using Dropbox.Api;
using Dropbox.Api.Files;
using Logger;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Connect.DBX
{
    public static class UploadShortcuts
    {
        private static DropboxClient user = DropboxStartup.Admin;

        public static async Task CreateUploadShortcutsinFolder()
        {
            var folders = user.Files.ListFolderAsync(@"/Tax Clients/Personal Clients").Result;
            var tasklist = new List<Task>();
            foreach (var folder in folders.Entries)
            {
                var foldermeta = folder.AsFolder;
                try
                {
                    tasklist.Add(CreateUploadShortcut(foldermeta));
                }
                catch (RateLimitException ex)
                {
                    Log.crash($"Rate limit has been achieved. The upload function stopped at {folder.Name}. {ex.Message}");
                    throw;
                }
            }

            await Task.WhenAll(tasklist);
            Log.crash($"There were {Log.errorcounter} significant errors involved in this operation.");
        }

        public static async Task CreateUploadShortcut(FolderMetadata folder)
        {
            var metadatafile = await user.Files.GetMetadataAsync(folder.PathLower + "/Client/Upload", false, false, false, null);
            var metadata = metadatafile.AsFolder;

            var uploadexists = await user.Files.SearchAsync(folder.PathLower + "/Client/Upload", "Please click me to upload!");
            if (uploadexists.Matches.Count == 1)
            {
                Log.info("The user of " + folder.Name + " already has an upload link.");
                return;
            }

            var filereq = await user.FileRequests.CreateAsync("Please upload all relevant documents. Thank you for your assistance!", metadata.PathLower, null, true);
            string URLShortPath = Helpers.WriteURLShort(filereq.Url, "Please click me to UPLOAD!");
            await Upload.File(metadata, URLShortPath, WriteMode.Overwrite.Instance);
        }
    }
}