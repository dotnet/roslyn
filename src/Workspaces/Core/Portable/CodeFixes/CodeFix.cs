// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Represents a single fix. This is essentially a tuple
/// that holds on to a <see cref="CodeAction"/> and the set of
/// <see cref="Diagnostic"/>s that this <see cref="CodeAction"/> will fix.
/// </summary>
internal sealed class CodeFix
{
    public readonly CodeAction Action;

    /// <summary>
    /// Note: a code fix can fix one or more diagnostics.  For the purposes of display in a UI, it is recommended that
    /// the first diagnostic in this list be treated as the "primary" diagnostic.  For example, withing Visual Studio,
    /// all fixes with the same primary diagnostic are grouped together in the light bulb menu.
    /// </summary>
    public readonly ImmutableArray<Diagnostic> Diagnostics;

    public CodeFix(CodeAction action, ImmutableArray<Diagnostic> diagnostics)
    {
        Contract.ThrowIfTrue(diagnostics.IsDefaultOrEmpty);
        Action = action;
        Diagnostics = diagnostics;
    }
}
