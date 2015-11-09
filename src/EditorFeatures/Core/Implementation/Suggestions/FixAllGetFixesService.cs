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
using Roslyn.Utilities;

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

        public async Task<Solution> GetFixAllChangedSolutionAsync(FixAllProvider fixAllProvider, FixAllContext fixAllContext, string fixAllTitle, string waitDialogMessage)
        {
            // Compute fix all occurrences code fix for the given fix all context.
            // Bring up a cancellable wait dialog.
            bool userCancelled;
            var codeAction = GetFixAllCodeAction(fixAllProvider, fixAllContext, fixAllTitle, waitDialogMessage, out userCancelled);
            if (codeAction == null)
            {
                return userCancelled ? null : fixAllContext.Solution;
            }

            fixAllContext.CancellationToken.ThrowIfCancellationRequested();
            return await codeAction.GetChangedSolutionInternalAsync(cancellationToken: fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<CodeActionOperation>> GetFixAllOperationsAsync(FixAllProvider fixAllProvider, FixAllContext fixAllContext, string fixAllTitle, string waitDialogMessage, bool showPreviewChangesDialog)
        {
            // Compute fix all occurrences code fix for the given fix all context.
            // Bring up a cancellable wait dialog.
            bool userCancelled;
            var codeAction = GetFixAllCodeAction(fixAllProvider, fixAllContext, fixAllTitle, waitDialogMessage, out userCancelled);
            if (codeAction == null)
            {
                return null;
            }

            return await GetFixAllOperationsAsync(codeAction, fixAllContext, fixAllTitle, showPreviewChangesDialog).ConfigureAwait(false);
        }

        private CodeAction GetFixAllCodeAction(FixAllProvider fixAllProvider, FixAllContext fixAllContext, string fixAllTitle, string waitDialogMessage, out bool userCancelled)
        {
            userCancelled = false;

            // Compute fix all occurrences code fix for the given fix all context.
            // Bring up a cancellable wait dialog.
            CodeAction codeAction = null;

            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation, fixAllContext.CancellationToken))
            {
                var result = _waitIndicator.Wait(
                    fixAllTitle,
                    waitDialogMessage,
                    allowCancel: true,
                    action: waitContext =>
                    {
                        fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                        using (var linkedCts =
                            CancellationTokenSource.CreateLinkedTokenSource(waitContext.CancellationToken, fixAllContext.CancellationToken))
                        {
                            try
                            {
                                var fixAllContextWithCancellation = fixAllContext.WithCancellationToken(linkedCts.Token);
                                var fixTask = fixAllProvider.GetFixAsync(fixAllContextWithCancellation);
                                if (fixTask != null)
                                {
                                    codeAction = fixTask.WaitAndGetResult(linkedCts.Token);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                    });

                userCancelled = result == WaitIndicatorResult.Canceled;
                var cancelled = userCancelled || codeAction == null;

                if (cancelled)
                {
                    FixAllLogger.LogComputationResult(completed: false, timedOut: result != WaitIndicatorResult.Canceled);
                    return null;
                }
            }

            FixAllLogger.LogComputationResult(completed: true);
            return codeAction;
        }

        private async Task<IEnumerable<CodeActionOperation>> GetFixAllOperationsAsync(CodeAction codeAction, FixAllContext fixAllContext, string fixAllPreviewChangesTitle, bool showPreviewChangesDialog)
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
                    fixAllPreviewChangesTitle,
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
