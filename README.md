# SaaedBackup
</head><body id="preview">

<p><a href="https://nodesource.com/products/nsolid"><img src="https://www.saaedco.com/Content/images/saaed-name-logo.png?v=1" alt="N|Solid"></a></p>
<p><a href="https://travis-ci.org/joemccann/dillinger"><img src="https://travis-ci.org/joemccann/dillinger.svg?branch=master" alt="Build Status"></a></p>
<p>SaaedBackup is a backup library for .Net &amp; .Net Standard written in C#.</p>
<h1><a id="Features_8"></a>Features</h1>
<ul>
<li>Support for Single-File backup</li>
<li>Support for Directory backup</li>
<li>Support for SQL database backup</li>
<li>Support for FTP directory backup</li>
<li>Support for store sources into local directories</li>
<li>Support for store sources into FTP</li>
<li>Compress all types of sources into Password-Protected archives</li>
<li>Check all types of sources to validate all containing files</li>
<li>Full-cover logging service</li>
</ul>
<p>You can also visit <a href="https://www.saaedco.com">https://www.saaedco.com</a></p>
<h1><a id="Releases_22"></a>Releases</h1>
<p>SaaedBackup supports .Net Standard 2.0 and lower and it can be used with .Net Framework up to 4.6.1.<br>
Also SaaedBackup supports Mono 4.6, Xamarin.iOS 10.14, Xamarin.Android 3.8, Xamarin.Mac 3.8 and UWP 10.0.16299 via .Net Standard 2.0.</p>
<h1><a id="Example_Usage_26"></a>Example Usage</h1>
<pre><code>//create a file source to get backup from pic.jpg file 
var fileSource = new FileSource(@&quot;X:\pic.jpg&quot;);

//create a folder source to get backup from files in test directory
var folderSource = new FolderSource(@&quot;X:\test&quot;);

//create a SQL source to get full backup from databases of a SQL server
var sqlSource = new SqlSource(&quot;serverName&quot;, &quot;databaseName&quot;, &quot;username&quot;, &quot;password&quot;, &quot;backupName&quot;, SqlSource.BackupMode.FullBackup);

//create a compress source from sources above
var compressSource = new CompressSource(new [] { fileSource, folderSource, sqlSource }, CompressMode.Zip, null, null, SourceBase.ValidationMode.Validate);

//create a folder store to store sources in test diectory
var folderStore = new FolderStore(@&quot;X:\test&quot;);

