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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IPdbSourceDocumentLoaderService)), Shared]
    internal sealed class PdbSourceDocumentLoaderService : IPdbSourceDocumentLoaderService
    {
        private const int SourceLinkTimeout = 1000;
        private const int ExtendedSourceLinkTimeout = 4000;

        /// <summary>
        /// Lazy import ISourceLinkService because it can cause debugger 
        /// binaries to be eagerly loaded even if they are never used.
        /// </summary>
        private readonly Lazy<ISourceLinkService?> _sourceLinkService;
        private readonly IPdbSourceDocumentLogger? _logger;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code")]
        public PdbSourceDocumentLoaderService(
            [Import(AllowDefault = true)] Lazy<ISourceLinkService?> sourceLinkService,
            [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger)
        {
            _sourceLinkService = sourceLinkService;
            _logger = logger;
        }

        public async Task<SourceFileInfo?> LoadSourceDocumentAsync(string tempFilePath, SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry, bool useExtendedTimeout, CancellationToken cancellationToken)
        {
            // First we try getting "local" files, either from embedded source or a local file on disk
            // and if they don't work we call the debugger to download a file from SourceLink info
            return TryGetEmbeddedSourceFile(tempFilePath, sourceDocument, encoding, telemetry) ??
                TryGetOriginalFile(sourceDocument, encoding, telemetry) ??
                await TryGetSourceLinkFileAsync(sourceDocument, encoding, telemetry, useExtendedTimeout, cancellationToken).ConfigureAwait(false);
        }

        private SourceFileInfo? TryGetEmbeddedSourceFile(string tempFilePath, SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry)
        {
            if (sourceDocument.EmbeddedTextBytes is null)
                return null;

            var filePath = Path.Combine(tempFilePath, Path.GetFileName(sourceDocument.FilePath));

            // We might have already navigated to this file before, so it might exist, but
            // we still need to re-validate the checksum and make sure its not the wrong file
            if (File.Exists(filePath) &&
                LoadSourceFile(filePath, sourceDocument, encoding, FeaturesResources.embedded, ignoreChecksum: false, fromRemoteLocation: false) is { } existing)
            {
                telemetry.SetSourceFileSource("embedded");
                _logger?.Log(FeaturesResources._0_found_in_embedded_PDB_cached_source_file, sourceDocument.FilePath);
                return existing;
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
                using (stream)
                {
                    try
                    {
                        stream.Position = 0;
                        using (var file = File.OpenWrite(filePath))
                        {
                            stream.CopyTo(file);
                        }

                        new FileInfo(filePath).IsReadOnly = true;
                    }
                    catch (Exception ex) when (IOUtilities.IsNormalIOException(ex))
                    {
                        _logger?.Log(FeaturesResources._0_found_in_embedded_PDB_but_could_not_write_file_1, sourceDocument.FilePath, ex.Message);
                        return null;
                    }
                }

                var result = LoadSourceFile(filePath, sourceDocument, encoding, FeaturesResources.embedded, ignoreChecksum: false, fromRemoteLocation: false);
                if (result is not null)
                {
                    telemetry.SetSourceFileSource("embedded");
                    _logger?.Log(FeaturesResources._0_found_in_embedded_PDB, sourceDocument.FilePath);
                }
                else
                {
                    _logger?.Log(FeaturesResources._0_found_in_embedded_PDB_but_checksum_failed, sourceDocument.FilePath);
                }

                return result;
            }

            return null;
        }

        private async Task<SourceFileInfo?> TryGetSourceLinkFileAsync(SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry, bool useExtendedTimeout, CancellationToken cancellationToken)
        {
            if (sourceDocument.SourceLinkUrl is null || _sourceLinkService.Value is null)
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
                    var result = LoadSourceFile(sourceFile.SourceFilePath, sourceDocument, encoding, "SourceLink", ignoreChecksum: true, fromRemoteLocation: true);
                    if (result is not null)
                    {
                        telemetry.SetSourceFileSource("sourcelink");
                        _logger?.Log(FeaturesResources._0_found_via_SourceLink, sourceDocument.FilePath);
                    }
                    else
                    {
                        _logger?.Log(FeaturesResources._0_found_via_SourceLink_but_couldnt_read_file, sourceDocument.FilePath);
                    }

                    return result;
                }
                else
                {
                    telemetry.SetSourceFileSource("timeout");
                    _logger?.Log(FeaturesResources.Timeout_SourceLink);
                }
            }

            return null;
        }

        private SourceFileInfo? TryGetOriginalFile(SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry)
        {
            if (File.Exists(sourceDocument.FilePath))
            {
                var result = LoadSourceFile(sourceDocument.FilePath, sourceDocument, encoding, FeaturesResources.external, ignoreChecksum: false, fromRemoteLocation: false);
                if (result is not null)
                {
                    telemetry.SetSourceFileSource("ondisk");
                    _logger?.Log(FeaturesResources._0_found_in_original_location, sourceDocument.FilePath);
                }
                else
                {
                    _logger?.Log(FeaturesResources._0_found_in_original_location_but_checksum_failed, sourceDocument.FilePath);
                }

                return result;
            }

            return null;
        }

        private static SourceFileInfo? LoadSourceFile(string filePath, SourceDocument sourceDocument, Encoding encoding, string sourceDescription, bool ignoreChecksum, bool fromRemoteLocation)
        {
            return IOUtilities.PerformIO(() =>
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

                var sourceText = SourceText.From(stream, encoding, sourceDocument.HashAlgorithm, throwIfBinaryDetected: true);

                var fileChecksum = sourceText.GetChecksum();
                if (ignoreChecksum || fileChecksum.SequenceEqual(sourceDocument.Checksum))
                {
                    var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default, filePath);
                    var textLoader = TextLoader.From(textAndVersion);
                    return new SourceFileInfo(filePath, sourceDescription, textLoader, fromRemoteLocation);
                }

                return null;
            });
        }
    }
}
