using System;


namespace SaaedBackup
{
    public class StatusResult
    {
        public enum StatusType
        {
            Failed = 0,
            Complete = 1,
        }

        public StatusType Status { get; }
        public string Result { get; }
        public Exception Exception { get; }

        public StatusResult(StatusType status, string result, Exception exception=null)
        {
            Status = status;
            Result = result;
            Exception = exception;
        }
    }
}
