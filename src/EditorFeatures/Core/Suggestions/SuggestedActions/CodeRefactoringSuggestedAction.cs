// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <summary>
/// Represents light bulb menu item for code refactorings.
/// </summary>
internal sealed class CodeRefactoringSuggestedAction(
    IThreadingContext threadingContext,
    SuggestedActionsSourceProvider sourceProvider,
    TextDocument originalDocument,
    ITextBuffer subjectBuffer,
    CodeRefactoringProvider provider,
    CodeAction codeAction,
    SuggestedActionSet? fixAllFlavors)
    : SuggestedActionWithNestedFlavors(
        threadingContext, sourceProvider, originalDocument, subjectBuffer, provider, codeAction, fixAllFlavors), ICodeRefactoringSuggestedAction
{
    public CodeRefactoringProvider CodeRefactoringProvider { get; } = provider;
}
