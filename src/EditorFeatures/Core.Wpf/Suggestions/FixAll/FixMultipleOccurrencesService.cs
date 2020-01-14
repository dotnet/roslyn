// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Service to compute and apply <see cref="FixMultipleCodeAction"/> code fixes.
    /// </summary>
    [ExportWorkspaceService(typeof(IFixMultipleOccurrencesService), ServiceLayer.Host), Shared]
    internal class FixMultipleOccurrencesService : IFixMultipleOccurrencesService
    {
        [ImportingConstructor]
        public FixMultipleOccurrencesService(IAsynchronousOperationListenerProvider listenerProvider)
        {
            listenerProvider.GetListener(FeatureAttribute.LightBulb);
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
            var fixMultipleState = FixAllState.Create(
                fixAllProvider, diagnosticsToFix, fixProvider, equivalenceKey);

            return GetFixedSolution(
                fixMultipleState, workspace, waitDialogTitle,
                waitDialogMessage, cancellationToken);
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
            var fixMultipleState = FixAllState.Create(
                fixAllProvider, diagnosticsToFix, fixProvider, equivalenceKey);

            return GetFixedSolution(
                fixMultipleState, workspace, waitDialogTitle,
                waitDialogMessage, cancellationToken);
        }

        private Solution GetFixedSolution(
            FixAllState fixAllState,
            Workspace workspace,
            string title,
            string waitDialogMessage,
            CancellationToken cancellationToken)
        {
            var fixMultipleCodeAction = new FixMultipleCodeAction(
                fixAllState, title, waitDialogMessage);

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
