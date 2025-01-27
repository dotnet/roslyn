// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal abstract class AbstractFixAllGetFixesService : IFixAllGetFixesService
{
    protected abstract Solution? GetChangedSolution(
        Workspace workspace,
        Solution currentSolution,
        Solution newSolution,
        string fixAllPreviewChangesTitle,
        string fixAllTopLevelHeader,
        Glyph glyph);

    public async Task<Solution?> GetFixAllChangedSolutionAsync(IFixAllContext fixAllContext)
    {
        var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
        if (codeAction == null)
        {
            return fixAllContext.Solution;
        }

        fixAllContext.CancellationToken.ThrowIfCancellationRequested();
        return await codeAction.GetChangedSolutionInternalAsync(fixAllContext.Solution, fixAllContext.Progress, cancellationToken: fixAllContext.CancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(
        IFixAllContext fixAllContext, bool showPreviewChangesDialog)
    {
        var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
        if (codeAction == null)
        {
            return [];
        }

        return await GetFixAllOperationsAsync(
            codeAction, showPreviewChangesDialog, fixAllContext.Progress, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
    }

    protected async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(
        CodeAction codeAction,
        bool showPreviewChangesDialog,
        IProgress<CodeAnalysisProgress> progressTracker,
        IFixAllState fixAllState,
        CancellationToken cancellationToken)
    {
        // We have computed the fix all occurrences code fix.
        // Now fetch the new solution with applied fix and bring up the Preview changes dialog.

        var workspace = fixAllState.Project.Solution.Workspace;

        cancellationToken.ThrowIfCancellationRequested();
        var operations = await codeAction.GetOperationsAsync(
            fixAllState.Solution, progressTracker, cancellationToken).ConfigureAwait(false);
        if (operations == null)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        var newSolution = await codeAction.GetChangedSolutionInternalAsync(
            fixAllState.Solution, progressTracker, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (newSolution is null)
        {
            // No changed documents
            return [];
        }

        if (showPreviewChangesDialog)
        {
            newSolution = PreviewChanges(
                workspace,
                fixAllState.Project.Solution,
                newSolution,
                fixAllState.FixAllKind,
                FeaturesResources.Fix_all_occurrences,
                codeAction.Title,
                fixAllState.Project.Language,
                fixAllState.CorrelationId,
                cancellationToken);
            if (newSolution == null)
            {
                return [];
            }
        }

        // Get a code action, with apply changes operation replaced with the newSolution.
        return GetNewFixAllOperations(operations, newSolution, cancellationToken);
    }

    public Solution? PreviewChanges(
        Workspace workspace,
        Solution currentSolution,
        Solution newSolution,
        FixAllKind fixAllKind,
        string previewChangesTitle,
        string topLevelHeader,
        string? language,
        int? correlationId,
        CancellationToken cancellationToken)
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
            var glyph = language == null
                ? Glyph.Assembly
                : language == LanguageNames.CSharp
                    ? Glyph.CSharpProject
                    : Glyph.BasicProject;

            var changedSolution = GetChangedSolution(
                workspace, currentSolution, newSolution, previewChangesTitle, topLevelHeader, glyph);

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

    private static async Task<CodeAction?> GetFixAllCodeActionAsync(IFixAllContext fixAllContext)
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
            CodeAction? action = null;
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

    protected static ImmutableArray<CodeActionOperation> GetNewFixAllOperations(ImmutableArray<CodeActionOperation> operations, Solution newSolution, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<CodeActionOperation>.GetInstance(operations.Length, out var result);
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

        return result.ToImmutableAndClear();
    }
}
