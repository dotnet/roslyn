// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionWithNestedFlavors
    {
        private partial class RefineUsingCopilotSuggestedAction
        {
            private sealed class RefineUsingCopilotCodeAction(
                Solution originalSolution,
                CodeAction originalCodeAction,
                DiagnosticData? primaryDiagnostic,
                ICopilotCodeAnalysisService copilotCodeAnalysisService) : CodeAction
            {
                public override string Title => EditorFeaturesResources.Refine_using_Copilot;

                protected override async Task<Solution?> GetChangedSolutionAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
                {
                    var newSolution = await originalCodeAction.GetChangedSolutionInternalAsync(originalSolution, progress, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (newSolution == null)
                        return null;

                    var changes = newSolution.GetChanges(originalSolution);
                    var changeSummary = new SolutionChangeSummary(originalSolution, newSolution, changes);
                    if (changeSummary.TotalFilesAffected != 1
                        || changeSummary.TotalProjectsAffected != 1
                        || changeSummary.NewSolution.GetChangedDocuments(changeSummary.OldSolution).FirstOrDefault() is not { } changedDocument)
                    {
                        return null;
                    }

                    var oldDocument = changeSummary.OldSolution.GetDocument(changedDocument);
                    var newDocument = changeSummary.NewSolution.GetDocument(changedDocument);
                    if (oldDocument == null || newDocument == null)
                        return null;

                    cancellationToken.ThrowIfCancellationRequested();
                    var convertedPrimaryDiagnostic = primaryDiagnostic != null
                        ? await primaryDiagnostic.ToDiagnosticAsync(oldDocument.Project, cancellationToken).ConfigureAwait(false)
                        : null;

                    // Trigger the Copilot refinement session in background
                    _ = Task.Run(() => copilotCodeAnalysisService.StartRefinementSessionAsync(oldDocument, newDocument, convertedPrimaryDiagnostic, CancellationToken.None), CancellationToken.None);

                    return null;
                }
            }
        }
    }
}
