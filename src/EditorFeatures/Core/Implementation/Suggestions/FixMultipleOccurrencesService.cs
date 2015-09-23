// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Service to compute and apply <see cref="FixMultipleCodeAction"/> code fixes.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IFixMultipleOccurrencesService), ServiceLayer.Host), Shared]
    internal class FixMultipleOccurrencesService : IFixMultipleOccurrencesService, IWorkspaceServiceFactory
    {
        private readonly ICodeActionEditHandlerService _editHandler;

        [ImportingConstructor]
        public FixMultipleOccurrencesService(
            ICodeActionEditHandlerService editHandler)
        {
            _editHandler = editHandler;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        public void ComputeAndApplyFix(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            Workspace workspace,
            CodeFixProvider fixProvider,
            FixAllProvider fixAllProvider,
            string equivalenceKey,
            string waitDialogAndPreviewChangesTitle,
            string waitDialogMessage,
            bool showPreviewChangesDialog,
            CancellationToken cancellationToken)
        {
            var fixMultipleContext = FixMultipleContext.Create(diagnosticsToFix, fixProvider, equivalenceKey, cancellationToken);
            ComputeAndApplyFix(fixMultipleContext, workspace, fixAllProvider, waitDialogAndPreviewChangesTitle, waitDialogMessage, showPreviewChangesDialog, cancellationToken);
        }

        public void ComputeAndApplyFix(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            Workspace workspace,
            CodeFixProvider fixProvider,
            FixAllProvider fixAllProvider,
            string equivalenceKey,
            string waitDialogAndPreviewChangesTitle,
            string waitDialogMessage,
            bool showPreviewChangesDialog,
            CancellationToken cancellationToken)
        {
            var fixMultipleContext = FixMultipleContext.Create(diagnosticsToFix, fixProvider, equivalenceKey, cancellationToken);
            ComputeAndApplyFix(fixMultipleContext, workspace, fixAllProvider, waitDialogAndPreviewChangesTitle, waitDialogMessage, showPreviewChangesDialog, cancellationToken);
        }

        private void ComputeAndApplyFix(
            FixMultipleContext fixMultipleContext,
            Workspace workspace,
            FixAllProvider fixAllProvider,
            string waitDialogAndPreviewChangesTitle,
            string waitDialogMessage,
            bool showPreviewChangesDialog,
            CancellationToken cancellationToken)
        {
            var fixMultipleCodeAction = new FixMultipleCodeAction(fixMultipleContext, fixAllProvider, title: waitDialogAndPreviewChangesTitle, previewChangesDialogTitle: waitDialogAndPreviewChangesTitle, computingFixWaitDialogMessage: waitDialogMessage, showPreviewChangesDialog: showPreviewChangesDialog);
            var fixMultipleSuggestedAction = new FixMultipleSuggestedAction(workspace, _editHandler, fixMultipleCodeAction, fixAllProvider);
            fixMultipleSuggestedAction.Invoke(cancellationToken);
        }
    }
}
