// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

internal partial class SuggestedActionWithNestedFlavors
{
    /// <summary>
    /// Suggested action for showing the 'Refine with Copilot' hyperlink in lightbulb preview pane.
    /// Note: This hyperlink is shown for **all** suggested actions lightbulb preview, i.e.
    /// regular code fixes and refactorings, in addition to Copilot suggested code fixes.
    /// It wraps the core <see cref="RefineUsingCopilotCodeAction"/> that invokes into
    /// Copilot service to start a Copilot refinement session on top of the document changes
    /// from the original code action corresponding to the lightbulb preview.
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
            // NOTE: Refine with Copilot functionality is only available for code actions
            // that change a source document, such that the option guarding this Refine feature
            // is enabled and the Copilot service is available within this VS session.

            if (suggestedAction.OriginalDocument is not Document originalDocument)
                return null;

            var copilotService = originalDocument.GetLanguageService<ICopilotCodeAnalysisService>();
            if (copilotService == null || !await copilotService.IsRefineOptionEnabledAsync().ConfigureAwait(false))
                return null;

            var isAvailable = await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
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
