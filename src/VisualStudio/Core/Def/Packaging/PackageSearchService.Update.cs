// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using static System.FormattableString;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// A service which enables searching for packages matching certain criteria.
    /// It works against an <see cref="Microsoft.CodeAnalysis.Elfie"/> database to find results.
    /// 
    /// This implementation also spawns a task which will attempt to keep that database up to
    /// date by downloading patches on a daily basis.
    /// </summary>
    internal partial class PackageSearchService
    {
        // Internal for testing purposes.
        internal const string ContentAttributeName = "content";
        internal const string ChecksumAttributeName = "checksum";
        internal const string UpToDateAttributeName = "upToDate";
        internal const string TooOldAttributeName = "tooOld";
        internal const string NugetOrgSource = "nuget.org";

        private const string HostId = "RoslynNuGetSearch";
        private const string MicrosoftAssemblyReferencesName = "MicrosoftAssemblyReferences";
        private static readonly LinkedList<string> s_log = new LinkedList<string>();

        private readonly int _dataFormatVersion = AddReferenceDatabase.TextFileFormatVersion;

        /// <summary>
        /// Cancellation support for the task we use to keep the local database up to date.
        /// When VS shuts down it will dispose us.  We'll cancel the task at that point.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private readonly ConcurrentDictionary<string, object> _sourceToUpdateSentinel =
            new ConcurrentDictionary<string, object>();

        private readonly DirectoryInfo _cacheDirectoryInfo;
        //private readonly FileInfo _databaseFileInfo;

        // Interfaces that abstract out the external functionality we need.  Used so we can easily
        // mock behavior during tests.
        private readonly IPackageInstallerService _installerService;
        private readonly IPackageSearchDelayService _delayService;
        private readonly IPackageSearchIOService _ioService;
        private readonly IPackageSearchLogService _logService;
        private readonly IPackageSearchRemoteControlService _remoteControlService;
        private readonly IPackageSearchPatchService _patchService;
        private readonly IPackageSearchDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, bool> _reportAndSwallowException;

        public void Dispose()
        {
            // Cancel any existing work.
            _cancellationTokenSource.Cancel();
        }

        private void LogInfo(string text) => _logService.LogInfo(text);

        private void LogException(Exception e, string text) => _logService.LogException(e, text);

        private void OnPackageSourcesChanged(object sender, EventArgs e)
        {
            // Kick off a database update.  Wait a few seconds before starting so we don't
            // interfere too much with solution loading.
            var sources = _installerService.PackageSources;
            foreach (var source in sources)
            {
                Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
                    UpdateSourceInBackgroundAsync(source.Name), TaskScheduler.Default);
            }
        }

        // internal for testing purposes.
        internal Task UpdateSourceInBackgroundAsync(string source)
        {
            // Only the first thread to try to update this source should succeed
            // and cause us to actually being the update loop. 
            var ourSentinel = new object();
            var currentSentinel = _sourceToUpdateSentinel.GetOrAdd(source, ourSentinel);

            if (ourSentinel != currentSentinel)
            {
                // We already have an update loop for this source.  Nothing for us to do.
                return SpecializedTasks.EmptyTask;
            }

            // We were the first ones to try to update this source.  Spawn off a task to do
            // the updating.
            return new Updater(this, source).UpdateInBackgroundAsync();
        }

        private class Updater
        {
            private readonly PackageSearchService _service;
            private readonly string _source;
            private readonly FileInfo _databaseFileInfo;

            public Updater(PackageSearchService service, string source)
            {
                _service = service;
                _source = source;

                var fileName = ConvertToFileName(source);
                _databaseFileInfo = new FileInfo(
                    Path.Combine(_service._cacheDirectoryInfo.FullName, fileName + ".txt"));
            }

            /// <summary>
            /// Internal for testing purposes.
            /// </summary>
            internal async Task UpdateInBackgroundAsync()
            {
                // We only support this single source currently.
                if (_source != NugetOrgSource)
                {
                    return;
                }

                // Keep on looping until we're told to shut down.
                while (!_service._cancellationToken.IsCancellationRequested)
                {
                    _service.LogInfo("Starting update");
                    try
                    {
                        var delayUntilNextUpdate = await UpdateDatabaseInBackgroundWorkerAsync().ConfigureAwait(false);

                        _service.LogInfo($"Waiting {delayUntilNextUpdate} until next update");
                        await Task.Delay(delayUntilNextUpdate, _service._cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _service.LogInfo("Update canceled. Ending update loop");
                        return;
                    }
                }
            }

            private string ConvertToFileName(string source)
            {
                // Replace all occurrences of a single underscore with a double underscore.
                // Now we know any single underscores in the text come from escapaing some
                // character.
                source = source.Replace("_", "__");

                var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
                var builder = new StringBuilder();

                // Excape any character not allowed in a path.  Escaping is simple, we just
                // use a single underscore followed by the ascii numeric value of the character,
                // followed by another undersscoe.
                // i.e. ":" is 58 is ascii, and becomes _58_ in the final name.
                foreach (var c in source)
                {
                    if (invalidChars.Contains(c))
                    {
                        builder.Append("_" + (int)c + "_");
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }

                return builder.ToString();
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
                    //
                    // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                    if (_service._ioService.Exists(_databaseFileInfo))
                    {
                        _service.LogInfo("Local database file exists. Patching local database");
                        return await PatchLocalDatabaseAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        _service.LogInfo("Local database file does not exist. Downloading full database");
                        return await DownloadFullDatabaseAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Just allow our caller to handle this (they will use this to stop their loop).
                    throw;
                }
                catch (Exception e) when (_service._reportAndSwallowException(e))
                {
                    // Something bad happened (IO Exception, network exception etc.).
                    // ask our caller to try updating again a minute from now.
                    //
                    // Note: we skip OperationCanceledException because it's not 'bad'.
                    // It's the standard way to indicate that we've been asked to shut
                    // down.
                    var delay = _service._delayService.ExpectedFailureDelay;
                    _service.LogException(e, $"Error occurred updating. Retrying update in {delay}");
                    return delay;
                }
            }

            private void CleanCacheDirectory()
            {
                _service.LogInfo("Cleaning cache directory");

                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                if (!_service._ioService.Exists(_service._cacheDirectoryInfo))
                {
                    _service.LogInfo("Creating cache directory");

                    // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                    _service._ioService.Create(_service._cacheDirectoryInfo);
                    _service.LogInfo("Cache directory created");
                }

                _service._cancellationToken.ThrowIfCancellationRequested();
            }

            private async Task<TimeSpan> DownloadFullDatabaseAsync()
            {
                var serverPath = Invariant($"Elfie_V{_service._dataFormatVersion}/Latest.xml");

                _service.LogInfo($"Downloading and processing full database: {serverPath}");

                var element = await DownloadFileAsync(serverPath).ConfigureAwait(false);
                var delayUntilNextUpdate = await ProcessFullDatabaseXElementAsync(element).ConfigureAwait(false);

                _service.LogInfo("Downloading and processing full database completed");
                return delayUntilNextUpdate;
            }

            private async Task<TimeSpan> ProcessFullDatabaseXElementAsync(XElement element)
            {
                _service.LogInfo("Processing full database element");

                // Convert the database contents in the xml to a byte[].
                byte[] bytes;
                if (!TryParseDatabaseElement(element, out bytes))
                {
                    // Something was wrong with the full database.  Trying again soon after won't
                    // really help.  We'll just get the same busted XML from the remote service
                    // cache.  So we want to actually wait some long enough amount of time so that
                    // we can retrieve good data the next time around.

                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    _service.LogInfo($"Unable to parse full database element. Update again in {failureDelay}");
                    return failureDelay;
                }

                // Make a database out of that and set it to our in memory database that we'll be 
                // searching.
                try
                {
                    CreateAndSetInMemoryDatabase(bytes);
                }
                catch (Exception e) when (_service._reportAndSwallowException(e))
                {
                    // We retrieved bytes from the server, but we couldn't make a DB
                    // out of it.  That's very bad.  Just trying again one minute later
                    // isn't going to help.  We need to wait until there is good data
                    // on the server for us to download.
                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    _service.LogInfo($"Unable to create database from full database element. Update again in {failureDelay}");
                    return failureDelay;
                }

                // Write the file out to disk so we'll have it the next time we launch VS.  Do this
                // after we set the in-memory instance so we at least have something to search while
                // we're waiting to write.
                await WriteDatabaseFile(bytes).ConfigureAwait(false);

                var delay = _service._delayService.UpdateSucceededDelay;
                _service.LogInfo($"Processing full database element completed. Update again in {delay}");
                return delay;
            }

            private async Task WriteDatabaseFile(byte[] bytes)
            {
                _service.LogInfo("Writing database file");

                await RepeatIOAsync(
                    () =>
                    {
                        var guidString = Guid.NewGuid().ToString();
                        var tempFilePath = Path.Combine(_service._cacheDirectoryInfo.FullName, guidString + ".tmp");

                        _service.LogInfo($"Temp file path: {tempFilePath}");

                        try
                        {
                            // First, write to a temporary file next to the actual database file.
                            // Note that we explicitly use FileStream so that we can call .Flush to ensure the
                            // file has been completely written to disk (at least as well as the OS can guarantee
                            // things).

                            _service.LogInfo("Writing temp file");

                            // (intentionally not wrapped in IOUtilities.  If this throws we want to retry writing).
                            _service._ioService.WriteAndFlushAllBytes(tempFilePath, bytes);
                            _service.LogInfo("Writing temp file completed");

                            // If we have an existing db file, try to replace it file with the temp file.
                            // Otherwise, just move the temp file into place.
                            if (_service._ioService.Exists(_databaseFileInfo))
                            {
                                _service.LogInfo("Replacing database file");
                                _service._ioService.Replace(tempFilePath, _databaseFileInfo.FullName, destinationBackupFileName: null, ignoreMetadataErrors: true);
                                _service.LogInfo("Replace database file completed");
                            }
                            else
                            {
                                _service.LogInfo("Moving database file");
                                _service._ioService.Move(tempFilePath, _databaseFileInfo.FullName);
                                _service.LogInfo("Moving database file completed");
                            }
                        }
                        finally
                        {
                            // Try to delete the tmp file if it is still around.
                            // If this fails, that's unfortunately, but just proceed.
                            IOUtilities.PerformIO(() => _service._ioService.Delete(new FileInfo(tempFilePath)));
                        }
                    }).ConfigureAwait(false);

                _service.LogInfo("Writing database file completed");
            }

            private async Task<TimeSpan> PatchLocalDatabaseAsync()
            {
                _service.LogInfo("Patching local database");

                _service.LogInfo("Reading in local database");
                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                var databaseBytes = _service._ioService.ReadAllBytes(_databaseFileInfo.FullName);
                _service.LogInfo($"Reading in local database completed. databaseBytes.Length={databaseBytes.Length}");

                // Make a database instance out of those bytes and set is as the current in memory database
                // that searches will run against.  If we can't make a database instance from these bytes
                // then our local database is corrupt and we need to download the full database to get back
                // into a good state.
                AddReferenceDatabase database;
                try
                {
                    database = CreateAndSetInMemoryDatabase(databaseBytes);
                }
                catch (Exception e) when (_service._reportAndSwallowException(e))
                {
                    _service.LogException(e, "Error creating database from local copy. Downloading full database");
                    return await DownloadFullDatabaseAsync().ConfigureAwait(false);
                }

                var databaseVersion = database.DatabaseVersion;

                // Now attempt to download and apply patch file.
                var serverPath = Invariant($"Elfie_V{_service._dataFormatVersion}/{database.DatabaseVersion}_Patch.xml");

                _service.LogInfo("Downloading and processing patch file: " + serverPath);

                var element = await DownloadFileAsync(serverPath).ConfigureAwait(false);
                var delayUntilUpdate = await ProcessPatchXElementAsync(element, databaseBytes).ConfigureAwait(false);

                _service.LogInfo("Downloading and processing patch file completed");
                _service.LogInfo("Patching local database completed");

                return delayUntilUpdate;
            }

            /// <summary>
            /// Creates a database instance with the bytes passed in.  If creating the database succeeds,
            /// then it will be set as the current in memory version.  In the case of failure (which 
            /// indicates that our data is corrupt), the exception will bubble up and must be appropriately
            /// dealt with by the caller.
            /// </summary>
            private AddReferenceDatabase CreateAndSetInMemoryDatabase(byte[] bytes)
            {
                var database = CreateDatabaseFromBytes(bytes);
                _service._sourceToDatabase[_source] = database;
                return database;
            }

            private async Task<TimeSpan> ProcessPatchXElementAsync(XElement patchElement, byte[] databaseBytes)
            {
                try
                {
                    _service.LogInfo("Processing patch element");
                    var delayUntilUpdate = await TryProcessPatchXElementAsync(patchElement, databaseBytes).ConfigureAwait(false);
                    if (delayUntilUpdate != null)
                    {
                        _service.LogInfo($"Processing patch element completed. Update again in {delayUntilUpdate.Value}");
                        return delayUntilUpdate.Value;
                    }

                    // Fall through and download full database.
                }
                catch (Exception e) when (_service._reportAndSwallowException(e))
                {
                    _service.LogException(e, "Error occurred while processing patch element. Downloading full database");
                    // Fall through and download full database.
                }

                return await DownloadFullDatabaseAsync().ConfigureAwait(false);
            }

            private async Task<TimeSpan?> TryProcessPatchXElementAsync(XElement patchElement, byte[] databaseBytes)
            {
                bool upToDate, tooOld;
                byte[] patchBytes;
                ParsePatchElement(patchElement, out upToDate, out tooOld, out patchBytes);

                if (upToDate)
                {
                    _service.LogInfo("Local version is up to date");
                    return _service._delayService.UpdateSucceededDelay;
                }

                if (tooOld)
                {
                    _service.LogInfo("Local version too old");
                    return null;
                }

                _service.LogInfo($"Got patch. databaseBytes.Length={databaseBytes.Length} patchBytes.Length={patchBytes.Length}.");

                // We have patch data.  Apply it to our current database bytes to produce the new
                // database.
                _service.LogInfo("Applying patch");
                var finalBytes = _service._patchService.ApplyPatch(databaseBytes, patchBytes);
                _service.LogInfo($"Applying patch completed. finalBytes.Length={finalBytes.Length}");

                CreateAndSetInMemoryDatabase(finalBytes);

                await WriteDatabaseFile(finalBytes).ConfigureAwait(false);

                return _service._delayService.UpdateSucceededDelay;
            }

            private void ParsePatchElement(XElement patchElement, out bool upToDate, out bool tooOld, out byte[] patchBytes)
            {
                patchBytes = null;

                var upToDateAttribute = patchElement.Attribute(UpToDateAttributeName);
                upToDate = upToDateAttribute != null && (bool)upToDateAttribute;

                var tooOldAttribute = patchElement.Attribute(TooOldAttributeName);
                tooOld = tooOldAttribute != null && (bool)tooOldAttribute;

                var contentsAttribute = patchElement.Attribute(ContentAttributeName);
                if (contentsAttribute != null)
                {
                    var contents = contentsAttribute.Value;
                    patchBytes = Convert.FromBase64String(contents);
                }

                var hasPatchBytes = patchBytes != null;

                var value = (upToDate ? 1 : 0) +
                            (tooOld ? 1 : 0) +
                            (hasPatchBytes ? 1 : 0);
                if (value != 1)
                {
                    throw new FormatException($"Patch format invalid. {nameof(upToDate)}={upToDate} {nameof(tooOld)}={tooOld} {nameof(hasPatchBytes)}={hasPatchBytes}");
                }
            }

            private AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes)
            {
                _service.LogInfo("Creating database from bytes");
                var result = _service._databaseFactoryService.CreateDatabaseFromBytes(bytes);
                _service.LogInfo("Creating database from bytes completed");
                return result;
            }

            private async Task<XElement> DownloadFileAsync(string serverPath)
            {
                _service.LogInfo("Creating download client: " + serverPath);

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
                using (var client = _service._remoteControlService.CreateClient(HostId, serverPath, pollingMinutes))
                {
                    _service.LogInfo("Creating download client completed");

                    // Poll the client every minute until we get the file.
                    while (true)
                    {
                        _service._cancellationToken.ThrowIfCancellationRequested();

                        var resultOpt = await TryDownloadFileAsync(client).ConfigureAwait(false);
                        if (resultOpt == null)
                        {
                            var delay = _service._delayService.CachePollDelay;
                            _service.LogInfo($"File not downloaded. Trying again in {delay}");
                            await Task.Delay(delay, _service._cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // File was downloaded.  
                            return resultOpt;
                        }
                    }
                }
            }

            /// <summary>Returns 'null' if download is not available and caller should keep polling.</summary>
            private async Task<XElement> TryDownloadFileAsync(IPackageSearchRemoteControlClient client)
            {
                _service.LogInfo("Read file from client");

                // "ReturnsNull": Only return a file if we have it locally *and* it's not older than our polling time (1 day).

                using (var stream = await client.ReadFileAsync(__VsRemoteControlBehaviorOnStale.ReturnsNull).ConfigureAwait(false))
                {
                    if (stream == null)
                    {
                        _service.LogInfo("Read file completed. Client returned no data");
                        return null;
                    }

                    _service.LogInfo("Read file completed. Client returned data");
                    _service.LogInfo("Converting data to XElement");

                    // We're reading in our own XML file, but even so, use conservative settings
                    // just to be on the safe side.  First, disallow DTDs entirely (we will never
                    // have one ourself).  And also, prevent any external resolution of files when
                    // processing the xml.
                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null
                    };
                    using (var reader = XmlReader.Create(stream, settings))
                    {
                        var result = XElement.Load(reader);
                        _service.LogInfo("Converting data to XElement completed");
                        return result;
                    }
                }
            }

            private async Task RepeatIOAsync(Action action)
            {
                const int repeat = 6;
                for (var i = 0; i < repeat; i++)
                {
                    _service._cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        // Normal IO exception. Don't bother reporting it.  We don't want to get
                        // lots of hits just because we couldn't write a file because of something
                        // like an anti-virus tool lockign the file.
                        if (!IOUtilities.IsNormalIOException(e))
                        {
                            _service._reportAndSwallowException(e);
                        }

                        var delay = _service._delayService.FileWriteDelay;
                        _service.LogException(e, $"Operation failed. Trying again after {delay}");
                        await Task.Delay(delay, _service._cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            private bool TryParseDatabaseElement(XElement element, out byte[] bytes)
            {
                _service.LogInfo("Parsing database element");
                var contentsAttribute = element.Attribute(ContentAttributeName);
                if (contentsAttribute == null)
                {
                    _service._reportAndSwallowException(
                        new FormatException($"Database element invalid. Missing '{ContentAttributeName}' attribute"));

                    bytes = null;
                    return false;
                }

                var contentBytes = ConvertContentAttribute(contentsAttribute);

                var checksumAttribute = element.Attribute(ChecksumAttributeName);
                if (checksumAttribute != null)
                {
                    var expectedChecksum = checksumAttribute.Value;
                    string actualChecksum;
                    using (var sha256 = SHA256.Create())
                    {
                        actualChecksum = Convert.ToBase64String(sha256.ComputeHash(contentBytes));
                    }

                    if (expectedChecksum != actualChecksum)
                    {
                        _service._reportAndSwallowException(
                            new FormatException($"Checksum mismatch: expected != actual. {expectedChecksum} != {actualChecksum}"));

                        bytes = null;
                        return false;
                    }
                }

                bytes = contentBytes;
                return true;
            }

            private byte[] ConvertContentAttribute(XAttribute contentsAttribute)
            {
                var text = contentsAttribute.Value;
                var compressedBytes = Convert.FromBase64String(text);

                using (var outStream = new MemoryStream())
                {
                    using (var inStream = new MemoryStream(compressedBytes))
                    using (var deflateStream = new DeflateStream(inStream, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(outStream);
                    }

                    var bytes = outStream.ToArray();

                    _service.LogInfo($"Parsing complete. bytes.length={bytes.Length}");
                    return bytes;
                }
            }
        }
    }
}