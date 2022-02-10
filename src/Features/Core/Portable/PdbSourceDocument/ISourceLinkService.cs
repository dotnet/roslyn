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

    // The following types mirror types in Microsoft.VisualStudio.Debugger.Contracts which cannot be referenced at this layer

    /// <summary>
    /// The result of findding a PDB file
    /// </summary>
    /// <param name="PdbFilePath">The path to the PDB file in the debugger cache</param>
    /// <param name="Status">Status of the operation</param>
    /// <param name="Log">Any log messages the debugger wrote during the operation</param>
    /// <param name="IsPortablePdb">Whether the PDB found is portable</param>
    internal record PdbFilePathResult(string PdbFilePath, string Status, string? Log, bool IsPortablePdb);

    /// <summary>
    /// The result of finding a source file via SourceLink
    /// </summary>
    /// <param name="SourceFilePath">The path to the source file in the debugger cache</param>
    /// <param name="Log">Any log messages the debugger wrote during the operation</param>
    internal record SourceFilePathResult(string SourceFilePath, string? Log);
}
