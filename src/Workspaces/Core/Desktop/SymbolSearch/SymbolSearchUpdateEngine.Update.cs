﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.RemoteControl;
using Roslyn.Utilities;
using static System.FormattableString;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// A service which enables searching for packages matching certain criteria.
    /// It works against an <see cref="Microsoft.CodeAnalysis.Elfie"/> database to find results.
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
        internal const string NugetOrgSource = "nuget.org";

        public const string HostId = "RoslynNuGetSearch";
        private const string MicrosoftAssemblyReferencesName = "MicrosoftAssemblyReferences";

        /// <summary>
        /// Cancellation support for the task we use to keep the local database up to date.
        /// When VS shuts down it will dispose us.  We'll cancel the task at that point.
        /// </summary>
        private readonly CancellationToken _updateCancellationToken;

        private readonly ConcurrentDictionary<string, object> _sourceToUpdateSentinel =
            new ConcurrentDictionary<string, object>();

        // Interfaces that abstract out the external functionality we need.  Used so we can easily
        // mock behavior during tests.
        private readonly IDelayService _delayService;
        private readonly IIOService _ioService;
        private readonly ISymbolSearchLogService _logService;
        private readonly IRemoteControlService _remoteControlService;
        private readonly IPatchService _patchService;
        private readonly IDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, bool> _reportAndSwallowException;

        private Task LogInfoAsync(string text) => _logService.LogInfoAsync(text);

        private Task LogExceptionAsync(Exception e, string text) => _logService.LogExceptionAsync(e.ToString(), text);

        public Task UpdateContinuouslyAsync(string source, string localSettingsDirectory)
        {
            // Only the first thread to try to update this source should succeed
            // and cause us to actually begin the update loop. 
            var ourSentinel = new object();
            var currentSentinel = _sourceToUpdateSentinel.GetOrAdd(source, ourSentinel);

            if (ourSentinel != currentSentinel)
            {
                // We already have an update loop for this source.  Nothing for us to do.
                return SpecializedTasks.EmptyTask;
            }

            // We were the first ones to try to update this source.  Spawn off a task to do
            // the updating.
            return new Updater(this, source, localSettingsDirectory).UpdateInBackgroundAsync();
        }

        private class Updater
        {
            private readonly SymbolSearchUpdateEngine _service;
            private readonly string _source;
            private readonly DirectoryInfo _cacheDirectoryInfo;
            private readonly FileInfo _databaseFileInfo;

            public Updater(SymbolSearchUpdateEngine service, string source, string localSettingsDirectory)
            {
                _service = service;
                _source = source;

                _cacheDirectoryInfo = new DirectoryInfo(Path.Combine(
                    localSettingsDirectory, "PackageCache", string.Format(Invariant($"Format{AddReferenceDatabase.TextFileFormatVersion}"))));

                _databaseFileInfo = new FileInfo(
                    Path.Combine(_cacheDirectoryInfo.FullName, ConvertToFileName(source) + ".txt"));
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
                while (!_service._updateCancellationToken.IsCancellationRequested)
                {
                    await _service.LogInfoAsync("Starting update").ConfigureAwait(false);
                    try
                    {
                        var delayUntilNextUpdate = await UpdateDatabaseInBackgroundWorkerAsync().ConfigureAwait(false);

                        await _service.LogInfoAsync($"Waiting {delayUntilNextUpdate} until next update").ConfigureAwait(false);
                        await Task.Delay(delayUntilNextUpdate, _service._updateCancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await _service.LogInfoAsync("Update canceled. Ending update loop").ConfigureAwait(false);
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
                    await CleanCacheDirectoryAsync().ConfigureAwait(false);

                    // If we have a local database, then see if it needs to be patched.
                    // Otherwise download the full database.
                    //
                    // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                    if (_service._ioService.Exists(_databaseFileInfo))
                    {
                        await _service.LogInfoAsync("Local database file exists. Patching local database").ConfigureAwait(false);
                        return await PatchLocalDatabaseAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await _service.LogInfoAsync("Local database file does not exist. Downloading full database").ConfigureAwait(false);
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
                    await _service.LogExceptionAsync(e, $"Error occurred updating. Retrying update in {delay}").ConfigureAwait(false);
                    return delay;
                }
            }

            private async Task CleanCacheDirectoryAsync()
            {
                await _service.LogInfoAsync("Cleaning cache directory").ConfigureAwait(false);

                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                if (!_service._ioService.Exists(_cacheDirectoryInfo))
                {
                    await _service.LogInfoAsync("Creating cache directory").ConfigureAwait(false);

                    // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                    _service._ioService.Create(_cacheDirectoryInfo);
                    await _service.LogInfoAsync("Cache directory created").ConfigureAwait(false);
                }

                _service._updateCancellationToken.ThrowIfCancellationRequested();
            }

            private async Task<TimeSpan> DownloadFullDatabaseAsync()
            {
                var serverPath = Invariant($"Elfie_V{AddReferenceDatabase.TextFileFormatVersion}/Latest.xml");

                await _service.LogInfoAsync($"Downloading and processing full database: {serverPath}").ConfigureAwait(false);

                var element = await DownloadFileAsync(serverPath).ConfigureAwait(false);
                var delayUntilNextUpdate = await ProcessFullDatabaseXElementAsync(element).ConfigureAwait(false);

                await _service.LogInfoAsync("Downloading and processing full database completed").ConfigureAwait(false);
                return delayUntilNextUpdate;
            }

            private async Task<TimeSpan> ProcessFullDatabaseXElementAsync(XElement element)
            {
                await _service.LogInfoAsync("Processing full database element").ConfigureAwait(false);

                // Convert the database contents in the xml to a byte[].
                var result = await TryParseDatabaseElementAsync(element).ConfigureAwait(false);

                if (!result.Item1)
                {
                    // Something was wrong with the full database.  Trying again soon after won't
                    // really help.  We'll just get the same busted XML from the remote service
                    // cache.  So we want to actually wait some long enough amount of time so that
                    // we can retrieve good data the next time around.

                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    await _service.LogInfoAsync($"Unable to parse full database element. Update again in {failureDelay}").ConfigureAwait(false);
                    return failureDelay;
                }

                var bytes = result.Item2;

                // Make a database out of that and set it to our in memory database that we'll be 
                // searching.
                try
                {
                    await CreateAndSetInMemoryDatabaseAsync(bytes).ConfigureAwait(false);
                }
                catch (Exception e) when (_service._reportAndSwallowException(e))
                {
                    // We retrieved bytes from the server, but we couldn't make a DB
                    // out of it.  That's very bad.  Just trying again one minute later
                    // isn't going to help.  We need to wait until there is good data
                    // on the server for us to download.
                    var failureDelay = _service._delayService.CatastrophicFailureDelay;
                    await _service.LogInfoAsync($"Unable to create database from full database element. Update again in {failureDelay}").ConfigureAwait(false);
                    return failureDelay;
                }

                // Write the file out to disk so we'll have it the next time we launch VS.  Do this
                // after we set the in-memory instance so we at least have something to search while
                // we're waiting to write.
                await WriteDatabaseFile(bytes).ConfigureAwait(false);

                var delay = _service._delayService.UpdateSucceededDelay;
                await _service.LogInfoAsync($"Processing full database element completed. Update again in {delay}").ConfigureAwait(false);
                return delay;
            }

            private async Task WriteDatabaseFile(byte[] bytes)
            {
                await _service.LogInfoAsync("Writing database file").ConfigureAwait(false);

                await RepeatIOAsync(
                    async () =>
                    {
                        var guidString = Guid.NewGuid().ToString();
                        var tempFilePath = Path.Combine(_cacheDirectoryInfo.FullName, guidString + ".tmp");

                        await _service.LogInfoAsync($"Temp file path: {tempFilePath}").ConfigureAwait(false);

                        try
                        {
                            // First, write to a temporary file next to the actual database file.
                            // Note that we explicitly use FileStream so that we can call .Flush to ensure the
                            // file has been completely written to disk (at least as well as the OS can guarantee
                            // things).

                            await _service.LogInfoAsync("Writing temp file").ConfigureAwait(false);

                            // (intentionally not wrapped in IOUtilities.  If this throws we want to retry writing).
                            _service._ioService.WriteAndFlushAllBytes(tempFilePath, bytes);
                            await _service.LogInfoAsync("Writing temp file completed").ConfigureAwait(false);

                            // If we have an existing db file, try to replace it file with the temp file.
                            // Otherwise, just move the temp file into place.
                            if (_service._ioService.Exists(_databaseFileInfo))
                            {
                                await _service.LogInfoAsync("Replacing database file").ConfigureAwait(false);
                                _service._ioService.Replace(tempFilePath, _databaseFileInfo.FullName, destinationBackupFileName: null, ignoreMetadataErrors: true);
                                await _service.LogInfoAsync("Replace database file completed").ConfigureAwait(false);
                            }
                            else
                            {
                                await _service.LogInfoAsync("Moving database file").ConfigureAwait(false);
                                _service._ioService.Move(tempFilePath, _databaseFileInfo.FullName);
                                await _service.LogInfoAsync("Moving database file completed").ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            // Try to delete the tmp file if it is still around.
                            // If this fails, that's unfortunately, but just proceed.
                            IOUtilities.PerformIO(() => _service._ioService.Delete(new FileInfo(tempFilePath)));
                        }
                    }).ConfigureAwait(false);

                await _service.LogInfoAsync("Writing database file completed").ConfigureAwait(false);
            }

            private async Task<TimeSpan> PatchLocalDatabaseAsync()
            {
                await _service.LogInfoAsync("Patching local database").ConfigureAwait(false);

                await _service.LogInfoAsync("Reading in local database").ConfigureAwait(false);
                // (intentionally not wrapped in IOUtilities.  If this throws we want to restart).
                var databaseBytes = _service._ioService.ReadAllBytes(_databaseFileInfo.FullName);
                await _service.LogInfoAsync($"Reading in local database completed. databaseBytes.Length={databaseBytes.Length}").ConfigureAwait(false);

                // Make a database instance out of those bytes and set is as the current in memory database
                // that searches will run against.  If we can't make a database instance from these bytes
                // then our local database is corrupt and we need to download the full database to get back
                // into a good state.
                AddReferenceDatabase database;
                try
                {
                    database = await CreateAndSetInMemoryDatabaseAsync(databaseBytes).ConfigureAwait(false);
                }
                catch (Exception e) when (_service._reportAndSwallowException(e))
                {
                    await _service.LogExceptionAsync(e, "Error creating database from local copy. Downloading full database").ConfigureAwait(false);
                    return await DownloadFullDatabaseAsync().ConfigureAwait(false);
                }

                var databaseVersion = database.DatabaseVersion;

                // Now attempt to download and apply patch file.
                var serverPath = Invariant($"Elfie_V{AddReferenceDatabase.TextFileFormatVersion}/{database.DatabaseVersion}_Patch.xml");

                await _service.LogInfoAsync("Downloading and processing patch file: " + serverPath).ConfigureAwait(false);

                var element = await DownloadFileAsync(serverPath).ConfigureAwait(false);
                var delayUntilUpdate = await ProcessPatchXElementAsync(element, databaseBytes).ConfigureAwait(false);

                await _service.LogInfoAsync("Downloading and processing patch file completed").ConfigureAwait(false);
                await _service.LogInfoAsync("Patching local database completed").ConfigureAwait(false);

                return delayUntilUpdate;
            }

            /// <summary>
            /// Creates a database instance with the bytes passed in.  If creating the database succeeds,
            /// then it will be set as the current in memory version.  In the case of failure (which 
            /// indicates that our data is corrupt), the exception will bubble up and must be appropriately
            /// dealt with by the caller.
            /// </summary>
            private async Task<AddReferenceDatabase> CreateAndSetInMemoryDatabaseAsync(byte[] bytes)
            {
                var database = await CreateDatabaseFromBytesAsync(bytes).ConfigureAwait(false);
                _service._sourceToDatabase[_source] = new AddReferenceDatabaseWrapper(database);
                return database;
            }

            private async Task<TimeSpan> ProcessPatchXElementAsync(XElement patchElement, byte[] databaseBytes)
            {
                try
                {
                    await _service.LogInfoAsync("Processing patch element").ConfigureAwait(false);
                    var delayUntilUpdate = await TryProcessPatchXElementAsync(patchElement, databaseBytes).ConfigureAwait(false);
                    if (delayUntilUpdate != null)
                    {
                        await _service.LogInfoAsync($"Processing patch element completed. Update again in {delayUntilUpdate.Value}").ConfigureAwait(false);
                        return delayUntilUpdate.Value;
                    }

                    // Fall through and download full database.
                }
                catch (Exception e) when (_service._reportAndSwallowException(e))
                {
                    await _service.LogExceptionAsync(e, "Error occurred while processing patch element. Downloading full database").ConfigureAwait(false);
                    // Fall through and download full database.
                }

                return await DownloadFullDatabaseAsync().ConfigureAwait(false);
            }

            private async Task<TimeSpan?> TryProcessPatchXElementAsync(XElement patchElement, byte[] databaseBytes)
            {
                ParsePatchElement(patchElement, out var upToDate, out var tooOld, out var patchBytes);

                if (upToDate)
                {
                    await _service.LogInfoAsync("Local version is up to date").ConfigureAwait(false);
                    return _service._delayService.UpdateSucceededDelay;
                }

                if (tooOld)
                {
                    await _service.LogInfoAsync("Local version too old").ConfigureAwait(false);
                    return null;
                }

                await _service.LogInfoAsync($"Got patch. databaseBytes.Length={databaseBytes.Length} patchBytes.Length={patchBytes.Length}.").ConfigureAwait(false);

                // We have patch data.  Apply it to our current database bytes to produce the new
                // database.
                await _service.LogInfoAsync("Applying patch").ConfigureAwait(false);
                var finalBytes = _service._patchService.ApplyPatch(databaseBytes, patchBytes);
                await _service.LogInfoAsync($"Applying patch completed. finalBytes.Length={finalBytes.Length}").ConfigureAwait(false);

                await CreateAndSetInMemoryDatabaseAsync(finalBytes).ConfigureAwait(false);

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

            private async Task<AddReferenceDatabase> CreateDatabaseFromBytesAsync(byte[] bytes)
            {
                await _service.LogInfoAsync("Creating database from bytes").ConfigureAwait(false);
                var result = _service._databaseFactoryService.CreateDatabaseFromBytes(bytes);
                await _service.LogInfoAsync("Creating database from bytes completed").ConfigureAwait(false);
                return result;
            }

            private async Task<XElement> DownloadFileAsync(string serverPath)
            {
                await _service.LogInfoAsync("Creating download client: " + serverPath).ConfigureAwait(false);

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
                    await _service.LogInfoAsync("Creating download client completed").ConfigureAwait(false);

                    // Poll the client every minute until we get the file.
                    while (true)
                    {
                        _service._updateCancellationToken.ThrowIfCancellationRequested();

                        var resultOpt = await TryDownloadFileAsync(client).ConfigureAwait(false);
                        if (resultOpt == null)
                        {
                            var delay = _service._delayService.CachePollDelay;
                            await _service.LogInfoAsync($"File not downloaded. Trying again in {delay}").ConfigureAwait(false);
                            await Task.Delay(delay, _service._updateCancellationToken).ConfigureAwait(false);
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
            private async Task<XElement> TryDownloadFileAsync(IRemoteControlClient client)
            {
                await _service.LogInfoAsync("Read file from client").ConfigureAwait(false);

                // "ReturnsNull": Only return a file if we have it locally *and* it's not older than our polling time (1 day).

                using (var stream = await client.ReadFileAsync(BehaviorOnStale.ReturnNull).ConfigureAwait(false))
                {
                    if (stream == null)
                    {
                        await _service.LogInfoAsync("Read file completed. Client returned no data").ConfigureAwait(false);
                        return null;
                    }

                    await _service.LogInfoAsync("Read file completed. Client returned data").ConfigureAwait(false);
                    await _service.LogInfoAsync("Converting data to XElement").ConfigureAwait(false);

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
                        await _service.LogInfoAsync("Converting data to XElement completed").ConfigureAwait(false);
                        return result;
                    }
                }
            }

            private async Task RepeatIOAsync(Func<Task> action)
            {
                const int repeat = 6;
                for (var i = 0; i < repeat; i++)
                {
                    _service._updateCancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await action().ConfigureAwait(false);
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
                        await _service.LogExceptionAsync(e, $"Operation failed. Trying again after {delay}").ConfigureAwait(false);
                        await Task.Delay(delay, _service._updateCancellationToken).ConfigureAwait(false);
                    }
                }
            }

            private async Task<ValueTuple<bool, byte[]>> TryParseDatabaseElementAsync(XElement element)
            {
                await _service.LogInfoAsync("Parsing database element").ConfigureAwait(false);
                var contentsAttribute = element.Attribute(ContentAttributeName);
                if (contentsAttribute == null)
                {
                    _service._reportAndSwallowException(
                        new FormatException($"Database element invalid. Missing '{ContentAttributeName}' attribute"));

                    return ValueTuple.Create(false, (byte[])null);
                }

                var contentBytes = await ConvertContentAttributeAsync(contentsAttribute).ConfigureAwait(false);

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
                        _service._reportAndSwallowException(
                            new FormatException($"Checksum mismatch: expected != actual. {expectedChecksum} != {actualChecksum}"));

                        return ValueTuple.Create(false, (byte[])null);
                    }
                }

                return ValueTuple.Create(true, contentBytes);
            }

            private async Task<byte[]> ConvertContentAttributeAsync(XAttribute contentsAttribute)
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

                    await _service.LogInfoAsync($"Parsing complete. bytes.length={bytes.Length}").ConfigureAwait(false);
                    return bytes;
                }
            }
        }
    }
}