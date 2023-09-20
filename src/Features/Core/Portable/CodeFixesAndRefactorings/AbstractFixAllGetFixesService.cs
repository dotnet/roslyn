// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings
{
    internal abstract class AbstractFixAllGetFixesService : IFixAllGetFixesService
    {
        protected abstract Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(CodeAction codeAction, bool showPreviewChangesDialog, IProgressTracker progressTracker, IFixAllState fixAllState, CancellationToken cancellationToken);

        public async Task<Solution> GetFixAllChangedSolutionAsync(IFixAllContext fixAllContext)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return fixAllContext.Solution;
            }

            fixAllContext.CancellationToken.ThrowIfCancellationRequested();
            return await codeAction.GetChangedSolutionInternalAsync(fixAllContext.Solution, cancellationToken: fixAllContext.CancellationToken).ConfigureAwait(false);
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
                codeAction, showPreviewChangesDialog, fixAllContext.ProgressTracker, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
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
}
