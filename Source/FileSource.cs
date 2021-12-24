using SaaedBackup.Logic;
using SaaedBackup.Store;
using SaaedBackup.Store.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SaaedBackup.Source
{
	/// <summary>
	/// FileSource Object to backup a specific file.
	/// </summary>
	public class FileSource : Base.SourceBase
	{

		public override event EventHandler<IEnumerable<BackupException>> SourceExThrown;
		public override event EventHandler<StatusResult> ValidationResultEvent;

		public override event EventHandler<StatusResult> SourceStatus;

		/// <summary>
		/// Gets the destination file path.
		/// </summary>
		private string FullPath { get; }

		/// <summary>
		/// FileSource is used to backup a specific file.
		/// </summary>
		/// <param name="fullPath">Full Path of the file</param>
		/// <param name="validationMode">Type of file validation</param>
		/// <param name="meta"></param>
		/// <example></example>
		public FileSource(string fullPath, ValidationMode validationMode = 0, string meta = null)
		{
			FullPath = fullPath.TrimEnd('/', '\\');
			Meta = string.IsNullOrWhiteSpace(meta) ? $"File: {FullPath}" : meta;
			Validation = validationMode;
		}


		/// <summary>
		/// Gets backup of destination file source to a SourceData
		/// </summary>
		/// <returns>A Single-Member IEnumerable of SourceInfos of a file</returns>
		internal override IEnumerable<SourceInfo> GetBackupFiles()
		{
			var sourceInfoMeta = $"File: {FullPath}";
			try
			{
				var fileStream = File.OpenRead(FullPath);
				FilesFullPath.Add(FullPath);
				FilesRelationalPath.Add(FullPath.Replace(Path.GetDirectoryName(FullPath), ".\\"));
				var result = new[]
				{
					new SourceInfo(fileStream, FullPath, FullPath.Replace(Path.GetDirectoryName(FullPath), ".\\"),sourceInfoMeta)
				};

				SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Complete, $"File:{FullPath} correcly loaded"));
				//put file stream and meta path to SourceInfo 
				return result;
			}

			//if cannot put file to stream
			catch (Exception ex)
			{
				var backupException = new BackupException($"Error in loading file:{FullPath}", ex);
				SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Error in file:{FullPath}", backupException));
				throw backupException;
				return new[] { new SourceInfo(null, FullPath, FullPath.Replace(Path.GetDirectoryName(FullPath), "."), sourceInfoMeta, ex) };
			}
		}

		public override void Dispose()
		{

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
					var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
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
	}
}
