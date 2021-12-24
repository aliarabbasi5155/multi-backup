using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using FluentFTP;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SaaedBackup.Logic;
using SaaedBackup.Store;
using SaaedBackup.Store.Base;
using static System.String;

namespace SaaedBackup.Source
{
    /// <summary>
    ///Specify type of compression
    /// </summary>
    public enum CompressMode
    {
        Zip = 1,
        //Rar = 2,
    }

    /// <summary>
    /// Get compress other Sources into compressed stream
    /// </summary>
    public class CompressSource : Base.SourceBase
    {
        public override event EventHandler<IEnumerable<BackupException>> SourceExThrown;
        public override event EventHandler<StatusResult> ValidationResultEvent;

        public override event EventHandler<StatusResult> SourceStatus;

        private readonly CompressMode _compressMode;
        private readonly string _password, _compressFileName;

        /// <summary>
        /// unique folder name to work in it.
        /// </summary>
        private IEnumerable<SourceInfo> SourceInfos { get; set; }

        /// <summary>
        /// full path of final compressed file
        /// </summary>
        private string CompressFilePath { get; set; }

        private List<BackupException> _exceptionsThrow = new List<BackupException>();
        private DirectoryInfo TempFolder { get; set; }
        private List<string> _sourceMetas = new List<string>();
        private IEnumerable<Base.SourceBase> Sources;
        /// <summary>
        /// to compress different sources to multiple types
        /// </summary>
        /// <param name="sources">Input source</param>
        /// <param name="compressMode"></param>
        /// <param name="password"></param>
        /// <param name="meta"></param>
        /// <param name="compressFileName"></param>
        /// <param name="validationMode">Type of file validation</param>
        public CompressSource(IEnumerable<Base.SourceBase> sources, CompressMode compressMode, string password = null, string compressFileName = null, ValidationMode validationMode = 0)
        {
            _password = password;
            _compressMode = compressMode;
            _compressFileName = compressFileName ??
                                DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss", CultureInfo.GetCultureInfo("en-US"));
            if (!_compressFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) _compressFileName += ".zip";
            Sources = sources;
            //Meta = IsNullOrWhiteSpace(meta) ? $"Compress: {Join(" ,", _sourceMetas.ToArray())}" : meta;
            Validation = validationMode;

            //  {
            //try
            //{
            //s.GetBackupFiles();
            //f++;
            //}
            //catch (BackupException ex)
            //{
            //    var backupException = new BackupException($"Error in loading file:{s.Meta} in compression process", ex);
            //    SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
            //        $"Error in file:{s.Meta} in compression process", backupException));

            //    SourceExThrown?.Invoke(this, new[] { backupException });
            //    throw backupException;
            //}
            // });
        }


        /// <summary>
        /// put files into source datas(stream+relative path)
        /// </summary>
        /// <returns>returns IEnumerable of SourceDatas(stream+relative path)</returns>
        internal override IEnumerable<SourceInfo> GetBackupFiles()
        {

            //foreach (var item in Sources)
            //{
            //    var a = item.GetBackupFiles();
            //    SourceInfos.ToList().AddRange(a);
            //}

            SourceInfos = Sources.SelectMany(s => s.GetBackupFiles());

            foreach (var source in Sources)
            {
                _sourceMetas.Add(source.Meta);
            }

            try
            {
                DirectoryConfiguration();
                switch (_compressMode)
                {
                    case CompressMode.Zip:
                        Zipper();
                        break;
                    //case CompressMode.Rar:
                    //    throw new NotImplementedException();
                    //break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                var backupException = new BackupException("Error in compression process.", ex);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
                    "Error in compression process.", backupException));

                SourceExThrown?.Invoke(this, new[] { backupException });
                throw backupException;
                return null;
            }

