using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Elfie.Model;
using Elfie.Model.Structures;
using Elfie.Model.Tree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Settings;
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

    internal class PackageSearchService : IPackageSearchService, IDisposable
    {
        private const string HostId = "RoslynNuGetSearch";
        private const string BackupExtension = ".bak";

        private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(10);

        private readonly object gate = new object();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private IMemberDatabase memberDatabase_doNotAccessDirectly;

        private readonly DirectoryInfo _cacheDirectoryInfo;
        private readonly FileInfo _databaseFileInfo;

        private readonly VSShell.SVsServiceProvider _serviceProvider;
        private readonly IVsRemoteControlService _remoteControlService;

        public PackageSearchService(VSShell.SVsServiceProvider serviceProvider)
        {
            _remoteControlService = (IVsRemoteControlService)serviceProvider.GetService(typeof(SVsRemoteControlService));
            if (_remoteControlService == null)
            {
                // If we can't access the file update service, then there's nothing we can do.
                return;
            }

            _serviceProvider = serviceProvider;
            var settingsManager = new ShellSettingsManager(serviceProvider);

            var localSettingsDirectory = settingsManager.GetApplicationDataFolder(ApplicationDataFolder.LocalSettings);
            _cacheDirectoryInfo = new DirectoryInfo(Path.Combine(localSettingsDirectory, "NuGetCache", DataFormatVersion));
            _databaseFileInfo = new FileInfo(Path.Combine(_cacheDirectoryInfo.FullName, "NuGetCache.txt"));

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            // Kick off a database update.  Wait a few seconds before starting so we don't
            // interfere too much with solution loading.
            Task.Delay(TenSeconds).ContinueWith(_ => UpdateDatabaseInBackgroundAsync(), TaskScheduler.Default);
        }

        public void Dispose()
        {
            // Cancel any existing work.
            _cancellationTokenSource.Cancel();
        }

        private IMemberDatabase MemberDatabase
        {
            get
            {
                lock (gate)
                {
                    return memberDatabase_doNotAccessDirectly;
                }
            }

            set
            {
                lock (gate)
                {
                    // If we don't have an existing database, or the database version has changed
                    // then update the database we're currently pointing at.
                    if (memberDatabase_doNotAccessDirectly == null || 
                        GetDatabaseVersion(memberDatabase_doNotAccessDirectly) != GetDatabaseVersion(value))
                    {
                        memberDatabase_doNotAccessDirectly = value;
                    }
                }
            }
        }

        public string DataFormatVersion { get { throw new NotImplementedException(); } }

        private string GetDatabaseVersion(IMemberDatabase database)
        {
            throw new NotImplementedException();
        }

        private async Task UpdateDatabaseInBackgroundAsync()
        {
            // Keep on looping until we're told to shut down.
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var delayUntilNextCheck = await UpdateDatabaseInBackgroundWorkerAsync().ConfigureAwait(false);
                    await Task.Delay(delayUntilNextCheck, _cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // We got canceled/disposed.  Just stop what we're doing.
                    return;
                }
            }
        }

        /// <returns>The timespan the caller should wait until calling this method again.</returns>
        private async Task<TimeSpan> UpdateDatabaseInBackgroundWorkerAsync()
        {
            try
            {
                CleanCacheDirectory();

                // If we have a local database, then see if it needs to be patched.
                // Otherwise download the full database.
                if (_databaseFileInfo.Exists)
                {
                    await PatchLocalDatabaseAsync().ConfigureAwait(false);
                }
                else
                {
                    await DownloadFullDatabaseAsync().ConfigureAwait(false);
                }

                // Everything succeeded.  Ask our caller to update one day from now.
                return OneDay;
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                // Something bad happened (IO Exception, network exception etc.).
                // ask our caller to try updating again a minute from now.
                //
                // Note: we skip OperationCanceledException because it's not 'bad'.
                // It's the standard way to indicate that we've been asked to shut
                // down.
                return OneMinute;
            }
        }

        private void CleanCacheDirectory()
        {
            // Make sure we've got the directory to place our cache file.
            if (!_cacheDirectoryInfo.Exists)
            {
                _cacheDirectoryInfo.Create();
            }

            // Now remove any stale .bak files we might have.
            foreach (var file in _cacheDirectoryInfo.EnumerateFiles())
            {
                if (file.Extension == BackupExtension)
                {
                    IOUtilities.PerformIO(() =>
                    {
                        file.Delete();
                        return true;
                    });
                }
            }
        }

        private async Task DownloadFullDatabaseAsync()
        {
            var serverPath = $"{DataFormatVersion}/LatestDatabase.xml";
            await DownloadAndProcessFileAsync(
                serverPath,
                callback: HandleFullDatabaseXElementAsync).ConfigureAwait(false);
        }

        private async Task HandleFullDatabaseXElementAsync(XElement element)
        {
            // Convert the database contents in the xml to a byte[].
            var bytes = ParseDatabaseElement(element);

            // Create a DB out of those bytes.
            var database = CreateDatabaseFromBytes(bytes);

            // we successfully created a database instance.  Set it as our current in-memory database
            // that searches will run against.
            this.MemberDatabase = database;

            // Write the file out to disk so we'll have it the next time we launch VS.  Do this
            // after we set the in-memory instance so we at least have something to search while
            // we're waiting to write.
            await WriteDatabaseFile(bytes).ConfigureAwait(false);
        }

        private async Task WriteDatabaseFile(byte[] bytes)
        {
            var guidString = new Guid().ToString();
            var tempFilePath = Path.Combine(_cacheDirectoryInfo.FullName, guidString + ".tmp");
            var backupFilePath = Path.Combine(_cacheDirectoryInfo.FullName, guidString + ".bak");

            // First, write to a temporary file next to the actual database file.
            // Note that we explicitly use FileStream so that we can call .Flush to ensure the
            // file has been completely written to disk (at least as well as the OS can guarantee
            // things).
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                // Write out all the bytes.
                fileStream.Write(bytes, 0, bytes.Length);

                // Actually flush them (to the best of the OS' ability) so they're on disk.
                fileStream.Flush(flushToDisk: true);
            }

            // Now try to replace hte existing DB file with the temp file.
            // Try up to a minute just in case the file is locked.
            await RepeatAsync(
                () => File.Replace(tempFilePath, _databaseFileInfo.FullName, backupFilePath, ignoreMetadataErrors: true),
                repeat: 6, delay: TenSeconds).ConfigureAwait(false);
        }

        private async Task PatchLocalDatabaseAsync()
        {
            // Read in the current database from the bytes we have on disk.
            var bytes = File.ReadAllBytes(_databaseFileInfo.FullName);

            // Make a database instance out of those bytes and set is as the ccurrent in memory database
            // that searches will run against.  If we can't make a database instance from these bytes
            // then our local database is corrupt and we need to download the full database to get back
            // into a good state.
            var database = await CreateAndSetDatabase_RedownloadFullDatabaseOnFailureAsync(bytes).ConfigureAwait(false);
            var databaseVersion = GetDatabaseVersion(database);

            // Now attempt to download and apply patch file.
            var serverPath = $"{DataFormatVersion}/{databaseVersion}/Patch.xml";
            await DownloadAndProcessFileAsync(
                serverPath,
                callback: x => HandlePatchXElementAsync(x, bytes)).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a database instance with the bytes passed in.  If creating the database succeeds,
        /// then it will be set as the current in memory version.  In the case of failure (which 
        /// indicates that our data is corrupt), the local database will be deleted so that we will 
        /// end up downloading the full database again.
        /// </summary>
        private async Task<IMemberDatabase> CreateAndSetDatabase_RedownloadFullDatabaseOnFailureAsync(byte[] bytes)
        {
            IMemberDatabase database;
            try
            {
                database = CreateDatabaseFromBytes(bytes);
            }
            catch
            {
                // Failure parsing the DB.  This means our local data is just busted.
                // Just delete what we have locally and start all over again.  This will
                // At least get us to download the latest version.
                await DeleteLocalDatabaseFile().ConfigureAwait(false);
                throw;
            }

            // we successfully loaded a database.  Set it as our current database for now.
            this.MemberDatabase = database;
            return database;
        }

        private async Task DeleteLocalDatabaseFile()
        {
            await RepeatAsync(() =>
            {
                if (_databaseFileInfo.Exists)
                {
                    _databaseFileInfo.Delete();
                }
            }, repeat: 6, delay: TenSeconds).ConfigureAwait(false);
        }

        private async Task HandlePatchXElementAsync(XElement patchElement, byte[] databaseBytes)
        {
            bool isUpToDate, isTooOld;
            byte[] patchBytes;
            ParsePatchElement(patchElement, out isUpToDate, out isTooOld, out patchBytes);

            if (isUpToDate)
            {
                // Our local version is up to date.  We don't need to do anything.  Try again in a day.
                return;
            }

            if (isTooOld)
            {
                // Our local version is so out of date that we have no choice but to download the full
                // database.
                await DownloadFullDatabaseAsync().ConfigureAwait(false);
                return;
            }

            // We have patch data.  Apply it to our current database bytes to produce the new
            // database.
            byte[] finalDatabaseBytes;
            try
            {
                finalDatabaseBytes = Patching.Delta.ApplyPatch(databaseBytes, patchBytes);
            }
            catch
            {
                // An error occurred while applying the patch.  At this point we have to assume
                // either the local data is bad, or the patch is bad.  Downloading the patch 
                // again likely won't help (and we'll just be stuck in the state where the local
                // data is just getting staler).  So it's probably best if we download the latest
                // version.
                await DownloadFullDatabaseAsync().ConfigureAwait(false);
                return;
            }

            // We've patched the file successfully.  Now try to make a database out of it and set
            // it as the current in memory version.  If this fails then that means there's something
            // wrong with the patch.  Just download the full database in this event so we can get
            // into a good state.
            await CreateAndSetDatabase_RedownloadFullDatabaseOnFailureAsync(finalDatabaseBytes).ConfigureAwait(false);

            // Finally write out the patched database file to disk.  
            await WriteDatabaseFile(finalDatabaseBytes).ConfigureAwait(false);
        }

        private void ParsePatchElement(XElement patchElement, out bool isUpToDate, out bool isTooOld, out byte[] patchBytes)
        {
            throw new NotImplementedException();
        }

        private IMemberDatabase CreateDatabaseFromBytes(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream))
            {
                var database = new AddReferenceDatabase();
                database.ReadText(streamReader);
                return database;
            }
        }

        private async Task DownloadAndProcessFileAsync(string serverPath, Func<XElement, Task> callback)
        {
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
            var client = _remoteControlService.CreateClient(
                szHostId: HostId, szRelativeFilePath: serverPath, pollingIntervalMinutes: 24 * 60);

            // Poll the client every minute until we get the file.
            try
            {
                while (true)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var success = await TryDownloadAndProcessFileAsync(client, callback).ConfigureAwait(false);
                    if (success)
                    {
                        // We handled the file.  Our work here is done.
                        return;
                    }

                    // Client hasn't downloaded the file.  Wait a minute and try again.
                    await Task.Delay(OneMinute, _cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                client.Close();
            }
        }

        private async Task<bool> TryDownloadAndProcessFileAsync(
            IVsRemoteControlClient client, Func<XElement, Task> callback)
        {
            // Only return a file if we have it locally *and* it's not older than our polling time (1 day).
            var stream = client.ReadFile((int)__VsRemoteControlBehaviorOnStale.ReturnsNull);
            if (stream == null)
            {
                // No local data for this yet. Keep polling.
                return false;
            }

            var element = DownloadXElement(stream);
            await callback(element).ConfigureAwait(false);

            // We're done.  
            return true;
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
                catch
                {
                    // Failed for some reason.  try again after the delay has passed.
                    await Task.Delay(delay, _cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private byte[] ParseDatabaseElement(XElement element)
        {
            throw new NotImplementedException();
        }

        private static XElement DownloadXElement(ISequentialStream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                byte[] temp = new byte[1 << 16];

                uint amountRead;
                do
                {
                    stream.Read(temp, (uint)temp.Length, out amountRead);
                    memoryStream.Write(temp, 0, (int)amountRead);
                }
                while (amountRead > 0);

                // Reset the stream to the beginning so we can parse out the xml from it.
                memoryStream.Position = 0;
                return XElement.Load(memoryStream);
            }
        }

        // TODO(cyrusn): remove arity.
        public IEnumerable<PackageSearchResult> Search(string name, int arity, CancellationToken cancellationToken)
        {
            var database = this.MemberDatabase;
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
    }

}
