// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportWorkspaceServiceFactory(typeof(IFixAllGetFixesService), ServiceLayer.Host), Shared]
    internal class FixAllGetFixesService : IFixAllGetFixesService, IWorkspaceServiceFactory
    {
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public FixAllGetFixesService(IWaitIndicator waitIndicator)
        {
            _waitIndicator = waitIndicator;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        public async Task<Solution> GetFixAllChangedSolutionAsync(FixAllProvider fixAllProvider, FixAllContext fixAllContext)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllProvider, fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return fixAllContext.Solution;
            }

            fixAllContext.CancellationToken.ThrowIfCancellationRequested();
            return await codeAction.GetChangedSolutionInternalAsync(cancellationToken: fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<CodeActionOperation>> GetFixAllOperationsAsync(FixAllProvider fixAllProvider, FixAllContext fixAllContext, bool showPreviewChangesDialog)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllProvider, fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return null;
            }

            return await GetFixAllOperationsAsync(codeAction, fixAllContext, showPreviewChangesDialog).ConfigureAwait(false);
        }

        private async Task<CodeAction> GetFixAllCodeActionAsync(FixAllProvider fixAllProvider, FixAllContext fixAllContext)
        {
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation, fixAllContext.CancellationToken))
            {
                CodeAction action = null;
                try
                {
                    action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    FixAllLogger.LogComputationResult(completed: false);
                }
                finally
                {
                    if (action != null)
                    {
                        FixAllLogger.LogComputationResult(completed: true);
                    }
                    else
                    {
                        FixAllLogger.LogComputationResult(completed: false, timedOut: true);
                    }
                }

                return action;
            }
        }

        private async Task<IEnumerable<CodeActionOperation>> GetFixAllOperationsAsync(CodeAction codeAction, FixAllContext fixAllContext, bool showPreviewChangesDialog)
        {
            // We have computed the fix all occurrences code fix.
            // Now fetch the new solution with applied fix and bring up the Preview changes dialog.

            var cancellationToken = fixAllContext.CancellationToken;
            var workspace = fixAllContext.Project.Solution.Workspace;

            cancellationToken.ThrowIfCancellationRequested();
            var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            if (operations == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var newSolution = await codeAction.GetChangedSolutionInternalAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (showPreviewChangesDialog)
            {
                newSolution = PreviewChanges(
                    fixAllContext.Project.Solution,
                    newSolution,
                    FeaturesResources.FixAllOccurrences,
                    codeAction.Title,
                    fixAllContext.Project.Language,
                    workspace,
                    cancellationToken);
                if (newSolution == null)
                {
                    return null;
                }
            }

            // Get a code action, with apply changes operation replaced with the newSolution.
            return GetNewFixAllOperations(operations, newSolution, cancellationToken);
        }

        internal static Solution PreviewChanges(
            Solution currentSolution,
            Solution newSolution,
            string fixAllPreviewChangesTitle,
            string fixAllTopLevelHeader,
            string languageOpt,
            Workspace workspace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesPreviewChanges, cancellationToken))
            {
                var previewService = workspace.Services.GetService<IPreviewDialogService>();
                var glyph = languageOpt == null ?
                    Glyph.Assembly :
                    languageOpt == LanguageNames.CSharp ? Glyph.CSharpProject : Glyph.BasicProject;

                var changedSolution = previewService.PreviewChanges(
                    string.Format(EditorFeaturesResources.PreviewChangesOf, fixAllPreviewChangesTitle),
                    "vs.codefix.fixall",
                    fixAllTopLevelHeader,
                    fixAllPreviewChangesTitle,
                    glyph,
                    newSolution,
                    currentSolution);

                if (changedSolution == null)
                {
                    // User clicked cancel.
                    FixAllLogger.LogPreviewChangesResult(applied: false);
                    return null;
                }

                FixAllLogger.LogPreviewChangesResult(applied: true, allChangesApplied: changedSolution == newSolution);
                return changedSolution;
            }
        }

        private IEnumerable<CodeActionOperation> GetNewFixAllOperations(IEnumerable<CodeActionOperation> operations, Solution newSolution, CancellationToken cancellationToken)
        {
            bool foundApplyChanges = false;
            foreach (var operation in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!foundApplyChanges)
                {
                    var applyChangesOperation = operation as ApplyChangesOperation;
                    if (applyChangesOperation != null)
                    {
                        foundApplyChanges = true;
                        yield return new ApplyChangesOperation(newSolution);
                        continue;
                    }
                }

                yield return operation;
            }
        }
    }
}
