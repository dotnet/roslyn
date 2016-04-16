// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal class PreviewChangesCodeAction : CodeAction
    {
        private readonly Workspace _workspace;
        private readonly CodeAction _originalCodeAction;
        private readonly SolutionChangeSummary _changeSummary;

        public PreviewChangesCodeAction(Workspace workspace, CodeAction originalCodeAction, SolutionChangeSummary changeSummary)
        {
            _workspace = workspace;
            _originalCodeAction = originalCodeAction;
            _changeSummary = changeSummary;
        }

        public override string Title
        {
            get
            {
                return EditorFeaturesResources.PreviewChangesSummaryText;
            }
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previewDialogService = _workspace.Services.GetService<IPreviewDialogService>();
            if (previewDialogService == null)
            {
                return null;
            }

            var changedSolution = previewDialogService.PreviewChanges(
                EditorFeaturesResources.PreviewChanges,
                "vs.codefix.previewchanges",
                _originalCodeAction.Title,
                EditorFeaturesResources.PreviewChangesRootNodeText,
                CodeAnalysis.Glyph.OpenFolder,
                _changeSummary.NewSolution,
                _changeSummary.OldSolution,
                showCheckBoxes: false);

            if (changedSolution == null)
            {
                // User pressed the cancel button.
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await _originalCodeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
