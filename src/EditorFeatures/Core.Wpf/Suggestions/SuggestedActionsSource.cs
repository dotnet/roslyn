// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using CodeFixGroupKey = System.Tuple<Microsoft.CodeAnalysis.Diagnostics.DiagnosticData, Microsoft.CodeAnalysis.CodeActions.CodeActionPriority, Microsoft.CodeAnalysis.CodeActions.CodeActionPriority?>;
using IUIThreadOperationContext = Microsoft.VisualStudio.Utilities.IUIThreadOperationContext;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        private class SuggestedActionsSource : ForegroundThreadAffinitizedObject, ISuggestedActionsSource3
        {
            private readonly ISuggestedActionCategoryRegistryService _suggestedActionCategoryRegistry;

            // state that will be only reset when source is disposed.
            private SuggestedActionsSourceProvider _owner;
            private ITextView _textView;
            private ITextBuffer _subjectBuffer;
            private WorkspaceRegistration _registration;

            // mutable state
            private Workspace? _workspace;
            private IWorkspaceStatusService? _workspaceStatusService;
            private int _lastSolutionVersionReported;

            public event EventHandler<EventArgs>? SuggestedActionsChanged;

            public SuggestedActionsSource(
                IThreadingContext threadingContext,
                SuggestedActionsSourceProvider owner,
                ITextView textView,
                ITextBuffer textBuffer,
                ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistry)
                : base(threadingContext)
            {
                _owner = owner;
                _textView = textView;
                _textView.Closed += OnTextViewClosed;
                _subjectBuffer = textBuffer;
                _suggestedActionCategoryRegistry = suggestedActionCategoryRegistry;
                _registration = Workspace.GetWorkspaceRegistration(textBuffer.AsTextContainer());

                _lastSolutionVersionReported = InvalidSolutionVersion;
                var updateSource = (IDiagnosticUpdateSource)_owner._diagnosticService;
                updateSource.DiagnosticsUpdated += OnDiagnosticsUpdated;

                RegisterEventsToWorkspace(_registration.Workspace);

                _registration.WorkspaceChanged += OnWorkspaceChanged;
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    var updateSource = (IDiagnosticUpdateSource)_owner._diagnosticService;
                    updateSource.DiagnosticsUpdated -= OnDiagnosticsUpdated;
                }

                if (_workspaceStatusService != null)
                {
                    _workspaceStatusService.StatusChanged -= OnWorkspaceStatusChanged;
                }

                if (_workspace != null)
                {
                    _workspace.DocumentActiveContextChanged -= OnActiveContextChanged;
                }

                if (_registration != null)
                {
                    _registration.WorkspaceChanged -= OnWorkspaceChanged;
                }

                if (_textView != null)
                {
                    _textView.Closed -= OnTextViewClosed;
                }

                _owner = null!;
                _workspace = null;
                _workspaceStatusService = null;
                _registration = null!;
                _textView = null!;
                _subjectBuffer = null!;
            }

            private bool IsDisposed => _subjectBuffer == null;

            public bool TryGetTelemetryId(out Guid telemetryId)
            {
                telemetryId = default;

                var workspace = _workspace;
                if (workspace == null || _subjectBuffer == null)
                {
                    return false;
                }

                var documentId = workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());
                if (documentId == null)
                {
                    return false;
                }

                var project = workspace.CurrentSolution.GetProject(documentId.ProjectId);
                if (project == null)
                {
                    return false;
                }

                switch (project.Language)
                {
                    case LanguageNames.CSharp:
                        telemetryId = s_CSharpSourceGuid;
                        return true;
                    case LanguageNames.VisualBasic:
                        telemetryId = s_visualBasicSourceGuid;
                        return true;
                    case "Xaml":
                        telemetryId = s_xamlSourceGuid;
                        return true;
                    default:
                        return false;
                }
            }

            public IEnumerable<SuggestedActionSet>? GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken)
                => GetSuggestedActions(requestedActionCategories, range, operationContext: null, cancellationToken);

            public IEnumerable<SuggestedActionSet>? GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                IUIThreadOperationContext operationContext)
            {
                return GetSuggestedActions(
                    requestedActionCategories,
                    range,
                    operationContext,
                    operationContext.UserCancellationToken);
            }

            public IEnumerable<SuggestedActionSet>? GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                IUIThreadOperationContext? operationContext,
                CancellationToken cancellationToken)
            {
                AssertIsForeground();

                if (IsDisposed)
                {
                    return null;
                }

                if (_workspaceStatusService != null)
                {
                    using (operationContext?.AddScope(allowCancellation: true, description: EditorFeaturesWpfResources.Gathering_Suggestions_Waiting_for_the_solution_to_fully_load))
                    {
                        // This needs to run under threading context otherwise, we can deadlock on VS
                        ThreadingContext.JoinableTaskFactory.Run(() => _workspaceStatusService.WaitUntilFullyLoadedAsync(cancellationToken));
                    }
                }

                using (Logger.LogBlock(FunctionId.SuggestedActions_GetSuggestedActions, cancellationToken))
                {
                    var document = range.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        // this is here to fail test and see why it is failed.
                        Trace.WriteLine("given range is not current");
                        return null;
                    }

                    var workspace = document.Project.Solution.Workspace;
                    var supportsFeatureService = workspace.Services.GetRequiredService<ITextBufferSupportsFeatureService>();

                    var selectionOpt = TryGetCodeRefactoringSelection(range);

                    Func<string, IDisposable?> addOperationScope =
                        description => operationContext?.AddScope(allowCancellation: true, string.Format(EditorFeaturesWpfResources.Gathering_Suggestions_0, description));

                    var fixes = GetCodeFixes(supportsFeatureService, requestedActionCategories, workspace, document, range, addOperationScope, cancellationToken);
                    var refactorings = GetRefactorings(supportsFeatureService, requestedActionCategories, workspace, document, selectionOpt, addOperationScope, cancellationToken);

                    // Get the initial set of action sets, with refactorings and fixes appropriately
                    // ordered against each other.
                    var result = UnifiedSuggestedActionSource.GetInitiallyOrderedActionSets(selectionOpt, fixes, refactorings);
                    if (result.IsEmpty)
                    {
                        return null;
                    }

                    // Now that we have the entire set of action sets, inline, sort and filter
                    // them appropriately against each other.
                    var allActionSets = UnifiedSuggestedActionSource.InlineActionSetsIfDesirable(result);
                    var orderedActionSets = UnifiedSuggestedActionSource.OrderActionSets(allActionSets, selectionOpt);
                    var filteredSets = UnifiedSuggestedActionSource.FilterActionSetsByTitle(orderedActionSets);

                    return filteredSets.Select(s => ConvertToSuggestedActionSet(s));
                }
            }

            private SuggestedActionSet ConvertToSuggestedActionSet(UnifiedSuggestedActionSet unifiedSuggestedActionSet)
            {
                using var _ = ArrayBuilder<ISuggestedAction>.GetInstance(out var suggestedActions);
                foreach (var action in unifiedSuggestedActionSet.Actions)
                {
                    suggestedActions.Add(ConvertToSuggestedAction(action));
                }

                Span? applicableToSpan = null;
                if (unifiedSuggestedActionSet.ApplicableToSpan.HasValue)
                {
                    applicableToSpan = unifiedSuggestedActionSet.ApplicableToSpan.Value.ToSpan();
                }

                return new SuggestedActionSet(
                    categoryName: unifiedSuggestedActionSet.CategoryName,
                    actions: suggestedActions,
                    title: unifiedSuggestedActionSet.Title,
                    priority: ConvertToSuggestedActionSetPriority(unifiedSuggestedActionSet.Priority),
                    applicableToSpan: applicableToSpan);

                // Local functions
                ISuggestedAction ConvertToSuggestedAction(IUnifiedSuggestedAction unifiedSuggestedAction)
                    => unifiedSuggestedAction switch
                    {
                        UnifiedCodeFixSuggestedAction codeFixAction => new CodeFixSuggestedAction(
                            ThreadingContext, _owner, codeFixAction.Workspace, _subjectBuffer,
                            codeFixAction.CodeFix, codeFixAction.Provider, codeFixAction.CodeAction,
                            codeFixAction.FixAllFlavors == null ? null : ConvertToSuggestedActionSet(codeFixAction.FixAllFlavors)),
                        UnifiedCodeRefactoringSuggestedAction codeRefactoringAction => new CodeRefactoringSuggestedAction(
                            ThreadingContext, _owner, codeRefactoringAction.Workspace, _subjectBuffer,
                            codeRefactoringAction.CodeRefactoringProvider, codeRefactoringAction.CodeAction),
                        UnifiedFixAllSuggestedAction fixAllAction => new FixAllSuggestedAction(
                            ThreadingContext, _owner, fixAllAction.Workspace, _subjectBuffer,
                            fixAllAction.FixAllState, fixAllAction.Diagnostic, fixAllAction.CodeAction),
                        UnifiedSuggestedActionWithNestedActions nestedAction => new SuggestedActionWithNestedActions(
                            ThreadingContext, _owner, nestedAction.Workspace, _subjectBuffer,
                            nestedAction.Provider ?? this, nestedAction.CodeAction,
                            nestedAction.NestedActionSets.Select(s => ConvertToSuggestedActionSet(s)).ToImmutableArray()),
                        _ => throw ExceptionUtilities.Unreachable
                    };

                static SuggestedActionSetPriority ConvertToSuggestedActionSetPriority(
                    UnifiedSuggestedActionSetPriority unifiedSuggestedActionSetPriority)
                    => unifiedSuggestedActionSetPriority switch
                    {
                        UnifiedSuggestedActionSetPriority.None => SuggestedActionSetPriority.None,
                        UnifiedSuggestedActionSetPriority.Low => SuggestedActionSetPriority.Low,
                        UnifiedSuggestedActionSetPriority.Medium => SuggestedActionSetPriority.Medium,
                        UnifiedSuggestedActionSetPriority.High => SuggestedActionSetPriority.High,
                        _ => throw ExceptionUtilities.Unreachable,
                    };
            }

            private ImmutableArray<UnifiedSuggestedActionSet> GetCodeFixes(
                ITextBufferSupportsFeatureService supportsFeatureService,
                ISuggestedActionCategorySet requestedActionCategories,
                Workspace workspace,
                Document document,
                SnapshotSpan range,
                Func<string, IDisposable?> addOperationScope,
                CancellationToken cancellationToken)
            {
                this.AssertIsForeground();

                if (_owner._codeFixService != null &&
                    supportsFeatureService.SupportsCodeFixes(_subjectBuffer) &&
                    requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                {
                    // Make sure we include the suppression fixes even when the light bulb is only asking for only code fixes.
                    // See https://github.com/dotnet/roslyn/issues/29589
                    const bool includeSuppressionFixes = true;

                    var fixes = Task.Run(
                        () => _owner._codeFixService.GetFixesAsync(
                                document, range.Span.ToTextSpan(), includeSuppressionFixes, isBlocking: true, addOperationScope, cancellationToken),
                        cancellationToken).WaitAndGetResult(cancellationToken);

                    var filteredFixes = FilterOnUIThread(fixes, workspace);

                    return OrganizeFixes(workspace, filteredFixes, includeSuppressionFixes);
                }

                return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
            }

            private ImmutableArray<CodeFixCollection> FilterOnUIThread(
                ImmutableArray<CodeFixCollection> collections, Workspace workspace)
            {
                this.AssertIsForeground();

                return collections.Select(c => FilterOnUIThread(c, workspace)).WhereNotNull().ToImmutableArray();
            }

            private CodeFixCollection? FilterOnUIThread(
                CodeFixCollection collection,
                Workspace workspace)
            {
                this.AssertIsForeground();

                var applicableFixes = collection.Fixes.WhereAsArray(f => IsApplicable(f.Action, workspace));
                return applicableFixes.Length == 0
                    ? null
                    : applicableFixes.Length == collection.Fixes.Length
                        ? collection
                        : new CodeFixCollection(
                            collection.Provider, collection.TextSpan, applicableFixes,
                            collection.FixAllState, collection.SupportedScopes, collection.FirstDiagnostic);
            }

            private bool IsApplicable(CodeAction action, Workspace workspace)
            {
                if (!action.PerformFinalApplicabilityCheck)
                {
                    // If we don't even need to perform the final applicability check,
                    // then the code action is applicable.
                    return true;
                }

                // Otherwise, defer to the action to make the decision.
                this.AssertIsForeground();
                return action.IsApplicable(workspace);
            }

            private ImmutableArray<CodeRefactoring> FilterOnUIThread(ImmutableArray<CodeRefactoring> refactorings, TextSpan selection, bool filterOutsideSelection, Workspace workspace)
                => refactorings.Select(r => FilterOnUIThread(r, selection, filterOutsideSelection, workspace)).WhereNotNull().ToImmutableArray();

            private CodeRefactoring? FilterOnUIThread(CodeRefactoring refactoring, TextSpan selection, bool filterOutsideSelection, Workspace workspace)
            {
                var actions = refactoring.CodeActions.WhereAsArray(IsActionAndSpanApplicable);
                return actions.Length == 0
                    ? null
                    : actions.Length == refactoring.CodeActions.Length
                        ? refactoring
                        : new CodeRefactoring(refactoring.Provider, actions);

                bool IsActionAndSpanApplicable((CodeAction action, TextSpan? applicableSpan) actionAndSpan)
                {
                    if (!IsApplicable(actionAndSpan.action, workspace))
                    {
                        return false;
                    }

                    if (filterOutsideSelection)
                    {
                        // Filter out refactorings with applicable span outside the selection span.
                        if (!actionAndSpan.applicableSpan.HasValue ||
                            !selection.IntersectsWith(actionAndSpan.applicableSpan.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            /// <summary>
            /// Arrange fixes into groups based on the issue (diagnostic being fixed) and prioritize these groups.
            /// </summary>
            private ImmutableArray<UnifiedSuggestedActionSet> OrganizeFixes(
                Workspace workspace, ImmutableArray<CodeFixCollection> fixCollections,
                bool includeSuppressionFixes)
            {
                var map = ImmutableDictionary.CreateBuilder<CodeFixGroupKey, IList<UnifiedSuggestedAction>>();
                using var orderDisposer = ArrayBuilder<CodeFixGroupKey>.GetInstance(out var order);

                // First group fixes by diagnostic and priority.
                GroupFixes(workspace, fixCollections, map, order, includeSuppressionFixes);

                // Then prioritize between the groups.
                return PrioritizeFixGroups(map.ToImmutable(), order.ToImmutable(), workspace);
            }

            /// <summary>
            /// Groups fixes by the diagnostic being addressed by each fix.
            /// </summary>
            private void GroupFixes(
                Workspace workspace,
                ImmutableArray<CodeFixCollection> fixCollections,
                IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order,
                bool includeSuppressionFixes)
            {
                foreach (var fixCollection in fixCollections)
                {
                    ProcessFixCollection(
                        workspace, map, order, includeSuppressionFixes, fixCollection);
                }
            }

            private void ProcessFixCollection(
                Workspace workspace,
                IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order,
                bool includeSuppressionFixes,
                CodeFixCollection fixCollection)
            {
                var fixes = fixCollection.Fixes;
                var fixCount = fixes.Length;

                UnifiedSuggestedActionSet? getFixAllSuggestedActionSet(CodeAction codeAction) => GetUnifiedFixAllSuggestedActionSet(
                        codeAction, fixCount, fixCollection.FixAllState,
                        fixCollection.SupportedScopes, fixCollection.FirstDiagnostic,
                        workspace);

                var nonSupressionCodeFixes = fixes.WhereAsArray(f => !IsTopLevelSuppressionAction(f.Action));
                var supressionCodeFixes = fixes.WhereAsArray(f => IsTopLevelSuppressionAction(f.Action));

                AddCodeActions(workspace, map, order, fixCollection,
                    getFixAllSuggestedActionSet, nonSupressionCodeFixes);

                // Add suppression fixes to the end of a given SuggestedActionSet so that they
                // always show up last in a group.
                if (includeSuppressionFixes)
                {
                    AddCodeActions(workspace, map, order, fixCollection,
                        getFixAllSuggestedActionSet, supressionCodeFixes);
                }
            }

            private static bool IsTopLevelSuppressionAction(CodeAction action)
                => action is AbstractConfigurationActionWithNestedActions;

            private static bool IsBulkConfigurationAction(CodeAction action)
                => (action as AbstractConfigurationActionWithNestedActions)?.IsBulkConfigurationAction == true;

            private void AddCodeActions(
                Workspace workspace, IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order, CodeFixCollection fixCollection,
                Func<CodeAction, UnifiedSuggestedActionSet?> getFixAllSuggestedActionSet,
                ImmutableArray<CodeFix> codeFixes)
            {
                foreach (var fix in codeFixes)
                {
                    var unifiedSuggestedAction = GetUnifiedSuggestedAction(fix.Action, fix);
                    AddFix(fix, unifiedSuggestedAction, map, order);
                }

                return;

                // Local functions
                UnifiedSuggestedAction GetUnifiedSuggestedAction(CodeAction action, CodeFix fix)
                {
                    if (action.NestedCodeActions.Length > 0)
                    {
                        var nestedActions = action.NestedCodeActions.SelectAsArray(
                            nestedAction => GetUnifiedSuggestedAction(nestedAction, fix));

                        var set = new UnifiedSuggestedActionSet(categoryName: null,
                            actions: nestedActions,
                            priority: UnifiedSuggestedActionSource.GetUnifiedSuggestedActionSetPriority(action.Priority),
                            applicableToSpan: fix.PrimaryDiagnostic.Location.SourceSpan);

                        return new UnifiedSuggestedActionWithNestedActions(
                            workspace, fixCollection.Provider, action, set);
                    }
                    else
                    {
                        return new UnifiedCodeFixSuggestedAction(
                            workspace, fix, fixCollection.Provider,
                            action, getFixAllSuggestedActionSet(action));
                    }
                }
            }

            private static void AddFix(
                CodeFix fix, UnifiedSuggestedAction suggestedAction,
                IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order)
            {
                var groupKey = GetGroupKey(fix);
                if (!map.ContainsKey(groupKey))
                {
                    order.Add(groupKey);
                    map[groupKey] = ImmutableArray.CreateBuilder<UnifiedSuggestedAction>();
                }

                map[groupKey].Add(suggestedAction);

                static CodeFixGroupKey GetGroupKey(CodeFix fix)
                {
                    var diag = fix.GetPrimaryDiagnosticData();
                    if (fix.Action is AbstractConfigurationActionWithNestedActions configurationAction)
                    {
                        return new CodeFixGroupKey(diag, configurationAction.Priority, configurationAction.AdditionalPriority);
                    }

                    return new CodeFixGroupKey(diag, fix.Action.Priority, null);
                }
            }

            /// <summary>
            /// If the provided fix all context is non-null and the context's code action Id matches the given code action's Id then,
            /// returns the set of fix all occurrences actions associated with the code action.
            /// </summary>
            internal UnifiedSuggestedActionSet? GetUnifiedFixAllSuggestedActionSet(
                CodeAction action,
                int actionCount,
                FixAllState fixAllState,
                ImmutableArray<FixAllScope> supportedScopes,
                Diagnostic firstDiagnostic,
                Workspace workspace)
            {

                if (fixAllState == null)
                {
                    return null;
                }

                if (actionCount > 1 && action.EquivalenceKey == null)
                {
                    return null;
                }

                using var fixAllSuggestedActionsDisposer = ArrayBuilder<UnifiedFixAllSuggestedAction>.GetInstance(
                    out var fixAllSuggestedActions);
                foreach (var scope in supportedScopes)
                {
                    var fixAllStateForScope = fixAllState.WithScopeAndEquivalenceKey(scope, action.EquivalenceKey);
                    var fixAllSuggestedAction = new UnifiedFixAllSuggestedAction(
                        workspace, fixAllStateForScope, firstDiagnostic, action);

                    fixAllSuggestedActions.Add(fixAllSuggestedAction);
                }

                return new UnifiedSuggestedActionSet(
                    categoryName: null,
                    actions: fixAllSuggestedActions.ToImmutable(),
                    title: EditorFeaturesResources.Fix_all_occurrences_in);
            }

            /// <summary>
            /// Return prioritized set of fix groups such that fix group for suppression always show up at the bottom of the list.
            /// </summary>
            /// <remarks>
            /// Fix groups are returned in priority order determined based on <see cref="ExtensionOrderAttribute"/>.
            /// Priority for all <see cref="SuggestedActionSet"/>s containing fixes is set to <see cref="SuggestedActionSetPriority.Medium"/> by default.
            /// The only exception is the case where a <see cref="SuggestedActionSet"/> only contains suppression fixes -
            /// the priority of such <see cref="SuggestedActionSet"/>s is set to <see cref="SuggestedActionSetPriority.None"/> so that suppression fixes
            /// always show up last after all other fixes (and refactorings) for the selected line of code.
            /// </remarks>
            private ImmutableArray<UnifiedSuggestedActionSet> PrioritizeFixGroups(
                ImmutableDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
                ImmutableArray<CodeFixGroupKey> order,
                Workspace workspace)
            {
                using var nonSuppressionSetsDisposer = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(out var nonSuppressionSets);
                using var suppressionSetsDisposer = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(out var suppressionSets);
                using var bulkConfigurationActionsDisposer = ArrayBuilder<UnifiedSuggestedAction>.GetInstance(out var bulkConfigurationActions);

                foreach (var groupKey in order)
                {
                    var actions = map[groupKey];

                    var nonSuppressionActions = actions.Where(a => !IsTopLevelSuppressionAction(a.CodeAction));
                    AddUnifiedSuggestedActionsSet(nonSuppressionActions, groupKey, nonSuppressionSets);

                    var suppressionActions = actions.Where(a => IsTopLevelSuppressionAction(a.CodeAction) && !IsBulkConfigurationAction(a.CodeAction));
                    AddUnifiedSuggestedActionsSet(suppressionActions, groupKey, suppressionSets);

                    bulkConfigurationActions.AddRange(actions.Where(a => IsBulkConfigurationAction(a.CodeAction)));
                }

                var sets = nonSuppressionSets.ToImmutable();

                // Append bulk configuration fixes at the end of suppression/configuration fixes.
                if (bulkConfigurationActions.Count > 0)
                {
                    var bulkConfigurationSet = new UnifiedSuggestedActionSet(
                        UnifiedPredefinedSuggestedActionCategoryNames.CodeFix, bulkConfigurationActions);
                    suppressionSets.Add(bulkConfigurationSet);
                }

                if (suppressionSets.Count > 0)
                {
                    // Wrap the suppression/configuration actions within another top level suggested action
                    // to avoid clutter in the light bulb menu.
                    var wrappingSuggestedAction = new UnifiedSuggestedActionWithNestedActions(
                        workspace, provider: null,
                        codeAction: new NoChangeAction(CodeFixesResources.Suppress_or_Configure_issues),
                        nestedActionSets: suppressionSets.ToImmutable());

                    // Combine the spans and the category of each of the nested suggested actions
                    // to get the span and category for the new top level suggested action.
                    var (span, category) = CombineSpansAndCategory(suppressionSets);
                    var wrappingSet = new UnifiedSuggestedActionSet(
                        category,
                        actions: SpecializedCollections.SingletonEnumerable(wrappingSuggestedAction),
                        title: CodeFixesResources.Suppress_or_Configure_issues,
                        priority: UnifiedSuggestedActionSetPriority.None,
                        applicableToSpan: span);
                    sets = sets.Add(wrappingSet);
                }

                return sets;

                // Local functions
                static (TextSpan? span, string category) CombineSpansAndCategory(IEnumerable<UnifiedSuggestedActionSet> sets)
                {
                    // We are combining the spans and categories of the given set of suggested action sets
                    // to generate a result span containing the spans of individual suggested action sets and
                    // a result category which is the maximum severity category amongst the set
                    var minStart = -1;
                    var maxEnd = -1;
                    var category = PredefinedSuggestedActionCategoryNames.CodeFix;

                    foreach (var set in sets)
                    {
                        if (set.ApplicableToSpan.HasValue)
                        {
                            var currentStart = set.ApplicableToSpan.Value.Start;
                            var currentEnd = set.ApplicableToSpan.Value.End;

                            if (minStart == -1 || currentStart < minStart)
                            {
                                minStart = currentStart;
                            }

                            if (maxEnd == -1 || currentEnd > maxEnd)
                            {
                                maxEnd = currentEnd;
                            }
                        }

                        Debug.Assert(set.CategoryName == PredefinedSuggestedActionCategoryNames.CodeFix ||
                                     set.CategoryName == PredefinedSuggestedActionCategoryNames.ErrorFix);

                        // If this set contains an error fix, then change the result category to ErrorFix
                        if (set.CategoryName == PredefinedSuggestedActionCategoryNames.ErrorFix)
                        {
                            category = PredefinedSuggestedActionCategoryNames.ErrorFix;
                        }
                    }

                    var combinedSpan = minStart >= 0 ? new TextSpan(minStart, maxEnd) : (TextSpan?)null;
                    return (combinedSpan, category);
                }
            }

            private static void AddUnifiedSuggestedActionsSet(
                IEnumerable<UnifiedSuggestedAction> actions,
                CodeFixGroupKey groupKey,
                ArrayBuilder<UnifiedSuggestedActionSet> sets)
            {
                foreach (var group in actions.GroupBy(a => a.Priority))
                {
                    var priority = UnifiedSuggestedActionSource.GetUnifiedSuggestedActionSetPriority(group.Key);

                    // diagnostic from things like build shouldn't reach here since we don't support LB for those diagnostics
                    Debug.Assert(groupKey.Item1.HasTextSpan);
                    var category = GetFixCategory(groupKey.Item1.Severity);
                    sets.Add(new UnifiedSuggestedActionSet(
                        category, group, priority: priority, applicableToSpan: groupKey.Item1.GetTextSpan()));
                }
            }

            private static string GetFixCategory(DiagnosticSeverity severity)
            {
                switch (severity)
                {
                    case DiagnosticSeverity.Hidden:
                    case DiagnosticSeverity.Info:
                    case DiagnosticSeverity.Warning:
                        return PredefinedSuggestedActionCategoryNames.CodeFix;
                    case DiagnosticSeverity.Error:
                        return PredefinedSuggestedActionCategoryNames.ErrorFix;
                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            private static SuggestedActionSetPriority GetSuggestedActionSetPriority(CodeActionPriority key)
                => key switch
                {
                    CodeActionPriority.None => SuggestedActionSetPriority.None,
                    CodeActionPriority.Low => SuggestedActionSetPriority.Low,
                    CodeActionPriority.Medium => SuggestedActionSetPriority.Medium,
                    CodeActionPriority.High => SuggestedActionSetPriority.High,
                    _ => throw new InvalidOperationException(),
                };

            private ImmutableArray<UnifiedSuggestedActionSet> GetRefactorings(
                ITextBufferSupportsFeatureService supportsFeatureService,
                ISuggestedActionCategorySet requestedActionCategories,
                Workspace workspace,
                Document document,
                TextSpan? selectionOpt,
                Func<string, IDisposable?> addOperationScope,
                CancellationToken cancellationToken)
            {
                this.AssertIsForeground();

                if (!selectionOpt.HasValue)
                {
                    // this is here to fail test and see why it is failed.
                    Trace.WriteLine("given range is not current");
                    return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
                }

                var selection = selectionOpt.Value;

                if (workspace.Options.GetOption(EditorComponentOnOffOptions.CodeRefactorings) &&
                    _owner._codeRefactoringService != null &&
                    supportsFeatureService.SupportsRefactorings(_subjectBuffer))
                {
                    // It may seem strange that we kick off a task, but then immediately 'Wait' on 
                    // it. However, it's deliberate.  We want to make sure that the code runs on 
                    // the background so that no one takes an accidentally dependency on running on 
                    // the UI thread.
                    var refactorings = Task.Run(
                        () => _owner._codeRefactoringService.GetRefactoringsAsync(
                            document, selection, isBlocking: true, addOperationScope, cancellationToken),
                        cancellationToken).WaitAndGetResult(cancellationToken);

                    // If we are computing refactorings outside the 'Refactoring' context, i.e. for example, from the lightbulb under a squiggle or selection,
                    // then we want to filter out refactorings outside the selection span.
                    var filterOutsideSelection = !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring);

                    var filteredRefactorings = FilterOnUIThread(refactorings, selection, filterOutsideSelection, workspace);

                    return filteredRefactorings.SelectAsArray(
                        r => UnifiedSuggestedActionSource.OrganizeRefactorings(workspace, r));
                }

                return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
            }

            public Task<bool> HasSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                // We implement GetSuggestedActionCategoriesAsync so this should not be called
                throw new NotImplementedException($"We implement {nameof(GetSuggestedActionCategoriesAsync)}. This should not be called.");
            }

            private async Task<TextSpan?> GetSpanAsync(SnapshotSpan range, CancellationToken cancellationToken)
            {
                // First, ensure that the snapshot we're being asked about is for an actual
                // roslyn document.  This can fail, for example, in projection scenarios where
                // we are called with a range snapshot that refers to the projection buffer
                // and not the actual roslyn code that is being projected into it.
                var document = range.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return null;
                }

                // Also make sure the range is from the same buffer that this source was created for
                Contract.ThrowIfFalse(
                    range.Snapshot.TextBuffer.Equals(_subjectBuffer),
                    $"Invalid text buffer passed to {nameof(HasSuggestedActionsAsync)}");

                // Next, before we do any async work, acquire the user's selection, directly grabbing
                // it from the UI thread if htat's what we're on. That way we don't have any reentrancy
                // blocking concerns if VS wants to block on this call (for example, if the user 
                // explicitly invokes the 'show smart tag' command).
                //
                // This work must happen on the UI thread as it needs to access the _textView's mutable
                // state.
                //
                // Note: we may be called in one of two VS scenarios:
                //      1) User has moved caret to a new line.  In this case VS will call into us in the
                //         bg to see if we have any suggested actions for this line.  In order to figure
                //         this out, we need to see what selectoin the user has (for refactorings), which
                //         necessitates going back to the fg.
                //
                //      2) User moves to a line and immediately hits ctrl-dot.  In this case, on the UI
                //         thread VS will kick us off and then immediately block to get the results so
                //         that they can expand the lightbulb.  In this case we cannot do BG work first,
                //         then call back into the UI thread to try to get the user selection.  This will
                //         deadlock as the UI thread is blocked on us.  
                //
                // There are two solution to '2'.  Either introduce reentrancy (which we really don't
                // like to do), or just ensure that we acquire and get the users selection up front.
                // This means that when we're called from the UI therad, we never try to go back to the
                // UI thread.
                TextSpan? selection = null;
                if (IsForeground())
                {
                    selection = TryGetCodeRefactoringSelection(range);
                }
                else
                {
                    await InvokeBelowInputPriorityAsync(() =>
                    {
                        // Make sure we were not disposed between kicking off this work and getting
                        // to this point.
                        if (IsDisposed)
                        {
                            return;
                        }

                        selection = TryGetCodeRefactoringSelection(range);
                    }, cancellationToken).ConfigureAwait(false);
                }

                return selection;
            }

            private async Task<string?> GetFixLevelAsync(
                SuggestedActionsSourceProvider provider,
                Document document,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                if (provider._codeFixService != null &&
                    _subjectBuffer.SupportsCodeFixes())
                {
                    var result = await provider._codeFixService.GetMostSevereFixableDiagnosticAsync(
                            document, range.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);

                    if (result.HasFix)
                    {
                        Logger.Log(FunctionId.SuggestedActions_HasSuggestedActionsAsync);
                        return GetFixCategory(result.Diagnostic.Severity);
                    }

                    if (result.PartialResult)
                    {
                        // reset solution version number so that we can raise suggested action changed event
                        Volatile.Write(ref _lastSolutionVersionReported, InvalidSolutionVersion);
                        return null;
                    }
                }

                return null;
            }

            private async Task<string?> TryGetRefactoringSuggestedActionCategoryAsync(
                SuggestedActionsSourceProvider provider,
                Document document,
                TextSpan? selection,
                CancellationToken cancellationToken)
            {
                if (!selection.HasValue)
                {
                    // this is here to fail test and see why it is failed.
                    Trace.WriteLine("given range is not current");
                    return null;
                }

                if (document.Project.Solution.Options.GetOption(EditorComponentOnOffOptions.CodeRefactorings) &&
                    provider._codeRefactoringService != null &&
                    _subjectBuffer.SupportsRefactorings())
                {
                    if (await provider._codeRefactoringService.HasRefactoringsAsync(
                            document, selection.Value, cancellationToken).ConfigureAwait(false))
                    {
                        return PredefinedSuggestedActionCategoryNames.Refactoring;
                    }
                }

                return null;
            }

            private TextSpan? TryGetCodeRefactoringSelection(SnapshotSpan range)
            {
                this.AssertIsForeground();
                Debug.Assert(!this.IsDisposed);

                var selectedSpans = _textView.Selection.SelectedSpans
                    .SelectMany(ss => _textView.BufferGraph.MapDownToBuffer(ss, SpanTrackingMode.EdgeExclusive, _subjectBuffer))
                    .Where(ss => !_textView.IsReadOnlyOnSurfaceBuffer(ss))
                    .ToList();

                // We only support refactorings when there is a single selection in the document.
                if (selectedSpans.Count != 1)
                {
                    return null;
                }

                var translatedSpan = selectedSpans[0].TranslateTo(range.Snapshot, SpanTrackingMode.EdgeInclusive);

                // We only support refactorings when selected span intersects with the span that the light bulb is asking for.
                if (!translatedSpan.IntersectsWith(range))
                {
                    return null;
                }

                return translatedSpan.Span.ToTextSpan();
            }

            private void OnTextViewClosed(object sender, EventArgs e)
                => Dispose();

            private void OnWorkspaceChanged(object sender, EventArgs e)
            {
                // REVIEW: this event should give both old and new workspace as argument so that
                // one doesn't need to hold onto workspace in field.

                // remove existing event registration
                if (_workspaceStatusService != null)
                {
                    _workspaceStatusService.StatusChanged -= OnWorkspaceStatusChanged;
                }

                if (_workspace != null)
                {
                    _workspace.DocumentActiveContextChanged -= OnActiveContextChanged;
                }

                // REVIEW: why one need to get new workspace from registration? why not just pass in the new workspace?
                // add new event registration
                RegisterEventsToWorkspace(_registration.Workspace);
            }

            private void RegisterEventsToWorkspace(Workspace? workspace)
            {
                _workspace = workspace;

                if (_workspace == null)
                {
                    return;
                }

                _workspace.DocumentActiveContextChanged += OnActiveContextChanged;
                _workspaceStatusService = _workspace.Services.GetService<IWorkspaceStatusService>();
                if (_workspaceStatusService != null)
                {
                    _workspaceStatusService.StatusChanged += OnWorkspaceStatusChanged;
                }
            }

            private void OnActiveContextChanged(object sender, DocumentActiveContextChangedEventArgs e)
            {
                // REVIEW: it would be nice for changed event to pass in both old and new document.
                OnSuggestedActionsChanged(e.Solution.Workspace, e.NewActiveContextDocumentId, e.Solution.WorkspaceVersion);
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                // document removed case. no reason to raise event
                if (e.Solution == null)
                {
                    return;
                }

                OnSuggestedActionsChanged(e.Workspace, e.DocumentId, e.Solution.WorkspaceVersion);
            }

            private void OnWorkspaceStatusChanged(object sender, EventArgs args)
            {
                var document = _subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                if (document == null)
                {
                    // document is already closed
                    return;
                }

                // ask editor to refresh lightbulb when workspace solution status is changed
                this.SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
            }

            private void OnSuggestedActionsChanged(Workspace currentWorkspace, DocumentId? currentDocumentId, int solutionVersion)
            {
                // Explicitly hold onto the _subjectBuffer field in a local and use this local in this function to avoid crashes
                // if this field happens to be cleared by Dispose() below. This is required since this code path involves code
                // that can run on background thread.
                var buffer = _subjectBuffer;
                if (buffer == null)
                {
                    return;
                }

                var workspace = buffer.GetWorkspace();

                // workspace is not ready, nothing to do.
                if (workspace == null || workspace != currentWorkspace)
                {
                    return;
                }

                if (currentDocumentId != workspace.GetDocumentIdInCurrentContext(buffer.AsTextContainer()) ||
                    solutionVersion == Volatile.Read(ref _lastSolutionVersionReported))
                {
                    return;
                }

                this.SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);

                Volatile.Write(ref _lastSolutionVersionReported, solutionVersion);
            }

            public async Task<ISuggestedActionCategorySet?> GetSuggestedActionCategoriesAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                if (_workspaceStatusService != null && !await _workspaceStatusService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false))
                {
                    // never show light bulb if solution is not fully loaded yet
                    return null;
                }

                var provider = _owner;
                using var asyncToken = provider.OperationListener.BeginAsyncOperation(nameof(GetSuggestedActionCategoriesAsync));
                var document = range.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return null;
                }

                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var linkedToken = linkedTokenSource.Token;

                var errorTask = Task.Run(
                    () => GetFixLevelAsync(provider, document, range, linkedToken), linkedToken);

                var selection = await GetSpanAsync(range, linkedToken).ConfigureAwait(false);

                var refactoringTask = SpecializedTasks.Null<string>();
                if (selection != null)
                {
                    refactoringTask = Task.Run(
                        () => TryGetRefactoringSuggestedActionCategoryAsync(provider, document, selection, linkedToken), linkedToken);
                }

                // If we happen to get the result of the error task before the refactoring task,
                // and that result is non-null, we can just cancel the refactoring task.
                var result = await errorTask.ConfigureAwait(false) ?? await refactoringTask.ConfigureAwait(false);
                linkedTokenSource.Cancel();

                return result == null
                    ? null
                    : _suggestedActionCategoryRegistry.CreateSuggestedActionCategorySet(result);
            }
        }
    }
}
