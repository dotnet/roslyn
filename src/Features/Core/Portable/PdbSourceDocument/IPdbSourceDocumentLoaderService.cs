// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    internal interface IPdbSourceDocumentLoaderService
    {
        Task<SourceFileInfo?> LoadSourceDocumentAsync(string tempFilePath, SourceDocument sourceDocument, Encoding encoding, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken);
    }

    internal sealed record SourceFileInfo(string FilePath, TextLoader Loader);
}
