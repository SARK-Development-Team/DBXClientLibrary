using Dropbox.Api;
using Dropbox.Api.Files;
using Logger;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Connect.DBX
{
    public static class Upload
    {
        private static DropboxClient user = DropboxStartup.Admin;

        #region Dropbox directory creating methods.

        /// <summary>
        /// Creates a directory in Dropbox based on the dropbox path, then adds all existing files and directories from the local FolderPath.
        /// </summary>
        /// <param name="DropboxPath">The directory in Dropbox where the folder will be placed.</param>
        /// <param name="FolderPath">The local directory where files and folders are located.</param>
        public static async Task Folder(Metadata DropboxPath, string FolderPath)
        {
            string FolderName = new DirectoryInfo(FolderPath).Name;
            var DBFolderPath = await Add.CreateorGetFolder(DropboxPath.PathLower + "/" + FolderName);

            var Uploads = new List<Task>();

            foreach (var file in Directory.GetFiles(FolderPath, "*", SearchOption.TopDirectoryOnly))
            {
                if (!((file is null) || (Path.GetFileName(file) == "desktop.ini")))
                {
                    Uploads.Add(File(DBFolderPath, file, WriteMode.Overwrite.Instance));
                }
            }
            await Task.WhenAll(Uploads);

            foreach (var subfolder in Directory.GetDirectories(FolderPath))
            {
                if (!(subfolder is null))
                {
                    await Folder(DBFolderPath, subfolder);
                }
            }
        }

        /// <summary>
        /// Creates a directory in Dropbox based on the dropbox path, then adds all existing files and directories from the local FolderPath.
        /// </summary>
        /// <param name="DropboxPath">The metadata folder path of the Dropbox directory.</param>
        /// <param name="FolderPath">The local directory where files and folders are located.</param>
        public static async Task Folder(string DropboxPath, string FolderPath)
        {
            string FolderName = new DirectoryInfo(FolderPath).Name;
            var DBFolderPath = await Add.CreateorGetFolder(DropboxPath + "/" + FolderName);

            var Uploads = new List<Task>();

            foreach (var file in Directory.GetFiles(FolderPath, "*", SearchOption.TopDirectoryOnly))
            {
                if (!((file is null) || (Path.GetFileName(file) == "desktop.ini")))
                {
                    Uploads.Add(File(DBFolderPath, file, WriteMode.Overwrite.Instance));
                }
            }
            await Task.WhenAll(Uploads);

            foreach (var subfolder in Directory.GetDirectories(FolderPath))
            {
                if (!(subfolder is null))
                {
                    await Folder(DBFolderPath, subfolder);
                }
            }
        }

        #endregion Dropbox directory creating methods.

        #region File uploading methods.

        /// <summary>
        /// Uploads the file into the DropboxPath, which will point to a directory.
        /// </summary>
        /// <param name="DropboxPath">The Dropbox directory where the file will be uploaded to.</param>
        /// <param name="FilePath">The local path of the file.</param>
        /// <param name="FileName">The filename, if the user wishes to change it.</param>
        public static async Task File(Metadata DropboxPath, string FilePath, string FileName, WriteMode writeMode)
        {
            Stream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            string ext = Path.GetExtension(FilePath);
            string path = DropboxPath.PathLower + "/" + FileName + ext;
            await user.Files.UploadAsync(path, writeMode,
                true, null, false, null, false, stream);
            stream.Close();
        }

        /// <summary>
        /// Uploads the file into the DropboxPath, which will point to a directory.
        /// </summary>
        /// <param name="DropboxPath">The Dropbox directory where the file will be uploaded to.</param>
        /// <param name="FilePath">The local path of the file. The filename will stay the same on upload.</param>
        public static async Task File(Metadata DropboxPath, string FilePath, WriteMode writeMode)
        {
            Stream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            string FileName = Path.GetFileName(FilePath);
            string path = DropboxPath.PathLower + "/" + FileName;
            try
            {
                await user.Files.UploadAsync(path, writeMode,
                true, null, false, null, false, stream);
            }
            catch (DropboxException)
            {
                Log.error("There was an error with this file: " + FileName + " Please rename to a more usable form.");
                await File(DropboxPath, FilePath, "FileNameError", writeMode);
            }
            stream.Close();
        }

        /// <summary>
        /// Uploads the file into the DropboxPath, which will point to a directory.
        /// </summary>
        /// <param name="DropboxPath">The Dropbox directory metadata path where the file will be uploaded to.</param>
        /// <param name="FilePath">The local path of the file.</param>
        /// <param name="FileName">The filename, if the user wishes to change it.</param>
        /// <returns></returns>
        public static async Task File(string DropboxPath, string FilePath, string FileName, WriteMode writeMode)
        {
            Stream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            string ext = Path.GetExtension(FilePath);
            string path = DropboxPath + "/" + FileName + ext;
            await user.Files.UploadAsync(path, writeMode,
                true, null, false, null, false, stream);
            stream.Close();
        }

        /// <summary>
        /// Uploads the file into the DropboxPath, which will point to a directory.
        /// </summary>
        /// <param name="DropboxPath">The Dropbox directory metadata path where the file will be uploaded to.</param>
        /// <param name="FilePath">The local path of the file.</param>
        public static async Task File(string DropboxPath, string FilePath, WriteMode writeMode)
        {
            Stream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            string FileName = Path.GetFileName(FilePath);
            string path = DropboxPath + "/" + FileName;
            try
            {
                await user.Files.UploadAsync(path, writeMode,
                true, null, false, null, false, stream);
            }
            catch (DropboxException)
            {
                Log.error("There was an error with this file:" + FileName + "/n Please rename to a more usable form.");
                await File(DropboxPath, FilePath, "FileNameError", writeMode);
            }
            stream.Close();
        }

        /// <summary>
        /// Uploads the file into the DropboxPath, which will point to a directory.
        /// </summary>
        /// <param name="DropboxPath">The Dropbox directory where the file will be uploaded to.</param>
        /// <param name="fileStream">The stream that contains the file information.</param>
        /// <param name="filename">The file name for the uploaded file.</param>
        public static async Task File(Metadata DropboxPath, Stream fileStream, string filename, WriteMode writeMode)
        {
            string path = DropboxPath.PathLower + "/" + filename;

            await user.Files.UploadAsync(path, writeMode,
            true, null, false, null, false, fileStream);
            fileStream.Dispose();
        }

        /// <summary>
        /// Uploads the file into the DropboxPath, which will point to a directory.
        /// </summary>
        /// <param name="DropboxPath">The Dropbox directory metadata path where the file will be uploaded to.</param>
        /// <param name="fileStream">The stream that contains the file information.</param>
        /// <param name="filename">The file name for the uploaded file.</param>
        public static async Task File(string DropboxFolderPath, Stream fileStream, string filename, WriteMode writeMode)
        {
            string path = DropboxFolderPath + "/" + filename;

            await user.Files.UploadAsync(path, writeMode,
            true, null, false, null, false, fileStream);
            fileStream.Dispose();
        }

        /// <summary>
        /// By default files are not overwritten, only created. Use this method to override existing files.
        /// </summary>
        public static async Task FileOverwrite(string DropboxPath, string FilePath, string FileName, WriteMode writeMode)
        {
            Stream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            string ext = Path.GetExtension(FilePath);
            string path = DropboxPath + "/" + FileName + ext;
            await user.Files.UploadAsync(path, writeMode,
                true, null, false, null, false, stream);
            stream.Close();
        }

        #endregion File uploading methods.
    }
}