//create a FTP store to store sources in FTP test diectory
var ftpStore = new FtpStore(@&quot;ftp://serverNameOrAddress/test&quot;, &quot;username&quot;, &quot;password&quot;);

//getting backup from sources (tempFolder is customizable)
var backup = new Backupper();
backup.Backup(new SourceBase[] { fileSource, compressSource }, new StoreBase[] { folderStore, ftpStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<blockquote>
<p>This code snippet will backup from a file, a folder and an SQL database, then will combine them and put them into a compress file and finally store compress file and file in a folder and FTP destination.</p>
</blockquote>
<h1><a id="Documentation_52"></a>Documentation</h1>
<h3><a id="Sources_53"></a>Sources</h3>
<h5><a id="FileSource_54"></a>FileSource</h5>
<p>FileSource is used to backup from a single file like jpg, doc, exe, etc.</p>
<pre><code>FileSource(string fullPath, ValidationMode validationMode, string meta)
</code></pre>
<blockquote>
<p><code>fullPath</code>: The complete path of the file like: C:\Directory\File.exe<br>
<code>validationMode</code>: If validation is necessary it should be set as 1 otherwise set as 0<br>
<code>meta</code>: Additional information about source.</p>
</blockquote>
<h5><a id="FolderSource_63"></a>FolderSource</h5>
<p>FolderSource is used to backup from a directory and recursively all of its sub files and sub directories.</p>
<pre><code>FolderSource(string path, ValidationMode validationMode, string meta)
</code></pre>
<blockquote>
<p><code>path</code>: The complete path of specified directory<br>
<code>validationMode</code>: If validation is necessary it should be set as 1 otherwise set as 0<br>
<code>meta</code>: Additional information about source.</p>
</blockquote>
<h5><a id="SqlSource_72"></a>SqlSource</h5>
<p>SqlSource is used to get full or differential backup from a database on a SQL server.</p>
<pre><code>SqlSource(string serverName, string databaseName, string databaseUsername, string databasePassword, string backupName, BackupMode backupMode, string meta)
</code></pre>
<blockquote>
<p><code>serverName</code>: The name or IP address of  the destination server.<br>
<code>databaseName</code>: The name of the destination database on the server.<br>
<code>databaseUsername</code>: The username of the server to use for authentication.<br>
<code>databasePassword</code>: The password of the server to use for authentication.<br>
<code>backupName</code>: The name of final backed up file.<br>
<code>backupMode</code>: Determine type of the backup (Full or Differential).<br>
<code>meta</code>: Additional information about source. directory</p>
</blockquote>
<h5><a id="CompressSource_84"></a>CompressSource</h5>
<p>CompressSource is used when you want to put some other sources in a compress archive.</p>
<pre><code>CompressSource(IEnumerable&lt;Base.SourceBase&gt; sources, CompressMode compressMode, string password, string compressFileName, ValidationMode validationMode)
</code></pre>
<blockquote>
<p><code>sources</code>: The intended sources to compress.<br>
<code>compressMode</code>: The type of compression (zip, rar, etc.).<br>
<code>password</code>: The password of the output file.<br>
<code>CompressFileName</code>: The name of the output file.<br>
<code>validationMode</code>: If validation is necessary it should be set as 1 otherwise set as 0.</p>
</blockquote>
<h3><a id="Store_95"></a>Store</h3>
<h5><a id="FolderStore_96"></a>FolderStore</h5>
<p>FolderStore is used to store any type of sources into a directory.</p>
<pre><code>FolderStore(string fullPath)
</code></pre>
<blockquote>
<p><code>fullPath</code>: The complete path of destination directory.</p>
</blockquote>
<h5><a id="FtpStore_103"></a>FtpStore</h5>
<p>FtpStore is used to store any type of sources into a FTP directory.</p>
<pre><code>FtpStore(string destPath, string username, string password)
</code></pre>
<blockquote>
<p><code>destPath</code>: The full path of destination directory on FTP server starting with ftp:// .<br>
<code>username</code>: The username of the FTP server.<br>
<code>password</code>: The password of the FTP server.</p>
</blockquote>
<h1><a id="Backup_Process_111"></a>Backup Process</h1>
<h5><a id="Backupper_112"></a>Backupper</h5>
<p>Backupper object is used to get start backup process and bring sources and stores together.</p>
<pre><code>Backup(IEnumerable&lt;SourceBase&gt; sources, IEnumerable&lt;StoreBase&gt; stores, string tempDir)
</code></pre>
<blockquote>
<p><code>sources</code>: The input source objects to backup.<br>
<code>stores</code>: The output store objects to store file.<br>
<code>tempDir</code>: The temporary directory to store temporary files (default: %tmp%).</p>
</blockquote>
<h1><a id="Events_121"></a>Events</h1>
<h5><a id="Sources_122"></a>Sources</h5>
<blockquote>
<p><code>SourceStatus</code>: Invokes when source backup process has completed or has failed.<br>
<code>ValidationResultEvent</code>: Invokes when source validation process is completed.</p>
</blockquote>
<h5><a id="Stores_125"></a>Stores</h5>
<blockquote>
<p><code>StoreStatus</code>: Invokes when store process has completed or has failed.</p>
</blockquote>
<h5><a id="Backupper_127"></a>Backupper</h5>
<blockquote>
<p><code>SourcesStatus</code>: Invokes when a source backup process has completed or has failed.<br>
<code>StoresStatus</code>: Invokes when a store process has completed or has failed.<br>
<code>SourcesAndStoresStatus</code>: Invokes when a source backup process has fully backed up and has stored or has failed.<br>
<code>BackupperStatus</code>: Invokes when an exception has occurred in Backupper.Backup() or it has completed without error.<br>
<code>AllErrors</code>: Invokes when any exception has occurred in any part of backup process or each part has completed without error.</p>
</blockquote>
<h1><a id="Dependencies_133"></a>Dependencies</h1>
<p>SaaedBackup has some dependencies which is listed below:</p>
<blockquote>
<p><code>FluentFTP</code> (<a href="https://github.com/robinrodricks/FluentFTP">https://github.com/robinrodricks/FluentFTP</a>)<br>
<code>SharpZipLib</code> (<a href="https://github.com/icsharpcode/SharpZipLib">https://github.com/icsharpcode/SharpZipLib</a>)</p>
</blockquote>
<h1><a id="FAQ_138"></a>FAQ</h1>
<h5><a id="1How_to_backup_from_a_single_file_140"></a>1.How to backup from a single file?</h5>
<pre><code>var fileSource = new FileSource(@&quot;X:\pic.jpg&quot;);
var folderStore = new FolderStore(@&quot;X:\test&quot;);
var backup = new Backupper();
backup.Backup(new SourceBase[] { fileSource }, new StoreBase[] { folderStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<h5><a id="2How_to_backup_from_a_directory_147"></a>2.How to backup from a directory?</h5>
<pre><code>var folderSource = new FolderSource(@&quot;X:\sourceDir&quot;);
var folderStore = new FolderStore(@&quot;X:\test&quot;);
var backup = new Backupper();
backup.Backup(new SourceBase[] { folderSource }, new StoreBase[] { folderStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<h5><a id="3How_to_backup_from_a_SQL_server_database_155"></a>3.How to backup from a SQL server database?</h5>
<pre><code>var sqlSource = new SqlSource(&quot;serverName&quot;, &quot;databaseName&quot;, &quot;username&quot;, &quot;password&quot;, &quot;backupName&quot;, SqlSource.BackupMode.FullBackup);
var folderStore = new FolderStore(@&quot;X:\test&quot;);
var backup = new Backupper();
backup.Backup(new SourceBase[] { sqlSource }, new StoreBase[] { folderStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<h5><a id="4How_to_backup_from_some_sources_and_compress_them_into_an_archive_163"></a>4.How to backup from some sources and compress them into an archive?</h5>
<pre><code>var fileSource = new FileSource(@&quot;X:\pic.jpg&quot;);
var folderSource = new FolderSource(@&quot;X:\sourceDir&quot;);
var sqlSource = new SqlSource(&quot;serverName&quot;, &quot;databaseName&quot;, &quot;username&quot;, &quot;password&quot;, &quot;backupName&quot;, SqlSource.BackupMode.FullBackup);
var compressSource = new CompressSource(new [] { fileSource, folderSource, sqlSource }, CompressMode.Zip, &quot;archivePassword&quot;, &quot;archiveName&quot;, SourceBase.ValidationMode.Validate);
backup.Backup(new SourceBase[] { compressSource }, new StoreBase[] { folderStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<h5><a id="5How_to_backup_from_a_source_and_store_in_a_directory_172"></a>5.How to backup from a source and store in a directory?</h5>
<pre><code>var fileSource = new FileSource(@&quot;X:\pic.jpg&quot;);
var folderStore = new FolderStore(@&quot;X:\test&quot;);
var backup = new Backupper();
backup.Backup(new SourceBase[] { fileSource }, new StoreBase[] { folderStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<h5><a id="6How_to_backup_from_a_source_and_store_in_a_FTP_server_180"></a>6.How to backup from a source and store in a FTP server?</h5>
<pre><code>var fileSource = new FileSource(@&quot;X:\pic.jpg&quot;);
var ftpStore = new FtpStore(&quot;ftp://127.0.0.1&quot;, &quot;username&quot;, &quot;password&quot;);
var backup = new Backupper();
backup.Backup(new SourceBase[] { fileSource }, new StoreBase[] { ftpStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<h5><a id="7How_to_backup_from_multiple_sources_188"></a>7.How to backup from multiple sources?</h5>
<pre><code>var fileSource0 = new FileSource(@&quot;X:\pic.jpg&quot;);
var fileSource1 = new FileSource(@&quot;X:\mydoc.docx&quot;);
var folderSource = new FolderSource(@&quot;X:\sourceDir&quot;);
var sqlSource = new SqlSource(&quot;serverName&quot;, &quot;databaseName&quot;, &quot;username&quot;, &quot;password&quot;, &quot;backupName&quot;, SqlSource.BackupMode.FullBackup);
var compressSource = new CompressSource(new [] { fileSource1, folderSource}, CompressMode.Zip, &quot;archivePassword&quot;, &quot;archiveName&quot;, SourceBase.ValidationMode.Validate);
var folderStore = new FolderStore(@&quot;X:\test&quot;);
var backup = new Backupper();
backup.Backup(new SourceBase[] { fileSource0, fileSource1 , folderSource, sqlSource, compressSource }, new StoreBase[] { folderStore }, @&quot;X:\tempFolder&quot;);
</code></pre>
<h5><a id="8How_to_backup_from_a_source_and_store_in_multiple_places_200"></a>8.How to backup from a source and store in multiple places?</h5>
<pre><code>var fileSource = new FileSource(@&quot;X:\pic.jpg&quot;);
var ftpStore = new FtpStore(&quot;ftp://127.0.0.1&quot;, &quot;username&quot;, &quot;password&quot;);
var folderStore = new FolderStore(@&quot;X:\test&quot;);
var backup = new Backupper();
backup.Backup(new SourceBase[] { fileSource }, new StoreBase[] { ftpStore, folderStore }, @&quot;X:\tempFolder&quot;);
</code></pre>

</body></html>
