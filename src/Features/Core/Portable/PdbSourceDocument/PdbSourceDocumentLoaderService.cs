// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

[Export(typeof(IPdbSourceDocumentLoaderService)), Shared]
[method: ImportingConstructor]
[SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code")]
internal sealed class PdbSourceDocumentLoaderService(
    [Import(AllowDefault = true)] Lazy<ISourceLinkService>? sourceLinkService,
    [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger) : IPdbSourceDocumentLoaderService
{
    private const int SourceLinkTimeout = 1000;
    private const int ExtendedSourceLinkTimeout = 4000;

    /// <summary>
    /// Lazy import ISourceLinkService because it can cause debugger 
    /// binaries to be eagerly loaded even if they are never used.
    /// </summary>
    private readonly Lazy<ISourceLinkService>? _sourceLinkService = sourceLinkService;
    private readonly IPdbSourceDocumentLogger? _logger = logger;

    public async Task<SourceFileInfo?> LoadSourceDocumentAsync(SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry, bool useExtendedTimeout, ProjectId projectId, IMetadataDocumentPersister persister, CancellationToken cancellationToken)
    {
        // First we try getting "local" files, either from embedded source or a local file on disk
        // and if they don't work we call the debugger to download a file from SourceLink info
        return (await TryGetEmbeddedSourceFileAsync(projectId, sourceDocument, encoding, telemetry, persister, cancellationToken).ConfigureAwait(false)) ??
            TryGetOriginalFile(sourceDocument, encoding, telemetry, projectId, persister) ??
            await TryGetSourceLinkFileAsync(sourceDocument, encoding, telemetry, useExtendedTimeout, projectId, persister, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SourceFileInfo?> TryGetEmbeddedSourceFileAsync(ProjectId projectId, SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry, IMetadataDocumentPersister persister, CancellationToken cancellationToken)
    {
        if (sourceDocument.EmbeddedTextBytes is null)
            return null;

        var fileName = Path.GetFileName(sourceDocument.FilePath);
        var documentPath = persister.GenerateDocumentPath(projectId.Id, PdbSourceDocumentMetadataAsSourceFileProvider.ProviderName, fileName);

        // We might have already navigated to this file before, so it might exist, but
        // we still need to re-validate the checksum and make sure its not the wrong file
        var existingSourceText = await persister
            .TryGetExistingTextAsync(documentPath, encoding, sourceDocument.ChecksumAlgorithm, (sourceText) => VerifySourceText(sourceText, sourceDocument), cancellationToken)
            .ConfigureAwait(false);

        if (existingSourceText is not null)
        {
            telemetry.SetSourceFileSource("embedded");
            _logger?.Log(FeaturesResources._0_found_in_embedded_PDB_cached_source_file, sourceDocument.FilePath);
            return new SourceFileInfo(documentPath, FeaturesResources.embedded, existingSourceText, sourceDocument.ChecksumAlgorithm, FromRemoteLocation: false);
        }

        var embeddedTextBytes = sourceDocument.EmbeddedTextBytes;
        var uncompressedSize = BitConverter.ToInt32(embeddedTextBytes, 0);
        var stream = new MemoryStream(embeddedTextBytes, sizeof(int), embeddedTextBytes.Length - sizeof(int));

        if (uncompressedSize != 0)
        {
            var decompressed = new MemoryStream(uncompressedSize);

            using (var deflater = new DeflateStream(stream, CompressionMode.Decompress))
            {
                deflater.CopyTo(decompressed);
            }

            if (decompressed.Length != uncompressedSize)
            {
                return null;
            }

            stream = decompressed;
        }

        if (stream is not null)
        {
            // Even though Roslyn supports loading SourceTexts from a stream, Visual Studio requires
            // a file to exist on disk so we have to write embedded source to a temp file.
            SourceText sourceText;
            using (stream)
            {
                sourceText = SourceText.From(stream, encoding, sourceDocument.ChecksumAlgorithm, throwIfBinaryDetected: true);

                var didWrite = await persister.WriteMetadataDocumentAsync(documentPath, encoding, sourceText,
                    logFailure: (ex) => _logger?.Log(FeaturesResources._0_found_in_embedded_PDB_but_could_not_write_file_1, sourceDocument.FilePath, ex.Message),
                    cancellationToken).ConfigureAwait(false);
                if (!didWrite)
                {
                    return null;
                }
            }

            var checksumMatches = VerifySourceText(sourceText, sourceDocument);
            if (checksumMatches)
            {
                telemetry.SetSourceFileSource("embedded");
                _logger?.Log(FeaturesResources._0_found_in_embedded_PDB, sourceDocument.FilePath);
                return new SourceFileInfo(documentPath, FeaturesResources.embedded, sourceText, sourceDocument.ChecksumAlgorithm, FromRemoteLocation: false);
            }
            else
            {
                _logger?.Log(FeaturesResources._0_found_in_embedded_PDB_but_checksum_failed, sourceDocument.FilePath);
                return null;
            }
        }

        return null;
    }

    private async Task<SourceFileInfo?> TryGetSourceLinkFileAsync(SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry, bool useExtendedTimeout, ProjectId projectId, IMetadataDocumentPersister persister, CancellationToken cancellationToken)
    {
        if (sourceDocument.SourceLinkUrl is null || _sourceLinkService is null)
            return null;

        var timeout = useExtendedTimeout ? ExtendedSourceLinkTimeout : SourceLinkTimeout;

        // This should ideally be the repo-relative path to the file, and come from SourceLink: https://github.com/dotnet/sourcelink/pull/699
        var relativePath = Path.GetFileName(sourceDocument.FilePath);

        var delay = Task.Delay(timeout, cancellationToken);
        var sourceFileTask = _sourceLinkService.Value.GetSourceFilePathAsync(sourceDocument.SourceLinkUrl, relativePath, cancellationToken);

        var winner = await Task.WhenAny(sourceFileTask, delay).ConfigureAwait(false);

        if (winner == sourceFileTask)
        {
            var sourceFile = await sourceFileTask.ConfigureAwait(false);
            if (sourceFile is not null)
            {
                // TODO: Don't ignore the checksum here: https://github.com/dotnet/roslyn/issues/55834
                // The source link file is always persisted to disk by the debugger service, so we always need to load it from there.
                var sourceText = LoadSourceFileFromDisk(sourceFile.SourceFilePath, sourceDocument, encoding, ignoreChecksum: true);
                if (sourceText is not null)
                {
                    // Get the persister's view of the file path. If using virtual documents, this will be different from the original file path.
                    var documentPath = persister.ConvertFilePathToDocumentPath(projectId.Id, PdbSourceDocumentMetadataAsSourceFileProvider.ProviderName, sourceFile.SourceFilePath);
                    telemetry.SetSourceFileSource("sourcelink");
                    _logger?.Log(FeaturesResources._0_found_via_SourceLink, sourceDocument.FilePath);
                    return new SourceFileInfo(documentPath, "SourceLink", sourceText, sourceDocument.ChecksumAlgorithm, FromRemoteLocation: true);
                }
                else
                {
                    _logger?.Log(FeaturesResources._0_found_via_SourceLink_but_couldnt_read_file, sourceDocument.FilePath);
                    return null;
                }
            }
            else
            {
                telemetry.SetSourceFileSource("timeout");
                _logger?.Log(FeaturesResources.Timeout_SourceLink);
            }
        }

        return null;
    }

    private SourceFileInfo? TryGetOriginalFile(SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry, ProjectId projectId, IMetadataDocumentPersister persister)
    {
        // Always attempt to load this path from disk as it comes from the PDB.
        if (File.Exists(sourceDocument.FilePath))
        {
            var existingSourceText = LoadSourceFileFromDisk(sourceDocument.FilePath, sourceDocument, encoding, ignoreChecksum: false);
            if (existingSourceText is not null)
            {
                // Get the persister's view of the file path.  If using virtual documents, this will be different from the original file path.
                var documentPath = persister.ConvertFilePathToDocumentPath(projectId.Id, PdbSourceDocumentMetadataAsSourceFileProvider.ProviderName, sourceDocument.FilePath);

                telemetry.SetSourceFileSource("ondisk");
                _logger?.Log(FeaturesResources._0_found_in_original_location, sourceDocument.FilePath);
                return new SourceFileInfo(documentPath, FeaturesResources.external, existingSourceText, sourceDocument.ChecksumAlgorithm, FromRemoteLocation: false);
            }
        }

        return null;
    }

    private static SourceText? LoadSourceFileFromDisk(string filePath, SourceDocument sourceDocument, Encoding encoding, bool ignoreChecksum)
    {
        return IOUtilities.PerformIO(() =>
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

            var sourceText = SourceText.From(stream, encoding, sourceDocument.ChecksumAlgorithm, throwIfBinaryDetected: true);

            if (ignoreChecksum || VerifySourceText(sourceText, sourceDocument))
            {
                return sourceText;
            }

            return null;
        });
    }

    private static bool VerifySourceText(SourceText sourceText, SourceDocument sourceDocument)
    {
        var sourceTextChecksum = sourceText.GetChecksum();
        return sourceTextChecksum.SequenceEqual(sourceDocument.Checksum);
    }
}
