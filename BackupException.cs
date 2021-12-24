using System;

namespace SaaedBackup
{
    public class BackupException : Exception
    {
        public string ErrorMessage { get; }
        public BackupException(string message, Exception exception) : base(exception.Message, exception)
        {
            ErrorMessage = message;
        }

    }
}
