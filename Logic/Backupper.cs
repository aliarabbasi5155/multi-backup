using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SaaedBackup.Source;
using SaaedBackup.Source.Base;
using SaaedBackup.Store;
using SaaedBackup.Store.Base;
using static System.String;

namespace SaaedBackup.Logic
{
    public class Backupper
    {
        public event EventHandler<IEnumerable<BackupException>> ExceptionThrown;

        public event EventHandler<StatusResult> SourcesStatus;//Errors in sources
        public event EventHandler<StatusResult> StoresStatus;//Errors in stores


        public event EventHandler<StatusResult> SourcesAndStoresStatus;//Errors in sources and stores

        public event EventHandler<StatusResult> BackupperStatus;//Error in Backupper part

        public event EventHandler<StatusResult> AllErrors;//All of above errors


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="tempDir"></param>
        /// <param name="stores"></param>
        /// <returns>returns backup process exception</returns>
        public void Backup(IEnumerable<SourceBase> sources, IEnumerable<StoreBase> stores, string tempDir = null)
        {

            if (!sources.Any() || sources == null)
            {
                throw new ArgumentException($"{nameof(sources)} cannot be null or empty");
            }
            if (!stores.Any() || stores == null)
            {
                throw new ArgumentException($"{nameof(stores)} cannot be null or empty");
            }
            //Check temDir for null or white space value
            if (IsNullOrWhiteSpace(tempDir))
            {
                tempDir = Path.GetTempPath();
            }
            //create a directory inside temp directory with Guid
            tempDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
            //set TemPath location
            SourceBase.TempPath = tempDir;

            //create temporary directory
            try
            {
                Directory.CreateDirectory(SourceBase.TempPath);
            }


            //catch exception and Throw an event if fail to create temporary directory
            catch (Exception ex)
            {
                var backupException = new BackupException($"Cannot create temporary directory {SourceBase.TempPath}.", ex);
                BackupperStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Cannot create temporary directory {SourceBase.TempPath}."));
                ExceptionThrown?.Invoke(this, new[] { backupException });
                throw backupException;
            }

            sources = sources.ToArray();

            //store sources in each store
            foreach (var source in sources)
            {
                source.SourceExThrown += BackupExceptionThrow;
                source.SourceStatus += SourceStatusEventHandler;

                IEnumerable<SourceInfo> result = new List<SourceInfo>();
                //Backing up of sources
                try
                {
                    result = source.GetBackupFiles();
                }
                catch (Exception ex)
                {
                    //For Next Version
                    ExceptionThrown?.Invoke(this, new[] { new BackupException("Error in backup process", ex) });
                }
                if (result == null) continue;

                foreach (var store in stores)
                {
                    store.StoreExThrown += BackupExceptionThrow;
                    store.StoreStatus += StoreStatusEventHandler;
                    try
                    {
                        store.Save(source, ref result);
                    }
                    catch (Exception ex)
                    {
                        ExceptionThrown?.Invoke(this, new[] { new BackupException("Error in saving backup files process", ex) });
                    }
                    if (source.Validation.HasFlag(SourceBase.ValidationMode.Validate))
                    {
                        //foreach (var item in result)
                        //{
                        //    source.ValidateSavedFiles(store, item);
                        //}
                        source.ValidateSavedFiles(store, result);
                    }

                }

            }
            try
            {
                //To close all streams of tempDir
                sources = null;
                if (Directory.Exists(SourceBase.TempPath))
                    Directory.Delete(SourceBase.TempPath, true);
            }
            catch (Exception ex)
            {
                BackupperStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Cannot delete temporary directory.{SourceBase.TempPath}"));
                ExceptionThrown?.Invoke(this, new[] { new BackupException($"Cannot delete temporary directory.{SourceBase.TempPath}", ex) });
            }

            BackupperStatus += BackupperEventHandler;
        }

        private void BackupExceptionThrow(object sender, IEnumerable<BackupException> ex)
        {
            ExceptionThrown?.Invoke(this, ex);
        }

        private void SourceStatusEventHandler(object sender, StatusResult sResult)
        {
            SourcesStatus?.Invoke(this, sResult);
            AllErrors?.Invoke(this, sResult);
            SourcesAndStoresStatus?.Invoke(this, sResult);
        }
        private void StoreStatusEventHandler(object sender, StatusResult sResult)
        {
            StoresStatus?.Invoke(this, sResult);
            AllErrors?.Invoke(this, sResult);
            SourcesAndStoresStatus?.Invoke(this, sResult);
        }

        private void BackupperEventHandler(object sender, StatusResult sResult)
        {
            AllErrors?.Invoke(this, sResult);
            SourcesAndStoresStatus?.Invoke(this, sResult);
        }
    }
}

