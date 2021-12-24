using System;
using System.IO;
using SaaedBackup.Logic;

namespace SaaedBackup
{
    public class SourceInfo : IDisposable
    {
        public readonly Stream Stream;
        public readonly string RelationalPath;
        public readonly string FullPath;
        internal string CheckSum;
        internal string NewRelationalPath { get; set; }
        public readonly string Meta;
        public readonly Exception Exception;
        public SourceInfo(Stream stream, string fullPath, string relationalPath, string meta, Exception exception = null)
        {
            Stream = stream;
            RelationalPath = relationalPath;
            FullPath = fullPath;
            Meta = meta;
            Exception = exception;
            CheckSum = Funcs.StreamMd5(stream);
        }

        public void Dispose()
        {

        }


    }
}
