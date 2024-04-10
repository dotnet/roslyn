// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionWithNestedFlavors
    {
        private partial class PreviewChangesSuggestedAction
        {
            private sealed class PreviewChangesCodeAction : CodeAction
            {
                private readonly Workspace _workspace;
                private readonly CodeAction _originalCodeAction;
                private readonly Func<CancellationToken, Task<SolutionPreviewResult?>> _getPreviewResultAsync;

                public PreviewChangesCodeAction(Workspace workspace, CodeAction originalCodeAction, Func<CancellationToken, Task<SolutionPreviewResult?>> getPreviewResultAsync)
                {
                    _workspace = workspace;
                    _originalCodeAction = originalCodeAction;
                    _getPreviewResultAsync = getPreviewResultAsync;
                }

                public override string Title => EditorFeaturesResources.Preview_changes2;

                private protected override async Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(
                    Solution originalSolution, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var previewDialogService = _workspace.Services.GetService<IPreviewDialogService>();
                    if (previewDialogService == null)
                    {
                        return ImmutableArray<CodeActionOperation>.Empty;
                    }

                    var previewResult = await _getPreviewResultAsync(cancellationToken).ConfigureAwait(true);
                    if (previewResult?.ChangeSummary is not { } changeSummary)
                    {
                        return ImmutableArray<CodeActionOperation>.Empty;
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
                        return ImmutableArray<CodeActionOperation>.Empty;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return await _originalCodeAction.GetOperationsAsync(originalSolution, progressTracker, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
