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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Telemetry;
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
                _threadingContext.ThrowIfNotOnUIThread();

                // We should only be called with the orderings we exported in order from highest pri to lowest pri.
                Contract.ThrowIfFalse(Orderings.SequenceEqual(collectors.SelectAsArray(c => c.Priority)));

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
                _threadingContext.ThrowIfNotOnUIThread();
                using var state = _state.TryAddReference();
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
                    using var _1 = await RemoteKeepAliveSession.CreateAsync(document.Project.Solution, cancellationToken).ConfigureAwait(false);

                    // Keep track of how many actions we've put in the lightbulb at each priority level.  We do
                    // this as each priority level will both sort and inline actions.  However, we don't want to
                    // inline actions at each priority if it's going to make the total number of actions too high.
                    // This does mean we might inline actions from a higher priority group, and then disable 
                    // inlining for lower pri groups.  However, intuitively, that is what we want.  More important
                    // items should be pushed higher up, and less important items shouldn't take up that much space.
                    var currentActionCount = 0;

                    using var _ = PooledDictionary<CodeActionRequestPriority, ArrayBuilder<SuggestedActionSet>>.GetInstance(out var pendingActionSets);

                    try
                    {
                        // Keep track of the diagnostic analyzers that have been deprioritized across calls to the
                        // diagnostic engine.  We'll run them once we get around to the low-priority bucket.  We want to
                        // keep track of this *across* calls to each priority. So we create this set outside of the loop and
                        // then pass it continuously from one priority group to the next.
                        var lowPriorityAnalyzers = new ConcurrentSet<DiagnosticAnalyzer>();
                        var lowPriorityAnalyzerSupportedDiagnosticIds = new ConcurrentSet<string>();

                        using var _2 = TelemetryLogging.LogBlockTimeAggregated(FunctionId.SuggestedAction_Summary, $"Total");

                        // Collectors are in priority order.  So just walk them from highest to lowest.
                        foreach (var collector in collectors)
                        {
                            if (TryGetPriority(collector.Priority) is CodeActionRequestPriority priority)
                            {
                                using var _3 = TelemetryLogging.LogBlockTimeAggregated(FunctionId.SuggestedAction_Summary, $"Total.Pri{(int)priority}");

                                var allSets = GetCodeFixesAndRefactoringsAsync(
                                    state, requestedActionCategories, document,
                                    range, selection,
                                    new SuggestedActionPriorityProvider(priority, lowPriorityAnalyzers, lowPriorityAnalyzerSupportedDiagnosticIds),
                                    currentActionCount, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false);

                                await foreach (var set in allSets)
                                {
                                    // Determine the corresponding lightbulb priority class corresponding to the priority
                                    // group the set says it wants to be in.
                                    var actualSetPriority = set.Priority switch
                                    {
                                        SuggestedActionSetPriority.None => CodeActionRequestPriority.Lowest,
                                        SuggestedActionSetPriority.Low => CodeActionRequestPriority.Low,
                                        SuggestedActionSetPriority.Medium => CodeActionRequestPriority.Default,
                                        SuggestedActionSetPriority.High => CodeActionRequestPriority.High,
                                        _ => throw ExceptionUtilities.UnexpectedValue(set.Priority),
                                    };

                                    // if the actual priority class is lower than the one we're currently in, then hold onto
                                    // this set for later, and place it in that priority group once we get there.
                                    if (actualSetPriority < priority)
                                    {
                                        var builder = pendingActionSets.GetOrAdd(actualSetPriority, _ => ArrayBuilder<SuggestedActionSet>.GetInstance());
                                        builder.Add(set);
                                    }
                                    else
                                    {
                                        currentActionCount += set.Actions.Count();
                                        collector.Add(set);
                                    }
                                }

                                // We're finishing up with a particular priority group, and we're about to go to a priority
                                // group one lower than what we have (hence `priority - 1`).  Take any pending items in the
                                // group we're *about* to go into and add them at the end of this group.
                                //
                                // For example, if we're in the high group, and we have an pending items in the normal
                                // bucket, then add them at the end of the high group.  The reason for this is that we
                                // already have computed the items and we don't want to force them to have to wait for all
                                // the processing in their own group to show up.  i.e. imagine if we added at the start of
                                // the next group.  They'd be in the same location in the lightbulb as when we add at the
                                // end of the current group, but they'd show up only when that group totally finished,
                                // instead of right now.
                                //
                                // This is critical given that the lower pri groups are often much lower (which is why they
                                // they choose to be in that class).  We don't want a fast item computed by a higher pri
                                // provider to still have to wait on those slow items.
                                if (pendingActionSets.TryGetValue(priority - 1, out var setBuilder))
                                {
                                    foreach (var set in setBuilder)
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
                    finally
                    {
                        foreach (var (_, builder) in pendingActionSets)
                            builder.Free();
                    }
                }
            }

            private async IAsyncEnumerable<SuggestedActionSet> GetCodeFixesAndRefactoringsAsync(
                ReferenceCountedDisposable<State> state,
                ISuggestedActionCategorySet requestedActionCategories,
                TextDocument document,
                SnapshotSpan range,
                TextSpan? selection,
                ICodeActionRequestPriorityProvider priorityProvider,
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
                var convertedSets = filteredSets.Select(s => ConvertToSuggestedActionSet(s, document)).WhereNotNull().ToImmutableArray();

                foreach (var set in convertedSets)
                    yield return set;

                yield break;

                async Task<ImmutableArray<UnifiedSuggestedActionSet>> GetCodeFixesAsync()
                {
                    using var _ = TelemetryLogging.LogBlockTimeAggregated(FunctionId.SuggestedAction_Summary, $"Total.Pri{priorityProvider.Priority.GetPriorityInt()}.{nameof(GetCodeFixesAsync)}");

                    if (owner._codeFixService == null ||
                        !supportsFeatureService.SupportsCodeFixes(target.SubjectBuffer) ||
                        !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                    {
                        return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
                    }

                    return await UnifiedSuggestedActionsSource.GetFilterAndOrderCodeFixesAsync(
                        workspace, owner._codeFixService, document, range.Span.ToTextSpan(),
                        priorityProvider, options, cancellationToken).ConfigureAwait(false);
                }

                async Task<ImmutableArray<UnifiedSuggestedActionSet>> GetRefactoringsAsync()
                {
                    using var _ = TelemetryLogging.LogBlockTimeAggregated(FunctionId.SuggestedAction_Summary, $"Total.Pri{priorityProvider.Priority.GetPriorityInt()}.{nameof(GetRefactoringsAsync)}");

                    if (!selection.HasValue)
                    {
                        // this is here to fail test and see why it is failed.
                        Trace.WriteLine("given range is not current");
                        return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
                    }

                    if (!this.GlobalOptions.GetOption(EditorComponentOnOffOptions.CodeRefactorings) ||
                        owner._codeRefactoringService == null ||
                        !supportsFeatureService.SupportsRefactorings(subjectBuffer))
                    {
                        return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
                    }

                    // 'CodeActionRequestPriority.Lowest' is reserved for suppression/configuration code fixes.
                    // No code refactoring should have this request priority.
                    if (priorityProvider.Priority == CodeActionRequestPriority.Lowest)
                        return ImmutableArray<UnifiedSuggestedActionSet>.Empty;

                    // If we are computing refactorings outside the 'Refactoring' context, i.e. for example, from the lightbulb under a squiggle or selection,
                    // then we want to filter out refactorings outside the selection span.
                    var filterOutsideSelection = !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring);

                    return await UnifiedSuggestedActionsSource.GetFilterAndOrderCodeRefactoringsAsync(
                        workspace, owner._codeRefactoringService, document, selection.Value, priorityProvider.Priority, options,
                        filterOutsideSelection, cancellationToken).ConfigureAwait(false);
                }

                [return: NotNullIfNotNull(nameof(unifiedSuggestedActionSet))]
                SuggestedActionSet? ConvertToSuggestedActionSet(UnifiedSuggestedActionSet? unifiedSuggestedActionSet, TextDocument originalDocument)
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
                                _threadingContext, owner, codeFixAction.Workspace, originalDocument, subjectBuffer,
                                codeFixAction.CodeFix, codeFixAction.Provider, codeFixAction.OriginalCodeAction,
                                ConvertToSuggestedActionSet(codeFixAction.FixAllFlavors, originalDocument)),
                            UnifiedCodeRefactoringSuggestedAction codeRefactoringAction => new CodeRefactoringSuggestedAction(
                                _threadingContext, owner, codeRefactoringAction.Workspace, originalDocument, subjectBuffer,
                                codeRefactoringAction.CodeRefactoringProvider, codeRefactoringAction.OriginalCodeAction,
                                ConvertToSuggestedActionSet(codeRefactoringAction.FixAllFlavors, originalDocument)),
                            UnifiedFixAllCodeFixSuggestedAction fixAllAction => new FixAllCodeFixSuggestedAction(
                                _threadingContext, owner, fixAllAction.Workspace, originalSolution, subjectBuffer,
                                fixAllAction.FixAllState, fixAllAction.Diagnostic, fixAllAction.OriginalCodeAction),
                            UnifiedFixAllCodeRefactoringSuggestedAction fixAllCodeRefactoringAction => new FixAllCodeRefactoringSuggestedAction(
                                _threadingContext, owner, fixAllCodeRefactoringAction.Workspace, originalSolution, subjectBuffer,
                                fixAllCodeRefactoringAction.FixAllState, fixAllCodeRefactoringAction.OriginalCodeAction),
                            UnifiedSuggestedActionWithNestedActions nestedAction => new SuggestedActionWithNestedActions(
                                _threadingContext, owner, nestedAction.Workspace, originalSolution, subjectBuffer,
                                nestedAction.Provider ?? this, nestedAction.OriginalCodeAction,
                                nestedAction.NestedActionSets.SelectAsArray(s => ConvertToSuggestedActionSet(s, originalDocument))),
                            _ => throw ExceptionUtilities.Unreachable()
                        };
                }

                static SuggestedActionSetPriority ConvertToSuggestedActionSetPriority(CodeActionPriority priority)
                    => priority switch
                    {
                        CodeActionPriority.Lowest => SuggestedActionSetPriority.None,
                        CodeActionPriority.Low => SuggestedActionSetPriority.Low,
                        CodeActionPriority.Default => SuggestedActionSetPriority.Medium,
                        CodeActionPriority.High => SuggestedActionSetPriority.High,
                        _ => throw ExceptionUtilities.Unreachable(),
                    };
            }
        }
    }
}
