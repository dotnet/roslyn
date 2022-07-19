// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Represent the rename context information for the given <paramref name="RenameLocation"/>.
    /// </summary>
    /// <param name="SymbolContext">The linked rename symbol for this location.</param>
    internal record TextSpanRenameContext(RenameLocation RenameLocation, RenameSymbolContext SymbolContext)
}
