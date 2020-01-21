// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportWorkspaceServiceFactory(typeof(IFixAllGetFixesService), ServiceLayer.Host), Shared]
    internal class FixAllGetFixesService : IFixAllGetFixesService, IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public FixAllGetFixesService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        public async Task<Solution> GetFixAllChangedSolutionAsync(FixAllContext fixAllContext)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return fixAllContext.Solution;
            }

            fixAllContext.CancellationToken.ThrowIfCancellationRequested();
            return await codeAction.GetChangedSolutionInternalAsync(cancellationToken: fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(
            FixAllContext fixAllContext, bool showPreviewChangesDialog)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            return await GetFixAllOperationsAsync(
                codeAction, showPreviewChangesDialog, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        private async Task<CodeAction> GetFixAllCodeActionAsync(FixAllContext fixAllContext)
        {
            using (Logger.LogBlock(
                FunctionId.CodeFixes_FixAllOccurrencesComputation,
                KeyValueLogMessage.Create(LogType.UserAction, m =>
                {
                    m[FixAllLogger.CorrelationId] = fixAllContext.State.CorrelationId;
                    m[FixAllLogger.FixAllScope] = fixAllContext.State.Scope.ToString();
                }),
                fixAllContext.CancellationToken))
            {
                CodeAction action = null;
                try
                {
                    action = await fixAllContext.FixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    FixAllLogger.LogComputationResult(fixAllContext.State.CorrelationId, completed: false);
                }
                finally
                {
                    if (action != null)
                    {
                        FixAllLogger.LogComputationResult(fixAllContext.State.CorrelationId, completed: true);
                    }
                    else
                    {
                        FixAllLogger.LogComputationResult(fixAllContext.State.CorrelationId, completed: false, timedOut: true);
                    }
                }

                return action;
            }
        }

        private async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(
            CodeAction codeAction, bool showPreviewChangesDialog,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            // We have computed the fix all occurrences code fix.
            // Now fetch the new solution with applied fix and bring up the Preview changes dialog.

            var workspace = fixAllState.Project.Solution.Workspace;

            cancellationToken.ThrowIfCancellationRequested();
            var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            if (operations == null)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var newSolution = await codeAction.GetChangedSolutionInternalAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (showPreviewChangesDialog)
            {
                newSolution = PreviewChanges(
                    fixAllState.Project.Solution,
                    newSolution,
                    FeaturesResources.Fix_all_occurrences,
                    codeAction.Title,
                    fixAllState.Project.Language,
                    workspace,
                    fixAllState.CorrelationId,
                    cancellationToken);
                if (newSolution == null)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
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
            int? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (Logger.LogBlock(
                FunctionId.CodeFixes_FixAllOccurrencesPreviewChanges,
                KeyValueLogMessage.Create(LogType.UserAction, m =>
                {
                    // only set when correlation id is given
                    // we might not have this info for suppression
                    if (correlationId.HasValue)
                    {
                        m[FixAllLogger.CorrelationId] = correlationId;
                    }
                }),
                cancellationToken))
            {
                var previewService = workspace.Services.GetService<IPreviewDialogService>();
                var glyph = languageOpt == null
                    ? Glyph.Assembly
                    : languageOpt == LanguageNames.CSharp
                        ? Glyph.CSharpProject
                        : Glyph.BasicProject;

                var changedSolution = previewService.PreviewChanges(
                    string.Format(EditorFeaturesResources.Preview_Changes_0, fixAllPreviewChangesTitle),
                    "vs.codefix.fixall",
                    fixAllTopLevelHeader,
                    fixAllPreviewChangesTitle,
                    glyph,
                    newSolution,
                    currentSolution);

                if (changedSolution == null)
                {
                    // User clicked cancel.
                    FixAllLogger.LogPreviewChangesResult(correlationId, applied: false);
                    return null;
                }

                FixAllLogger.LogPreviewChangesResult(correlationId, applied: true, allChangesApplied: changedSolution == newSolution);
                return changedSolution;
            }
        }

        private static ImmutableArray<CodeActionOperation> GetNewFixAllOperations(ImmutableArray<CodeActionOperation> operations, Solution newSolution, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<CodeActionOperation>.GetInstance();
            var foundApplyChanges = false;
            foreach (var operation in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!foundApplyChanges)
                {
                    if (operation is ApplyChangesOperation applyChangesOperation)
                    {
                        foundApplyChanges = true;
                        result.Add(new ApplyChangesOperation(newSolution));
                        continue;
                    }
                }

                result.Add(operation);
            }

            return result.ToImmutableAndFree();
        }
    }
}
