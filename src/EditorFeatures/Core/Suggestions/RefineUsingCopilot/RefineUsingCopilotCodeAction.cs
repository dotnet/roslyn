// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

internal partial class EditorSuggestedActionWithNestedFlavors
{
    /// <summary>
    /// Code action that triggers Copilot refinement session to add further
    /// code changes on top of the changes from the wrapped <paramref name="originalCodeAction"/>.
    /// </summary>
    private sealed class RefineUsingCopilotCodeAction(
        Solution originalSolution,
        CodeAction originalCodeAction,
        Diagnostic? primaryDiagnostic,
        ICopilotCodeAnalysisService copilotCodeAnalysisService) : CodeAction
    {
        public override string Title => EditorFeaturesResources.Refine_using_Copilot;

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            // Make sure we don't trigger the refinement session for preview operation
            return Task.FromResult(SpecializedCollections.EmptyEnumerable<CodeActionOperation>());
        }

        protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            // This method is called when the user has clicked on the 'Refine using Copilot'
            // hyperlink in the lightbulb preview.
            // We want to bring up Copilot refinement session on top of the code changes
            // from the underlying code action. Additionally, if the underlying code action
            // came from a prior Copilot code fix suggestion, we also want to pass in the Copilot
            // diagnostic to the refinement session. This diagnostic would be mapped to the prior
            // Copilot session id to ensure that the Copilot refinement session has the historical
            // context on the Copilot conversation that produce the underlying diagnostic/code action.
            // 
            // We have a bunch of checks upfront before we bring up the Copilot refinement:
            //  - Applying the underlying code action produces a non-null newSolution.
            //  - The underlying code action produces change(s) to exactly one source document.
            //
            // TODO: Currently, we start a task to spawn a new Copilot refinement session
            //       at the end of this method, without waiting for the refinement session to complete.
            //       Consider if there could be better UX/platform support for such flavored actions
            //       where clicking on the hyperlink needs to bring up another unrelated UI.

            var newSolution = await originalCodeAction.GetChangedSolutionInternalAsync(originalSolution, progress, cancellationToken).ConfigureAwait(false);
            if (newSolution == null)
                return [];

            var changes = newSolution.GetChanges(originalSolution);
            var changeSummary = new SolutionChangeSummary(originalSolution, newSolution, changes);
            if (changeSummary.TotalFilesAffected != 1
                || changeSummary.TotalProjectsAffected != 1
                || changeSummary.NewSolution.GetChangedDocuments(changeSummary.OldSolution).FirstOrDefault() is not { } changedDocumentId)
            {
                return [];
            }

            var oldDocument = changeSummary.OldSolution.GetRequiredDocument(changedDocumentId);
            var newDocument = changeSummary.NewSolution.GetRequiredDocument(changedDocumentId);

            cancellationToken.ThrowIfCancellationRequested();
            return [new OpenRefinementSessionOperation(oldDocument, newDocument, primaryDiagnostic, copilotCodeAnalysisService)];
        }

        /// <summary>
        /// A code action operation for trigger Copilot Chat inline refinement session.
        /// </summary>
        private sealed class OpenRefinementSessionOperation(
            Document oldDocument,
            Document newDocument,
            Diagnostic? convertedPrimaryDiagnostic,
            ICopilotCodeAnalysisService copilotCodeAnalysisService) : CodeActionOperation
        {
            internal override async Task<bool> TryApplyAsync(Workspace workspace, Solution originalSolution, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
            {
                // Trigger the Copilot refinement session in background, passing in the old and new document for
                // the base code changes on top of which we want to perform further refinement.
                // Note that we do not pass in our cancellation token to the StartRefinementSessionAsync
                // call as bringing up the refinement session is a quick operation and the refinement session
                // has it's own cancellation token source to allow users to dismiss the session.
                // Additionally, we do not want cancellation triggered on the token passed into
                // GetChangedSolutionAsync to suddenly dismiss the refinement session UI without user explicitly
                // dismissing the session.
                await copilotCodeAnalysisService.StartRefinementSessionAsync(oldDocument, newDocument, convertedPrimaryDiagnostic, CancellationToken.None).ConfigureAwait(false);
                return true;
            }
        }
    }
}
