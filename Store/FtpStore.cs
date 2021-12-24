using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using SaaedBackup.Logic;
using FluentFTP;
using SaaedBackup.Source.Base;

namespace SaaedBackup.Store
{
    public class FtpStore : Base.StoreBase
    {
        public override event EventHandler<IEnumerable<BackupException>> StoreExThrown;

        public override event EventHandler<StatusResult> StoreStatus;

        private readonly string _destPath;
        private readonly string _userName;
        private readonly string _password;

		private List<BackupException> _exceptionThrows = new List<BackupException>();
        public FtpStore(string destPath, string userName, string password)
        {
            _destPath = destPath.TrimEnd('\\', '/');
            //_destPath = destPath.TrimEnd('\\', '/') + '/' + DateTime.UtcNow.ToString("yyyyMMddhhmmss", CultureInfo.GetCultureInfo("en-US"));
            _userName = userName;
            _password = password;
            SavePath = _destPath;
        }

		internal FtpClient GetNewFtpClient()
		{
            var ftpAddress = new Uri(_destPath);
            var ftpServerAddress = Uri.UnescapeDataString(ftpAddress.Scheme + Uri.SchemeDelimiter + ftpAddress.Authority);
			return new FtpClient()
			{
				Host = ftpServerAddress,
				Credentials = new NetworkCredential(_userName, _password)
			};
		}

        public override void Save(SourceBase sourceBase, ref IEnumerable<SourceInfo> sources)
        {
            //ftp path validation
            if (!_destPath.StartsWith("ftp://"))
                throw new ArgumentException("Ftp address must start with 'ftp://'");
			var client = GetNewFtpClient();
            //Check server connection
            try
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                var backupException = new BackupException($"Cannot connect to ftp server {client.Host}", ex);
                StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                    $"Cannot connect to ftp server {client.Host}", backupException));
                StoreExThrown?.Invoke(this, new[] { backupException });
                throw backupException;
                return;
            }
            //store each source in destination path
            foreach (var source in sources)
            {
                string fullPath = null;

                try
                {
                    //fullPath uri configuration
                    fullPath = _destPath + source.RelationalPath.Replace("\\", "/").Remove(0, 1);

                }
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Error in saving file '{fullPath}' to the Ftp", ex);
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                        $"Error in saving file '{fullPath}' to the Ftp", backupException));
                    StoreExThrown?.Invoke(this, new[] { backupException });
                    _exceptionThrows.Add(backupException);
                    throw backupException;
                    return;
                }
                var fileNameLength = Path.GetFileName(fullPath).Length;
                var destinationDirectory = fullPath.Remove(fullPath.Length - fileNameLength - 1, fileNameLength + 1);
                try
                {
                    DirectoryChecker(client, destinationDirectory);
                }
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Cannot prepare directory: {destinationDirectory} to save file: {fullPath}", ex);
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                       $"Cannot prepare directory: {destinationDirectory} to save file: {fullPath}", backupException));
                    StoreExThrown?.Invoke(this, new[] { backupException });
                    _exceptionThrows.Add(backupException);
                    throw backupException;
                    continue;
                }
                //if there is no problem in destination path and file name configuration
                try
                {
                    fullPath = FtpDuplicateFileNameChecker(client, fullPath);
                    source.NewRelationalPath = "./" + Path.GetFileName(fullPath);
                }
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Cannot check duplicate file name to save file: {fullPath}", ex);
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                        $"Cannot check duplicate file name to save file: {fullPath}", backupException));
                    StoreExThrown?.Invoke(this, new[] { backupException });
                    _exceptionThrows.Add(backupException);
                    throw backupException;
                    continue;
                }
                //check saving files in destination path
                try
                {
                    var absolutePath = Uri.UnescapeDataString(new Uri(fullPath).AbsolutePath);
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Upload(source.Stream, absolutePath, FtpExists.NoCheck, true);
					client.Disconnect();
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Complete, $"FTP:{fullPath} completely stored"));
                    source.Stream.Close();
                }
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Error in uploading file:{fullPath}.", ex);
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                        $"Error in uploading file:{fullPath}.", backupException));
                    StoreExThrown?.Invoke(this, new[] { backupException });
                    _exceptionThrows.Add(backupException);
                    throw backupException;
                    continue;
                }
                StoreExThrown?.Invoke(this, _exceptionThrows);
            }

        }

        /// <summary>
        /// Check and prepare destination directory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ftpUri">destination directory path</param>
        /// <returns></returns>
        private static void DirectoryChecker(FtpClient client, string ftpUri)
        {
            var uri = new Uri(ftpUri);
            //check the directory is exists or if doesn't exist create it
            //check if directory is exists
            //var directoryExists = ;
            if (client.DirectoryExists(Uri.UnescapeDataString(uri.AbsolutePath)))
            {
                return;
            }
            try
            {
                //if directory doesn't exists create it
                client.CreateDirectory(Uri.UnescapeDataString(uri.LocalPath));
            }
            catch (Exception ex)
            {
                throw new BackupException($"Error in creating directory {Uri.UnescapeDataString(ftpUri)}", ex);
            }
        }

        /// <summary>
        /// to check file name exists
        /// </summary>
        /// <param name="client"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string FtpDuplicateFileNameChecker(FtpClient client, string path)
        {
            var uri = new Uri(path);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var parentDirectory = Uri.UnescapeDataString(uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length - 1].Length));
            var a = uri.AbsolutePath.Length - uri.Segments[uri.Segments.Length - 1].Length;
            var parentDirectoryAbsolutePath = Uri.UnescapeDataString(uri.AbsolutePath.Substring(0, a));
            while (client.FileExists(Uri.UnescapeDataString(parentDirectoryAbsolutePath + fileNameWithoutExtension + ext)))
            {
                fileNameWithoutExtension = Funcs.ProductNextName(fileNameWithoutExtension);
            }
            return Uri.UnescapeDataString(parentDirectory + fileNameWithoutExtension + ext);

        }


    }
}
