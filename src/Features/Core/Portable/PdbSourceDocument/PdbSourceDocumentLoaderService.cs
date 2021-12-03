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
        private readonly ISourceLinkService? _sourceLinkService;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code")]
        public PdbSourceDocumentLoaderService([Import(AllowDefault = true)] ISourceLinkService? sourceLinkService)
        {
            _sourceLinkService = sourceLinkService;
        }

        public async Task<SourceFileInfo?> LoadSourceDocumentAsync(string tempFilePath, SourceDocument sourceDocument, Encoding encoding, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken)
        {
            // First we try getting "local" files, either from embedded source or a local file on disk
            // and if they don't work we call the debugger to download a file from SourceLink info
            return TryGetEmbeddedSourceFile(tempFilePath, sourceDocument, encoding) ??
                TryGetOriginalFile(sourceDocument, encoding) ??
                await TryGetSourceLinkFileAsync(sourceDocument, encoding, logger, cancellationToken).ConfigureAwait(false);
        }

        private static SourceFileInfo? TryGetEmbeddedSourceFile(string tempFilePath, SourceDocument sourceDocument, Encoding encoding)
        {
            if (sourceDocument.EmbeddedTextBytes is null)
                return null;

            var filePath = Path.Combine(tempFilePath, Path.GetFileName(sourceDocument.FilePath));

            // We might have already navigated to this file before, so it might exist, but
            // we still need to re-validate the checksum and make sure its not the wrong file
            if (File.Exists(filePath) &&
                LoadSourceFile(filePath, sourceDocument, encoding, ignoreChecksum: false) is { } existing)
            {
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
                    catch (IOException)
                    {
                        // TODO: Log message to inform the user what went wrong: https://github.com/dotnet/roslyn/issues/57352
                        return null;
                    }
                }

                return LoadSourceFile(filePath, sourceDocument, encoding, ignoreChecksum: false);
            }

            return null;
        }

        private async Task<SourceFileInfo?> TryGetSourceLinkFileAsync(SourceDocument sourceDocument, Encoding encoding, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken)
        {
            if (_sourceLinkService is null || sourceDocument.SourceLinkUrl is null)
                return null;

            // This should ideally be the repo-relative path to the file, and come from SourceLink: https://github.com/dotnet/sourcelink/pull/699
            var relativePath = Path.GetFileName(sourceDocument.FilePath);

            var delay = Task.Delay(SourceLinkTimeout, cancellationToken);
            var sourceFileTask = _sourceLinkService.GetSourceFilePathAsync(sourceDocument.SourceLinkUrl, relativePath, logger, cancellationToken);

            var winner = await Task.WhenAny(sourceFileTask, delay).ConfigureAwait(false);

            if (winner == sourceFileTask)
            {
                var sourceFile = await sourceFileTask.ConfigureAwait(false);
                if (sourceFile is not null)
                {
                    // TODO: Log results from sourceFile.Log: https://github.com/dotnet/roslyn/issues/57352
                    // TODO: Don't ignore the checksum here: https://github.com/dotnet/roslyn/issues/55834
                    return LoadSourceFile(sourceFile.SourceFilePath, sourceDocument, encoding, ignoreChecksum: true);
                }
                else
                {
                    // TODO: Log the timeout: https://github.com/dotnet/roslyn/issues/57352
                }
            }

            return null;
        }

        private static SourceFileInfo? TryGetOriginalFile(SourceDocument sourceDocument, Encoding encoding)
        {
            if (File.Exists(sourceDocument.FilePath))
            {
                return LoadSourceFile(sourceDocument.FilePath, sourceDocument, encoding, ignoreChecksum: false);
            }

            return null;
        }

        private static SourceFileInfo? LoadSourceFile(string filePath, SourceDocument sourceDocument, Encoding encoding, bool ignoreChecksum)
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
                    return new SourceFileInfo(filePath, textLoader);
                }

                return null;
            });
        }
    }
}
