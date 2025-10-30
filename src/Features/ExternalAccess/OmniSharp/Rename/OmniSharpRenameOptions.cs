// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp;

internal readonly record struct OmniSharpRenameOptions(
    bool RenameInComments,
    bool RenameInStrings,
    bool RenameOverloads)
{
    internal SymbolRenameOptions ToRenameOptions()
        => new(
            RenameOverloads: RenameOverloads,
            RenameInStrings: RenameInStrings,
            RenameInComments: RenameInComments,
            RenameFile: false);
}
