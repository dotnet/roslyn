// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal readonly record struct RenamedSymbolInfo(
        ISymbol RenamedSymbolInNewSolution,
        SymbolicRenameLocations SymbolicRenameLocations,
        DocumentId DocumentIdOfOriginalSymbolDeclaration,
        Location OriginalSymbolDeclarationLocation);
}
