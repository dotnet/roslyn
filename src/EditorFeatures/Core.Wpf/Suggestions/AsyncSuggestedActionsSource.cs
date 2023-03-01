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
                    var document = range.Snapshot.GetOpenTextDocumentInCurrentContextWithChanges();
                    if (document is null)
                        return;

                    // Keep track of how many actions we've put in the lightbulb at each priority level.  We do
                    // this as each priority level will both sort and inline actions.  However, we don't want to
                    // inline actions at each priority if it's going to make the total number of actions too high.
                    // This does mean we might inline actions from a higher priority group, and then disable 
                    // inlining for lower pri groups.  However, intuitively, that is what we want.  More important
                    // items should be pushed higher up, and less important items shouldn't take up that much space.
                    var currentActionCount = 0;

                    // Builders to store low and medium priority sets that were reported for a higher request priority.
                    // These mismatched priority sets are stored in one of the below pending priority sets, and later added
                    // when we are computing sets with matching request priority.
                    // Note that lowest request priority is reserved for suppression/configuration action sets,
                    // and we expect the code fix service to only return suppression/configuration actions for this
                    // request priority. We have an assert for this invariant below and don't need to track pendingLowestPrioritySets.
                    using var _ = ArrayBuilder<SuggestedActionSet>.GetInstance(out var pendingLowPrioritySets);
                    using var _2 = ArrayBuilder<SuggestedActionSet>.GetInstance(out var pendingMediumPrioritySets);

                    // Collectors are in priority order.  So just walk them from highest to lowest.
                    foreach (var collector in collectors)
                    {
                        var priority = TryGetPriority(collector.Priority);

                        if (priority != null)
                        {
                            // Compute the actions sets for the current request priority.
                            var allSets = GetCodeFixesAndRefactoringsAsync(
                                state, requestedActionCategories, document,
                                range, selection,
                                addOperationScope: _ => null,
                                priority.Value,
                                currentActionCount, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false);

                            // Add those computed actions sets to the current collector set which they have
                            // matching or higher priority compared to the request priority.
                            // For computed action sets with a lower priority compared to the request priority,
                            // add them to the appropriate pending set so it gets added to the collector set in later iteration.
                            await foreach (var set in allSets)
                            {
                                switch (priority)
                                {
                                    case CodeActionRequestPriority.High:
                                        switch (set.Priority)
                                        {
                                            case SuggestedActionSetPriority.High:
                                                break;

                                            case SuggestedActionSetPriority.Medium:
                                                pendingMediumPrioritySets.Add(set);
                                                continue;

                                            case SuggestedActionSetPriority.Low:
                                                pendingLowPrioritySets.Add(set);
                                                continue;

                                            default:
                                                throw ExceptionUtilities.UnexpectedValue(set.Priority);
                                        }

                                        break;

                                    case CodeActionRequestPriority.Normal:
                                        switch (set.Priority)
                                        {
                                            case SuggestedActionSetPriority.High:
                                            case SuggestedActionSetPriority.Medium:
                                                break;

                                            case SuggestedActionSetPriority.Low:
                                                pendingLowPrioritySets.Add(set);
                                                continue;

                                            default:
                                                throw ExceptionUtilities.UnexpectedValue(set.Priority);
                                        }

                                        break;

                                    case CodeActionRequestPriority.Low:
                                        switch (set.Priority)
                                        {
                                            case SuggestedActionSetPriority.High:
                                            case SuggestedActionSetPriority.Medium:
                                            case SuggestedActionSetPriority.Low:
                                                break;

                                            default:
                                                throw ExceptionUtilities.UnexpectedValue(set.Priority);
                                        }

                                        break;

                                    case CodeActionRequestPriority.Lowest:
                                        // We only expect suppression/configuration fixes for 'Lowest' request priority.
                                        // These action sets have 'SuggestedActionSetPriority.None'.
                                        Contract.ThrowIfFalse(set.Priority == SuggestedActionSetPriority.None);
                                        break;

                                    case CodeActionRequestPriority.None:
                                        // Add all the sets for 'None' request priority.
                                        break;

                                    default:
                                        throw ExceptionUtilities.UnexpectedValue(priority);
                                }

                                // We have an action set with matching or higher priority compared to the request priority,
                                // so add it to the collector set.
                                currentActionCount += set.Actions.Count();
                                collector.Add(set);
                            }

                            // Drain any pending actions sets for the next request priority which were computed for a higher request priority.
                            var pendingSetsToDrain = priority switch
                            {
                                CodeActionRequestPriority.High => pendingMediumPrioritySets,
                                CodeActionRequestPriority.Normal => pendingLowPrioritySets,
                                _ => null,
                            };

                            if (pendingSetsToDrain != null)
                            {
                                foreach (var set in pendingSetsToDrain)
                                {
                                    currentActionCount += set.Actions.Count();
                                    collector.Add(set);
                                }
                            }
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
                TextDocument document,
                SnapshotSpan range,
                TextSpan? selection,
                Func<string, IDisposable?> addOperationScope,
                CodeActionRequestPriority priority,
                int currentActionCount,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var workspace = document.Project.Solution.Workspace;
                var supportsFeatureService = workspace.Services.GetRequiredService<ITextBufferSupportsFeatureService>();

                var options = GlobalOptions.GetCodeActionOptionsProvider();

                var fixesTask = GetCodeFixesAsync(
                    state, supportsFeatureService, requestedActionCategories, workspace, document, range,
                    addOperationScope, priority, options, isBlocking: false, cancellationToken);
                var refactoringsTask = GetRefactoringsAsync(
                    state, supportsFeatureService, requestedActionCategories, GlobalOptions, workspace, document, selection,
                    addOperationScope, priority, options, isBlocking: false, cancellationToken);

                await Task.WhenAll(fixesTask, refactoringsTask).ConfigureAwait(false);

                var fixes = await fixesTask.ConfigureAwait(false);
                var refactorings = await refactoringsTask.ConfigureAwait(false);
                foreach (var set in ConvertToSuggestedActionSets(state, selection, fixes, refactorings, currentActionCount))
                    yield return set;
            }
        }
    }
}
