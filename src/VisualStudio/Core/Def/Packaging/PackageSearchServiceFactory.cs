using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Elfie.Model;
using Elfie.Model.Structures;
using Elfie.Model.Tree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Roslyn.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    [ExportWorkspaceServiceFactory(typeof(IPackageSearchService), WorkspaceKind.Host), Shared]
    internal class PackageSearchServiceFactory : IWorkspaceServiceFactory
    {
        private readonly VSShell.SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public PackageSearchServiceFactory(VSShell.SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // Only support package search in vs workspace.
            return workspaceServices.Workspace is VisualStudioWorkspace
                ? new PackageSearchService(_serviceProvider)
                : (IPackageSearchService)new NullPackageSearchService();
        }

        private class NullPackageSearchService : IPackageSearchService
        {
            public IEnumerable<PackageSearchResult> Search(string name, int arity, CancellationToken cancellationToken)
            {
                return SpecializedCollections.EmptyEnumerable<PackageSearchResult>();
            }
        }
    }

    internal class PackageSearchService : ForegroundThreadAffinitizedObject, IPackageSearchService, IDisposable
    {
        private const string HostId = "RoslynNuGetSearch";
        private const string BackupExtension = ".bak";

        private static readonly LinkedList<string> _log = new LinkedList<string>();

        private readonly int DataFormatVersion = AddReferenceDatabase.TextFileFormatVersion;

        private readonly object gate = new object();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private AddReferenceDatabase database_doNotAccessDirectly;

        private readonly DirectoryInfo _cacheDirectoryInfo;
        private readonly FileInfo _databaseFileInfo;

        private readonly IPackageSearchDelayService _delayService;
        private readonly IPackageSearchIOService _ioService;
        private readonly IPackageSearchLogService _logService;
        private readonly IPackageSearchRemoteControlService _remoteControlService;
        private readonly IPackageSearchPatchService _patchService;
        private readonly IPackageSearchDatabaseFactoryService _databaseFactoryService;

        public PackageSearchService(VSShell.SVsServiceProvider serviceProvider)
            : this(CreateRemoteControlService(serviceProvider),
                   new DefaultPackageSearchLogService((IVsActivityLog)serviceProvider.GetService(typeof(SVsActivityLog))),
                   new DefaultPackageSearchDelayService(), 
                   new DefaultPackageSearchIOService(),
                   new DefaultPackageSearchPatchService(),
                   new DefaultPackageSearchDatabaseFactoryService(),
                   new ShellSettingsManager(serviceProvider).GetApplicationDataFolder(ApplicationDataFolder.LocalSettings))
        {
            // Kick off a database update.  Wait a few seconds before starting so we don't
            // interfere too much with solution loading.
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ => UpdateDatabaseInBackgroundAsync(), TaskScheduler.Default);
        }

        private static IPackageSearchRemoteControlService CreateRemoteControlService(VSShell.SVsServiceProvider serviceProvider)
        {
            var vsService = serviceProvider.GetService(typeof(SVsRemoteControlService));
            if (vsService == null)
            {
                // If we can't access the file update service, then there's nothing we can do.
                return null;
            }

            return new DefaultPackageRemoteControlService(vsService);
        }

        public PackageSearchService(
            IPackageSearchRemoteControlService remoteControlService,
            IPackageSearchLogService logService,
            IPackageSearchDelayService delayService,
            IPackageSearchIOService ioService,
            IPackageSearchPatchService patchService,
            IPackageSearchDatabaseFactoryService databaseFactoryService,
            string localSettingsDirectory)
        {
            if (remoteControlService == null)
            {
                // If we can't access the file update service, then there's nothing we can do.
                return;
            }

            _delayService = delayService;
            _ioService = ioService;
            _logService = logService;
            _remoteControlService = remoteControlService;
            _patchService = patchService;
            _databaseFactoryService = databaseFactoryService;

            _cacheDirectoryInfo = new DirectoryInfo(Path.Combine(
                localSettingsDirectory, "NuGetCache", string.Format($"Format{DataFormatVersion}")));
            _databaseFileInfo = new FileInfo(Path.Combine(_cacheDirectoryInfo.FullName, "NuGetCache.txt"));

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public void Dispose()
        {
            // Cancel any existing work.
            _cancellationTokenSource.Cancel();
        }

        private AddReferenceDatabase Database
        {
            get
            {
                lock (gate)
                {
                    return database_doNotAccessDirectly;
                }
            }

            set
            {
                lock (gate)
                {
                    // If we don't have an existing database, or the database version has changed
                    // then update the database we're currently pointing at.
                    if (database_doNotAccessDirectly == null ||
                        database_doNotAccessDirectly.DatabaseVersion != value.DatabaseVersion)
                    {
                        database_doNotAccessDirectly = value;
                    }
                }
            }
        }

        private void LogInfo(string text) => _logService.LogInfo(text);

        private void LogException(Exception e, string text) => _logService.LogException(e, text);

        /// <summary>
        /// Internal for testing purposes.
        /// </summary>
        internal async Task UpdateDatabaseInBackgroundAsync()
        {
            // Keep on looping until we're told to shut down.
            while (!_cancellationToken.IsCancellationRequested)
            {
                LogInfo("Starting update");
                try
                {
                    var delayUntilNextUpdate = await UpdateDatabaseInBackgroundWorkerAsync().ConfigureAwait(false);

                    LogInfo($"Waiting {delayUntilNextUpdate} until next update");
                    await Task.Delay(delayUntilNextUpdate, _cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    LogInfo("Update canceled. Ending update loop");
                    return;
                }
            }
        }

        /// <returns>The timespan the caller should wait until calling this method again.</returns>
        private async Task<TimeSpan> UpdateDatabaseInBackgroundWorkerAsync()
        {
            // Attempt to update the local db if we have one, or download a full db
            // if we don't.  In the event of any error back off a minute and try 
            // again.  Lot of errors are possible here as IO/network/other-libraries
            // are involved.  For example, we might get errors trying to write to 
            // disk.
            try
            {
                CleanCacheDirectory();

                // If we have a local database, then see if it needs to be patched.
                // Otherwise download the full database.
                if (_ioService.Exists(_databaseFileInfo))
                {
                    LogInfo("Local database file exists. Patching local database");
                    return await PatchLocalDatabaseAsync().ConfigureAwait(false);
                }
                else
                {
                    LogInfo("Local database file does not exist. Downloading full database");
                    return await DownloadFullDatabaseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                // Something bad happened (IO Exception, network exception etc.).
                // ask our caller to try updating again a minute from now.
                //
                // Note: we skip OperationCanceledException because it's not 'bad'.
                // It's the standard way to indicate that we've been asked to shut
                // down.
                var delay = _delayService.UpdateFailedDelay;
                LogException(e, $"Error occurred updating. Retrying update in {delay}");
                return _delayService.UpdateFailedDelay;
            }
        }

        private void CleanCacheDirectory()
        {
            LogInfo("Cleaning cache directory");

            if (!_ioService.Exists(_cacheDirectoryInfo))
            {
                LogInfo("Creating cache directory");
                _ioService.Create(_cacheDirectoryInfo);
                LogInfo("Cache directory created");
            }

            // Now remove any stale .bak files we might have.
            foreach (var file in _ioService.EnumerateFiles(_cacheDirectoryInfo))
            {
                if (file.Extension == BackupExtension)
                {
                    IOUtilities.PerformIO(() =>
                    {
                        LogInfo($"Deleting backup file: {file.FullName}");
                        _ioService.Delete(file);
                        return true;
                    });
                }
            }
        }

        private async Task<TimeSpan> DownloadFullDatabaseAsync()
        {
            var serverPath = $"{DataFormatVersion}/LatestDatabase.xml";

            LogInfo($"Downloading and processing full database: {serverPath}");

            var element = await DownloadFileAsync(serverPath).ConfigureAwait(false);
            var delayUntilNextUpdate = await ProcessFullDatabaseXElementAsync(element).ConfigureAwait(false);

            LogInfo("Downloading and processing full database completed");
            return delayUntilNextUpdate;
        }

        private async Task<TimeSpan> ProcessFullDatabaseXElementAsync(XElement element)
        {
            LogInfo("Processing full database element");

            // Convert the database contents in the xml to a byte[].
            var bytes = ParseDatabaseElement(element);

            // Make a database out of that and set it to our in memory database that we'll be 
            // searching.
            CreateAndSetInMemoryDatabase(bytes);

            // Write the file out to disk so we'll have it the next time we launch VS.  Do this
            // after we set the in-memory instance so we at least have something to search while
            // we're waiting to write.
            await WriteDatabaseFile(bytes).ConfigureAwait(false);

            var delay = _delayService.UpdateSucceededDelay;
            LogInfo($"Processing full database element completed. Update again in {delay}");
            return delay;
        }

        private async Task WriteDatabaseFile(byte[] bytes)
        {
            LogInfo("Writing database file");

            var guidString = new Guid().ToString();
            var tempFilePath = Path.Combine(_cacheDirectoryInfo.FullName, guidString + ".tmp");
            var backupFilePath = Path.Combine(_cacheDirectoryInfo.FullName, guidString + ".bak");

            LogInfo($"Temp file path  : {tempFilePath}");
            LogInfo($"Backup file path: {backupFilePath}");

            // First, write to a temporary file next to the actual database file.
            // Note that we explicitly use FileStream so that we can call .Flush to ensure the
            // file has been completely written to disk (at least as well as the OS can guarantee
            // things).

            LogInfo("Writing temp file");
            _ioService.WriteAndFlushAllBytes(tempFilePath, bytes);
            LogInfo("Writing temp file completed");

            // Now try to replace the existing DB file with the temp file. Try up to a minute just 
            // in case the file is locked.  The .bak file will be deleted in the future the next 
            // time we do an update.

            await RepeatAsync(
                () =>
                {
                    LogInfo("Replacing database file");
                    _ioService.Replace(tempFilePath, _databaseFileInfo.FullName, backupFilePath, ignoreMetadataErrors: true);
                    LogInfo("Replace database file completed");
                },
                repeat: 6, delay: _delayService.FileWriteDelay).ConfigureAwait(false);
        }

        private async Task<TimeSpan> PatchLocalDatabaseAsync()
        {
            LogInfo("Patching local database");

            LogInfo("Reading in local database");
            var databaseBytes = _ioService.ReadAllBytes(_databaseFileInfo.FullName);
            LogInfo($"Reading in local database completed. databaseBytes.Length={databaseBytes.Length}");

            // Make a database instance out of those bytes and set is as the current in memory database
            // that searches will run against.  If we can't make a database instance from these bytes
            // then our local database is corrupt and we need to download the full database to get back
            // into a good state.
            AddReferenceDatabase database;
            try
            {
                database = CreateAndSetInMemoryDatabase(databaseBytes);
            }
            catch (Exception e)
            {
                LogException(e, "Error creating database from local copy. Downloading full database");
                return await DownloadFullDatabaseAsync().ConfigureAwait(false);
            }

            var databaseVersion = database.DatabaseVersion;

            // Now attempt to download and apply patch file.
            var serverPath = $"{DataFormatVersion}/{database.DatabaseVersion}/Patch.xml";

            LogInfo("Downloading and processing patch file: " + serverPath);

            var element = await DownloadFileAsync(serverPath).ConfigureAwait(false);
            var delayUntilUpdate = await ProcessPatchXElementAsync(element, databaseBytes).ConfigureAwait(false);

            LogInfo("Downloading and processing patch file completed");
            LogInfo("Patching local database completed");

            return delayUntilUpdate;
        }

        /// <summary>
        /// Creates a database instance with the bytes passed in.  If creating the database succeeds,
        /// then it will be set as the current in memory version.  In the case of failure (which 
        /// indicates that our data is corrupt), the local database will be deleted so that we will 
        /// end up downloading the full database again.
        /// </summary>
        private AddReferenceDatabase CreateAndSetInMemoryDatabase(byte[] bytes)
        {
            var database = CreateDatabaseFromBytes(bytes);
            this.Database = database;
            return database;
        }

        private async Task<TimeSpan> ProcessPatchXElementAsync(XElement patchElement, byte[] databaseBytes)
        {
            try
            {
                LogInfo("Processing patch element");
                var delayUntilUpdate = await TryProcessPatchXElementAsync(patchElement, databaseBytes).ConfigureAwait(false);
                if (delayUntilUpdate != null)
                {
                    LogInfo($"Processing patch element completed. Update again in {delayUntilUpdate.Value}");
                    return delayUntilUpdate.Value;
                }

                // Fall through and download full database.
            }
            catch (Exception e)
            {
                LogException(e, "Error occurred while processing patch element. Downloading full database");
                // Fall through and download full database.
            }

            return await DownloadFullDatabaseAsync().ConfigureAwait(false);
        }

        private async Task<TimeSpan?> TryProcessPatchXElementAsync(XElement patchElement, byte[] databaseBytes)
        {
            bool isUpToDate, isTooOld;
            byte[] patchBytes;
            ParsePatchElement(patchElement, out isUpToDate, out isTooOld, out patchBytes);

            if (isUpToDate)
            {
                LogInfo("Local version is up to date");
                return _delayService.UpdateSucceededDelay;
            }

            if (isTooOld)
            {
                LogInfo("Local version too old");
                return null;
            }

            LogInfo($"Got patch. databaseBytes.Length={databaseBytes.Length} patchBytes.Length={patchBytes.Length}.");

            // We have patch data.  Apply it to our current database bytes to produce the new
            // database.
            LogInfo("Applying patch");
            var finalBytes = _patchService.ApplyPatch(databaseBytes, patchBytes);
            LogInfo($"Applying patch completed. finalBytes.Length={finalBytes.Length}");

            CreateAndSetInMemoryDatabase(finalBytes);

            await WriteDatabaseFile(finalBytes).ConfigureAwait(false);

            return _delayService.UpdateSucceededDelay;
        }

        private void ParsePatchElement(XElement patchElement, out bool isUpToDate, out bool isTooOld, out byte[] patchBytes)
        {
            patchBytes = null;

            var isUpToDateAttribute = patchElement.Attribute("isUpToDate");
            isUpToDate = isUpToDateAttribute != null && (bool)isUpToDateAttribute;

            var isTooOldAttribute = patchElement.Attribute("isTooOld");
            isTooOld = isTooOldAttribute != null && (bool)isTooOldAttribute;

            var contentsAttribute = patchElement.Attribute("contents");
            if (contentsAttribute != null)
            {
                var contents = contentsAttribute.Value;
                patchBytes = Convert.FromBase64String(contents);
            }

            var hasPatchBytes = patchBytes != null;

            var value = (isUpToDate ? 1 : 0) +
                        (isTooOld ? 1 : 0) +
                        (hasPatchBytes ? 1 : 0);
            if (value != 1)
            {
                throw new FormatException($"Patch format invalid. isUpToDate={isUpToDate} isTooOld={isTooOld} hasPatchBytes={hasPatchBytes}");
            }
        }

        private AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes)
        {
            LogInfo("Creating database from bytes");
            var result = _databaseFactoryService.CreateDatabaseFromBytes(bytes);
            LogInfo("Creating database from bytes completed");
            return result;
        }

        private async Task<XElement> DownloadFileAsync(string serverPath)
        {
            LogInfo("Creating download client: " + serverPath);

            // Create a client that will attempt to download the specified file.  The client works
            // in the following manner:
            //
            //      1) If the file is not cached locally it will download it in the background.
            //         Until the file is downloaded, null will be returned from client.ReadFile.
            //      2) If the file is cached locally and was downloaded less than (24 * 60) 
            //         minutes ago, then the client will do nothing (until that time has elapsed).
            //         Calls to client.ReadFile will return the cached file.
            //      3) If the file is cached locally and was downloaded more than (24 * 60) 
            //         minutes ago, then the client will attempt to download the file.
            //         In the interim period null will be returned from client.ReadFile.
            var pollingMinutes = (int)TimeSpan.FromDays(1).TotalMinutes;
            using (var client = _remoteControlService.CreateClient(HostId, serverPath, pollingMinutes))
            {
                LogInfo("Creating download client completed");

                // Poll the client every minute until we get the file.
                while (true)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var resultOpt = await TryDownloadFileAsync(client).ConfigureAwait(false);
                    if (resultOpt == null)
                    {
                        LogInfo("File not downloaded. Trying again in one minute");
                        await Task.Delay(_delayService.CachePollDelay, _cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // File was downloaded.  
                        return resultOpt;
                    }
                }
            }
        }

        /// Returns 'null' if download is not available and caller should keep polling.
        private async Task<XElement> TryDownloadFileAsync(IPackageSearchRemoteControlClient client)
        {
            LogInfo("Read file from client");

            // "ReturnsNull": Only return a file if we have it locally *and* it's not older than our polling time (1 day).

            using (var stream = await client.ReadFileAsync(__VsRemoteControlBehaviorOnStale.ReturnsNull).ConfigureAwait(false))
            {
                if (stream == null)
                {
                    LogInfo("Read file completed. Client returned no data");
                    return null;
                }

                LogInfo("Read file completed. Client returned data");
                LogInfo("Converting data to XElement");
                var result = XElement.Load(stream);
                LogInfo("Converting data to XElement completed");
                return result;
            }
        }

        private async Task RepeatAsync(Action action, int repeat, TimeSpan delay)
        {
            for (var i = 0; i < repeat; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    action();
                    return;
                }
                catch (Exception e)
                {
                    LogException(e, $"Operation failed. Trying again after {delay}");
                    await Task.Delay(delay, _cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private byte[] ParseDatabaseElement(XElement element)
        {
            LogInfo("Parsing database element");
            var contentsAttribute = element.Attribute("contents");
            if (contentsAttribute == null)
            {
                throw new FormatException("Database element invalid. Missing 'contents' attribute");
            }

            var text = contentsAttribute.Value;
            var compressedBytes = Convert.FromBase64String(text);

            using (var inStream = new MemoryStream(compressedBytes))
            using (var gzipStream = new GZipStream(inStream, CompressionMode.Decompress))
            using (var outStream = new MemoryStream())
            {
                gzipStream.CopyTo(outStream);
                var bytes = outStream.ToArray();

                LogInfo($"Parsing complete. bytes.length = {bytes.Length}");
                return bytes;
            }
        }

        // TODO(cyrusn): remove arity.
        public IEnumerable<PackageSearchResult> Search(string name, int arity, CancellationToken cancellationToken)
        {
            var database = this.Database;
            if (database == null)
            {
                // Don't have a database to search.  
                yield break;
            }

            var query = new MemberQuery(name, isFullSuffix: true, isFullNamespace: false);

            var symbols = new PartialArray<Symbol>(1);
            if (query.TryFindMembers(database, ref symbols))
            {
                var result = new List<PackageSearchResult>();
                foreach (var symbol in symbols)
                {
                    var nameParts = new List<string>();
                    GetFullName(nameParts, symbol.FullName.Parent);

                    if (nameParts.Count > 0)
                    {
                        yield return new PackageSearchResult(nameParts, symbol.PackageName.ToString());
                        yield break;
                    }
                }
            }
        }

        private void GetFullName(List<string> nameParts, Path8 path)
        {
            if (!path.IsEmpty)
            {
                GetFullName(nameParts, path.Parent);
                nameParts.Add(path.Name.ToString());
            }
        }

        private class DefaultPackageSearchDelayService : IPackageSearchDelayService
        {
            public TimeSpan CachePollDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan FileWriteDelay { get; } = TimeSpan.FromSeconds(10);
            public TimeSpan UpdateFailedDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan UpdateSucceededDelay { get; } = TimeSpan.FromDays(1);
        }

        private class DefaultPackageSearchIOService : IPackageSearchIOService
        {
            public void Create(DirectoryInfo directory) => directory.Create();

            public void Delete(FileInfo file) => file.Delete();

            public IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory) => directory.EnumerateFiles();

            public bool Exists(FileSystemInfo info) => info.Exists;

            public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors) =>
                File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);

            public void WriteAndFlushAllBytes(string path, byte[] bytes)
            {
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    fileStream.Write(bytes, 0, bytes.Length);
                    fileStream.Flush(flushToDisk: true);
                }
            }
        }

        private class DefaultPackageSearchLogService : ForegroundThreadAffinitizedObject, IPackageSearchLogService
        {
            private readonly IVsActivityLog _activityLog;

            public DefaultPackageSearchLogService(IVsActivityLog activityLog)
            {
                _activityLog = activityLog;
            }

            public void LogInfo(string text)
            {
                Log(text, __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION);
            }

            public void LogException(Exception e, string text)
            {
                Log(text + ". " + e.ToString(), __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
            }

            private void Log(string text, __ACTIVITYLOG_ENTRYTYPE type)
            {
                if (!this.IsForeground())
                {
                    this.InvokeBelowInputPriority(() => Log(text, type));
                    return;
                }

                AssertIsForeground();
                _activityLog?.LogEntry((uint)type, HostId, text);

                // Keep a running in memory log as well for debugging purposes.
                _log.AddLast(text);
                while (_log.Count > 100)
                {
                    _log.RemoveFirst();
                }
            }
        }

        private class DefaultPackageSearchPatchService : IPackageSearchPatchService
        {
            public byte[] ApplyPatch(byte[] databaseBytes, byte[] patchBytes)
            {
                return Patching.Delta.ApplyPatch(databaseBytes, patchBytes);
            }
        }

        private class DefaultPackageRemoteControlService : IPackageSearchRemoteControlService
        {
            private readonly object _remoteControlService;

            public DefaultPackageRemoteControlService(object remoteControlService)
            {
                _remoteControlService = remoteControlService;
            }

            public IPackageSearchRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes)
            {
                var serviceType = _remoteControlService.GetType();
                var serviceAssembly = serviceType.Assembly;
                var clientType = serviceAssembly.GetType("Microsoft.VisualStudio.Services.RemoteControl.VSRemoteControlClient");

                var vsClient = Activator.CreateInstance(clientType, args: new object[]
                {
                    HostId,
                    serverPath,
                    pollingMinutes,
                    null
                });

                var clientField = vsClient.GetType().GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
                var client = clientField.GetValue(vsClient) as IDisposable;
                return new DefaultRemoteControlClient(client);
            }
        }

        private class DefaultRemoteControlClient : IPackageSearchRemoteControlClient
        {
            private readonly IDisposable client;

            public DefaultRemoteControlClient(IDisposable client)
            {
                this.client = client;
            }

            public void Dispose()
            {
                client.Dispose();
            }

            public Task<Stream> ReadFileAsync(__VsRemoteControlBehaviorOnStale behavior)
            {
                var clientType = client.GetType();
                var readFileAsyncMethod = clientType.GetMethod("ReadFileAsync");
                var streamTask = (Task<Stream>)readFileAsyncMethod.Invoke(client, new object[] { (int)behavior});

                return streamTask;
            }
        }

        private class DefaultPackageSearchDatabaseFactoryService : IPackageSearchDatabaseFactoryService
        {
            public AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes)
            {
                using (var memoryStream = new MemoryStream(bytes))
                using (var streamReader = new StreamReader(memoryStream))
                {
                    var database = new AddReferenceDatabase();
                    database.ReadText(streamReader);
                    return database;
                }
            }
        }
    }

    internal interface IPackageSearchDatabaseFactoryService
    {
        AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes);
    }

    /// <summary>
    /// Used so we can mock out the remote control service in unit tests.
    /// </summary>
    internal interface IPackageSearchRemoteControlService
    {
        IPackageSearchRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes);
    }

    /// <summary>
    /// Used so we can mock out the client in unit tests.
    /// </summary>
    internal interface IPackageSearchRemoteControlClient : IDisposable
    {
        Task<Stream> ReadFileAsync(__VsRemoteControlBehaviorOnStale behavior);
    }

    /// <summary>
    /// Used so we can mock out patching in unit tests.
    /// </summary>
    internal interface IPackageSearchPatchService
    {
        byte[] ApplyPatch(byte[] databaseBytes, byte[] patchBytes);
    }

    /// <summary>
    /// Used so we can mock out logging in unit tests.
    /// </summary>
    internal interface IPackageSearchLogService
    {
        void LogException(Exception e, string text);
        void LogInfo(string text);
    }

    /// <summary>
    /// Used so we can mock out how the search service does IO for testing purposes.
    /// </summary>
    internal interface IPackageSearchIOService
    {
        void Create(DirectoryInfo directory);
        void Delete(FileInfo file);
        IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory);
        bool Exists(FileSystemInfo info);
        byte[] ReadAllBytes(string path);
        void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors);
        void WriteAndFlushAllBytes(string path, byte[] bytes);
    }

    /// <summary>
    /// Used so we can mock out how the search service delays work for testing purposes.
    /// </summary>
    internal interface IPackageSearchDelayService
    {
        /// <summary>
        /// They time to wait after a successful update (default = 1 day).
        /// </summary>
        TimeSpan UpdateSucceededDelay { get; }

        /// <summary>
        /// They time to wait after a failed update (default = 1 minute).
        /// </summary>
        TimeSpan UpdateFailedDelay { get; }

        /// <summary>
        /// They time to wait after writing to disk fails (default = 10 seconds).
        /// </summary>
        TimeSpan FileWriteDelay { get; }

        /// <summary>
        /// How long to wait between each poll of the cache (default = 1 minute).
        /// </summary>
        TimeSpan CachePollDelay { get; }
    }
}