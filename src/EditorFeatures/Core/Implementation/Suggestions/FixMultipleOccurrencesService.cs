// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Service to compute and apply <see cref="FixMultipleCodeAction"/> code fixes.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IFixMultipleOccurrencesService), ServiceLayer.Host), Shared]
    internal class FixMultipleOccurrencesService : IFixMultipleOccurrencesService, IWorkspaceServiceFactory
    {
        private readonly ICodeActionEditHandlerService _editHandler;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public FixMultipleOccurrencesService(
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _editHandler = editHandler;
            _waitIndicator = waitIndicator;
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.LightBulb);
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
            var fixMultipleState = FixAllState.Create(fixAllProvider, diagnosticsToFix, fixProvider, equivalenceKey);
            var triggerDiagnostic = diagnosticsToFix.First().Value.First();

            var result = GetFixedSolution(
                fixMultipleState, triggerDiagnostic, workspace,
                waitDialogTitle, waitDialogMessage, cancellationToken);
            return result;
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
            var fixMultipleState = FixAllState.Create(fixAllProvider, diagnosticsToFix, fixProvider, equivalenceKey);
            var triggerDiagnostic = diagnosticsToFix.First().Value.First();

            var result = GetFixedSolution(
                fixMultipleState, triggerDiagnostic, workspace, 
                waitDialogTitle, waitDialogMessage, cancellationToken);

            return result;
        }

        private Solution GetFixedSolution(
            FixAllState fixAllState,
            Diagnostic triggerDiagnostic,
            Workspace workspace,
            string title,
            string waitDialogMessage,
            CancellationToken cancellationToken)
        {
            var fixMultipleCodeAction = new FixMultipleCodeAction(
                fixAllState, triggerDiagnostic, title, waitDialogMessage);

            Solution newSolution = null;
            var extensionManager = workspace.Services.GetService<IExtensionManager>();
            extensionManager.PerformAction(fixAllState.FixAllProvider, () =>
            {
                // We don't need to post process changes here as the inner code action created for Fix multiple code fix already executes.
                newSolution = fixMultipleCodeAction.GetChangedSolutionInternalAsync(
                    postProcessChanges: false, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            });

            return newSolution;
        }
    }
}
