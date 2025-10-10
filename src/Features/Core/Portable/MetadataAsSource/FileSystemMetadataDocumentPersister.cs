// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal sealed class FileSystemMetadataDocumentPersister : IMetadataDocumentPersister
{
    /// <summary>
    /// We create a mutex so other processes can see if our directory is still alive.  As long as we own the mutex, no
    /// other VS instance will try to delete our _rootTemporaryPathWithGuid folder.
    /// </summary>
#pragma warning disable IDE0052 // Remove unread private members
    private readonly Mutex _mutex;
#pragma warning restore IDE0052 // Remove unread private members
    private readonly string _rootTemporaryPathWithGuid;
    private readonly string _rootTemporaryPath = Path.Combine(Path.GetTempPath(), MetadataAsSourceFileService.MetadataAsSource);

    public FileSystemMetadataDocumentPersister()
    {
        var guidString = Guid.NewGuid().ToString("N");
        _rootTemporaryPathWithGuid = Path.Combine(_rootTemporaryPath, guidString);
        _mutex = new Mutex(initiallyOwned: true, name: CreateMutexName(guidString));
    }

    private static string CreateMutexName(string directoryName)
        => $"{MetadataAsSourceFileService.MetadataAsSource}-{directoryName}";

    public bool TryGetExistingText(string documentPath, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, Func<SourceText, bool> verifyExistingDocument, [NotNullWhen(true)] out SourceText? sourceText)
    {
        sourceText = null;
        if (!File.Exists(documentPath))
            return false;

        using var stream = new FileStream(documentPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

        sourceText = SourceText.From(stream, encoding, checksumAlgorithm, throwIfBinaryDetected: true);

        if (verifyExistingDocument is not null && !verifyExistingDocument(sourceText))
            return false;

        return true;
    }

    public async Task<bool> WriteMetadataDocumentAsync(string documentPath, Encoding encoding, SourceText text, Action<Exception>? logFailure, CancellationToken cancellationToken)
    {
        try
        {
            var directoryName = Path.GetDirectoryName(documentPath)!;
            if (!await EnsureDirectoryExistsAsync(directoryName, cancellationToken).ConfigureAwait(false))
                throw new IOException($"Unable to create directory: {directoryName}");

            using (var textWriter = new StreamWriter(documentPath, append: false, encoding))
            {
                text.Write(textWriter, cancellationToken);
            }

            // Mark read-only
            new FileInfo(documentPath).IsReadOnly = true;
            return true;
        }
        catch (Exception ex) when (IOUtilities.IsNormalIOException(ex))
        {
            // If we hit an IO exception, we assume the file could not be written.
            logFailure?.Invoke(ex);
            return false;
        }
    }

    public string GenerateDocumentPath(Guid identifier, string providerName, string fileName)
    {
        return Path.Combine(_rootTemporaryPathWithGuid, providerName, identifier.ToString(), fileName);
    }

    public string ConvertFilePathToDocumentPath(Guid identifier, string providerName, string filePath)
    {
        // We already have a file path, and this persister operates on files.  No need to do anything to it.
        return filePath;
    }

    public void CleanupGeneratedDocuments()
    {
        try
        {
            if (Directory.Exists(_rootTemporaryPath))
            {
                // Let's look through directories to delete.
                foreach (var directoryInfo in new DirectoryInfo(_rootTemporaryPath).EnumerateDirectories())
                {
                    // Is there a mutex for this one?  If so, that means it's a folder open in another VS instance.
                    // We should leave it alone.  If not, then it's a folder from a previous VS run.  Delete that
                    // now.
                    if (Mutex.TryOpenExisting(CreateMutexName(directoryInfo.Name), out var acquiredMutex))
                    {
                        acquiredMutex.Dispose();
                    }
                    else
                    {
                        TryDeleteFolderWhichContainsReadOnlyFiles(directoryInfo.FullName);
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        static void TryDeleteFolderWhichContainsReadOnlyFiles(string directoryPath)
        {
            try
            {
                foreach (var fileInfo in new DirectoryInfo(directoryPath).EnumerateFiles("*", SearchOption.AllDirectories))
                    IOUtilities.PerformIO(() => fileInfo.IsReadOnly = false);

                IOUtilities.PerformIO(() => Directory.Delete(directoryPath, recursive: true));
            }
            catch (Exception)
            {
            }
        }
    }

    private static async Task<bool> EnsureDirectoryExistsAsync(string directoryName, CancellationToken cancellationToken)
    {
        // Create the directory. It's possible a parallel deletion is happening in another process, so we may have
        // to retry this a few times.
        //
        // If we still can't create the folder after 5 seconds, assume we will not be able to create it and
        // continue without actually writing the text to disk.

        var stopwatch = SharedStopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(5);
        var firstAttempt = true;
        var failedToWriteDirectory = false;

        while (!IOUtilities.PerformIO(() => Directory.Exists(directoryName)))
        {
            if (stopwatch.Elapsed > timeout)
            {
                // If we still can't create the folder after 5 seconds, assume we will not be able to create it.
                failedToWriteDirectory = true;
                break;
            }

            if (firstAttempt)
            {
                firstAttempt = false;
            }
            else
            {
                await Task.Delay(DelayTimeSpan.Short, cancellationToken).ConfigureAwait(false);
            }

            IOUtilities.PerformIO(() => Directory.CreateDirectory(directoryName));
        }

        return !failedToWriteDirectory;
    }
}
