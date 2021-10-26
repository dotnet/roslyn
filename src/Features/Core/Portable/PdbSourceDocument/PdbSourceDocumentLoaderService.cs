// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

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
            if (sourceDocument.EmbeddedText is not null)
            {
                var textAndVersion = TextAndVersion.Create(sourceDocument.EmbeddedText, VersionStamp.Default, sourceDocument.FilePath);
                return Task.FromResult<TextLoader?>(TextLoader.From(textAndVersion));
            }

            // Otherwise, check the easiest (but most unlikely) case which is the document exists on the disk
            if (File.Exists(sourceDocument.FilePath))
            {
                // TODO: Make sure the hash of the file is correct: https://github.com/dotnet/roslyn/issues/57351
                return Task.FromResult<TextLoader?>(new FileTextLoader(sourceDocument.FilePath, Encoding.UTF8));
            }

            // TODO: Call the debugger to download the file
            // Maybe they'll download to a temp file, in which case this method could return a string
            // or maybe they'll return a stream, in which case we could create a new StreamTextLoader

            return Task.FromResult<TextLoader?>(null);
        }
    }
}
