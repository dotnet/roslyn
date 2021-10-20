// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.RemoteControl;
using static System.FormattableString;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// A service which enables searching for packages matching certain criteria.
    /// It works against a <see cref="Microsoft.CodeAnalysis.Elfie"/> database to find results.
    /// 
    /// This implementation also spawns a task which will attempt to keep that database up to
    /// date by downloading patches on a daily basis.
    /// </summary>
    internal partial class SymbolSearchUpdateEngine
    {
        // Internal for testing purposes.
        internal const string ContentAttributeName = "content";
        internal const string ChecksumAttributeName = "checksum";
        internal const string UpToDateAttributeName = "upToDate";
        internal const string TooOldAttributeName = "tooOld";

        public const string HostId = "RoslynNuGetSearch";
        private const string MicrosoftAssemblyReferencesName = "MicrosoftAssemblyReferences";

        private const int AddReferenceDatabaseTextFileFormatVersion = 1;

        private readonly ConcurrentDictionary<string, object> _sourceToUpdateSentinel =
            new();

        // Interfaces that abstract out the external functionality we need.  Used so we can easily
        // mock behavior during tests.
        private readonly IDelayService _delayService;
        private readonly IIOService _ioService;
        private readonly IRemoteControlService _remoteControlService;
        private readonly IPatchService _patchService;
        private readonly IDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, CancellationToken, bool> _reportAndSwallowExceptionUnlessCanceled;

        /// <param name="cancellationToken">
        /// Cancellation support for the task we use to keep the local database up to date.
        /// Currently used only in tests so we can shutdown gracefully.  In normal VS+OOP scenarios
        /// we don't care about this and we just get torn down when the OOP process goes down.
        /// </param>
        public ValueTask UpdateContinuouslyAsync(string source, string localSettingsDirectory, ISymbolSearchLogService logService, CancellationToken cancellationToken)
        {
            // Only the first thread to try to update this source should succeed
            // and cause us to actually begin the update loop. 
            var ourSentinel = new object();
            var currentSentinel = _sourceToUpdateSentinel.GetOrAdd(source, ourSentinel);

            if (ourSentinel != currentSentinel)
            {
                // We already have an update loop for this source.  Nothing for us to do.
                return default;
            }

            // We were the first ones to try to update this source.  Spawn off a task to do
            // the updating.
            return new Updater(this, logService, source, localSettingsDirectory).UpdateInBackgroundAsync(cancellationToken);
        }

        private sealed class Updater
        {
            private readonly SymbolSearchUpdateEngine _service;
            private readonly string _source;
            private readonly DirectoryInfo _cacheDirectoryInfo;
            private readonly ISymbolSearchLogService _logService;

            public Updater(SymbolSearchUpdateEngine service, ISymbolSearchLogService logService, string source, string localSettingsDirectory)
            {
                _service = service;
                _source = source;
                _logService = logService;

                _cacheDirectoryInfo = new DirectoryInfo(Path.Combine(
                    localSettingsDirectory, "PackageCache", string.Format(Invariant($"Format{AddReferenceDatabaseTextFileFormatVersion}"))));
            }

            private ValueTask LogInfoAsync(string text, CancellationToken cancellationToken)
                => _logService.LogInfoAsync(text, cancellationToken);

            private ValueTask LogExceptionAsync(Exception e, string text, CancellationToken cancellationToken)
                => _logService.LogExceptionAsync(e.ToString(), text, cancellationToken);

            /// <summary>
            /// Internal for testing purposes.
            /// </summary>
            internal async ValueTask UpdateInBackgroundAsync(CancellationToken cancellationToken)
            {
                // We only support this single source currently.
                if (_source != PackageSourceHelper.NugetOrgSourceName)
                {
                    return;
                }

                // Keep on looping until we're told to shut down.
                while (!cancellationToken.IsCancellationRequested)
                {
                    await LogInfoAsync("Starting update", cancellationToken).ConfigureAwait(false);
                    try
                    {
                        var delayUntilNextUpdate = await UpdateDatabaseInBackgroundWorkerAsync(cancellationToken).ConfigureAwait(false);

                        await LogInfoAsync($"Waiting {delayUntilNextUpdate} until next update", cancellationToken).ConfigureAwait(false);
                        await Task.Delay(delayUntilNextUpdate, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await LogInfoAsync("Update canceled. Ending update loop", cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }
            }

            private static string ConvertToFileName(string source)
            {
                // Replace all occurrences of a single underscore with a double underscore.
                // Now we know any single underscores in the text come from escaping some
                // character.
                source = source.Replace("_", "__");

                var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
                var builder = new StringBuilder();

                // Escape any character not allowed in a path.  Escaping is simple, we just
                // use a single underscore followed by the ASCII numeric value of the character,
                // followed by another underscore.
                // i.e. ":" is 58 is ASCII, and becomes _58_ in the final name.
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
            private async Task<TimeSpan> UpdateDatabaseInBackgroundWorkerAsync(CancellationToken cancellationToken)
            {
                // Attempt to update the local db if we have one, or download a full db
                // if we don't.  In the event of any error back off a minute and try 
                // again.  Lot of errors are possible here as IO/network/other-libraries
                // are involved.  For example, we might get errors trying to write to 
                // disk.
                try
                {
                    await CleanCacheDirectoryAsync(cancellationToken).ConfigureAwait(false);

                    // If we have a local database, then see if it needs to be patched. Otherwise download the full
                    // database.
                    //
                    // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                    //
                    // Ensure we get a fresh FileInfo for the db.  We need to make sure that the data we're querying
                    // (like .Exists) is up to date and isn't the a cached value from a prior run.
                    var databaseFileInfo = new FileInfo(
                        Path.Combine(_cacheDirectoryInfo.FullName, ConvertToFileName(_source) + ".txt"));

                    if (_service._ioService.Exists(databaseFileInfo))
                    {
                        await LogInfoAsync("Local database file exists. Patching local database", cancellationToken).ConfigureAwait(false);
                        return await PatchLocalDatabaseAsync(databaseFileInfo, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await LogInfoAsync("Local database file does not exist. Downloading full database", cancellationToken).ConfigureAwait(false);
                        return await DownloadFullDatabaseAsync(databaseFileInfo, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    // Something bad happened (IO Exception, network exception etc.).
                    // ask our caller to try updating again a minute from now.
                    //
                    // Note: we skip OperationCanceledException because it's not 'bad'.
                    // It's the standard way to indicate that we've been asked to shut
                    // down.
                    var delay = _service._delayService.ExpectedFailureDelay;
                    await LogExceptionAsync(e, $"Error occurred updating. Retrying update in {delay}", cancellationToken).ConfigureAwait(false);
                    return delay;
                }
            }

            private async Task CleanCacheDirectoryAsync(CancellationToken cancellationToken)
            {
                await LogInfoAsync("Cleaning cache directory", cancellationToken).ConfigureAwait(false);

                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                if (!_service._ioService.Exists(_cacheDirectoryInfo))
                {
                    await LogInfoAsync("Creating cache directory", cancellationToken).ConfigureAwait(false);

                    // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                    _service._ioService.Create(_cacheDirectoryInfo);
                    await LogInfoAsync("Cache directory created", cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            private async Task<TimeSpan> DownloadFullDatabaseAsync(FileInfo databaseFileInfo, CancellationToken cancellationToken)
            {
                var (_, delay) = await DownloadFullDatabaseWorkerAsync(databaseFileInfo, cancellationToken).ConfigureAwait(false);
                return delay;
            }

            private async Task<(bool succeeded, TimeSpan delay)> DownloadFullDatabaseWorkerAsync(FileInfo databaseFileInfo, CancellationToken cancellationToken)
            {
                var serverPath = Invariant($"Elfie_V{AddReferenceDatabaseTextFileFormatVersion}/Latest.xml");

                await LogInfoAsync($"Downloading and processing full database: {serverPath}", cancellationToken).ConfigureAwait(false);

                var element = await DownloadFileAsync(serverPath, cancellationToken).ConfigureAwait(false);
                var result = await ProcessFullDatabaseXElementAsync(databaseFileInfo, element, cancellationToken).ConfigureAwait(false);

                await LogInfoAsync("Downloading and processing full database completed", cancellationToken).ConfigureAwait(false);
                return result;
            }

            private async Task<(bool succeeded, TimeSpan delay)> ProcessFullDatabaseXElementAsync(
                FileInfo databaseFileInfo, XElement element, CancellationToken cancellationToken)
            {
                await LogInfoAsync("Processing full database element", cancellationToken).ConfigureAwait(false);

                // Convert the database contents in the XML to a byte[].
                var (succeeded, contentBytes) = await TryParseDatabaseElementAsync(element, cancellationToken).ConfigureAwait(false);

                if (!succeeded)
                {
                    // Something was wrong with the full database.  Trying again soon after won't
                    // really help.  We'll just get the same busted XML from the remote service
                    // cache.  So we want to actually wait some long enough amount of time so that
                    // we can retrieve good data the next time around.

                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    await LogInfoAsync($"Unable to parse full database element. Update again in {failureDelay}", cancellationToken).ConfigureAwait(false);
                    return (succeeded: false, failureDelay);
                }

                var bytes = contentBytes;

                // Make a database out of that and set it to our in memory database that we'll be 
                // searching.
                try
                {
                    await CreateAndSetInMemoryDatabaseAsync(bytes, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    // We retrieved bytes from the server, but we couldn't make a DB
                    // out of it.  That's very bad.  Just trying again one minute later
                    // isn't going to help.  We need to wait until there is good data
                    // on the server for us to download.
                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    await LogInfoAsync($"Unable to create database from full database element. Update again in {failureDelay}", cancellationToken).ConfigureAwait(false);
                    return (succeeded: false, failureDelay);
                }

                // Write the file out to disk so we'll have it the next time we launch VS.  Do this
                // after we set the in-memory instance so we at least have something to search while
                // we're waiting to write.
                await WriteDatabaseFileAsync(databaseFileInfo, bytes, cancellationToken).ConfigureAwait(false);

                var delay = _service._delayService.UpdateSucceededDelay;
                await LogInfoAsync($"Processing full database element completed. Update again in {delay}", cancellationToken).ConfigureAwait(false);
                return (succeeded: true, delay);
            }

            private async Task WriteDatabaseFileAsync(FileInfo databaseFileInfo, byte[] bytes, CancellationToken cancellationToken)
            {
                await LogInfoAsync("Writing database file", cancellationToken).ConfigureAwait(false);

                await RepeatIOAsync(
                    async cancellationToken =>
                    {
                        var guidString = Guid.NewGuid().ToString();
                        var tempFilePath = Path.Combine(_cacheDirectoryInfo.FullName, guidString + ".tmp");

                        await LogInfoAsync($"Temp file path: {tempFilePath}", cancellationToken).ConfigureAwait(false);

                        try
                        {
                            // First, write to a temporary file next to the actual database file.
                            // Note that we explicitly use FileStream so that we can call .Flush to ensure the
                            // file has been completely written to disk (at least as well as the OS can guarantee
                            // things).

                            await LogInfoAsync("Writing temp file", cancellationToken).ConfigureAwait(false);

                            // (intentionally not wrapped in IOUtilities.  If this throws we want to retry writing).
                            _service._ioService.WriteAndFlushAllBytes(tempFilePath, bytes);
                            await LogInfoAsync("Writing temp file completed", cancellationToken).ConfigureAwait(false);

                            // If we have an existing db file, try to replace it file with the temp file.
                            // Otherwise, just move the temp file into place.
                            if (_service._ioService.Exists(databaseFileInfo))
                            {
                                await LogInfoAsync("Replacing database file", cancellationToken).ConfigureAwait(false);
                                _service._ioService.Replace(tempFilePath, databaseFileInfo.FullName, destinationBackupFileName: null, ignoreMetadataErrors: true);
                                await LogInfoAsync("Replace database file completed", cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await LogInfoAsync("Moving database file", cancellationToken).ConfigureAwait(false);
                                _service._ioService.Move(tempFilePath, databaseFileInfo.FullName);
                                await LogInfoAsync("Moving database file completed", cancellationToken).ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            // Try to delete the temp file if it is still around.
                            // If this fails, that's unfortunately, but just proceed.
                            IOUtilities.PerformIO(() => _service._ioService.Delete(new FileInfo(tempFilePath)));
                        }
                    }, cancellationToken).ConfigureAwait(false);

                await LogInfoAsync("Writing database file completed", cancellationToken).ConfigureAwait(false);
            }

            private async Task<TimeSpan> PatchLocalDatabaseAsync(FileInfo databaseFileInfo, CancellationToken cancellationToken)
            {
                await LogInfoAsync("Patching local database", cancellationToken).ConfigureAwait(false);

                await LogInfoAsync("Reading in local database", cancellationToken).ConfigureAwait(false);
                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                var databaseBytes = _service._ioService.ReadAllBytes(databaseFileInfo.FullName);
                await LogInfoAsync($"Reading in local database completed. databaseBytes.Length={databaseBytes.Length}", cancellationToken).ConfigureAwait(false);

                // Make a database instance out of those bytes and set is as the current in memory database
                // that searches will run against.  If we can't make a database instance from these bytes
                // then our local database is corrupt and we need to download the full database to get back
                // into a good state.
                AddReferenceDatabase database;
                try
                {
                    database = await CreateAndSetInMemoryDatabaseAsync(databaseBytes, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    await LogExceptionAsync(e, "Error creating database from local copy. Downloading full database", cancellationToken).ConfigureAwait(false);
                    return await DownloadFullDatabaseAsync(databaseFileInfo, cancellationToken).ConfigureAwait(false);
                }

                // Now attempt to download and apply patch file.
                var serverPath = Invariant($"Elfie_V{AddReferenceDatabaseTextFileFormatVersion}/{database.DatabaseVersion}_Patch.xml");

                await LogInfoAsync("Downloading and processing patch file: " + serverPath, cancellationToken).ConfigureAwait(false);

                var element = await DownloadFileAsync(serverPath, cancellationToken).ConfigureAwait(false);
                var delayUntilUpdate = await ProcessPatchXElementAsync(databaseFileInfo, element, databaseBytes, cancellationToken).ConfigureAwait(false);

                await LogInfoAsync("Downloading and processing patch file completed", cancellationToken).ConfigureAwait(false);
                await LogInfoAsync("Patching local database completed", cancellationToken).ConfigureAwait(false);

                return delayUntilUpdate;
            }

            /// <summary>
            /// Creates a database instance with the bytes passed in.  If creating the database succeeds,
            /// then it will be set as the current in memory version.  In the case of failure (which 
            /// indicates that our data is corrupt), the exception will bubble up and must be appropriately
            /// dealt with by the caller.
            /// </summary>
            private async Task<AddReferenceDatabase> CreateAndSetInMemoryDatabaseAsync(byte[] bytes, CancellationToken cancellationToken)
            {
                var database = await CreateDatabaseFromBytesAsync(bytes, cancellationToken).ConfigureAwait(false);
                _service._sourceToDatabase[_source] = new AddReferenceDatabaseWrapper(database);
                return database;
            }

            private async Task<TimeSpan> ProcessPatchXElementAsync(
                FileInfo databaseFileInfo, XElement patchElement, byte[] databaseBytes, CancellationToken cancellationToken)
            {
                try
                {
                    await LogInfoAsync("Processing patch element", cancellationToken).ConfigureAwait(false);
                    var delayUntilUpdate = await TryProcessPatchXElementAsync(databaseFileInfo, patchElement, databaseBytes, cancellationToken).ConfigureAwait(false);
                    if (delayUntilUpdate != null)
                    {
                        await LogInfoAsync($"Processing patch element completed. Update again in {delayUntilUpdate.Value}", cancellationToken).ConfigureAwait(false);
                        return delayUntilUpdate.Value;
                    }

                    // Fall through and download full database.
                }
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    await LogExceptionAsync(e, "Error occurred while processing patch element. Downloading full database", cancellationToken).ConfigureAwait(false);
                    // Fall through and download full database.
                }

                return await DownloadFullDatabaseAsync(databaseFileInfo, cancellationToken).ConfigureAwait(false);
            }

            private async Task<TimeSpan?> TryProcessPatchXElementAsync(
                FileInfo databaseFileInfo, XElement patchElement, byte[] databaseBytes, CancellationToken cancellationToken)
            {
                ParsePatchElement(patchElement, out var upToDate, out var tooOld, out var patchBytes);

                if (upToDate)
                {
                    await LogInfoAsync("Local version is up to date", cancellationToken).ConfigureAwait(false);
                    return _service._delayService.UpdateSucceededDelay;
                }

                if (tooOld)
                {
                    await LogInfoAsync("Local version too old", cancellationToken).ConfigureAwait(false);
                    return null;
                }

                await LogInfoAsync($"Got patch. databaseBytes.Length={databaseBytes.Length} patchBytes.Length={patchBytes.Length}.", cancellationToken).ConfigureAwait(false);

                // We have patch data.  Apply it to our current database bytes to produce the new
                // database.
                await LogInfoAsync("Applying patch", cancellationToken).ConfigureAwait(false);
                var finalBytes = _service._patchService.ApplyPatch(databaseBytes, patchBytes);
                await LogInfoAsync($"Applying patch completed. finalBytes.Length={finalBytes.Length}", cancellationToken).ConfigureAwait(false);

                await CreateAndSetInMemoryDatabaseAsync(finalBytes, cancellationToken).ConfigureAwait(false);

                await WriteDatabaseFileAsync(databaseFileInfo, finalBytes, cancellationToken).ConfigureAwait(false);

                return _service._delayService.UpdateSucceededDelay;
            }

            private static void ParsePatchElement(XElement patchElement, out bool upToDate, out bool tooOld, out byte[] patchBytes)
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

            private async Task<AddReferenceDatabase> CreateDatabaseFromBytesAsync(byte[] bytes, CancellationToken cancellationToken)
            {
                await LogInfoAsync("Creating database from bytes", cancellationToken).ConfigureAwait(false);
                var result = _service._databaseFactoryService.CreateDatabaseFromBytes(bytes);
                await LogInfoAsync("Creating database from bytes completed", cancellationToken).ConfigureAwait(false);
                return result;
            }

            private async Task<XElement> DownloadFileAsync(string serverPath, CancellationToken cancellationToken)
            {
                await LogInfoAsync("Creating download client: " + serverPath, cancellationToken).ConfigureAwait(false);

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
                using var client = _service._remoteControlService.CreateClient(HostId, serverPath, pollingMinutes);

                await LogInfoAsync("Creating download client completed", cancellationToken).ConfigureAwait(false);

                // Poll the client every minute until we get the file.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var resultOpt = await TryDownloadFileAsync(client, cancellationToken).ConfigureAwait(false);
                    if (resultOpt == null)
                    {
                        var delay = _service._delayService.CachePollDelay;
                        await LogInfoAsync($"File not downloaded. Trying again in {delay}", cancellationToken).ConfigureAwait(false);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // File was downloaded.  
                        return resultOpt;
                    }
                }
            }

            /// <summary>Returns 'null' if download is not available and caller should keep polling.</summary>
            private async Task<XElement> TryDownloadFileAsync(IRemoteControlClient client, CancellationToken cancellationToken)
            {
                await LogInfoAsync("Read file from client", cancellationToken).ConfigureAwait(false);

                // "ReturnsNull": Only return a file if we have it locally *and* it's not older than our polling time (1 day).

                using var stream = await client.ReadFileAsync(BehaviorOnStale.ReturnNull).ConfigureAwait(false);

                if (stream == null)
                {
                    await LogInfoAsync("Read file completed. Client returned no data", cancellationToken).ConfigureAwait(false);
                    return null;
                }

                await LogInfoAsync("Read file completed. Client returned data", cancellationToken).ConfigureAwait(false);
                await LogInfoAsync("Converting data to XElement", cancellationToken).ConfigureAwait(false);

                // We're reading in our own XML file, but even so, use conservative settings
                // just to be on the safe side.  First, disallow DTDs entirely (we will never
                // have one ourself).  And also, prevent any external resolution of files when
                // processing the XML.
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                using var reader = XmlReader.Create(stream, settings);

                var result = XElement.Load(reader);
                await LogInfoAsync("Converting data to XElement completed", cancellationToken).ConfigureAwait(false);
                return result;
            }

            private async Task RepeatIOAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
            {
                const int repeat = 6;
                for (var i = 0; i < repeat; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await action(cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception e) when (IOUtilities.IsNormalIOException(e) || _service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                    {
                        // The exception filter above might be a little funny looking. We always
                        // want to enter this catch block, but if we ran into a normal IO exception
                        // we shouldn't bother reporting it. We don't want to get lots of hits just
                        // because something like an anti-virus tool locked the file and we
                        // couldn't write to it. The call to IsNormalIOException will shortcut
                        // around the reporting in this case.

                        var delay = _service._delayService.FileWriteDelay;
                        await LogExceptionAsync(e, $"Operation failed. Trying again after {delay}", cancellationToken).ConfigureAwait(false);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            private async Task<(bool succeeded, byte[] contentBytes)> TryParseDatabaseElementAsync(XElement element, CancellationToken cancellationToken)
            {
                await LogInfoAsync("Parsing database element", cancellationToken).ConfigureAwait(false);
                var contentsAttribute = element.Attribute(ContentAttributeName);
                if (contentsAttribute == null)
                {
                    _service._reportAndSwallowExceptionUnlessCanceled(new FormatException($"Database element invalid. Missing '{ContentAttributeName}' attribute"), CancellationToken.None);

                    return (succeeded: false, null);
                }

                var contentBytes = await ConvertContentAttributeAsync(contentsAttribute, cancellationToken).ConfigureAwait(false);

                var checksumAttribute = element.Attribute(ChecksumAttributeName);
                if (checksumAttribute != null)
                {
                    var expectedChecksum = checksumAttribute.Value;
                    string actualChecksum;
                    using (var sha256 = SHA256.Create())
                    {
                        actualChecksum = Convert.ToBase64String(sha256.ComputeHash(contentBytes));
                    }

                    if (!StringComparer.Ordinal.Equals(expectedChecksum, actualChecksum))
                    {
                        _service._reportAndSwallowExceptionUnlessCanceled(new FormatException($"Checksum mismatch: expected != actual. {expectedChecksum} != {actualChecksum}"), CancellationToken.None);

                        return (succeeded: false, null);
                    }
                }

                return (succeeded: true, contentBytes);
            }

            private async Task<byte[]> ConvertContentAttributeAsync(XAttribute contentsAttribute, CancellationToken cancellationToken)
            {
                var text = contentsAttribute.Value;
                var compressedBytes = Convert.FromBase64String(text);

                using var outStream = new MemoryStream();

                using (var inStream = new MemoryStream(compressedBytes))
                using (var deflateStream = new DeflateStream(inStream, CompressionMode.Decompress))
                {
#if NETCOREAPP
                    await deflateStream.CopyToAsync(outStream, cancellationToken).ConfigureAwait(false);
#else
                    await deflateStream.CopyToAsync(outStream).ConfigureAwait(false);
#endif
                }

                var bytes = outStream.ToArray();

                await LogInfoAsync($"Parsing complete. bytes.length={bytes.Length}", cancellationToken).ConfigureAwait(false);
                return bytes;
            }
        }
    }
}
