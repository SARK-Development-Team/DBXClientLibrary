using Connect.ClientBase;
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
    public static class Add
    {
        private static DropboxClient user = DropboxStartup.Admin;

        /// <summary>
        /// Creates a new client folder for the provided client.
        /// </summary>
        /// <param name="client">Name and Email required.</param>
        /// <returns></returns>
        public static async Task AddNewFolderV2(Client client)
        {
            //Checks if name and email exist.
            if (client?.Name == null || client?.Email == null) { throw new NullReferenceException(); }
            string ClientName = client.Name;
            string Email = client.Email;

            //Logs creation of new client.
            Log.info($"Adding {ClientName} with {Email} as a {client.ClientType} account. Please standby.");
            FolderMetadata newFolder;

            //Initialize user.log.
            List<string> info = new List<string>();
            info.Add($"Name: {client.Name}");
            info.Add($"ID: {client.ID}");
            info.Add($"Email: {client.Email}");
            info.Add($"Phone: {client.Phone}\r\n");

            info.Add($"ClientType: {client.ClientType}");

            //Checks if ClientType is Personal or Business. TODO: Check if default everywhere is "Personal" or "Business". Should be personal.
            //newfolder become the base folder for the client, stored in client.internaldata.DropboxFolder.
            string newfolderPath = string.Empty;
            if (client.ClientType == ClientType.Personal)
            {
                newfolderPath = @"/Tax Clients/Personal Clients/" + ClientName;
            }
            else { newfolderPath = @"/Tax Clients/Business Clients/" + ClientName; }

            newFolder = await CreateorGetFolder(newfolderPath);


            //Creates internal folders. Start by creating "Internal" folder and "Client" folder.
            var ClientFolder = await CreateorGetFolder(newFolder.PathLower + "/" + client.Name + " - Your Tax Folder");
            var InternalFolder = await CreateorGetFolder(newFolder.PathLower + "/Internal");

            //Creates the "Upload", "Client Copies" and "Tax Filing - [Year]" folders in ClientFolder.
            var UploadFolder = await CreateorGetFolder(ClientFolder.PathLower + "/Uploads");
            await CreateorGetFolder(ClientFolder.PathLower + "/Uploads/" + Attributes.Year);
            var ClientCopyFolder = await CreateorGetFolder(ClientFolder.PathLower + "/Tax Copies");
            var ChecklistsFolder = await CreateorGetFolder(ClientFolder.PathLower + "/Checklists");

            //Creates a DropboxSharedURL for client folder access.
            var ClientUrl = (await user.Sharing.CreateSharedLinkAsync(ClientFolder.PathLower, false, null)).Url;

            //Define variables so that we can begin uploading and creating documents for this project.
            client.DropboxSharedURL = ClientUrl;
            client.InternalData.DropboxFolderPath = ClientFolder.PathLower;
            client.InternalData.DropboxFolder = newFolder;
            client.InternalData.FileCreated = DateTime.Now;

            //Update info.log with our new successes.
            info.Add($"DBSharedURL: {client.DropboxSharedURL}");
            info.Add($"DropboxFolderPath: { client.InternalData.DropboxFolderPath}");
            info.Add($"This folder was created on {client.InternalData.FileCreated}.\r\n");

            //Attempts to fill in folders with a)upload shortcut, b)introductory pdfs c)user.log d)estimator

            var filereqURL = "";
            //Upload shortcut and estimator both rely on Dropbox API functions.
            try { filereqURL = await Helpers.CreateFileReqShortcut(UploadFolder.PathLower); }
            catch (DropboxException)
            { Log.error("Upload shortcut could not be made for " + $"{client.Name} {client.InternalData.LocalFolderName} " + "Please try to adjust manually."); }

            //Welcome.pdf, What you need on Tax Day, and Contact us! are all uploadable, but will be modified by pdf before they are uploaded.
            var uploadlist = new List<Task>();
            uploadlist.Add(Upload.File(ChecklistsFolder, PDF.FillPDF(PDF.PDFType.Checklist, PDF.CreateChecklist(client)), client.Name + " - Tax Checklist - " + Attributes.Year + ".pdf", WriteMode.Overwrite.Instance));
            uploadlist.Add(Upload.File(ClientFolder, FilePaths.Templates + "User Guide.pdf", WriteMode.Overwrite.Instance));

            //Add upload pdf tasks here.

            try
            {
                await Task.WhenAll(uploadlist);
            }
            catch (FileNotFoundException exc)
            {
                Log.error(exc.Message);
            }

            //user.log is created, and then uploaded.
            await Upload.File(client.InternalData.DropboxFolder, Helpers.InfoLog(info), WriteMode.Add.Instance);

            string errormessage = $"The uploads have been completed. There were {Log.errorcounter} errors associated with {client.Name}. Please examine error log for issues.";
            Log.error(errormessage);
            Log.reset();

            //if (Local.MYCLOUDEX2ULTRA.CanAccess())
            //{
            //    Log.info("Adding folder contents to local server...");

            //    try
            //    {
            //        Local.MYCLOUDEX2ULTRA.AddFolder(client);
            //    }
            //    catch
            //    {
            //        Log.info("Could not add folder.");
            //        return;
            //    }

            //    Log.info("Local contents added.");
            //}
        }

        public static async Task<FolderMetadata> CreateorGetFolder(string path)
        {
            FolderMetadata newFolder;

            try
            {
                newFolder = (await user.Files.GetMetadataAsync(path)).AsFolder;
            }
            catch (Exception)
            {
                newFolder = (await user.Files.CreateFolderV2Async(path, true)).Metadata;
            }

            return newFolder;
        }
    }
}