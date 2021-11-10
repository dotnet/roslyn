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

        public Task<TextLoader?> LoadSourceDocumentAsync(SourceDocument sourceDocument, CancellationToken cancellationToken)
        {
            // If we already have the embedded text then use that directly
            SourceText? sourceText = null;
            if (sourceDocument.EmbeddedTextBytes is not null)
            {
                sourceText = TryLoadSourceFromEmbeddedSource(sourceDocument.EmbeddedTextBytes);
            }

            // Otherwise, check the easiest (but most unlikely) case which is the document exists on the disk
            if (sourceText is null && File.Exists(sourceDocument.FilePath))
            {
                sourceText = IOUtilities.PerformIO(() => TryLoadSourceFromDisk(sourceDocument));
            }

            if (sourceText is not null)
            {
                var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default, sourceDocument.FilePath);
                var textLoader = TextLoader.From(textAndVersion);
                return Task.FromResult<TextLoader?>(textLoader);
            }

            // TODO: Call the debugger to download the file
            // Maybe they'll download to a temp file, in which case this method could return a string
            // or maybe they'll return a stream, in which case we could create a new StreamTextLoader

            return Task.FromResult<TextLoader?>(null);
        }

        private static SourceText? TryLoadSourceFromEmbeddedSource(byte[] embeddedTextBytes)
        {
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

            using (stream)
            {
                return EncodedStringText.Create(stream);
            }
        }

        private static SourceText? TryLoadSourceFromDisk(SourceDocument sourceDocument)
        {
            using var fileStream = new FileStream(sourceDocument.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            if (fileStream is null)
                return null;

            // TODO: Don't hard code UTF8: https://github.com/dotnet/roslyn/issues/57350
            var sourceText = SourceText.From(fileStream, Encoding.UTF8, sourceDocument.HashAlgorithm);
            var fileChecksum = sourceText.GetChecksum();

            if (fileChecksum.SequenceEqual(sourceDocument.Checksum))
            {
                return sourceText;
            }

            return null;
        }
    }
}
