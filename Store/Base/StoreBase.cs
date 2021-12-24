using System;
using System.Collections.Generic;
using SaaedBackup.Source.Base;

namespace SaaedBackup.Store.Base
{
    public abstract class StoreBase
    {
        public abstract event EventHandler<IEnumerable<BackupException>> StoreExThrown;
        public abstract event EventHandler<StatusResult> StoreStatus;//Errors in stores



        internal string SavePath;
        //TODO:Test
        public abstract void Save(Source.Base.SourceBase sourceBase,ref IEnumerable<SourceInfo> sources);
    }
}
