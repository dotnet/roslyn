// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    internal interface ISourceLinkService
    {
        Task<SourceFilePathResult?> GetSourceFilePathAsync(string url, string relativePath, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken);

        Task<PdbFilePathResult?> GetPdbFilePathAsync(string dllPath, PEReader peReader, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken);
    }

    internal record PdbFilePathResult(string PdbFilePath, string Status, string? Log, bool IsPortablePdb);

    internal record SourceFilePathResult(string SourceFilePath, string? Log);
}
