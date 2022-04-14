// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportWorkspaceServiceFactory(typeof(IFixAllCodeRefactoringGetFixesService), ServiceLayer.Host), Shared]
    internal class FixAllCodeRefactoringGetFixesService : IFixAllCodeRefactoringGetFixesService, IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FixAllCodeRefactoringGetFixesService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => this;

        public async Task<Solution?> GetFixAllChangedSolutionAsync(FixAllContext fixAllContext)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return fixAllContext.Project.Solution;
            }

            fixAllContext.CancellationToken.ThrowIfCancellationRequested();
            return await codeAction.GetChangedSolutionInternalAsync(cancellationToken: fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(FixAllContext fixAllContext, bool showPreviewChangesDialog)
        {
            var codeAction = await GetFixAllCodeActionAsync(fixAllContext).ConfigureAwait(false);
            if (codeAction == null)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            return await FixAllGetFixesServiceHelper.GetFixAllOperationsAsync(
                codeAction, fixAllContext.State.Project, fixAllContext.State.CorrelationId,
                FunctionId.Refactoring_FixAllOccurrencesPreviewChanges,
                showPreviewChangesDialog, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        private static async Task<CodeAction?> GetFixAllCodeActionAsync(FixAllContext fixAllContext)
        {
            using (Logger.LogBlock(
                FunctionId.Refactoring_FixAllOccurrencesComputation,
                KeyValueLogMessage.Create(LogType.UserAction, m =>
                {
                    m[FixAllLogger.CorrelationId] = fixAllContext.State.CorrelationId;
                    m[FixAllLogger.FixAllScope] = fixAllContext.State.FixAllScope.ToString();
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
    }
}
