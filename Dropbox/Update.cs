using Connect.ClientBase;
using Connect.DB;
using Connect.PDFEdit;
using Dropbox.Api;
using Dropbox.Api.Files;
using Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Connect.DBX
{
    public static class Update
    {
        private static List<FileReqInfo> fileReqInfos = null;
        private static DropboxClient user = DropboxStartup.Admin;

        public class FileReqInfo
        {
            public int ClientID { get; set; }
            public string DBID { get; set; }
            public string URL { get; set; }
            public string folderPath { get; set; }

            public void SetID(int id)
            {
                ClientID = id;
            }
        }

        public static async Task UpdateClientFolder(Client client, string info)
        {
            if (client.InternalData?.DropboxFolderPath == null)
            {
                throw new NullReferenceException();
            }

            string userlog = System.IO.Path.GetTempPath();
            if (!userlog.EndsWith(@"\"))
            {
                userlog = userlog + @"\";
            }
            userlog = userlog + "userupdate " + DateTime.Now.ToString("yyyy-dd-MM") + ".log";
            System.IO.File.WriteAllText(userlog, info);

            string dir = client.InternalData.DropboxFolderPath;
            dir = Directory.GetParent(dir).FullName;
            dir = dir.Substring(2);
            dir = dir.Replace(@"\", "/");
            await Upload.File(dir, userlog, WriteMode.Add.Instance);
        }

        public static string GetSharedURL(Client client)
        {
            var url = user.Sharing.CreateSharedLinkAsync(client.InternalData.DropboxFolderPath, false, null);
            url.Wait();
            return url.Result.Url;
        }

        public static async Task<List<string>> GetClientPaths()
        {
            List<string> clientpaths = new List<string>();
            var filelist = (await user.Files.ListFolderAsync("/tax clients/personal clients/")).Entries;
            var fl2 = (await user.Files.ListFolderAsync("/tax clients/business clients/")).Entries;
            foreach (var item in fl2)
            {
                filelist.Add(item);
            }

            foreach (var file in filelist)
            {
                var folder = file.AsFolder;
                clientpaths.Add(folder.PathLower);
            }
            return clientpaths;
        }

        public static async Task<List<FileReqInfo>> GetFileRequests(List<Client> clientlist)
        {
            var requests = await user.FileRequests.ListV2Async();
            var filereqs = new List<FileReqInfo>();
            GetFileReqsLoop();
            while (requests.HasMore)
            {
                requests = await user.FileRequests.ListContinueAsync(requests.Cursor);
                GetFileReqsLoop();
            }

            foreach (var client in clientlist)
            {
                foreach (var fr in filereqs)
                {
                    if (fr.folderPath.ToLowerInvariant().Contains(client.InternalData.DropboxFolderPath))
                    {
                        fr.SetID(client.ID);
                    }
                }
            }

            return filereqs;

            void GetFileReqsLoop()
            {
                foreach (var request in requests.FileRequests)
                {
                    if (request.IsOpen)
                    {
                        var requestinfo = new FileReqInfo();
                        requestinfo.DBID = request.Id;
                        requestinfo.URL = request.Url;
                        requestinfo.folderPath = request.Destination;
                        filereqs.Add(requestinfo);
                    }
                }
            }
        }

        public static bool VerifyFileRequest(Client client, string uploadpath)
        {
            bool verified = false;
            foreach (var filereq in fileReqInfos)
            {
                if (filereq.ClientID == client.ID)
                {
                    if (filereq.folderPath.ToLowerInvariant() == (uploadpath + "/" + Attributes.Year).ToLowerInvariant())
                    {
                        verified = true;
                        break;
                    }
                    else if (filereq.folderPath.ToLowerInvariant() == uploadpath.ToLowerInvariant())
                    {
                        Log.info("Exist file request insufficient. Closing existing file request...");
                        user.FileRequests.UpdateAsync(filereq.DBID, open: false).Wait();
                        user.FileRequests.DeleteAsync(new List<string>() { filereq.DBID }).Wait();
                    }
                }
            }
            return verified;
        }

        public static async Task<List<string>> GetAllFiles(string folderpath, bool recursive = true)
        {
            //Gets all filepaths for files in client folder.
            var entries = new ListFolderResult().Entries;
            var entry = await user.Files.ListFolderAsync(folderpath, recursive);
            entries = entry.Entries;
            while (entry.HasMore)
            {
                entry = await user.Files.ListFolderContinueAsync(entry.Cursor);
                for (int i = 0; i < entry.Entries.Count; i++)
                {
                    entries.Add(entry.Entries[i]);
                }
            }

            List<string> filepaths = new List<string>();
            foreach (var file in entries)
            {
                filepaths.Add(file.PathLower);
                //Log.info(file.PathLower);
            }
            return filepaths;
        }

        public static async Task<Client> VerifyClient(Client client, List<string> clientpaths, List<Client> clientlist)
        {
            if (fileReqInfos == null)
            {
                fileReqInfos = await GetFileRequests(clientlist);
            }

            Log.info("Verifying Dropbox folder for ID:" + client.ID + " Name:" + client.Name + " Path:" + client.InternalData.DropboxFolderPath);
            //Creates a folder list that can be used to compare with DropboxFolderPath
            bool folderverified = false;
            string path = client.InternalData.DropboxFolderPath;
            string[] paths = path.Split(@"/");
            path = string.Empty;
            for (int i = 0; i < paths.Length - 1; i++)
            {
                path += paths[i] + "/";
            }
            Log.info(path);
            foreach (var clipath in clientpaths)
            {
                if (clipath + "/" == path)
                {
                    folderverified = true;
                    Log.info("Client folderpath found. Analyzing folder contents...");
                }
            }

            //If folderpath not found, creates a new Dropbox folder for client.
            if (!folderverified)
            {
                Log.error("The DropboxFolderPath for this client does not match with any Dropbox folder found. Please remake this client's folder.");
                await Add.AddNewFolderV2(client);
                return client;
            }

            //Gets all filepath for files in client folder.
            List<string> filepaths = await GetAllFiles(client.InternalData.DropboxFolderPath);

            //Searches to see if "Welcome!.pdf" is still a file. If so, renames to User Guide.pdf.
            string oldwelcomepath = client.InternalData.DropboxFolderPath + "/Welcome!.pdf";
            string newwelcomepath = client.InternalData.DropboxFolderPath + "/User Guide.pdf";
            if (filepaths.Contains(oldwelcomepath.ToLowerInvariant()))
            {
                Log.info("Client user guide is not up to date, updating User Guide...");
                await user.Files.DeleteAsync(oldwelcomepath);
                await Upload.File(client.InternalData.DropboxFolderPath, FilePaths.Templates + "/User Guide.pdf", WriteMode.Overwrite.Instance);
            }
            else if (filepaths.Contains(newwelcomepath.ToLowerInvariant()))
            {
                Log.info("Client user guide is updated.");
            }
            else
            {
                Log.error("User guide not found. Uploading new user guide.");
                await Upload.File(client.InternalData.DropboxFolderPath, FilePaths.Templates + "/User Guide.pdf", WriteMode.Overwrite.Instance);
            }

            //Renames Tax Filing - [Year] folder to Checklists.
            string oldchecklistspath = client.InternalData.DropboxFolderPath + "/Tax Filing - " + Attributes.Year;
            string newchecklistspath = client.InternalData.DropboxFolderPath + "/Checklists";

            if (filepaths.Contains(oldchecklistspath.ToLowerInvariant()))
            {
                if (filepaths.Contains(newchecklistspath.ToLowerInvariant()))
                {
                    Log.info("Old and new Checklist folders found. Deleting old checklists folder...");
                    await user.Files.DeleteAsync(oldchecklistspath);
                }
                else
                {
                    Log.info("Updating checklists folder to current version.");
                    await user.Files.MoveV2Async(oldchecklistspath, newchecklistspath, true, false, true);
                }
            }
            else if (filepaths.Contains(newchecklistspath.ToLowerInvariant()))
            {
                Log.info("Checklist folder is up to date.");
            }
            else
            {
                Log.info("Missing Checklist folder. Adding new folder...");
                await Add.CreateorGetFolder(newchecklistspath);
                await Upload.File(newchecklistspath, PDF.FillPDF(PDF.PDFType.Checklist, PDF.CreateChecklist(client)), 
                    client.Name + " - Tax Checklist - " + Attributes.Year + ".pdf", WriteMode.Overwrite.Instance);
            }

            //Check Uploads folder to see if it is old or new Uploads folder. If it is old Uploads folder, then rename folder and AddNewFolder for [Year].
            string olduploadspath = client.InternalData.DropboxFolderPath + "/Upload your documents!";
            string newuploadspath = client.InternalData.DropboxFolderPath + "/Uploads";

            if (filepaths.Contains(olduploadspath.ToLowerInvariant()))
            {
                Log.info("Updating uploads folder to current version.");
                await user.Files.MoveV2Async(olduploadspath, newuploadspath, true, false, true);
                await Add.CreateorGetFolder(newuploadspath + "/" + Attributes.Year);
            }
            else if (filepaths.Contains(newuploadspath.ToLowerInvariant()))
            {
                if (filepaths.Contains(newuploadspath.ToLowerInvariant() + "/" + Attributes.Year))
                {
                    Log.info("Uploads folder is up to date.");
                }
                else
                {
                    Log.info("Uploads folder missing " + Attributes.Year + " folder. Adding new folder...");
                    await Add.CreateorGetFolder(newuploadspath + "/" + Attributes.Year);
                }
            }
            else
            {
                Log.error("Uploads folders could not be found. Inserting an Uploads folder.");
                await Add.CreateorGetFolder(newuploadspath + "/" + Attributes.Year);
            }

            //Moves all files from a list of files to Uploads folder.
            var uploadfiles = await GetAllFiles(newuploadspath);
            foreach (var file in uploadfiles)
            {
                if (!file.Contains("Please click me to upload!".ToLowerInvariant()) &&
                    !file.Contains("Please click here to upload your documents!".ToLowerInvariant()) &&
                    !file.Contains(newuploadspath.ToLowerInvariant() + "/" + Attributes.Year) &&
                    !file.Contains(newuploadspath.ToLowerInvariant()))
                {
                    Log.info("Unorganized files found in Uploads folder. Moving " + file + " to " + Attributes.Year + " folder...");
                    await user.Files.MoveV2Async(file, newuploadspath + "/" + Attributes.Year + "/" + new FileInfo(file).Name + new FileInfo(file).Extension, true, true, true);
                }
            }

            //CURR: Generates a filerequest that links to [Year] Upload folder. Old file request should be closed with Dropbox move.
            //TODO: A new filereq is automatically created if an existing filereq cannot be verified.
            if (!VerifyFileRequest(client, newuploadspath))
            {
                try
                {
                    Log.info("File request info needs to be updated. Updating folder with new file request.");
                    string filereqURL = await Helpers.CreateFileReqShortcut(newuploadspath);
                }
                catch (DropboxException)
                { Log.error("Upload shortcut could not be made for " + $"{client.Name} {client.InternalData.DropboxFolderPath} " + "Please try to adjust manually."); }
            }
            else
            {
                Log.info("File request has been verified, and is currently working.");
            }

            return client;
        }

        /// <summary>
        /// Updates the files in accordance to changes in the Client name. The client directory is renamed to match the new name, and the pdf/service contract are each remade.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> UpdateFiles(Client client)
        {
            Log.info("Updating files for client " + client.ID + " " + client.Name + " at " + client.InternalData.DropboxFolderPath);
            List<string> clientfiles;
            string dir = client.InternalData.DropboxFolderPath;

            try
            {
                //Retrieves all the paths in the client directory.
                clientfiles = await GetAllFiles(client.InternalData.DropboxFolderPath);
            }
            catch (Exception)
            {
                throw new ArgumentException(Log.error("Could not find files using DropboxFolderPath. Please contact administrator."));
            }

            try
            {
                string[] basePaths = client.InternalData.DropboxFolderPath.Split("/");
                string basePath = basePaths[0] + "/" + basePaths[1] + "/" + basePaths[2] + "/";
                string fromParent = basePath + basePaths[3];
                string toParent = basePath + client.Name;

                string fromPath = toParent.ToLowerInvariant() + "/" + basePaths[4];
                string toPath = toParent.ToLowerInvariant() + "/" + client.Name + " - Your Tax Folder";

                if (toPath.ToLowerInvariant() != fromPath.ToLowerInvariant())
                {
                    Log.info("Changing directory from " + fromPath + " to " + toPath + "due to updated name on client file.");

                    if (fromParent.ToLowerInvariant() != toParent.ToLowerInvariant())
                    {
                        await user.Files.MoveV2Async(fromParent, toParent, true, true, true);
                    }

                    await user.Files.MoveV2Async(fromPath, toPath, true, true, true);
                    dir = toPath.ToLowerInvariant();
                }
            }
            catch (Exception)
            {
                throw new ArgumentException(Log.error("Changing DropboxFolderPath failed. Please contact administrator."));
            }

            try
            {
                foreach (var file in clientfiles)
                {
                    if (file.ToLowerInvariant() == (dir + "/User Guide.pdf").ToLowerInvariant())
                    {
                        //Log.info("Updating User Guide to current version.");
                        //await Upload.FileOverwrite(client.internalData.DropboxFolderPath, FilePaths.Templates + "User Guide.pdf", "User Guide");
                    }
                    if (file.ToLowerInvariant().Contains(dir + "/checklists/") && file.ToLowerInvariant().Contains("- tax checklist - " + Attributes.Year))
                    {
                        Log.info("Updating Checklist to current version.");
                        string deletepath = dir + "/checklists" + "/" + client.Name + " - Tax Checklist - " + Attributes.Year +".pdf";
                        deletepath = deletepath.ToLowerInvariant();
                        if (file.ToLowerInvariant() != deletepath)
                        {
                            await user.Files.DeleteV2Async(file);
                        }
                        await Upload.File(client.InternalData.DropboxFolderPath + "/checklists", PDF.FillPDF(PDF.PDFType.Checklist, PDF.CreateChecklist(client)), client.Name + " - Tax Checklist - " + Attributes.Year + ".pdf", writeMode: WriteMode.Overwrite.Instance);
                    }
                    if (file.ToLowerInvariant().Contains($"Services Agreement - {Attributes.Year}".ToLowerInvariant()))
                    {
                        string deletepath = dir + "/" + client.Name + $" - Services Agreement - {Attributes.Year}";
                        deletepath = deletepath.ToLowerInvariant();
                        if (file.ToLowerInvariant() != deletepath)
                        {
                            await user.Files.DeleteV2Async(file);
                        }
                        Log.info("Updating Services Agreement to current version.");
                        await Upload.File(client.InternalData.DropboxFolderPath, PDF.FillPDF(PDF.PDFType.Contract, PDF.CreateContract(client)), client.Name + $" - Services Agreement - {Attributes.Year}", WriteMode.Overwrite.Instance);
                    }
                }
            }
            catch (Exception)
            {
                throw new ArgumentException(Log.error("Client folder corrupted. Could not update files."));
            }

            return dir;
        }
    }
}