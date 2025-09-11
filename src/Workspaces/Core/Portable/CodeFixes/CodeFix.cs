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
    public readonly ImmutableArray<Diagnostic> Diagnostics;

    ///// <summary>
    ///// This is the diagnostic that will show up in the preview pane header when a particular fix is selected in the
    ///// light bulb menu. We also group all fixes with the same <see cref="PrimaryDiagnostic"/> together (into a single
    ///// SuggestedActionSet) in the light bulb menu.
    ///// </summary>
    ///// <remarks>
    ///// A given fix can fix one or more diagnostics. However, our light bulb UI (preview pane, grouping of fixes in the
    ///// light bulb menu etc.) currently keeps things simple and pretends that each fix fixes a single <see
    ///// cref="PrimaryDiagnostic"/>.
    ///// 
    ///// Implementation-wise the <see cref="PrimaryDiagnostic"/> is always the first diagnostic that the <see
    ///// cref="CodeFixProvider"/> supplied when registering the fix (<see
    ///// cref="CodeFixContext.RegisterCodeFix(CodeAction, IEnumerable{Diagnostic})"/>). This could change in the future,
    ///// if we decide to change the UI to depict the true mapping between fixes and diagnostics or if we decide to use
    ///// some other heuristic to determine the <see cref="PrimaryDiagnostic"/>.
    ///// </remarks>
    //public Diagnostic PrimaryDiagnostic => Diagnostics[0];

    public CodeFix(CodeAction action, Diagnostic diagnostic)
        : this(action, [diagnostic])
    {
    }

    public CodeFix(CodeAction action, ImmutableArray<Diagnostic> diagnostics)
    {
        Contract.ThrowIfTrue(diagnostics.IsDefaultOrEmpty);
        Action = action;
        Diagnostics = diagnostics;
    }
}
