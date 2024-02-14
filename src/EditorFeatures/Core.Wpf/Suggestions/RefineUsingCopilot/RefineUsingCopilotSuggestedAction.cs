// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionWithNestedFlavors
    {
        /// <summary>
        /// Suggested action for showing the 'Refine with Copilot' hyperlink in lightbulb preview pane.
        /// Note: this is only used as a 'flavor' inside CodeFixSuggestionAction and CodeRefactoringSuggestedAction.
        /// </summary>
        private sealed partial class RefineUsingCopilotSuggestedAction : SuggestedAction
        {
            private RefineUsingCopilotSuggestedAction(
                IThreadingContext threadingContext,
                SuggestedActionsSourceProvider sourceProvider,
                Workspace workspace,
                Solution originalSolution,
                ITextBuffer subjectBuffer,
                object provider,
                RefineUsingCopilotCodeAction codeAction)
                : base(threadingContext, sourceProvider, workspace, originalSolution, subjectBuffer, provider, codeAction)
            {
            }

            public static async Task<SuggestedAction?> TryCreateAsync(SuggestedActionWithNestedFlavors suggestedAction, CancellationToken cancellationToken)
            {
                if (suggestedAction.OriginalDocument is not Document originalDocument)
                    return null;

                var copilotService = suggestedAction.Workspace.Services.GetService<ICopilotCodeAnalysisService>();
                if (copilotService == null || !copilotService.IsRefineOptionEnabled(originalDocument))
                    return null;

                var isAvailable = await copilotService.IsAvailableAsync(originalDocument, cancellationToken).ConfigureAwait(false);
                if (!isAvailable)
                    return null;

                return new RefineUsingCopilotSuggestedAction(
                    suggestedAction.ThreadingContext,
                    suggestedAction.SourceProvider,
                    suggestedAction.Workspace,
                    suggestedAction.OriginalSolution,
                    suggestedAction.SubjectBuffer,
                    suggestedAction.Provider,
                    new RefineUsingCopilotCodeAction(
                        suggestedAction.OriginalSolution, suggestedAction.CodeAction, suggestedAction.GetDiagnostic(), copilotService));
            }
        }
    }
}
