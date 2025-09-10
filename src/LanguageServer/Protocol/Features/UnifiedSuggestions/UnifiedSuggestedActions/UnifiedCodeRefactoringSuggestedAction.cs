// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to CodeRefactoringSuggestedAction, but in a location that can be used by
/// both local Roslyn and LSP.
/// </summary>
internal sealed class UnifiedCodeRefactoringSuggestedAction(
    CodeAction codeAction,
    CodeActionPriority codeActionPriority,
    CodeRefactoringProvider codeRefactoringProvider,
    UnifiedSuggestedActionSet? fixAllFlavors)
    : UnifiedSuggestedAction(codeAction, codeActionPriority), ICodeRefactoringSuggestedAction
{
    public CodeRefactoringProvider CodeRefactoringProvider { get; } = codeRefactoringProvider;

    public UnifiedSuggestedActionSet? FixAllFlavors { get; } = fixAllFlavors;
}
