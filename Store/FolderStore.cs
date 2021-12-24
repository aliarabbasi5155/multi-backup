using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using SaaedBackup.Logic;
using SaaedBackup.Source.Base;

namespace SaaedBackup.Store
{
    /// <summary>
    ///Store sources into folder destination
    /// </summary>
    public class FolderStore : Base.StoreBase
    {

        public override event EventHandler<IEnumerable<BackupException>> StoreExThrown;
        public override event EventHandler<StatusResult> StoreStatus;


        private List<BackupException> _exceptionThrow = new List<BackupException>();

        /// <summary>
        /// The destination directory
        /// </summary>
        private DirectoryInfo SaveFolder { get; }

        /// <summary>
        /// Store sources into folders
        /// </summary>
        /// <param name="fullPath">Destination path</param>
        public FolderStore(string fullPath)
        {
            SaveFolder = new DirectoryInfo(fullPath);
            SavePath = fullPath;
            //TODO:THIS PART CAN CHANGE
            //SaveFolder=new DirectoryInfo(Path.Combine(fullPath, DateTime.UtcNow.ToString("yyyyMMddhhmmss", CultureInfo.GetCultureInfo("en-US"))));
        }

        /// <summary>
        /// store an IEnumerable of SourceInfos into the destination path
        /// </summary>
        /// <param name="sourceBase"></param>
        /// <param name="sources">Input sources</param>
        public override void Save(SourceBase sourceBase, ref IEnumerable<SourceInfo> sources)
        {


            //check destination directory exist
            //
            try
            {
                if (!SaveFolder.Exists)
                {
                    SaveFolder.Create();
                }
            }
            //error in destination directory
            catch (Exception ex)
            {
                var backupException = new BackupException($"Error in destination directory:{SaveFolder.FullName}", ex);
                StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                    $"Error in destination directory:{SaveFolder.FullName}", backupException));

                StoreExThrown?.Invoke(this, new[] { backupException });
                throw backupException;
                return;
            }
            foreach (var source in sources)
            {
                string fullPath;
                try
                {
                    //Destination source file path configuration
                    var str = source.RelationalPath;
                    str = str.Substring(1);
                    var s = SaveFolder.FullName.TrimEnd('/', '\\') + str;
                    fullPath = Funcs.RenameDuplicatedFile(s);
                    source.NewRelationalPath = ".\\" + Path.GetFileName(fullPath);
                }
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Error in store file:{SaveFolder}", ex);
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Error in store file:{SaveFolder}", backupException));
                    StoreExThrown?.Invoke(this, new[] { backupException });
                    StoreExThrown?.Invoke(this, new[] { backupException });
                    _exceptionThrow.Add(backupException);
                    throw backupException;
                }
                //check and create directory to save files
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new Exception());
                }
                catch (Exception ex)
                {
                    var backupException =
                        new BackupException(
                            $"The destination directory is not available and unable to make directory to store file:{Path.GetDirectoryName(fullPath)}",
                            ex);
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                        $"The destination directory is not available and unable to make directory to store file:{Path.GetDirectoryName(fullPath)}"
                        , backupException));

                    StoreExThrown?.Invoke(this, new[] { backupException });
                    _exceptionThrow.Add(backupException);
                    throw backupException;
                }
                //write file in destination directory
                try
                {
                    var storeStream = File.OpenWrite(fullPath);
                    source.Stream.Seek(0, SeekOrigin.Begin);
                    storeStream.Seek(0, SeekOrigin.Begin);
                    source.Stream.CopyTo(storeStream);
                    source.Stream.Close();
                    storeStream.Close();
                    StoreStatus?.Invoke(this,new StatusResult(StatusResult.StatusType.Complete,$"Folder:{fullPath} completely stored"));
                }
                //Exception in saving file in determined directory
                catch (Exception ex)
                {
                    var backupException = new BackupException($"Error in store file:{fullPath}", ex);
                    StoreStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Error in store file:{fullPath}", backupException));
                    StoreExThrown?.Invoke(this, new[] { backupException });
                    _exceptionThrow.Add(backupException);
                    throw backupException;
                    continue;
                }
            }

        }


    }

}

