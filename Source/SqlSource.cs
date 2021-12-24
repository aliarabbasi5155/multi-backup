using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using SaaedBackup.Store.Base;
using static System.String;

namespace SaaedBackup.Source
{
    public class SqlSource : Base.SourceBase
    {
        public enum BackupMode
        {
            FullBackup = 1,
            DifferentialBackup = 2,
        }

        public override event EventHandler<IEnumerable<BackupException>> SourceExThrown;

        public override event EventHandler<StatusResult> SourceStatus;
        public override event EventHandler<StatusResult> ValidationResultEvent;
        private readonly string _serverName, _databaseName, _databaseUsername, _databasePassword, _backupName;
        private string _tempPath;
        private readonly BackupMode _backupMode;

        /// <summary>
        /// Full Backup or differential backup of databases
        /// </summary>
        /// <param name="serverName">Name of the server</param>
        /// <param name="databaseName">Name of specific database</param>
        /// <param name="databaseUsername">Database username</param>
        /// <param name="databasePassword">Database Password</param>
        /// <param name="backupName">Name of backup file</param>
        /// <param name="backupMode">Full backup or differential backup</param>
        /// <param name="meta"></param>
        /// <param name="validationMode">Type of file validation</param>
        public SqlSource(string serverName, string databaseName, string databaseUsername, string databasePassword, string backupName, BackupMode backupMode, string meta = null/*, ValidationMode validationMode = 0 */)
        {
            _serverName = serverName;
            _databaseName = databaseName;
            _databaseUsername = databaseUsername;
            _databasePassword = databasePassword;
            _backupName = backupName ??
                                DateTime.UtcNow.ToString("yyyyMMddhhmmss", CultureInfo.GetCultureInfo("en-US"));
            if (!_backupName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                _backupName += ".bak";
            _backupName = backupName;
            _backupMode = backupMode;
            Meta = IsNullOrWhiteSpace(meta) ? $"SqlSource: {_serverName}/{_databaseName}" : meta;
            /*Validation = validationMode; */
        }


        /// <summary>
        /// put sql backup file into stream (SourceInfo)
        /// </summary>
        /// <returns>An IEnumerable<SourceInfo> of backed up files</SourceInfo></returns>
        internal override IEnumerable<SourceInfo> GetBackupFiles()
        {

            var sourceInfoMeta = $"SQL server:{_serverName}/{_databaseName}";
            try
            {
                _tempPath = TempPath;
                _tempPath = Path.Combine(_tempPath, Guid.NewGuid().ToString());
                Directory.CreateDirectory(_tempPath);
            }
            catch (Exception ex)
            {
                var backupException = new BackupException($"Unable to create temporary directory: {_tempPath}", ex);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Unable to create temporary directory: {_tempPath}",
                    backupException));
                SourceExThrown?.Invoke(this, new[] { backupException });
                throw backupException;
                return new[] { new SourceInfo(null, null, @".\" + _backupName, sourceInfoMeta, ex) };
            }

            try
            {
                var connectionString = BuildConnectionString(_serverName, _databaseName, _databaseUsername, _databasePassword);
                FullBackup(connectionString);
            }
            catch (Exception ex)
            {
                var backupException = new BackupException($"Cannot backup database {_serverName}/{_databaseName}", ex);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Cannot backup database {_serverName}/{_databaseName}",
                    backupException));
                SourceExThrown?.Invoke(this, new[] { backupException });
                throw backupException;
                return null;

            }
            try
            {
                var resfilePath = Path.Combine(_tempPath, Path.GetFileName(_backupName) ?? throw new InvalidOperationException());
                FilesFullPath.Add(resfilePath);
                FilesRelationalPath.Add(resfilePath.Replace(Path.GetDirectoryName(resfilePath), "."));
                var fileStream = File.OpenRead(resfilePath);
                var result = new List<SourceInfo>();
                var sourceData = new SourceInfo(fileStream, resfilePath, @".\" + _backupName, sourceInfoMeta);
                result.Add(sourceData);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Complete, $"SQL database:{_serverName}/{_databaseName} correcly loaded"));
                return result;
            }

            catch (Exception ex)
            {
                var backupException = new BackupException($"Error in backing up sql Server {_serverName}/{_databaseName}", ex);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Error in backing up sql Server {_serverName}/{_databaseName}",
                    backupException));
                SourceExThrown?.Invoke(this, new[] { backupException });
                throw backupException;
                return null;
            }

        }

        internal override void ValidateSavedFiles(StoreBase store, IEnumerable<SourceInfo> sourceInfos)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build connection string
        /// </summary>
        /// <param name="serverName">name the server</param>
        /// <param name="dbName">name of the database</param>
        /// <param name="dbUsername">username of the database</param>
        /// <param name="dbPassword">password of database</param>
        /// <returns></returns>
        private static string BuildConnectionString(string serverName, string dbName, string dbUsername, string dbPassword)
        {
            return @"data source=" + serverName + ";" +
                   "initial catalog=" + dbName + ";" +
                   "user id=" + dbUsername + ";" +
                   "password=" + dbPassword + ";";
        }

        /// <summary>
        /// Make query for backing up the database
        /// </summary>
        /// <param name="dbName">Name of database</param>
        /// <param name="tempPath">Database backup location</param>
        /// <param name="backupName">Name of backup file</param>
        /// <returns></returns>
        private static string MakeBackupQuery(string dbName, string tempPath, string backupName)
        {
            return "USE " + dbName + ";"
                   //Determine database
                   + " BACKUP DATABASE " + dbName
                   //Determine backup path
                   + " TO DISK = " + "'" + Path.Combine(tempPath, backupName) + "'"
                   + " WITH FORMAT,"
                   //Backup Name
                   + " NAME = 'Backup of " + dbName + "'";
        }

        private void FullBackup(string connectionString)
        {
            var query = MakeBackupQuery(_databaseName, _tempPath, _backupName);
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query))
            {
                connection.Open();
                command.Connection = connection;
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        /// <summary>
        /// Destroy source object
        /// </summary>
        public override void Dispose()
        {

        }
    }
}
