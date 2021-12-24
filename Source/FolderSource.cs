using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SaaedBackup.Logic;
using SaaedBackup.Store;
using SaaedBackup.Store.Base;

namespace SaaedBackup.Source
{
    public class FolderSource : Base.SourceBase
    {
        public override event EventHandler<IEnumerable<BackupException>> SourceExThrown;
        public override event EventHandler<StatusResult> ValidationResultEvent;
        public override event EventHandler<StatusResult> SourceStatus;

        /// <summary>
        /// Path of specified folder to backup
        /// </summary>
        private string Path { get; }

        private List<BackupException> _exceptionThrows = new List<BackupException>();

        /// <summary>
        /// get backup of folders recursively
        /// </summary>
        /// <param name="path">path of specified folder</param>
        /// <param name="meta"></param>
        /// <param name="validationMode">Type of file validation</param>
        public FolderSource(string path, ValidationMode validationMode = 0 , string meta = null)
        {
            Path = path.TrimEnd('/', '\\');
            Validation = validationMode;
            Meta = string.IsNullOrWhiteSpace(meta) ? $"FolderSource: {Path}" : meta;
        }



        /// <summary>
        /// put folder files into SourceInfos(stream+relative path)
        /// </summary>
        /// <returns>Returns an IEnumerable of SourceInfos</returns>
        internal override IEnumerable<SourceInfo> GetBackupFiles()
        {
            var result = DirectoryList(Path);
            SourceExThrown?.Invoke(this, _exceptionThrows);
            SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Complete, $"Folder:{Path} correcly loaded"));
            return result;
        }


        /// <summary>
        /// recursive function to put files to SourceInfos
        /// </summary>
        /// <param name="tPath"></param>
        /// <returns>An IEnumerable of SourceInfos</returns>
        private IEnumerable<SourceInfo> DirectoryList(string tPath)
        {

            var result = new List<SourceInfo>();
            DirectoryInfo folderInfo;
            FileInfo[] fileInfos;
            DirectoryInfo[] subFolderInfos;
            try
            {
                folderInfo = new DirectoryInfo(tPath);
                //get files of a directory
                fileInfos = folderInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                subFolderInfos = folderInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
            }
            //if cannot access to the folder (i.e:no enough permission)
            catch (Exception ex)
            {
                var backupException = new BackupException($"Error in getting list of directory:{Path} files and folder", ex);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                    $"Error in getting list of directory:{Path} files and folder", backupException));
                _exceptionThrows.Add(backupException);
                throw backupException;
                return null;

            }
            //get folders of a directory

            //return SourceInfos to parent function
            result.AddRange(fileInfos.Select(f =>
            {
                var sourceInfoMeta = $"File: {f.FullName}";
                try
                {
                    FilesFullPath.Add(f.FullName);
                    FilesRelationalPath.Add(f.FullName.Replace(Directory.GetParent(Path).ToString(), @".\"));
                    //configure and return SourceInfo
                    return new SourceInfo(File.OpenRead(f.FullName), f.FullName, f.FullName.Replace(Directory.GetParent(Path).ToString(), @".\"), sourceInfoMeta);
                }
                //if cannot open file stream
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Error in loading file:{tPath} from FolderSource:{Path}", ex);
                    SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                        $"Error in loading file:{tPath} from FolderSource:{Path}", backupException));

                    _exceptionThrows.Add(backupException);
                    throw backupException;
                    return new SourceInfo(null, f.FullName, f.FullName.Replace(Directory.GetParent(Path).ToString(), @".\"), sourceInfoMeta, ex);
                }
            }));
            try
            {
                //remove non-complete result
                result.RemoveAll(item => item == null);
            }
            catch (Exception ex)
            {
                var backupException = new BackupException($"Error:{Path}", ex);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Error:{Path}", backupException));
                _exceptionThrows.Add(backupException);
                throw backupException;
            }
            //for each subFolder add files recursively to result
            foreach (var directoryInfo in subFolderInfos)
            {
                try
                {
                    var list = DirectoryList(directoryInfo.FullName);
                    if (list != null)
                    {
                        result.AddRange(list);
                    }
                }
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Unknown error in directory:{Path}", ex);
                    SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Unknown error in directory:{Path}", backupException));
                    _exceptionThrows.Add(backupException);
                    throw backupException;
                }
            }

            //return result result (SourceInfos) to parent function
            return result;

        }

        internal override void ValidateSavedFiles(StoreBase store, IEnumerable<SourceInfo> sourceInfos)
        {
			foreach (var source in sourceInfos)
			{
				string savedFileChecksum = "";
				string filePath = store.SavePath + source.NewRelationalPath;

				if (store.GetType() == typeof(FolderStore))
				{
					savedFileChecksum = Funcs.FileMd5(filePath);
				}
				else if (store.GetType() == typeof(FtpStore))
				{
					var ftpStore = store as FtpStore;
					var ftpClient = ftpStore.GetNewFtpClient();
					try
					{
						ftpClient.Connect();
						ftpClient.Encoding = Encoding.UTF8;
					}
					catch (Exception ex)
					{
					}
					var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
					Directory.CreateDirectory(tempDir);
					ftpClient.DownloadFile(tempDir + source.NewRelationalPath, source.NewRelationalPath);
					ftpClient.Disconnect();
					savedFileChecksum = Funcs.FileMd5(tempDir + source.NewRelationalPath);
					if (Directory.Exists(tempDir))
					{
						Directory.Delete(tempDir, true);
					}
				}

				if (source.CheckSum == savedFileChecksum)
				{
					ValidationResultEvent?.Invoke(this, new StatusResult(StatusResult.StatusType.Complete, $"File {source.RelationalPath} is healthy"));
				}
				else
				{
					ValidationResultEvent?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"File {source.RelationalPath} is damaged"));
				}
			}
		}


        /// <summary>
        /// destroy FolderSource object and free up resources
        /// </summary>
        public override void Dispose()
        {

        }

    }
}
