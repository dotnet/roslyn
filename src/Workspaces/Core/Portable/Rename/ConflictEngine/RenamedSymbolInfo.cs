// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Information about the renamed symbol in new solution and the original symbol in old solution.
    /// </summary>
    /// <param name="RenamedSymbolInNewSolution">Renamed symbol in new Solution</param>
    /// <param name="SymbolicRenameLocations">RenameLocations of the original symbol</param>
    /// <param name="DocumentIdOfOriginalSymbolDeclaration">DocumentId of the original symbol's declaration</param>
    /// <param name="OriginalSymbolDeclarationLocation">Location of the original symbol's declaration</param>
    internal readonly record struct RenamedSymbolInfo(
        ISymbol RenamedSymbolInNewSolution,
        SymbolicRenameLocations SymbolicRenameLocations,
        DocumentId DocumentIdOfOriginalSymbolDeclaration,
        Location OriginalSymbolDeclarationLocation);
}
