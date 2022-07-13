// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// RenameSymbolContext contains all the immutable context information to rename the <paramref name="RenamedSymbol"/>.
    /// </summary>
    internal record RenameSymbolContext(
        int Priority,
        RenameAnnotation RenamableSymbolDeclarationAnnotation,
        Location? RenamableDeclarationLocation,
        string ReplacementText,
        string OriginalText,
        ICollection<string> PossibleNameConflicts,
        ISymbol RenamedSymbol,
        IAliasSymbol? AliasSymbol,
        bool ReplacementTextValid,
        bool IsRenamingInStrings,
        bool IsRenamingInComments);
}
