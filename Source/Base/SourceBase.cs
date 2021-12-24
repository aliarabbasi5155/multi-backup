using System;
using System.Collections.Generic;
using SaaedBackup.Store.Base;

namespace SaaedBackup.Source.Base
{
    public abstract class SourceBase : IDisposable
    {
        public enum ValidationMode
        {
            WithoutValidation = 0,
            Validate = 1,
        }

        internal ValidationMode Validation;

        public abstract event EventHandler<IEnumerable<BackupException>> SourceExThrown;
        public abstract event EventHandler<StatusResult> SourceStatus;//Errors in sources

        public abstract event EventHandler<StatusResult> ValidationResultEvent;//Validate files store in a correct way

        public string Meta { get; set; }
        internal static string TempPath { get; set; }
        internal abstract IEnumerable<SourceInfo> GetBackupFiles();
        internal abstract void ValidateSavedFiles(StoreBase store, IEnumerable<SourceInfo> sourceInfo);
        internal List<string> FilesFullPath { set; get; } = new List<string>();
        internal List<string> FilesRelationalPath { set; get; } = new List<string>();

        public abstract void Dispose();
    }
}
