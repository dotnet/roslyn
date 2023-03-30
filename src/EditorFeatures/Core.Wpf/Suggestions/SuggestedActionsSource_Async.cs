// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        private partial class SuggestedActionsSource : IAsyncSuggestedActionsSource
        {
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

                    // Create a single keep-alive session as we process each lightbulb priority group.  We want to
                    // ensure that all calls to OOP will reuse the same solution-snapshot on the oop side (including
                    // reusing all the same computed compilations that may have been computed on that side.  This is
                    // especially important as we are sending disparate requests for diagnostics, and we do not want the
                    // individual diagnostic requests to redo all the work to run source generators, create skeletons,
                    // etc.
                    using var _1 = RemoteKeepAliveSession.Create(document.Project.Solution, _listener);

                    // Keep track of how many actions we've put in the lightbulb at each priority level.  We do
                    // this as each priority level will both sort and inline actions.  However, we don't want to
                    // inline actions at each priority if it's going to make the total number of actions too high.
                    // This does mean we might inline actions from a higher priority group, and then disable 
                    // inlining for lower pri groups.  However, intuitively, that is what we want.  More important
                    // items should be pushed higher up, and less important items shouldn't take up that much space.
                    var currentActionCount = 0;

                    using var _2 = ArrayBuilder<SuggestedActionSet>.GetInstance(out var lowPrioritySets);

                    CodeActionRequestPriorityProvider? priorityProvider = null;

                    // Collectors are in priority order.  So just walk them from highest to lowest.
                    foreach (var collector in collectors)
                    {
                        var priority = TryGetPriority(collector.Priority);

                        if (priority != null)
                        {
                            priorityProvider = priorityProvider == null
                                ? CodeActionRequestPriorityProvider.Create(priority.Value)
                                : priorityProvider.With(priority.Value);

                            var allSets = GetCodeFixesAndRefactoringsAsync(
                                state, requestedActionCategories, document,
                                range, selection,
                                addOperationScope: _ => null,
                                priorityProvider,
                                currentActionCount, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false);

                            await foreach (var set in allSets)
                            {
                                if (priority == CodeActionRequestPriority.High && set.Priority == SuggestedActionSetPriority.Low)
                                {
                                    // if we're processing the high pri bucket, but we get action sets for lower pri
                                    // groups, then keep track of them and add them in later when we get to that group.
                                    lowPrioritySets.Add(set);
                                }
                                else
                                {
                                    currentActionCount += set.Actions.Count();
                                    collector.Add(set);
                                }
                            }

                            if (priority == CodeActionRequestPriority.Normal)
                            {
                                // now, add any low pri items we've been waiting on to the final group.
                                foreach (var set in lowPrioritySets)
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
                CodeActionRequestPriorityProvider priorityProvider,
                int currentActionCount,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var target = state.Target;
                var owner = target.Owner;
                var subjectBuffer = target.SubjectBuffer;
                var workspace = document.Project.Solution.Workspace;
                var supportsFeatureService = workspace.Services.GetRequiredService<ITextBufferSupportsFeatureService>();

                var options = GlobalOptions.GetCodeActionOptionsProvider();

                var fixesTask = GetCodeFixesAsync();
                var refactoringsTask = GetRefactoringsAsync();

                await Task.WhenAll(fixesTask, refactoringsTask).ConfigureAwait(false);

                var fixes = await fixesTask.ConfigureAwait(false);
                var refactorings = await refactoringsTask.ConfigureAwait(false);

                var filteredSets = UnifiedSuggestedActionsSource.FilterAndOrderActionSets(fixes, refactorings, selection, currentActionCount);
                var convertedSets = filteredSets.Select(s => ConvertToSuggestedActionSet(s)).WhereNotNull().ToImmutableArray();

                foreach (var set in convertedSets)
                    yield return set;

                yield break;

                Task<ImmutableArray<UnifiedSuggestedActionSet>> GetCodeFixesAsync()
                {
                    if (owner._codeFixService == null ||
                        !supportsFeatureService.SupportsCodeFixes(target.SubjectBuffer) ||
                        !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                    {
                        return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();
                    }

                    return UnifiedSuggestedActionsSource.GetFilterAndOrderCodeFixesAsync(
                        workspace, owner._codeFixService, document, range.Span.ToTextSpan(),
                        priorityProvider, options, isBlocking: false, addOperationScope, cancellationToken).AsTask();
                }

                Task<ImmutableArray<UnifiedSuggestedActionSet>> GetRefactoringsAsync()
                {
                    if (!selection.HasValue)
                    {
                        // this is here to fail test and see why it is failed.
                        Trace.WriteLine("given range is not current");
                        return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();
                    }

                    if (!this.GlobalOptions.GetOption(EditorComponentOnOffOptions.CodeRefactorings) ||
                        owner._codeRefactoringService == null ||
                        !supportsFeatureService.SupportsRefactorings(subjectBuffer))
                    {
                        return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();
                    }

                    // 'CodeActionRequestPriority.Lowest' is reserved for suppression/configuration code fixes.
                    // No code refactoring should have this request priority.
                    if (priorityProvider.Priority == CodeActionRequestPriority.Lowest)
                        return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();

                    // If we are computing refactorings outside the 'Refactoring' context, i.e. for example, from the lightbulb under a squiggle or selection,
                    // then we want to filter out refactorings outside the selection span.
                    var filterOutsideSelection = !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring);

                    return UnifiedSuggestedActionsSource.GetFilterAndOrderCodeRefactoringsAsync(
                        workspace, owner._codeRefactoringService, document, selection.Value, priorityProvider.Priority, options,
                        addOperationScope, filterOutsideSelection, cancellationToken);
                }

                [return: NotNullIfNotNull(nameof(unifiedSuggestedActionSet))]
                SuggestedActionSet? ConvertToSuggestedActionSet(UnifiedSuggestedActionSet? unifiedSuggestedActionSet)
                {
                    // May be null in cases involving CodeFixSuggestedActions since FixAllFlavors may be null.
                    if (unifiedSuggestedActionSet == null)
                        return null;

                    var originalSolution = unifiedSuggestedActionSet.OriginalSolution;

                    return new SuggestedActionSet(
                        unifiedSuggestedActionSet.CategoryName,
                        unifiedSuggestedActionSet.Actions.SelectAsArray(set => ConvertToSuggestedAction(set)),
                        unifiedSuggestedActionSet.Title,
                        ConvertToSuggestedActionSetPriority(unifiedSuggestedActionSet.Priority),
                        unifiedSuggestedActionSet.ApplicableToSpan?.ToSpan());

                    ISuggestedAction ConvertToSuggestedAction(IUnifiedSuggestedAction unifiedSuggestedAction)
                        => unifiedSuggestedAction switch
                        {
                            UnifiedCodeFixSuggestedAction codeFixAction => new CodeFixSuggestedAction(
                                ThreadingContext, owner, codeFixAction.Workspace, originalSolution, subjectBuffer,
                                codeFixAction.CodeFix, codeFixAction.Provider, codeFixAction.OriginalCodeAction,
                                ConvertToSuggestedActionSet(codeFixAction.FixAllFlavors)),
                            UnifiedCodeRefactoringSuggestedAction codeRefactoringAction => new CodeRefactoringSuggestedAction(
                                ThreadingContext, owner, codeRefactoringAction.Workspace, originalSolution, subjectBuffer,
                                codeRefactoringAction.CodeRefactoringProvider, codeRefactoringAction.OriginalCodeAction,
                                ConvertToSuggestedActionSet(codeRefactoringAction.FixAllFlavors)),
                            UnifiedFixAllCodeFixSuggestedAction fixAllAction => new FixAllCodeFixSuggestedAction(
                                ThreadingContext, owner, fixAllAction.Workspace, originalSolution, subjectBuffer,
                                fixAllAction.FixAllState, fixAllAction.Diagnostic, fixAllAction.OriginalCodeAction),
                            UnifiedFixAllCodeRefactoringSuggestedAction fixAllCodeRefactoringAction => new FixAllCodeRefactoringSuggestedAction(
                                ThreadingContext, owner, fixAllCodeRefactoringAction.Workspace, originalSolution, subjectBuffer,
                                fixAllCodeRefactoringAction.FixAllState, fixAllCodeRefactoringAction.OriginalCodeAction),
                            UnifiedSuggestedActionWithNestedActions nestedAction => new SuggestedActionWithNestedActions(
                                ThreadingContext, owner, nestedAction.Workspace, originalSolution, subjectBuffer,
                                nestedAction.Provider ?? this, nestedAction.OriginalCodeAction,
                                nestedAction.NestedActionSets.SelectAsArray(s => ConvertToSuggestedActionSet(s))),
                            _ => throw ExceptionUtilities.Unreachable()
                        };
                }

                static SuggestedActionSetPriority ConvertToSuggestedActionSetPriority(CodeActionPriority priority)
                    => priority switch
                    {
                        CodeActionPriority.Lowest => SuggestedActionSetPriority.None,
                        CodeActionPriority.Low => SuggestedActionSetPriority.Low,
                        CodeActionPriority.Medium => SuggestedActionSetPriority.Medium,
                        CodeActionPriority.High => SuggestedActionSetPriority.High,
                        _ => throw ExceptionUtilities.Unreachable(),
                    };
            }
        }
    }
}
