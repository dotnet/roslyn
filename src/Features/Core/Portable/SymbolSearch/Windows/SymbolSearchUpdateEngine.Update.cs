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
using Roslyn.Utilities;
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

        /// <summary>
        /// Logged messages kept in memory to help us diagnose what was going on previously if a crash occurs.
        /// </summary>
        private static readonly LinkedList<string> s_logs = new();

        private readonly ConcurrentDictionary<string, object> _sourceToUpdateSentinel = new();

        // Interfaces that abstract out the external functionality we need.  Used so we can easily
        // mock behavior during tests.
        private readonly IDelayService _delayService;
        private readonly IIOService _ioService;
        private readonly IFileDownloaderFactory _fileDownloaderFactory;
        private readonly IPatchService _patchService;
        private readonly IDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, CancellationToken, bool> _reportAndSwallowExceptionUnlessCanceled;

        /// <param name="cancellationToken">
        /// Cancellation support for the task we use to keep the local database up to date.
        /// Currently used only in tests so we can shutdown gracefully.  In normal VS+OOP scenarios
        /// we don't care about this and we just get torn down when the OOP process goes down.
        /// </param>
        public ValueTask UpdateContinuouslyAsync(string source, string localSettingsDirectory, CancellationToken cancellationToken)
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
            return new Updater(this, source, localSettingsDirectory).UpdateInBackgroundAsync(cancellationToken);
        }

        private static void LogInfo(string text)
            => Log(text);

        private static void LogException(Exception exception, string text)
            => Log(text + ". " + exception.ToString());

        private static void Log(string text)
        {
            // Keep a running in memory log as well for debugging purposes.
            s_logs.AddLast(text);
            while (s_logs.Count > 100)
                s_logs.RemoveFirst();
        }

        private sealed class Updater(SymbolSearchUpdateEngine service, string source, string localSettingsDirectory)
        {
            private readonly SymbolSearchUpdateEngine _service = service;
            private readonly string _source = source;
            private readonly DirectoryInfo _cacheDirectoryInfo = new DirectoryInfo(Path.Combine(
                    localSettingsDirectory, "PackageCache", string.Format(Invariant($"Format{AddReferenceDatabaseTextFileFormatVersion}"))));

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
                    LogInfo("Starting update");
                    try
                    {
                        var delayUntilNextUpdate = await UpdateDatabaseInBackgroundWorkerAsync(cancellationToken).ConfigureAwait(false);

                        LogInfo($"Waiting {delayUntilNextUpdate} until next update");
                        await Task.Delay(delayUntilNextUpdate, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        LogInfo("Update canceled. Ending update loop");
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
                    CleanCacheDirectory(cancellationToken);

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
                        LogInfo("Local database file exists. Patching local database");
                        return await PatchLocalDatabaseAsync(databaseFileInfo, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        LogInfo("Local database file does not exist. Downloading full database");
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
                    LogException(e, $"Error occurred updating. Retrying update in {delay}");
                    return delay;
                }
            }

            private void CleanCacheDirectory(CancellationToken cancellationToken)
            {
                LogInfo("Cleaning cache directory");

                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                if (!_service._ioService.Exists(_cacheDirectoryInfo))
                {
                    LogInfo("Creating cache directory");

                    // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                    _service._ioService.Create(_cacheDirectoryInfo);
                    LogInfo("Cache directory created");
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

                LogInfo($"Downloading and processing full database: {serverPath}");

                var element = await DownloadFileAsync(serverPath, cancellationToken).ConfigureAwait(false);
                var result = await ProcessFullDatabaseXElementAsync(databaseFileInfo, element, cancellationToken).ConfigureAwait(false);

                LogInfo("Downloading and processing full database completed");
                return result;
            }

            private async Task<(bool succeeded, TimeSpan delay)> ProcessFullDatabaseXElementAsync(
                FileInfo databaseFileInfo, XElement element, CancellationToken cancellationToken)
            {
                LogInfo("Processing full database element");

                // Convert the database contents in the XML to a byte[].
                var (succeeded, contentBytes) = await TryParseDatabaseElementAsync(element, cancellationToken).ConfigureAwait(false);

                if (!succeeded)
                {
                    // Something was wrong with the full database.  Trying again soon after won't
                    // really help.  We'll just get the same busted XML from the remote service
                    // cache.  So we want to actually wait some long enough amount of time so that
                    // we can retrieve good data the next time around.

                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    LogInfo($"Unable to parse full database element. Update again in {failureDelay}");
                    return (succeeded: false, failureDelay);
                }

                var bytes = contentBytes;

                // Make a database out of that and set it to our in memory database that we'll be 
                // searching.
                try
                {
                    CreateAndSetInMemoryDatabase(bytes);
                }
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    // We retrieved bytes from the server, but we couldn't make a DB
                    // out of it.  That's very bad.  Just trying again one minute later
                    // isn't going to help.  We need to wait until there is good data
                    // on the server for us to download.
                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    LogInfo($"Unable to create database from full database element. Update again in {failureDelay}");
                    return (succeeded: false, failureDelay);
                }

                // Write the file out to disk so we'll have it the next time we launch VS.  Do this
                // after we set the in-memory instance so we at least have something to search while
                // we're waiting to write.
                await WriteDatabaseFileAsync(databaseFileInfo, bytes, cancellationToken).ConfigureAwait(false);

                var delay = _service._delayService.UpdateSucceededDelay;
                LogInfo($"Processing full database element completed. Update again in {delay}");
                return (succeeded: true, delay);
            }

            private async Task WriteDatabaseFileAsync(FileInfo databaseFileInfo, byte[] bytes, CancellationToken cancellationToken)
            {
                LogInfo("Writing database file");

                await RepeatIOAsync(
                    cancellationToken =>
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
                            _service._ioService.WriteAndFlushAllBytes(tempFilePath, bytes);
                            LogInfo("Writing temp file completed");

                            // If we have an existing db file, try to replace it file with the temp file.
                            // Otherwise, just move the temp file into place.
                            if (_service._ioService.Exists(databaseFileInfo))
                            {
                                LogInfo("Replacing database file");
                                _service._ioService.Replace(tempFilePath, databaseFileInfo.FullName, destinationBackupFileName: null, ignoreMetadataErrors: true);
                                LogInfo("Replace database file completed");
                            }
                            else
                            {
                                LogInfo("Moving database file");
                                _service._ioService.Move(tempFilePath, databaseFileInfo.FullName);
                                LogInfo("Moving database file completed");
                            }
                        }
                        finally
                        {
                            // Try to delete the temp file if it is still around.
                            // If this fails, that's unfortunately, but just proceed.
                            IOUtilities.PerformIO(() => _service._ioService.Delete(new FileInfo(tempFilePath)));
                        }
                    }, cancellationToken).ConfigureAwait(false);

                LogInfo("Writing database file completed");
            }

            private async Task<TimeSpan> PatchLocalDatabaseAsync(FileInfo databaseFileInfo, CancellationToken cancellationToken)
            {
                LogInfo("Patching local database");

                LogInfo("Reading in local database");
                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                var databaseBytes = _service._ioService.ReadAllBytes(databaseFileInfo.FullName);
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
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    LogException(e, "Error creating database from local copy. Downloading full database");
                    return await DownloadFullDatabaseAsync(databaseFileInfo, cancellationToken).ConfigureAwait(false);
                }

                // Now attempt to download and apply patch file.
                var serverPath = Invariant($"Elfie_V{AddReferenceDatabaseTextFileFormatVersion}/{database.DatabaseVersion}_Patch.xml");

                LogInfo("Downloading and processing patch file: " + serverPath);

                var element = await DownloadFileAsync(serverPath, cancellationToken).ConfigureAwait(false);
                var delayUntilUpdate = await ProcessPatchXElementAsync(databaseFileInfo, element, databaseBytes, cancellationToken).ConfigureAwait(false);

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
                _service._sourceToDatabase[_source] = new AddReferenceDatabaseWrapper(database);
                return database;
            }

            private async Task<TimeSpan> ProcessPatchXElementAsync(
                FileInfo databaseFileInfo, XElement patchElement, byte[] databaseBytes, CancellationToken cancellationToken)
            {
                try
                {
                    LogInfo("Processing patch element");
                    var delayUntilUpdate = await TryProcessPatchXElementAsync(databaseFileInfo, patchElement, databaseBytes, cancellationToken).ConfigureAwait(false);
                    if (delayUntilUpdate != null)
                    {
                        LogInfo($"Processing patch element completed. Update again in {delayUntilUpdate.Value}");
                        return delayUntilUpdate.Value;
                    }

                    // Fall through and download full database.
                }
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    LogException(e, "Error occurred while processing patch element. Downloading full database");
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
                    LogInfo("Local version is up to date");
                    return _service._delayService.UpdateSucceededDelay;
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
                var finalBytes = _service._patchService.ApplyPatch(databaseBytes, patchBytes);
                LogInfo($"Applying patch completed. finalBytes.Length={finalBytes.Length}");

                CreateAndSetInMemoryDatabase(finalBytes);

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

            private AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes)
            {
                LogInfo("Creating database from bytes");
                var result = _service._databaseFactoryService.CreateDatabaseFromBytes(bytes);
                LogInfo("Creating database from bytes completed");
                return result;
            }

            private async Task<XElement> DownloadFileAsync(string serverPath, CancellationToken cancellationToken)
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
                using var client = _service._fileDownloaderFactory.CreateClient(HostId, serverPath, pollingMinutes);

                LogInfo("Creating download client completed");

                // Poll the client every minute until we get the file.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var (element, delay) = await TryDownloadFileAsync(client, cancellationToken).ConfigureAwait(false);
                    if (element == null)
                    {
                        LogInfo($"File not downloaded. Trying again in {delay}");
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // File was downloaded.  
                        return element;
                    }
                }
            }

            /// <summary>Returns 'null' if download is not available and caller should keep polling.</summary>
            private async Task<(XElement element, TimeSpan delay)> TryDownloadFileAsync(IFileDownloader fileDownloader, CancellationToken cancellationToken)
            {
                LogInfo("Read file from client");

                using var stream = await fileDownloader.ReadFileAsync().ConfigureAwait(false);

                if (stream == null)
                {
                    LogInfo("Read file completed. Client returned no data");
                    return (element: null, _service._delayService.CachePollDelay);
                }

                LogInfo("Read file completed. Client returned data");
                LogInfo("Converting data to XElement");

                // We're reading in our own XML file, but even so, use conservative settings
                // just to be on the safe side.  First, disallow DTDs entirely (we will never
                // have one ourself).  And also, prevent any external resolution of files when
                // processing the XML.
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                // This code must always succeed.  If it does not, that means that either the server reported bogus data
                // to the file-downloader, or the file-downloader is serving us bogus data.  In other event, there is
                // something wrong with those components, and we should both report the issue to Watson, and stop doing
                // the update.
                try
                {
                    using var reader = XmlReader.Create(stream, settings);

                    var element = XElement.Load(reader);
                    LogInfo("Converting data to XElement completed");
                    return (element, delay: default);
                }
                catch (Exception e) when (_service._reportAndSwallowExceptionUnlessCanceled(e, cancellationToken))
                {
                    // We retrieved bytes from the server, but we couldn't make parse it an xml. out of it.  That's very
                    // bad.  Just trying again one minute later isn't going to help.  We need to wait until there is
                    // good data on the server for us to download.
                    LogInfo($"Unable to parse file as XElement");
                    return (element: null, _service._delayService.CatastrophicFailureDelay);
                }
            }

            private async Task RepeatIOAsync(Action<CancellationToken> action, CancellationToken cancellationToken)
            {
                const int repeat = 6;
                for (var i = 0; i < repeat; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        action(cancellationToken);
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
                        LogException(e, $"Operation failed. Trying again after {delay}");
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            private async Task<(bool succeeded, byte[] contentBytes)> TryParseDatabaseElementAsync(XElement element, CancellationToken cancellationToken)
            {
                LogInfo("Parsing database element");
                var contentsAttribute = element.Attribute(ContentAttributeName);
                if (contentsAttribute == null)
                {
                    _service._reportAndSwallowExceptionUnlessCanceled(new FormatException($"Database element invalid. Missing '{ContentAttributeName}' attribute"), CancellationToken.None);

                    return (succeeded: false, null);
                }

                var contentBytes = await Updater.ConvertContentAttributeAsync(contentsAttribute, cancellationToken).ConfigureAwait(false);

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

            private static async Task<byte[]> ConvertContentAttributeAsync(XAttribute contentsAttribute, CancellationToken cancellationToken)
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

                LogInfo($"Parsing complete. bytes.length={bytes.Length}");
                return bytes;
            }
        }
    }
}
