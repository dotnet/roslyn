// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

internal partial class SuggestedActionWithNestedFlavors
{
    /// <summary>
    /// Suggested action for additional custom hyperlink in the lightbulb preview pane.
    /// Each <see cref="CodeAction"/> can define custom <see cref="CodeAction.AdditionalPreviewFlavors"/>
    /// code actions for adding these custom hyperlinks to the preview,
    /// similar to 'Preview Changes' and 'Fix All' hyperlinks that show up for all suggested actions.
    /// Note that this suggested action type just wraps the underlying original code action that comes from
    /// <see cref="CodeAction.AdditionalPreviewFlavors"/> and gets added to the suggested action set
    /// holding all the suggested actions for custom hyperlinks to show in the lightbulb preview pane.
    /// </summary>
    protected sealed class FlavoredSuggestedAction : SuggestedAction
    {
        private FlavoredSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            Solution originalSolution,
            ITextBuffer subjectBuffer,
            object provider,
            CodeAction originalCodeAction)
            : base(threadingContext, sourceProvider, workspace, originalSolution, subjectBuffer, provider, originalCodeAction)
        {
        }

        public static SuggestedAction Create(SuggestedActionWithNestedFlavors suggestedAction, CodeAction codeAction)
        {
            return new FlavoredSuggestedAction(
                suggestedAction.ThreadingContext,
                suggestedAction.SourceProvider,
                suggestedAction.Workspace,
                suggestedAction.OriginalSolution,
                suggestedAction.SubjectBuffer,
                suggestedAction.Provider,
                codeAction);
        }
    }
}
