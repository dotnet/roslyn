// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Text;
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

        public Task<TextLoader?> LoadSourceDocumentAsync(SourceDocument sourceDocument, DocumentDebugInfoReader documentDebugInfoReader)
        {
            // First, check the easiest case which is the document exists on the disk
            if (File.Exists(sourceDocument.FilePath))
            {
                return Task.FromResult<TextLoader?>(new FileTextLoader(sourceDocument.FilePath, Encoding.UTF8));
            }

            // Otherwise it might be embedded source
            var text = documentDebugInfoReader.TryGetEmbeddedSourceText(sourceDocument);

            if (text is not null)
            {
                var textAndVersion = TextAndVersion.Create(text, VersionStamp.Default, sourceDocument.FilePath);
                return Task.FromResult<TextLoader?>(TextLoader.From(textAndVersion));
            }

            // TODO: Call the debugger to download the file
            // Maybe they'll download to a temp file, in which case this method could return a string
            // or maybe they'll return a stream, in which case we could create a new StreamTextLoader

            return Task.FromResult<TextLoader?>(null);
        }
    }
}
