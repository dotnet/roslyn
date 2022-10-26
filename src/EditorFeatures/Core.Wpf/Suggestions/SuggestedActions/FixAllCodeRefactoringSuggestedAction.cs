// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix all occurrences for a code refactoring.  Note: this is only used
    /// as a 'flavor' inside CodeRefactoringSuggestionAction.
    /// </summary>
    internal sealed class FixAllCodeRefactoringSuggestedAction : AbstractFixAllSuggestedAction, IFixAllCodeRefactoringSuggestedAction
    {
        public FixAllCodeRefactoringSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            IFixAllState fixAllState,
            CodeAction originalCodeAction)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer, fixAllState,
                   originalCodeAction, new FixAllCodeRefactoringCodeAction(fixAllState))
        {
        }
    }
}
