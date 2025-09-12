// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to CodeRefactoringSuggestedAction, but in a location that can be used by
/// both local Roslyn and LSP.
/// </summary>
internal sealed class UnifiedSuggestedActionWithNestedFlavors(
    CodeAction codeAction,
    CodeActionPriority codeActionPriority,
    object provider,
    UnifiedSuggestedActionSet? fixAllFlavors,
    CodeRefactoringKind? codeRefactoringKind,
    ImmutableArray<Diagnostic> diagnostics)
    : UnifiedSuggestedAction(codeAction, codeActionPriority, provider, codeRefactoringKind, diagnostics)
{
    public UnifiedSuggestedActionSet? FixAllFlavors { get; } = fixAllFlavors;
}
