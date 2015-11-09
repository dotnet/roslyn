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

        public Solution GetFix(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            Workspace workspace,
            CodeFixProvider fixProvider,
            FixAllProvider fixAllProvider,
            string equivalenceKey,
            string waitDialogTitle,
            string waitDialogMessage,
            CancellationToken cancellationToken)
        {
            var fixMultipleContext = FixMultipleContext.Create(diagnosticsToFix, fixProvider, equivalenceKey, cancellationToken);
            var suggestedAction = GetSuggestedAction(fixMultipleContext, workspace, fixAllProvider, waitDialogTitle, waitDialogMessage, showPreviewChangesDialog: false, cancellationToken: cancellationToken);
            return suggestedAction.GetChangedSolution(cancellationToken);
        }

        public Solution GetFix(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            Workspace workspace,
            CodeFixProvider fixProvider,
            FixAllProvider fixAllProvider,
            string equivalenceKey,
            string waitDialogTitle,
            string waitDialogMessage,
            CancellationToken cancellationToken)
        {
            var fixMultipleContext = FixMultipleContext.Create(diagnosticsToFix, fixProvider, equivalenceKey, cancellationToken);
            var suggestedAction = GetSuggestedAction(fixMultipleContext, workspace, fixAllProvider, waitDialogTitle, waitDialogMessage, showPreviewChangesDialog: false, cancellationToken: cancellationToken);
            return suggestedAction.GetChangedSolution(cancellationToken);
        }

        private FixMultipleSuggestedAction GetSuggestedAction(
            FixMultipleContext fixMultipleContext,
            Workspace workspace,
            FixAllProvider fixAllProvider,
            string title,
            string waitDialogMessage,
            bool showPreviewChangesDialog,
            CancellationToken cancellationToken)
        {
            var fixMultipleCodeAction = new FixMultipleCodeAction(fixMultipleContext, fixAllProvider, title, waitDialogMessage, showPreviewChangesDialog);
            return new FixMultipleSuggestedAction(workspace, _editHandler, fixMultipleCodeAction, fixAllProvider);
        }
    }
}
