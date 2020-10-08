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

                    // We convert the code fixes and refactorings to UnifiedSuggestedActionSets instead of
                    // SuggestedActionSets so that we can share logic between local Roslyn and LSP.
                    var fixes = GetCodeFixes(
                        supportsFeatureService, requestedActionCategories, workspace,
                        document, range, addOperationScope, cancellationToken);
                    var refactorings = GetRefactorings(
                        supportsFeatureService, requestedActionCategories, workspace,
                        document, selectionOpt, addOperationScope, cancellationToken);

                    var filteredSets = UnifiedSuggestedActionsSource.FilterAndOrderActionSets(fixes, refactorings, selectionOpt);
                    if (!filteredSets.HasValue)
                    {
                        return null;
                    }

                    return filteredSets.Value.Select(s => ConvertToSuggestedActionSet(s)).WhereNotNull();
                }
            }

            private SuggestedActionSet? ConvertToSuggestedActionSet(UnifiedSuggestedActionSet? unifiedSuggestedActionSet)
            {
                // May be null in cases involving CodeFixSuggestedActions since FixAllFlavors may be null.
                if (unifiedSuggestedActionSet == null)
                {
                    return null;
                }

                using var _ = ArrayBuilder<ISuggestedAction>.GetInstance(out var suggestedActions);
                foreach (var action in unifiedSuggestedActionSet.Actions)
                {
                    suggestedActions.Add(ConvertToSuggestedAction(action));
                }

                return new SuggestedActionSet(
                    categoryName: unifiedSuggestedActionSet.CategoryName,
                    actions: suggestedActions,
                    title: unifiedSuggestedActionSet.Title,
                    priority: ConvertToSuggestedActionSetPriority(unifiedSuggestedActionSet.Priority),
                    applicableToSpan: unifiedSuggestedActionSet.ApplicableToSpan?.ToSpan());

                // Local functions
                ISuggestedAction ConvertToSuggestedAction(IUnifiedSuggestedAction unifiedSuggestedAction)
                    => unifiedSuggestedAction switch
                    {
                        UnifiedCodeFixSuggestedAction codeFixAction => new CodeFixSuggestedAction(
                            ThreadingContext, _owner, codeFixAction.Workspace, _subjectBuffer,
                            codeFixAction.CodeFix, codeFixAction.Provider, codeFixAction.OriginalCodeAction,
                            ConvertToSuggestedActionSet(codeFixAction.FixAllFlavors)),
                        UnifiedCodeRefactoringSuggestedAction codeRefactoringAction => new CodeRefactoringSuggestedAction(
                            ThreadingContext, _owner, codeRefactoringAction.Workspace, _subjectBuffer,
                            codeRefactoringAction.CodeRefactoringProvider, codeRefactoringAction.OriginalCodeAction),
                        UnifiedFixAllSuggestedAction fixAllAction => new FixAllSuggestedAction(
                            ThreadingContext, _owner, fixAllAction.Workspace, _subjectBuffer,
                            fixAllAction.FixAllState, fixAllAction.Diagnostic, fixAllAction.OriginalCodeAction),
                        UnifiedSuggestedActionWithNestedActions nestedAction => new SuggestedActionWithNestedActions(
                            ThreadingContext, _owner, nestedAction.Workspace, _subjectBuffer,
                            nestedAction.Provider ?? this, nestedAction.OriginalCodeAction,
                            nestedAction.NestedActionSets.SelectAsArray(s => ConvertToSuggestedActionSet(s))),
                        _ => throw ExceptionUtilities.Unreachable
                    };

                static SuggestedActionSetPriority ConvertToSuggestedActionSetPriority(UnifiedSuggestedActionSetPriority unifiedSuggestedActionSetPriority)
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

                if (_owner._codeFixService == null ||
                    !supportsFeatureService.SupportsCodeFixes(_subjectBuffer) ||
                    !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                {
                    return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
                }

                // Make sure we include the suppression fixes even when the light bulb is only asking for only code fixes.
                // See https://github.com/dotnet/roslyn/issues/29589
                const bool includeSuppressionFixes = true;

                return UnifiedSuggestedActionsSource.GetFilterAndOrderCodeFixesAsync(
                    workspace, _owner._codeFixService, document, range.Span.ToTextSpan(),
                    includeSuppressionFixes, isBlocking: true, addOperationScope, cancellationToken).WaitAndGetResult(cancellationToken);
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

                if (!workspace.Options.GetOption(EditorComponentOnOffOptions.CodeRefactorings) ||
                    _owner._codeRefactoringService == null ||
                    !supportsFeatureService.SupportsRefactorings(_subjectBuffer))
                {
                    return ImmutableArray<UnifiedSuggestedActionSet>.Empty;
                }

                // If we are computing refactorings outside the 'Refactoring' context, i.e. for example, from the lightbulb under a squiggle or selection,
                // then we want to filter out refactorings outside the selection span.
                var filterOutsideSelection = !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring);

                return UnifiedSuggestedActionsSource.GetFilterAndOrderCodeRefactoringsAsync(
                    workspace, _owner._codeRefactoringService, document, selection, isBlocking: true,
                    addOperationScope, filterOutsideSelection, cancellationToken).WaitAndGetResult(cancellationToken);
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

                // The _owner flag is cleared on Dispose. This should only occur when cancellation has already been
                // requested, but at least in 16.8 Preview 3.0 it was not always the case.
                cancellationToken.ThrowIfCancellationRequested();
                //Assumes.Present(provider);
                if (provider is null)
                {
                    // The object was unexpectedly disposed while still in use (caller error)
                    return null;
                }

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
