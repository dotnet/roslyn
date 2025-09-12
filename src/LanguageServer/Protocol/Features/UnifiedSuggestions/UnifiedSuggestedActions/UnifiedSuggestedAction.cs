// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to SuggestedAction, but in a location that can be used by
/// both local Roslyn and LSP.
/// </summary>
internal abstract class UnifiedSuggestedAction(
    CodeAction codeAction,
    CodeActionPriority codeActionPriority,
    object provider,
    CodeRefactoringKind? codeRefactoringKind,
    ImmutableArray<Diagnostic> diagnostics)
{
    public object Provider { get; } = provider;

    public CodeAction OriginalCodeAction { get; } = codeAction;

    public CodeActionPriority CodeActionPriority { get; } = codeActionPriority;

    public CodeRefactoringKind? CodeRefactoringKind { get; } = codeRefactoringKind;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}
