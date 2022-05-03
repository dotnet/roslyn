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
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportWorkspaceServiceFactory(typeof(IFixAllGetFixesService), ServiceLayer.Host), Shared]
    internal class FixAllGetFixesService : IFixAllGetFixesService, IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FixAllGetFixesService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => this;

        public async Task<Solution> GetFixAllChangedSolutionAsync(IFixAllContext fixAllContext)
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
            IFixAllContext fixAllContext, bool showPreviewChangesDialog)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            return await GetFixAllOperationsAsync(
                codeAction, showPreviewChangesDialog, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        private static async Task<CodeAction> GetFixAllCodeActionAsync(IFixAllContext fixAllContext)
        {
            var fixAllKind = fixAllContext.State.FixAllKind;
            var functionId = fixAllKind switch
            {
                FixAllKind.CodeFix => FunctionId.CodeFixes_FixAllOccurrencesComputation,
                FixAllKind.Refactoring => FunctionId.Refactoring_FixAllOccurrencesComputation,
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllKind)
            };

            using (Logger.LogBlock(
                functionId,
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
                    FixAllLogger.LogComputationResult(fixAllKind, fixAllContext.State.CorrelationId, completed: false);
                }
                finally
                {
                    if (action != null)
                    {
                        FixAllLogger.LogComputationResult(fixAllKind, fixAllContext.State.CorrelationId, completed: true);
                    }
                    else
                    {
                        FixAllLogger.LogComputationResult(fixAllKind, fixAllContext.State.CorrelationId, completed: false, timedOut: true);
                    }
                }

                return action;
            }
        }

        private static async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(
            CodeAction codeAction, bool showPreviewChangesDialog,
            IFixAllState fixAllState, CancellationToken cancellationToken)
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
                    fixAllState.FixAllKind,
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
            FixAllKind fixAllKind,
            string languageOpt,
            Workspace workspace,
            int? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var functionId = fixAllKind switch
            {
                FixAllKind.CodeFix => FunctionId.CodeFixes_FixAllOccurrencesPreviewChanges,
                FixAllKind.Refactoring => FunctionId.Refactoring_FixAllOccurrencesPreviewChanges,
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllKind)
            };

            using (Logger.LogBlock(
                functionId,
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
                var glyph = languageOpt == null
                    ? Glyph.Assembly
                    : languageOpt == LanguageNames.CSharp
                        ? Glyph.CSharpProject
                        : Glyph.BasicProject;
#if COCOA

                var previewService = workspace.Services.GetService<IPreviewDialogService>();

                // Until IPreviewDialogService is implemented, just execute all changes without user ability to pick and choose
                if (previewService == null)
                    return newSolution;
#else

                var previewService = workspace.Services.GetRequiredService<IPreviewDialogService>();

#endif

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
                    FixAllLogger.LogPreviewChangesResult(fixAllKind, correlationId, applied: false);
                    return null;
                }

                FixAllLogger.LogPreviewChangesResult(fixAllKind, correlationId, applied: true, allChangesApplied: changedSolution == newSolution);
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
                    if (operation is ApplyChangesOperation)
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
