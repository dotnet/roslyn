// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// RenameSymbolContext contains all the immutable context information to rename the RenamedSymbol/>.
    /// </summary>
    internal readonly record struct RenamedSymbolContext(
        string ReplacementText,
        string OriginalText,
        ICollection<string> PossibleNameConflicts,
        ISymbol RenamedSymbol,
        IAliasSymbol? AliasSymbol,
        bool ReplacementTextValid);
}
