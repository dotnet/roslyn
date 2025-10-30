// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal interface IMetadataAsSourceFileService
{
    /// <summary>
    /// Generates a file on disk containing general information about the symbol's containing assembly, and the
    /// formatted source code for the public, protected, and protected-or-internal interface of which the given
    /// ISymbol is or is a part of.
    /// </summary>
    /// <param name="sourceWorkspace">The workspace that <paramref name="sourceProject"/> came from.</param>
    /// <param name="sourceProject">The project from which the symbol to generate source for came from.</param>
    /// <param name="symbol">The symbol whose interface to generate source for</param>
    /// <param name="signaturesOnly"><see langword="false"/> to allow a decompiler or other technology to show a
    /// representation of the original sources; otherwise <see langword="true"/> to only show member
    /// signatures.</param>
    /// <param name="options">Options to use when navigating. See <see cref="MetadataAsSourceOptions"/> for details.</param>
    Task<MetadataAsSourceFile> GetGeneratedFileAsync(
        Workspace sourceWorkspace,
        Project sourceProject,
        ISymbol symbol,
        bool signaturesOnly,
        MetadataAsSourceOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the given file path is a metadata as source file and adds to the metadata workspace if it is.
    /// Callers must ensure this is only called serially.
    /// </summary>
    bool TryAddDocumentToWorkspace(string filePath, SourceTextContainer sourceTextContainer, [NotNullWhen(true)] out DocumentId? documentId);

    /// <summary>
    /// Checks if the given file path is a metadata as source file and removes from the metadata workspace if it is.
    /// Callers must ensure this is only called serially.
    /// </summary>
    bool TryRemoveDocumentFromWorkspace(string filePath);

    bool IsNavigableMetadataSymbol(ISymbol symbol);

    Workspace? TryGetWorkspace();
}
