using Dropbox.Api;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Connect.DBX
{
    internal static class Helpers
    {
        private static DropboxClient user = DropboxStartup.Admin;

        // Takes the UploadFolder object made during AddNewFolder method, and generates a file request to that folder. Then, it generates a .url shortcut to the folder and puts it inside
        // the upload folder.
        internal static async Task<string> CreateFileReqShortcut(string UploadFolder)
        {
            var filereq = await user.FileRequests.CreateAsync("Please upload all relevant documents. Thank you for your assistance!", UploadFolder + "/" + Attributes.Year, null, true);
            string URLShortPath = WriteURLShort(filereq.Url, "Please click me to UPLOAD!");
            await Upload.File(UploadFolder, URLShortPath, "Click to Upload!", writeMode: Dropbox.Api.Files.WriteMode.Overwrite.Instance);
            return filereq.Url;
        }

        //Takes a URL, a filename, and returns a path to a .url shortcut with that filename.
        internal static string WriteURLShort(string URL, string FileName)
        {
            string[] URLLines = {
                @"[InternetShortcut]",
                @"IDList=",
                @"URL=" + URL };

            string path = System.IO.Path.GetTempPath();
            if (!path.EndsWith(@"\"))
            {
                path = path + @"\";
            }
            path = path + FileName + ".url";

            System.IO.File.WriteAllLines(path, URLLines);
            return path;
        }

        // Creates an info log that is used at the end of the functions to provide Client information for admin inside Dropbox folder, then attempts to upload that info to Client folder.
        internal static string InfoLog(List<string> info)
        {
            string userlog = System.IO.Path.GetTempPath();
            if (!userlog.EndsWith(@"\"))
            {
                userlog = userlog + @"\";
            }
            userlog = userlog + "user.log";
            File.WriteAllLines(userlog, info);
            return userlog;
        }
    }

    #region //More team functions. Ignore these unless you are trying to find the root folder, in which case they will help you wind the path file.

    /*
    private async Task BrowseDocuments()
    {
        await AppInfo.TeamtoAdmin();
        var direct = await AppInfo.teamClient.Team.NamespacesListAsync(100);
        var FolderMeta = direct.Namespaces;
        for (int i = 0; i < FolderMeta.Count; ++i)
        {
            FolderNames.Add(FolderMeta[i].Name);

            if (FolderMeta[i].NamespaceType.IsAppFolder == true)
            { FolderPath.Add("AppFolder"); }
            else if (FolderMeta[i].NamespaceType.IsTeamFolder == true)
            { FolderPath.Add("TeamFolder"); }
            else if (FolderMeta[i].NamespaceType.IsSharedFolder == true)
            { FolderPath.Add("SharedFolder"); }
            else if (FolderMeta[i].NamespaceType.IsOther == true)
            { FolderPath.Add("Maybe Root?"); }
            else { FolderPath.Add("Eh, couldn't figure it out."); }
        }
        using (TextWriter writer = new StreamWriter(@"C:\Users\ramsi\Global Tax Services Dropbox\love.txt"))
        {
            foreach (string name in FolderNames)
            {
                int index = FolderNames.IndexOf(name);
                string namearray = string.Format("{0} has a namespace type of {1}", FolderNames[index], FolderPath[index]);
                writer.WriteLine(namearray);
            }
        }
    }
    private async Task<string> GetUserInfo()
    {
        await AppInfo.TeamtoAdmin();
        var full = await AppInfo.admin.Users.GetCurrentAccountAsync();
        string box = string.Format("{0} - {1}", full.Name.DisplayName, full.Email);
        return box;
    }
    */

    #endregion //More team functions. Ignore these unless you are trying to find the root folder, in which case they will help you wind the path file.
}