            try
            {
                var fileStream = File.OpenRead(CompressFilePath);
                var sourceInfoMeta = $"Compress of:({Join(" ,", _sourceMetas.ToArray())})";
                var source = new SourceInfo(fileStream, CompressFilePath, Path.Combine(".", _compressFileName), sourceInfoMeta);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Complete, $"Compress source:{_compressFileName} correcly loaded"));
                return new[] { source };
            }
            catch (Exception ex)
            {
                var backupException = new BackupException($"Cannot open temporary file {CompressFilePath}.", ex);
                SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Cannot open temporary file {CompressFilePath}.", backupException));
                SourceExThrown?.Invoke(this, new[] { backupException });
                throw backupException;
                return null;
            }
            //try
            //{
            //}
            //catch (Exception ex)
            //{
            //    var backupException = new BackupException($"Error in compress process", ex);
            //    SourceStatus?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed,
            //        $"Error in compress process", backupException));

            //    SourceExThrown?.Invoke(this, new[] { backupException });
            //    throw backupException;
            //    return null;
            //}
        }

        /// <summary>
        /// configure necessary  directories in compression process
        /// </summary>
        private void DirectoryConfiguration()
        {
            var dirName = Path.Combine(TempPath, Guid.NewGuid().ToString());
            TempFolder = new DirectoryInfo(dirName);
            TempFolder.Create();
            TempFolder.Attributes |= FileAttributes.Hidden;
            CompressFilePath = Path.Combine(TempFolder.FullName, _compressFileName);
        }

        /// <summary>
        /// compress streams to a zipoutputstream 
        /// </summary>
        private void Zipper()
        {
            try
            {
                var zip = new ZipOutputStream(File.OpenWrite(CompressFilePath)) { Password = _password };
                foreach (var sourceInfo in SourceInfos)
                {
                    try
                    {
                        var newEntry = new ZipEntry(sourceInfo.RelationalPath.Substring(2)) { IsUnicodeText = true };
                        zip.PutNextEntry(newEntry);
						sourceInfo.Stream.Seek(0, SeekOrigin.Begin);
                        sourceInfo.Stream.CopyTo(zip);
                        zip.CloseEntry();
                        sourceInfo.Stream.Close();
                    }
                    catch (Exception ex)
                    {
                        var backupException = new BackupException($"Error in compression process file: {CompressFilePath}", ex);
                        _exceptionsThrow.Add(backupException);
                        throw backupException;
                    }
                }
                zip.Close();
            }
            catch (Exception ex)
            {
                var backupException = new BackupException($"Error in compression process file: {CompressFilePath}", ex);
                SourceExThrown?.Invoke(this, new[] { backupException });
                _exceptionsThrow.Add(backupException);
                throw backupException;
            }
        }

        //Validate Archive is healthy or damaged
        internal override void ValidateSavedFiles(StoreBase store, IEnumerable<SourceInfo> sources)
        {
            foreach (var source in sources)
            {
				string filePath = store.SavePath + source.NewRelationalPath;
				bool archiveTestResult = false;

				if (store.GetType() == typeof(FolderStore))
				{
					archiveTestResult = TestArchive(filePath, _password);
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
					archiveTestResult = TestArchive(tempDir + source.NewRelationalPath, _password);
					if (Directory.Exists(tempDir))
					{
						Directory.Delete(tempDir, true);
					}
				}

				if (archiveTestResult)
				{
					ValidationResultEvent?.Invoke(this, new StatusResult(StatusResult.StatusType.Complete, $"Archive {filePath} is healthy"));
				}
				else
				{
					ValidationResultEvent?.Invoke(this, new StatusResult(StatusResult.StatusType.Failed, $"Archive {filePath} is damaged"));
				}
			}
        }

		private bool TestArchive(string path, string password)
		{
			bool result = true;
			var zipFile = new ZipFile(path);
			if (!IsPasswordProtectedZipFile(path))
			{
				result = zipFile.TestArchive(true, TestStrategy.FindFirstError, null);
			}
			else
			{
				var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
				Directory.CreateDirectory(tempDir);
				ExtractZipFile(path, password, tempDir);
				foreach (var sourceInfo in SourceInfos)
				{
					string extractedFilePath = Path.Combine(tempDir, sourceInfo.RelationalPath);
					string extractedFileChecksum = Funcs.FileMd5(extractedFilePath);
					if (sourceInfo.CheckSum != extractedFileChecksum)
					{
						result = false;
						break;
					}
				}
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, true);
				}
			}
			zipFile.Close();
			return result;
		}

        //file is password protected
        private static bool IsPasswordProtectedZipFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    using (var fileStreamIn = new FileStream(path, FileMode.Open, FileAccess.Read))
                    using (var zipInStream = new ZipInputStream(fileStreamIn))
                    {
                        var entry = zipInStream.GetNextEntry();
                        return entry.IsCrypted;
                    }
                }
                //File is not password protected
                catch
                {
                    return false;
                }
            }
            //File does not exist
            else
            {
                return false;
            }
        }

		private void ExtractZipFile(string archiveFilenameIn, string password, string outFolder)
		{
			ZipFile zf = null;
			try
			{
				FileStream fs = File.OpenRead(archiveFilenameIn);
				zf = new ZipFile(fs);
				if (!String.IsNullOrEmpty(password))
				{
					zf.Password = password;     // AES encrypted entries are handled automatically
				}
				foreach (ZipEntry zipEntry in zf)
				{
					if (!zipEntry.IsFile)
					{
						continue;           // Ignore directories
					}
					String entryFileName = zipEntry.Name;
					// to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
					// Optionally match entrynames against a selection list here to skip as desired.
					// The unpacked length is available in the zipEntry.Size property.

					byte[] buffer = new byte[4096];     // 4K is optimum
					Stream zipStream = zf.GetInputStream(zipEntry);

					// Manipulate the output filename here as desired.
					String fullZipToPath = Path.Combine(outFolder, entryFileName);
					string directoryName = Path.GetDirectoryName(fullZipToPath);
					if (directoryName.Length > 0)
						Directory.CreateDirectory(directoryName);

					// Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
					// of the file, but does not waste memory.
					// The "using" will close the stream even if an exception occurs.
					using (FileStream streamWriter = File.Create(fullZipToPath))
					{
						StreamUtils.Copy(zipStream, streamWriter, buffer);
					}
				}
			}
			finally
			{
				if (zf != null)
				{
					zf.IsStreamOwner = true; // Makes close also shut the underlying stream
					zf.Close(); // Ensure we release resources
				}
			}
		}

		public override void Dispose()
        {

        }
    }
}
