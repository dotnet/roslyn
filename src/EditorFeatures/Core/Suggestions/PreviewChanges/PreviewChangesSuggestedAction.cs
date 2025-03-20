// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionWithNestedFlavors
    {
        /// <summary>
        /// Suggested action for showing the preview-changes dialog.  Note: this is only used
        /// as a 'flavor' inside CodeFixSuggestionAction and CodeRefactoringSuggestedAction.
        /// </summary>
        private sealed partial class PreviewChangesSuggestedAction : SuggestedAction
        {
            private PreviewChangesSuggestedAction(
                IThreadingContext threadingContext,
                SuggestedActionsSourceProvider sourceProvider,
                Workspace workspace,
                Solution originalSolution,
                ITextBuffer subjectBuffer,
                object provider,
                PreviewChangesCodeAction codeAction)
                : base(threadingContext, sourceProvider, workspace, originalSolution, subjectBuffer, provider, codeAction)
            {
            }

            public static SuggestedAction Create(SuggestedActionWithNestedFlavors suggestedAction)
            {
                return new PreviewChangesSuggestedAction(
                    suggestedAction.ThreadingContext,
                    suggestedAction.SourceProvider,
                    suggestedAction.Workspace,
                    suggestedAction.OriginalSolution,
                    suggestedAction.SubjectBuffer,
                    suggestedAction.Provider,
                    new PreviewChangesCodeAction(
                        suggestedAction.Workspace, suggestedAction.CodeAction, suggestedAction.GetPreviewResultAsync));
            }
        }
    }
}
