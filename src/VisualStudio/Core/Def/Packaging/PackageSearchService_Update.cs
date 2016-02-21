// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using static System.FormattableString;
using System.Xml;

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

        private readonly DirectoryInfo _cacheDirectoryInfo;
        private readonly FileInfo _databaseFileInfo;

        // Interfaces that abstract out the external functionality we need.  Used so we can easily
        // mock behavior during tests.
        private readonly IPackageInstallerService _installerService;
        private readonly IPackageSearchDelayService _delayService;
        private readonly IPackageSearchIOService _ioService;
        private readonly IPackageSearchLogService _logService;
        private readonly IPackageSearchRemoteControlService _remoteControlService;
        private readonly IPackageSearchPatchService _patchService;
        private readonly IPackageSearchDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, bool> _swallowException;

        public void Dispose()
        {
            // Cancel any existing work.
            _cancellationTokenSource.Cancel();
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
                //
                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
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
            catch (OperationCanceledException)
            {
                // Just allow our caller to handle this (they will use this to stop their loop).
                throw;
            }
            catch (Exception e) when (_swallowException(e))
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

            // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
            if (!_ioService.Exists(_cacheDirectoryInfo))
            {
                LogInfo("Creating cache directory");

                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                _ioService.Create(_cacheDirectoryInfo);
                LogInfo("Cache directory created");
            }

            _cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task<TimeSpan> DownloadFullDatabaseAsync()
        {
            var serverPath = Invariant($"Elfie_V{_dataFormatVersion}/Latest.xml");

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

            await RepeatIOAsync(
                () =>
                {
                    var guidString = Guid.NewGuid().ToString();
                    var tempFilePath = Path.Combine(_cacheDirectoryInfo.FullName, guidString + ".tmp");

                    LogInfo($"Temp file path: {tempFilePath}");

                    try
                    {

                        // First, write to a temporary file next to the actual database file.
                        // Note that we explicitly use FileStream so that we can call .Flush to ensure the
                        // file has been completely written to disk (at least as well as the OS can guarantee
                        // things).

                        LogInfo("Writing temp file");

                        // (intentionally not wrapped in IOUtilities.  If this throws we want to retry writing).
                        _ioService.WriteAndFlushAllBytes(tempFilePath, bytes);
                        LogInfo("Writing temp file completed");

                        // If we have an existing db file, try to replace it file with the temp file.
                        // Otherwise, just move the temp file into place.
                        if (_ioService.Exists(_databaseFileInfo))
                        {
                            LogInfo("Replacing database file");
                            _ioService.Replace(tempFilePath, _databaseFileInfo.FullName, destinationBackupFileName: null, ignoreMetadataErrors: true);
                            LogInfo("Replace database file completed");
                        }
                        else
                        {
                            LogInfo("Moving database file");
                            _ioService.Move(tempFilePath, _databaseFileInfo.FullName);
                            LogInfo("Moving database file completed");
                        }
                    }
                    finally
                    {
                        // Try to delete the tmp file if it is still around.
                        // If this fails, that's unfortunately, but just proceed.
                        IOUtilities.PerformIO(() => _ioService.Delete(new FileInfo(tempFilePath)));
                    }
                }).ConfigureAwait(false);

            LogInfo("Writing database file completed");
        }

        private async Task<TimeSpan> PatchLocalDatabaseAsync()
        {
            LogInfo("Patching local database");

            LogInfo("Reading in local database");
            // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
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
            catch (Exception e) when (_swallowException(e))
            {
                LogException(e, "Error creating database from local copy. Downloading full database");
                return await DownloadFullDatabaseAsync().ConfigureAwait(false);
            }

            var databaseVersion = database.DatabaseVersion;

            // Now attempt to download and apply patch file.
            var serverPath = Invariant($"Elfie_V{_dataFormatVersion}/{database.DatabaseVersion}_Patch.xml");

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
        /// indicates that our data is corrupt), the exception will bubble up and must be appropriately
        /// dealt with by the caller.
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
            catch (Exception e) when (_swallowException(e))
            {
                LogException(e, "Error occurred while processing patch element. Downloading full database");
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
                LogInfo("Local version is up to date");
                return _delayService.UpdateSucceededDelay;
            }

            if (tooOld)
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
                        var delay = _delayService.CachePollDelay;
                        LogInfo($"File not downloaded. Trying again in {delay}");
                        await Task.Delay(delay, _cancellationToken).ConfigureAwait(false);
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
                    LogInfo("Converting data to XElement completed");
                    return result;
                }
            }
        }

        private async Task RepeatIOAsync(Action action)
        {
            const int repeat = 6;
            for (var i = 0; i < repeat; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    action();
                    return;
                }
                catch (Exception e) when (_swallowException(e))
                {
                    var delay = _delayService.FileWriteDelay;
                    LogException(e, $"Operation failed. Trying again after {delay}");
                    await Task.Delay(delay, _cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private byte[] ParseDatabaseElement(XElement element)
        {
            LogInfo("Parsing database element");
            var contentsAttribute = element.Attribute(ContentAttributeName);
            if (contentsAttribute == null)
            {
                throw new FormatException($"Database element invalid. Missing '{ContentAttributeName}' attribute");
            }

            var text = contentsAttribute.Value;
            var compressedBytes = Convert.FromBase64String(text);

            using (var inStream = new MemoryStream(compressedBytes))
            using (var outStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(inStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(outStream);
                }

                var bytes = outStream.ToArray();

                LogInfo($"Parsing complete. bytes.length={bytes.Length}");
                return bytes;
            }
        }
    }
}