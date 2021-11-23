// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IPdbSourceDocumentLoaderService)), Shared]
    internal sealed class PdbSourceDocumentLoaderService : IPdbSourceDocumentLoaderService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbSourceDocumentLoaderService()
        {
        }

        public Task<TextLoader?> LoadSourceDocumentAsync(SourceDocument sourceDocument, Encoding? defaultEncoding, CancellationToken cancellationToken)
        {
            // First we try getting "local" files, either from embedded source or a local file on disk
            var stream = TryGetEmbeddedSourceStream(sourceDocument) ??
                TryGetFileStream(sourceDocument);

            if (stream is not null)
            {
                using (stream)
                {
                    var encoding = defaultEncoding ?? Encoding.UTF8;
                    try
                    {
                        var sourceText = EncodedStringText.Create(stream, defaultEncoding: encoding, checksumAlgorithm: sourceDocument.HashAlgorithm);

                        var fileChecksum = sourceText.GetChecksum();
                        if (fileChecksum.SequenceEqual(sourceDocument.Checksum))
                        {
                            var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default, sourceDocument.FilePath);
                            var textLoader = TextLoader.From(textAndVersion);
                            return Task.FromResult<TextLoader?>(textLoader);
                        }
                    }
                    catch (IOException)
                    {
                        // TODO: Log message to inform the user what went wrong: https://github.com/dotnet/roslyn/issues/57352
                    }
                }
            }

            // TODO: Call the debugger to download the file
            // Maybe they'll download to a temp file, in which case this method could return a string
            // or maybe they'll return a stream, in which case we could create a new StreamTextLoader

            return Task.FromResult<TextLoader?>(null);
        }

        private static Stream? TryGetEmbeddedSourceStream(SourceDocument sourceDocument)
        {
            if (sourceDocument.EmbeddedTextBytes is null)
                return null;

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

            return stream;
        }

        private static Stream? TryGetFileStream(SourceDocument sourceDocument)
        {
            if (File.Exists(sourceDocument.FilePath))
            {
                return IOUtilities.PerformIO(() => new FileStream(sourceDocument.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));
            }

            return null;
        }
    }
}
