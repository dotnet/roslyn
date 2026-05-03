// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

internal sealed partial class EditorSuggestedActionWithNestedFlavors
{
    private sealed class PreviewChangesCodeAction(
        CodeAction originalCodeAction,
        Func<CancellationToken, Task<SolutionPreviewResult?>> getPreviewResultAsync) : CodeAction
    {
        private readonly CodeAction _originalCodeAction = originalCodeAction;
        private readonly Func<CancellationToken, Task<SolutionPreviewResult?>> _getPreviewResultAsync = getPreviewResultAsync;

        public override string Title => EditorFeaturesResources.Preview_changes2;

        private protected override async Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(
            Solution originalSolution, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previewDialogService = originalSolution.Services.GetService<IPreviewDialogService>();
            if (previewDialogService == null)
            {
                return [];
            }

            var previewResult = await _getPreviewResultAsync(cancellationToken).ConfigureAwait(true);
            if (previewResult?.ChangeSummary is not { } changeSummary)
            {
                return [];
            }

            var changedSolution = previewDialogService.PreviewChanges(
                EditorFeaturesResources.Preview_Changes,
                "vs.codefix.previewchanges",
                _originalCodeAction.Title,
                EditorFeaturesResources.Changes,
                CodeAnalysis.Glyph.OpenFolder,
                changeSummary.NewSolution,
                changeSummary.OldSolution,
                showCheckBoxes: false);

            if (changedSolution == null)
            {
                // User pressed the cancel button.
                return [];
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await _originalCodeAction.GetOperationsAsync(originalSolution, progressTracker, cancellationToken).ConfigureAwait(false);
        }
    }
}
