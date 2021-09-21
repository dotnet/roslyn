// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        private partial class AsyncSuggestedActionsSource : SuggestedActionsSource, IAsyncSuggestedActionsSource
        {
            public AsyncSuggestedActionsSource(
                IThreadingContext threadingContext,
                IGlobalOptionService globalOptions,
                SuggestedActionsSourceProvider owner,
                ITextView textView,
                ITextBuffer textBuffer,
                ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistry)
                : base(threadingContext, globalOptions, owner, textView, textBuffer, suggestedActionCategoryRegistry)
            {
            }

            public async Task GetSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                ImmutableArray<ISuggestedActionSetCollector> collectors,
                CancellationToken cancellationToken)
            {
                AssertIsForeground();
                using var _ = ArrayBuilder<ISuggestedActionSetCollector>.GetInstance(out var completedCollectors);
                try
                {
                    await GetSuggestedActionsWorkerAsync(
                        requestedActionCategories, range, collectors, completedCollectors, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    // Always ensure that all the collectors are marked as complete so we don't hang the UI.
                    foreach (var collector in collectors)
                    {
                        if (!completedCollectors.Contains(collector))
                            collector.Complete();
                    }
                }
            }

            private async Task GetSuggestedActionsWorkerAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                ImmutableArray<ISuggestedActionSetCollector> collectors,
                ArrayBuilder<ISuggestedActionSetCollector> completedCollectors,
                CancellationToken cancellationToken)
            {
                AssertIsForeground();
                using var state = SourceState.TryAddReference();
                if (state is null)
                    return;

                var workspace = state.Target.Workspace;
                if (workspace is null)
                    return;

                var selection = TryGetCodeRefactoringSelection(state, range);
                await workspace.Services.GetRequiredService<IWorkspaceStatusService>().WaitUntilFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                using (Logger.LogBlock(FunctionId.SuggestedActions_GetSuggestedActionsAsync, cancellationToken))
                {
                    var document = range.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document is null)
                        return;

                    // Collectors are in priority order.  So just walk them from highest to lowest.
                    foreach (var collector in collectors)
                    {
                        var priority = collector.Priority switch
                        {
                            VisualStudio.Utilities.DefaultOrderings.Highest => CodeActionRequestPriority.High,
                            VisualStudio.Utilities.DefaultOrderings.Default => CodeActionRequestPriority.Normal,
                            _ => (CodeActionRequestPriority?)null,
                        };

                        if (priority != null)
                        {
                            // Only request suppression fixes if we're in the lowest priority group.  The other groups
                            // should not show suppressions them as that would cause them to not appear at the end.

                            var allSets = GetCodeFixesAndRefactoringsAsync(
                                state, requestedActionCategories, document,
                                range, selection,
                                addOperationScope: _ => null,
                                includeSuppressionFixes: priority.Value == CodeActionRequestPriority.Normal,
                                priority.Value, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false);

                            await foreach (var set in allSets)
                                collector.Add(set);
                        }

                        // Ensure we always complete the collector even if we didn't add any items to it.
                        // This ensures that we unblock the UI from displaying all the results for that 
                        // priority class.
                        collector.Complete();
                        completedCollectors.Add(collector);
                    }
                }
            }

            private async IAsyncEnumerable<SuggestedActionSet> GetCodeFixesAndRefactoringsAsync(
                ReferenceCountedDisposable<State> state,
                ISuggestedActionCategorySet requestedActionCategories,
                Document document,
                SnapshotSpan range,
                TextSpan? selection,
                Func<string, IDisposable?> addOperationScope,
                bool includeSuppressionFixes,
                CodeActionRequestPriority priority,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var workspace = document.Project.Solution.Workspace;
                var supportsFeatureService = workspace.Services.GetRequiredService<ITextBufferSupportsFeatureService>();

                var fixesTask = GetCodeFixesAsync(
                    state, supportsFeatureService, requestedActionCategories, workspace, document, range,
                    addOperationScope, includeSuppressionFixes, priority, isBlocking: false, cancellationToken);
                var refactoringsTask = GetRefactoringsAsync(
                    state, supportsFeatureService, requestedActionCategories, GlobalOptions, workspace, document, selection,
                    addOperationScope, priority, isBlocking: false, cancellationToken);

                var actionsArray = await Task.WhenAll(fixesTask, refactoringsTask).ConfigureAwait(false);
                foreach (var set in ConvertToSuggestedActionSets(state, selection, fixes: actionsArray[0], refactorings: actionsArray[1]))
                    yield return set;
            }
        }
    }
}
