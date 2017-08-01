﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    using CodeFixGroupKey = Tuple<DiagnosticData, CodeActionPriority>;

    internal partial class SuggestedActionsSourceProvider
    {
        private class SuggestedActionsSource : ForegroundThreadAffinitizedObject, ISuggestedActionsSource
        {
            // state that will be only reset when source is disposed.
            private SuggestedActionsSourceProvider _owner;
            private ITextView _textView;
            private ITextBuffer _subjectBuffer;
            private WorkspaceRegistration _registration;

            // mutable state
            private Workspace _workspace;
            private int _lastSolutionVersionReported;

            public event EventHandler<EventArgs> SuggestedActionsChanged;

            public SuggestedActionsSource(SuggestedActionsSourceProvider owner, ITextView textView, ITextBuffer textBuffer)
            {
                _owner = owner;
                _textView = textView;
                _textView.Closed += OnTextViewClosed;
                _subjectBuffer = textBuffer;
                _registration = Workspace.GetWorkspaceRegistration(textBuffer.AsTextContainer());

                _lastSolutionVersionReported = InvalidSolutionVersion;
                var updateSource = (IDiagnosticUpdateSource)_owner._diagnosticService;
                updateSource.DiagnosticsUpdated += OnDiagnosticsUpdated;

                if (_registration.Workspace != null)
                {
                    _workspace = _registration.Workspace;
                    _workspace.DocumentActiveContextChanged += OnActiveContextChanged;
                }

                _registration.WorkspaceChanged += OnWorkspaceChanged;
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    var updateSource = (IDiagnosticUpdateSource)_owner._diagnosticService;
                    updateSource.DiagnosticsUpdated -= OnDiagnosticsUpdated;
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

                _owner = null;
                _workspace = null;
                _registration = null;
                _textView = null;
                _subjectBuffer = null;
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

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                AssertIsForeground();

                if (IsDisposed)
                {
                    return null;
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
                    var supportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();

                    var fixes = GetCodeFixes(supportsFeatureService, requestedActionCategories, workspace, document, range, cancellationToken);
                    var refactorings = GetRefactorings(supportsFeatureService, requestedActionCategories, workspace, document, range, cancellationToken);

                    var result = fixes.Concat(refactorings);

                    if (result.IsEmpty)
                    {
                        return null;
                    }

                    var allActionSets = InlineActionSetsIfDesirable(result);
                    var orderedActionSets = OrderActionSets(allActionSets);
                    var filteredSets = FilterActionSetsByTitle(orderedActionSets);

                    return filteredSets;
                }
            }

            private ImmutableArray<SuggestedActionSet> OrderActionSets(
                ImmutableArray<SuggestedActionSet> actionSets)
            {
                var caretPoint = _textView.GetCaretPoint(_subjectBuffer);
                return actionSets.OrderByDescending(s => s.Priority)
                                 .ThenBy(s => s, new SuggestedActionSetComparer(caretPoint))
                                 .ToImmutableArray();
            }

            private ImmutableArray<SuggestedActionSet> FilterActionSetsByTitle(ImmutableArray<SuggestedActionSet> allActionSets)
            {
                var result = ArrayBuilder<SuggestedActionSet>.GetInstance();

                var seenTitles = new HashSet<string>();

                foreach (var set in allActionSets)
                {
                    var filteredSet = FilterActionSetByTitle(set, seenTitles);
                    if (filteredSet != null)
                    {
                        result.Add(filteredSet);
                    }
                }

                return result.ToImmutableAndFree();
            }

            private SuggestedActionSet FilterActionSetByTitle(SuggestedActionSet set, HashSet<string> seenTitles)
            {
                var actions = ArrayBuilder<ISuggestedAction>.GetInstance();

                foreach (var action in set.Actions)
                {
                    if (seenTitles.Add(action.DisplayText))
                    {
                        actions.Add(action);
                    }
                }

                try
                {
                    return actions.Count == 0
                        ? null
                        : new SuggestedActionSet(actions.ToImmutable(), set.Title, set.Priority, set.ApplicableToSpan);
                }
                finally
                {
                    actions.Free();
                }
            }

            private ImmutableArray<SuggestedActionSet> InlineActionSetsIfDesirable(ImmutableArray<SuggestedActionSet> allActionSets)
            {
                // If we only have a single set of items, and that set only has three max suggestion 
                // offered.  Then we can consider inlining any nested actions into the top level list.
                // (but we only do this if the parent of the nested actions isn't invokable itself).
                if (allActionSets.Sum(a => a.Actions.Count()) > 3)
                {
                    return allActionSets;
                }

                return allActionSets.SelectAsArray(InlineActions);
            }

            private SuggestedActionSet InlineActions(SuggestedActionSet actionSet)
            {
                var newActions = ArrayBuilder<ISuggestedAction>.GetInstance();
                foreach (var action in actionSet.Actions)
                {
                    var actionWithNestedActions = action as SuggestedActionWithNestedActions;

                    // Only inline if the underlying code action allows it.
                    if (actionWithNestedActions?.CodeAction.IsInlinable == true)
                    {
                        newActions.AddRange(actionWithNestedActions.NestedActionSet.Actions);
                    }
                    else
                    {
                        newActions.Add(action);
                    }
                }

                return new SuggestedActionSet(
                    newActions.ToImmutableAndFree(), actionSet.Title, actionSet.Priority, actionSet.ApplicableToSpan);
            }

            private ImmutableArray<SuggestedActionSet> GetCodeFixes(
                IDocumentSupportsFeatureService supportsFeatureService,
                ISuggestedActionCategorySet requestedActionCategories,
                Workspace workspace,
                Document document,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                this.AssertIsForeground();

                if (_owner._codeFixService != null &&
                    supportsFeatureService.SupportsCodeFixes(document) &&
                    requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                {
                    // We only include suppressions if light bulb is asking for everything.
                    // If the light bulb is only asking for code fixes, then we don't include suppressions.
                    var includeSuppressionFixes = requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Any);

                    var fixes = Task.Run(
                        () => _owner._codeFixService.GetFixesAsync(
                                document, range.Span.ToTextSpan(), includeSuppressionFixes, cancellationToken),
                        cancellationToken).WaitAndGetResult(cancellationToken);

                    var filteredFixes = FilterOnUIThread(fixes, workspace);

                    return OrganizeFixes(workspace, filteredFixes, includeSuppressionFixes);
                }

                return ImmutableArray<SuggestedActionSet>.Empty;
            }

            private ImmutableArray<CodeFixCollection> FilterOnUIThread(
                ImmutableArray<CodeFixCollection> collections, Workspace workspace)
            {
                this.AssertIsForeground();

                return collections.Select(c => FilterOnUIThread(c, workspace)).WhereNotNull().ToImmutableArray();
            }

            private CodeFixCollection FilterOnUIThread(
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

            private ImmutableArray<CodeRefactoring> FilterOnUIThread(ImmutableArray<CodeRefactoring> refactorings, Workspace workspace)
            {
                return refactorings.Select(r => FilterOnUIThread(r, workspace)).WhereNotNull().ToImmutableArray();
            }

            private CodeRefactoring FilterOnUIThread(CodeRefactoring refactoring, Workspace workspace)
            {
                var actions = refactoring.Actions.WhereAsArray(a => IsApplicable(a, workspace));
                return actions.Length == 0
                    ? null
                    : actions.Length == refactoring.Actions.Length
                        ? refactoring
                        : new CodeRefactoring(refactoring.Provider, actions);
            }

            /// <summary>
            /// Arrange fixes into groups based on the issue (diagnostic being fixed) and prioritize these groups.
            /// </summary>
            private ImmutableArray<SuggestedActionSet> OrganizeFixes(
                Workspace workspace, ImmutableArray<CodeFixCollection> fixCollections,
                bool includeSuppressionFixes)
            {
                var map = ImmutableDictionary.CreateBuilder<CodeFixGroupKey, IList<SuggestedAction>>();
                var order = ArrayBuilder<CodeFixGroupKey>.GetInstance();

                // First group fixes by diagnostic and priority.
                GroupFixes(workspace, fixCollections, map, order, includeSuppressionFixes);

                // Then prioritize between the groups.
                return PrioritizeFixGroups(map.ToImmutable(), order.ToImmutableAndFree());
            }

            /// <summary>
            /// Groups fixes by the diagnostic being addressed by each fix.
            /// </summary>
            private void GroupFixes(
                Workspace workspace,
                ImmutableArray<CodeFixCollection> fixCollections,
                IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
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
                IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order,
                bool includeSuppressionFixes,
                CodeFixCollection fixCollection)
            {
                var fixes = fixCollection.Fixes;
                var fixCount = fixes.Length;

                Func<CodeAction, SuggestedActionSet> getFixAllSuggestedActionSet =
                    codeAction => GetFixAllSuggestedActionSet(
                        codeAction, fixCount, fixCollection.FixAllState,
                        fixCollection.SupportedScopes, fixCollection.FirstDiagnostic,
                        workspace);

                var nonSupressionCodeFixes = fixes.WhereAsArray(f => !(f.Action is TopLevelSuppressionCodeAction));
                var supressionCodeFixes = fixes.WhereAsArray(f => f.Action is TopLevelSuppressionCodeAction);

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

            private void AddCodeActions(
                Workspace workspace, IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order, CodeFixCollection fixCollection,
                Func<CodeAction, SuggestedActionSet> getFixAllSuggestedActionSet,
                ImmutableArray<CodeFix> codeFixes)
            {
                foreach (var fix in codeFixes)
                {
                    SuggestedAction suggestedAction;
                    if (fix.Action.NestedCodeActions.Length > 0)
                    {
                        var nestedActions = fix.Action.NestedCodeActions.SelectAsArray(
                            nestedAction => new CodeFixSuggestedAction(
                                _owner, workspace, _subjectBuffer, fix, fixCollection.Provider,
                                nestedAction, getFixAllSuggestedActionSet(nestedAction)));

                        var set = new SuggestedActionSet(
                            nestedActions, SuggestedActionSetPriority.Medium,
                            fix.PrimaryDiagnostic.Location.SourceSpan.ToSpan());

                        suggestedAction = new SuggestedActionWithNestedActions(
                            _owner, workspace, _subjectBuffer,
                            fixCollection.Provider, fix.Action, set);
                    }
                    else
                    {
                        suggestedAction = new CodeFixSuggestedAction(
                            _owner, workspace, _subjectBuffer, fix, fixCollection.Provider,
                            fix.Action, getFixAllSuggestedActionSet(fix.Action));
                    }

                    AddFix(fix, suggestedAction, map, order);
                }
            }

            private static void AddFix(
                CodeFix fix, SuggestedAction suggestedAction,
                IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order)
            {
                var diag = fix.GetPrimaryDiagnosticData();

                var groupKey = new CodeFixGroupKey(diag, fix.Action.Priority);
                if (!map.ContainsKey(groupKey))
                {
                    order.Add(groupKey);
                    map[groupKey] = ImmutableArray.CreateBuilder<SuggestedAction>();
                }

                map[groupKey].Add(suggestedAction);
            }

            /// <summary>
            /// If the provided fix all context is non-null and the context's code action Id matches the given code action's Id then,
            /// returns the set of fix all occurrences actions associated with the code action.
            /// </summary>
            internal SuggestedActionSet GetFixAllSuggestedActionSet(
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

                var fixAllSuggestedActions = ArrayBuilder<FixAllSuggestedAction>.GetInstance();
                foreach (var scope in supportedScopes)
                {
                    var fixAllStateForScope = fixAllState.WithScopeAndEquivalenceKey(scope, action.EquivalenceKey);
                    var fixAllSuggestedAction = new FixAllSuggestedAction(
                        _owner, workspace, _subjectBuffer, fixAllStateForScope,
                        firstDiagnostic, action);

                    fixAllSuggestedActions.Add(fixAllSuggestedAction);
                }

                return new SuggestedActionSet(
                    fixAllSuggestedActions.ToImmutableAndFree(),
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
            private static ImmutableArray<SuggestedActionSet> PrioritizeFixGroups(
                IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map, IList<CodeFixGroupKey> order)
            {
                var sets = ArrayBuilder<SuggestedActionSet>.GetInstance();

                foreach (var diag in order)
                {
                    var actions = map[diag];

                    foreach (var group in actions.GroupBy(a => a.Priority))
                    {
                        var priority = GetSuggestedActionSetPriority(group.Key);

                        // diagnostic from things like build shouldn't reach here since we don't support LB for those diagnostics
                        Contract.Requires(diag.Item1.HasTextSpan);
                        sets.Add(new SuggestedActionSet(group, priority, diag.Item1.TextSpan.ToSpan()));
                    }
                }

                return sets.ToImmutableAndFree();
            }

            private static SuggestedActionSetPriority GetSuggestedActionSetPriority(CodeActionPriority key)
            {
                switch (key)
                {
                    case CodeActionPriority.None: return SuggestedActionSetPriority.None;
                    case CodeActionPriority.Low: return SuggestedActionSetPriority.Low;
                    case CodeActionPriority.Medium: return SuggestedActionSetPriority.Medium;
                    case CodeActionPriority.High: return SuggestedActionSetPriority.High;
                    default:
                        throw new InvalidOperationException();
                }
            }

            private ImmutableArray<SuggestedActionSet> GetRefactorings(
                IDocumentSupportsFeatureService supportsFeatureService,
                ISuggestedActionCategorySet requestedActionCategories,
                Workspace workspace,
                Document document,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                this.AssertIsForeground();

                if (workspace.Options.GetOption(EditorComponentOnOffOptions.CodeRefactorings) &&
                    _owner._codeRefactoringService != null &&
                    supportsFeatureService.SupportsRefactorings(document) &&
                    requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring))
                {
                    // Get the selection while on the UI thread.
                    var selection = TryGetCodeRefactoringSelection(range);
                    if (!selection.HasValue)
                    {
                        // this is here to fail test and see why it is failed.
                        Trace.WriteLine("given range is not current");
                        return ImmutableArray<SuggestedActionSet>.Empty;
                    }

                    // It may seem strange that we kick off a task, but then immediately 'Wait' on 
                    // it. However, it's deliberate.  We want to make sure that the code runs on 
                    // the background so that no one takes an accidentally dependency on running on 
                    // the UI thread.
                    var refactorings = Task.Run(
                        () => _owner._codeRefactoringService.GetRefactoringsAsync(
                            document, selection.Value, cancellationToken),
                        cancellationToken).WaitAndGetResult(cancellationToken);

                    var filteredRefactorings = FilterOnUIThread(refactorings, workspace);

                    // Refactorings are given the span the user currently has selected.  That
                    // way they can be accurately sorted against other refactorings/fixes that
                    // are of the same priority.  i.e. refactorings are LowPriority by default.
                    // But we still want them to come first over a low-pri code fix that is
                    // further away.  A good example of this is "Add null parameter check" which
                    // should be higher in the list when the caret is on a parameter, vs the 
                    // code-fix for "use expression body" which is given the entire span of a 
                    // method.
                    return filteredRefactorings.SelectAsArray(
                        r => OrganizeRefactorings(workspace, r, selection.Value.ToSpan()));
                }

                return ImmutableArray<SuggestedActionSet>.Empty;
            }

            /// <summary>
            /// Arrange refactorings into groups.
            /// </summary>
            /// <remarks>
            /// Refactorings are returned in priority order determined based on <see cref="ExtensionOrderAttribute"/>.
            /// Priority for all <see cref="SuggestedActionSet"/>s containing refactorings is set to <see cref="SuggestedActionSetPriority.Low"/>
            /// and should show up after fixes but before suppression fixes in the light bulb menu.
            /// </remarks>
            private SuggestedActionSet OrganizeRefactorings(
                Workspace workspace, CodeRefactoring refactoring, Span applicableSpan)
            {
                var refactoringSuggestedActions = ArrayBuilder<SuggestedAction>.GetInstance();

                foreach (var action in refactoring.Actions)
                {
                    refactoringSuggestedActions.Add(new CodeRefactoringSuggestedAction(
                        _owner, workspace, _subjectBuffer, refactoring.Provider, action));
                }

                return new SuggestedActionSet(
                    refactoringSuggestedActions.ToImmutableAndFree(),
                    SuggestedActionSetPriority.Low,
                    applicableSpan);
            }

            public async Task<bool> HasSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                var provider = _owner;

                if (IsDisposed)
                {
                    // We've already been disposed.  No point in continuing.
                    return false;
                }

                using (var asyncToken = _owner.OperationListener.BeginAsyncOperation("HasSuggestedActionsAsync"))
                {
                    // First, ensure that the snapshot we're being asked about is for an actual
                    // roslyn document.  This can fail, for example, in projection scenarios where
                    // we are called with a range snapshot that refers to the projection buffer
                    // and not the actual roslyn code that is being projected into it.
                    var document = range.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        return false;
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
                        await InvokeBelowInputPriority(() =>
                        {
                            // Make sure we were not disposed between kicking off this work and getting
                            // to this point.
                            if (IsDisposed)
                            {
                                return;
                            }

                            selection = TryGetCodeRefactoringSelection(range);
                        }).ConfigureAwait(false);
                    }

                    return
                        await HasFixesAsync(provider, document, range, cancellationToken).ConfigureAwait(false) ||
                        await HasRefactoringsAsync(provider, document, selection, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<bool> HasFixesAsync(
                SuggestedActionsSourceProvider provider,
                Document document,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                var workspace = document.Project.Solution.Workspace;
                var supportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();

                if (provider._codeFixService != null &&
                    supportsFeatureService.SupportsCodeFixes(document))
                {
                    var result = await Task.Run(
                        () => provider._codeFixService.GetFirstDiagnosticWithFixAsync(
                            document, range.Span.ToTextSpan(), cancellationToken),
                            cancellationToken).ConfigureAwait(false);

                    if (result.HasFix)
                    {
                        Logger.Log(FunctionId.SuggestedActions_HasSuggestedActionsAsync);
                        return true;
                    }

                    if (result.PartialResult)
                    {
                        // reset solution version number so that we can raise suggested action changed event
                        Volatile.Write(ref _lastSolutionVersionReported, InvalidSolutionVersion);
                        return false;
                    }
                }

                return false;
            }

            private async Task<bool> HasRefactoringsAsync(
                SuggestedActionsSourceProvider provider,
                Document document,
                TextSpan? selection,
                CancellationToken cancellationToken)
            {
                if (!selection.HasValue)
                {
                    // this is here to fail test and see why it is failed.
                    Trace.WriteLine("given range is not current");
                    return false;
                }

                var workspace = document.Project.Solution.Workspace;
                var supportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();

                if (document.Project.Solution.Options.GetOption(EditorComponentOnOffOptions.CodeRefactorings) &&
                    provider._codeRefactoringService != null &&
                    supportsFeatureService.SupportsRefactorings(document))
                {
                    return await Task.Run(
                        () => provider._codeRefactoringService.HasRefactoringsAsync(
                            document, selection.Value, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                }

                return false;
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
            {
                Dispose();
            }

            private void OnWorkspaceChanged(object sender, EventArgs e)
            {
                // REVIEW: this event should give both old and new workspace as argument so that
                // one doesn't need to hold onto workspace in field.

                // remove existing event registration
                if (_workspace != null)
                {
                    _workspace.DocumentActiveContextChanged -= OnActiveContextChanged;
                }

                // REVIEW: why one need to get new workspace from registration? why not just pass in the new workspace?
                // add new event registration
                _workspace = _registration.Workspace;

                if (_workspace != null)
                {
                    _workspace.DocumentActiveContextChanged += OnActiveContextChanged;
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

            private void OnSuggestedActionsChanged(Workspace currentWorkspace, DocumentId currentDocumentId, int solutionVersion, DiagnosticsUpdatedArgs args = null)
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
        }
    }
}
