// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportWorkspaceServiceFactory(typeof(IFixAllGetFixesService), ServiceLayer.Host), Shared]
    internal class FeaturesFixAllGetFixesService : AbstractFixAllGetFixesService, IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FeaturesFixAllGetFixesService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => this;

        protected override async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(
            CodeAction codeAction,
            bool showPreviewChangesDialog,
            IProgressTracker progressTracker,
            IFixAllState fixAllState,
            CancellationToken cancellationToken)
        {
            // We have computed the fix all occurrences code fix.
            // Now fetch the new solution with applied fix and bring up the Preview changes dialog.
            cancellationToken.ThrowIfCancellationRequested();
            var operations = await codeAction.GetOperationsAsync(
                fixAllState.Solution, progressTracker, cancellationToken).ConfigureAwait(false);
            if (operations == null)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var newSolution = await codeAction.GetChangedSolutionInternalAsync(
                fixAllState.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Get a code action, with apply changes operation replaced with the newSolution.
            return GetNewFixAllOperations(operations, newSolution, cancellationToken);
        }
    }
}
