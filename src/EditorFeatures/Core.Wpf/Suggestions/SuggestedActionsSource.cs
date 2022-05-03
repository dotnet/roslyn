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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
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
        private abstract partial class SuggestedActionsSource : ForegroundThreadAffinitizedObject, ISuggestedActionsSource3
        {
            private readonly ISuggestedActionCategoryRegistryService _suggestedActionCategoryRegistry;

            private readonly ReferenceCountedDisposable<State> _state;

            public event EventHandler<EventArgs>? SuggestedActionsChanged;

            public readonly IGlobalOptionService GlobalOptions;

            protected SuggestedActionsSource(
                IThreadingContext threadingContext,
                IGlobalOptionService globalOptions,
                SuggestedActionsSourceProvider owner,
                ITextView textView,
                ITextBuffer textBuffer,
                ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistry)
                : base(threadingContext)
            {
                GlobalOptions = globalOptions;

                _suggestedActionCategoryRegistry = suggestedActionCategoryRegistry;
                _state = new ReferenceCountedDisposable<State>(new State(this, owner, textView, textBuffer));

                _state.Target.TextView.Closed += OnTextViewClosed;

                var updateSource = (IDiagnosticUpdateSource)owner._diagnosticService;
                updateSource.DiagnosticsUpdated += OnDiagnosticsUpdated;

                RegisterEventsToWorkspace(_state, _state.Target.Registration.Workspace);

                _state.Target.Registration.WorkspaceChanged += OnWorkspaceChanged;
            }

            public void Dispose()
            {
                _state.Dispose();
            }

            protected ReferenceCountedDisposable<State> SourceState => _state;

            public bool TryGetTelemetryId(out Guid telemetryId)
            {
                telemetryId = default;

                using var state = _state.TryAddReference();
                if (state is null)
                {
                    return false;
                }

                var workspace = state.Target.Workspace;
                if (workspace == null)
                {
                    return false;
                }

                var documentId = workspace.GetDocumentIdInCurrentContext(state.Target.SubjectBuffer.AsTextContainer());
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

            private ImmutableArray<SuggestedActionSet>? GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                IUIThreadOperationContext? operationContext,
                CancellationToken cancellationToken)
            {
                AssertIsForeground();

                using var state = _state.TryAddReference();
                if (state is null)
                    return null;

                if (state.Target.Workspace == null)
                    return null;

                using (operationContext?.AddScope(allowCancellation: true, description: EditorFeaturesResources.Gathering_Suggestions_Waiting_for_the_solution_to_fully_load))
                {
                    // This needs to run under threading context otherwise, we can deadlock on VS
                    var statusService = state.Target.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                    ThreadingContext.JoinableTaskFactory.Run(() => statusService.WaitUntilFullyLoadedAsync(cancellationToken));
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

                    var selection = TryGetCodeRefactoringSelection(state, range);

                    Func<string, IDisposable?> addOperationScope =
                        description => operationContext?.AddScope(allowCancellation: true, string.Format(EditorFeaturesResources.Gathering_Suggestions_0, description));

                    var options = GlobalOptions.GetBlockingCodeActionOptionsProvider();

                    // We convert the code fixes and refactorings to UnifiedSuggestedActionSets instead of
                    // SuggestedActionSets so that we can share logic between local Roslyn and LSP.
                    var fixesTask = GetCodeFixesAsync(
                        state, supportsFeatureService, requestedActionCategories, workspace, document, range,
                        addOperationScope, CodeActionRequestPriority.None,
                        options, cancellationToken);

                    var refactoringsTask = GetRefactoringsAsync(
                        state, supportsFeatureService, requestedActionCategories, GlobalOptions, workspace, document, selection,
                        addOperationScope, CodeActionRequestPriority.None, options, cancellationToken);

                    Task.WhenAll(fixesTask, refactoringsTask).WaitAndGetResult(cancellationToken);

                    return ConvertToSuggestedActionSets(
                        state, selection,
                        fixesTask.WaitAndGetResult(cancellationToken),
                        refactoringsTask.WaitAndGetResult(cancellationToken),
                        currentActionCount: 0);
                }
            }

            protected ImmutableArray<SuggestedActionSet> ConvertToSuggestedActionSets(
                ReferenceCountedDisposable<State> state,
                TextSpan? selection,
                ImmutableArray<UnifiedSuggestedActionSet> fixes,
                ImmutableArray<UnifiedSuggestedActionSet> refactorings,
                int currentActionCount)
            {
                var filteredSets = UnifiedSuggestedActionsSource.FilterAndOrderActionSets(fixes, refactorings, selection, currentActionCount);
                return filteredSets.Select(s => ConvertToSuggestedActionSet(s, state.Target.Owner, state.Target.SubjectBuffer)).WhereNotNull().ToImmutableArray();
            }

            [return: NotNullIfNotNull("unifiedSuggestedActionSet")]
            private SuggestedActionSet? ConvertToSuggestedActionSet(UnifiedSuggestedActionSet? unifiedSuggestedActionSet, SuggestedActionsSourceProvider owner, ITextBuffer subjectBuffer)
            {
                // May be null in cases involving CodeFixSuggestedActions since FixAllFlavors may be null.
                if (unifiedSuggestedActionSet == null)
                    return null;

                return new SuggestedActionSet(
                    unifiedSuggestedActionSet.CategoryName,
                    unifiedSuggestedActionSet.Actions.SelectAsArray(set => ConvertToSuggestedAction(set)),
                    unifiedSuggestedActionSet.Title,
                    ConvertToSuggestedActionSetPriority(unifiedSuggestedActionSet.Priority),
                    unifiedSuggestedActionSet.ApplicableToSpan?.ToSpan());

                // Local functions
                ISuggestedAction ConvertToSuggestedAction(IUnifiedSuggestedAction unifiedSuggestedAction)
                    => unifiedSuggestedAction switch
                    {
                        UnifiedCodeFixSuggestedAction codeFixAction => new CodeFixSuggestedAction(
                            ThreadingContext, owner, codeFixAction.Workspace, subjectBuffer,
                            codeFixAction.CodeFix, codeFixAction.Provider, codeFixAction.OriginalCodeAction,
                            ConvertToSuggestedActionSet(codeFixAction.FixAllFlavors, owner, subjectBuffer)),
                        UnifiedCodeRefactoringSuggestedAction codeRefactoringAction => new CodeRefactoringSuggestedAction(
                            ThreadingContext, owner, codeRefactoringAction.Workspace, subjectBuffer,
                            codeRefactoringAction.CodeRefactoringProvider, codeRefactoringAction.OriginalCodeAction,
                            ConvertToSuggestedActionSet(codeRefactoringAction.FixAllFlavors, owner, subjectBuffer)),
                        UnifiedFixAllCodeFixSuggestedAction fixAllAction => new FixAllCodeFixSuggestedAction(
                            ThreadingContext, owner, fixAllAction.Workspace, subjectBuffer,
                            fixAllAction.FixAllState, fixAllAction.Diagnostic, fixAllAction.OriginalCodeAction),
                        UnifiedFixAllCodeRefactoringSuggestedAction fixAllCodeRefactoringAction => new FixAllCodeRefactoringSuggestedAction(
                            ThreadingContext, owner, fixAllCodeRefactoringAction.Workspace, subjectBuffer,
                            fixAllCodeRefactoringAction.FixAllState, fixAllCodeRefactoringAction.OriginalCodeAction),
                        UnifiedSuggestedActionWithNestedActions nestedAction => new SuggestedActionWithNestedActions(
                            ThreadingContext, owner, nestedAction.Workspace, subjectBuffer,
                            nestedAction.Provider ?? this, nestedAction.OriginalCodeAction,
                            nestedAction.NestedActionSets.SelectAsArray((s, arg) => ConvertToSuggestedActionSet(s, arg.owner, arg.subjectBuffer), (owner, subjectBuffer))),
                        _ => throw ExceptionUtilities.Unreachable
                    };

                static SuggestedActionSetPriority ConvertToSuggestedActionSetPriority(UnifiedSuggestedActionSetPriority unifiedSuggestedActionSetPriority)
                    => unifiedSuggestedActionSetPriority switch
                    {
                        UnifiedSuggestedActionSetPriority.Lowest => SuggestedActionSetPriority.None,
                        UnifiedSuggestedActionSetPriority.Low => SuggestedActionSetPriority.Low,
                        UnifiedSuggestedActionSetPriority.Medium => SuggestedActionSetPriority.Medium,
                        UnifiedSuggestedActionSetPriority.High => SuggestedActionSetPriority.High,
                        _ => throw ExceptionUtilities.Unreachable,
                    };
            }

            protected static Task<ImmutableArray<UnifiedSuggestedActionSet>> GetCodeFixesAsync(
                ReferenceCountedDisposable<State> state,
                ITextBufferSupportsFeatureService supportsFeatureService,
                ISuggestedActionCategorySet requestedActionCategories,
                Workspace workspace,
                Document document,
                SnapshotSpan range,
                Func<string, IDisposable?> addOperationScope,
                CodeActionRequestPriority priority,
                CodeActionOptionsProvider options,
                CancellationToken cancellationToken)
            {
                if (state.Target.Owner._codeFixService == null ||
                    !supportsFeatureService.SupportsCodeFixes(state.Target.SubjectBuffer) ||
                    !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                {
                    return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();
                }

                return UnifiedSuggestedActionsSource.GetFilterAndOrderCodeFixesAsync(
                    workspace, state.Target.Owner._codeFixService, document, range.Span.ToTextSpan(),
                    priority, options, addOperationScope, cancellationToken).AsTask();
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

            protected static Task<ImmutableArray<UnifiedSuggestedActionSet>> GetRefactoringsAsync(
                ReferenceCountedDisposable<State> state,
                ITextBufferSupportsFeatureService supportsFeatureService,
                ISuggestedActionCategorySet requestedActionCategories,
                IGlobalOptionService globalOptions,
                Workspace workspace,
                Document document,
                TextSpan? selection,
                Func<string, IDisposable?> addOperationScope,
                CodeActionRequestPriority priority,
                CodeActionOptionsProvider options,
                CancellationToken cancellationToken)
            {
                if (!selection.HasValue)
                {
                    // this is here to fail test and see why it is failed.
                    Trace.WriteLine("given range is not current");
                    return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();
                }

                if (!globalOptions.GetOption(EditorComponentOnOffOptions.CodeRefactorings) ||
                    state.Target.Owner._codeRefactoringService == null ||
                    !supportsFeatureService.SupportsRefactorings(state.Target.SubjectBuffer))
                {
                    return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();
                }

                // 'CodeActionRequestPriority.Lowest' is reserved for suppression/configuration code fixes.
                // No code refactoring should have this request priority.
                if (priority == CodeActionRequestPriority.Lowest)
                {
                    return SpecializedTasks.EmptyImmutableArray<UnifiedSuggestedActionSet>();
                }

                // If we are computing refactorings outside the 'Refactoring' context, i.e. for example, from the lightbulb under a squiggle or selection,
                // then we want to filter out refactorings outside the selection span.
                var filterOutsideSelection = !requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring);

                return UnifiedSuggestedActionsSource.GetFilterAndOrderCodeRefactoringsAsync(
                    workspace, state.Target.Owner._codeRefactoringService, document, selection.Value, priority, options,
                    addOperationScope, filterOutsideSelection, cancellationToken);
            }

            public Task<bool> HasSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                // We implement GetSuggestedActionCategoriesAsync so this should not be called
                throw new NotImplementedException($"We implement {nameof(GetSuggestedActionCategoriesAsync)}. This should not be called.");
            }

            private async Task<TextSpan?> GetSpanAsync(ReferenceCountedDisposable<State> state, SnapshotSpan range, CancellationToken cancellationToken)
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
                    range.Snapshot.TextBuffer.Equals(state.Target.SubjectBuffer),
                    $"Invalid text buffer passed to {nameof(HasSuggestedActionsAsync)}");

                // Next, before we do any async work, acquire the user's selection, directly grabbing
                // it from the UI thread if that's what we're on. That way we don't have any reentrancy
                // blocking concerns if VS wants to block on this call (for example, if the user
                // explicitly invokes the 'show smart tag' command).
                //
                // This work must happen on the UI thread as it needs to access the _textView's mutable
                // state.
                //
                // Note: we may be called in one of two VS scenarios:
                //      1) User has moved caret to a new line.  In this case VS will call into us in the
                //         bg to see if we have any suggested actions for this line.  In order to figure
                //         this out, we need to see what selection the user has (for refactorings), which
                //         necessitates going back to the fg.
                //
                //      2) User moves to a line and immediately hits ctrl-dot.  In this case, on the UI
                //         thread VS will kick us off and then immediately block to get the results so
                //         that they can expand the light-bulb.  In this case we cannot do BG work first,
                //         then call back into the UI thread to try to get the user selection.  This will
                //         deadlock as the UI thread is blocked on us.
                //
                // There are two solution to '2'.  Either introduce reentrancy (which we really don't
                // like to do), or just ensure that we acquire and get the users selection up front.
                // This means that when we're called from the UI thread, we never try to go back to the
                // UI thread.
                TextSpan? selection = null;
                if (IsForeground())
                {
                    selection = TryGetCodeRefactoringSelection(state, range);
                }
                else
                {
                    await InvokeBelowInputPriorityAsync(() =>
                    {
                        // Make sure we were not disposed between kicking off this work and getting to this point.
                        using var state = _state.TryAddReference();
                        if (state is null)
                            return;

                        selection = TryGetCodeRefactoringSelection(state, range);
                    }, cancellationToken).ConfigureAwait(false);
                }

                return selection;
            }

            private static async Task<string?> GetFixLevelAsync(
                ReferenceCountedDisposable<State> state,
                Document document,
                SnapshotSpan range,
                CodeActionOptionsProvider options,
                CancellationToken cancellationToken)
            {
                foreach (var order in Orderings)
                {
                    var priority = TryGetPriority(order);
                    Contract.ThrowIfNull(priority);

                    var result = await GetFixLevelAsync(priority.Value).ConfigureAwait(false);
                    if (result != null)
                        return result;
                }

                return null;

                async Task<string?> GetFixLevelAsync(CodeActionRequestPriority priority)
                {
                    if (state.Target.Owner._codeFixService != null &&
                        state.Target.SubjectBuffer.SupportsCodeFixes())
                    {
                        var result = await state.Target.Owner._codeFixService.GetMostSevereFixAsync(
                            document, range.Span.ToTextSpan(), priority, options, cancellationToken).ConfigureAwait(false);

                        if (result.HasFix)
                        {
                            Logger.Log(FunctionId.SuggestedActions_HasSuggestedActionsAsync);
                            return GetFixCategory(result.CodeFixCollection.FirstDiagnostic.Severity);
                        }

                        if (!result.UpToDate)
                        {
                            // reset solution version number so that we can raise suggested action changed event
                            Volatile.Write(ref state.Target.LastSolutionVersionReported, InvalidSolutionVersion);
                            return null;
                        }
                    }

                    return null;
                }
            }

            private async Task<string?> TryGetRefactoringSuggestedActionCategoryAsync(
                Document document,
                TextSpan? selection,
                CodeActionOptionsProvider options,
                CancellationToken cancellationToken)
            {
                using var state = _state.TryAddReference();
                if (state is null)
                    return null;

                if (!selection.HasValue)
                {
                    // this is here to fail test and see why it is failed.
                    Trace.WriteLine("given range is not current");
                    return null;
                }

                if (GlobalOptions.GetOption(EditorComponentOnOffOptions.CodeRefactorings) &&
                    state.Target.Owner._codeRefactoringService != null &&
                    state.Target.SubjectBuffer.SupportsRefactorings())
                {
                    if (await state.Target.Owner._codeRefactoringService.HasRefactoringsAsync(
                            document, selection.Value, options, cancellationToken).ConfigureAwait(false))
                    {
                        return PredefinedSuggestedActionCategoryNames.Refactoring;
                    }
                }

                return null;
            }

            protected TextSpan? TryGetCodeRefactoringSelection(ReferenceCountedDisposable<State> state, SnapshotSpan range)
            {
                this.AssertIsForeground();

                var selectedSpans = state.Target.TextView.Selection.SelectedSpans
                    .SelectMany(ss => state.Target.TextView.BufferGraph.MapDownToBuffer(ss, SpanTrackingMode.EdgeExclusive, state.Target.SubjectBuffer))
                    .Where(ss => !state.Target.TextView.IsReadOnlyOnSurfaceBuffer(ss))
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
                using var state = _state.TryAddReference();
                if (state is null)
                    return;

                // REVIEW: this event should give both old and new workspace as argument so that
                // one doesn't need to hold onto workspace in field.

                // remove existing event registration
                if (state.Target.Workspace != null)
                {
                    state.Target.Workspace.Services.GetRequiredService<IWorkspaceStatusService>().StatusChanged -= OnWorkspaceStatusChanged;
                    state.Target.Workspace.DocumentActiveContextChanged -= OnActiveContextChanged;
                }

                // REVIEW: why one need to get new workspace from registration? why not just pass in the new workspace?
                // add new event registration
                RegisterEventsToWorkspace(state, state.Target.Registration.Workspace);
            }

            private void RegisterEventsToWorkspace(ReferenceCountedDisposable<State> state, Workspace? workspace)
            {
                state.Target.Workspace = workspace;

                if (state.Target.Workspace == null)
                {
                    return;
                }

                state.Target.Workspace.DocumentActiveContextChanged += OnActiveContextChanged;
                state.Target.Workspace.Services.GetRequiredService<IWorkspaceStatusService>().StatusChanged += OnWorkspaceStatusChanged;
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
                using var state = _state.TryAddReference();
                if (state is null)
                    return;

                var document = state.Target.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                if (document == null)
                {
                    // document is already closed
                    return;
                }

                // ask editor to refresh light-bulb when workspace solution status is changed
                this.SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
            }

            private void OnSuggestedActionsChanged(Workspace currentWorkspace, DocumentId? currentDocumentId, int solutionVersion)
            {
                using var state = _state.TryAddReference();
                if (state is null)
                    return;

                var buffer = state.Target.SubjectBuffer;
                var workspace = buffer.GetWorkspace();

                // workspace is not ready, nothing to do.
                if (workspace == null || workspace != currentWorkspace)
                {
                    return;
                }

                if (currentDocumentId != workspace.GetDocumentIdInCurrentContext(buffer.AsTextContainer()) ||
                    solutionVersion == Volatile.Read(ref state.Target.LastSolutionVersionReported))
                {
                    return;
                }

                this.SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);

                Volatile.Write(ref state.Target.LastSolutionVersionReported, solutionVersion);
            }

            public async Task<ISuggestedActionCategorySet?> GetSuggestedActionCategoriesAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                using var state = _state.TryAddReference();
                if (state is null)
                    return null;

                var workspace = state.Target.Workspace;
                if (workspace == null)
                    return null;

                // never show light bulb if solution is not fully loaded yet
                if (!await workspace.Services.GetRequiredService<IWorkspaceStatusService>().IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false))
                    return null;

                cancellationToken.ThrowIfCancellationRequested();

                using var asyncToken = state.Target.Owner.OperationListener.BeginAsyncOperation(nameof(GetSuggestedActionCategoriesAsync));
                var document = range.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                    return null;

                var options = GlobalOptions.GetCodeActionOptionsProvider();

                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var linkedToken = linkedTokenSource.Token;

                var errorTask = Task.Run(() => GetFixLevelAsync(state, document, range, options, linkedToken), linkedToken);

                var selection = await GetSpanAsync(state, range, linkedToken).ConfigureAwait(false);

                var refactoringTask = SpecializedTasks.Null<string>();
                if (selection != null)
                {
                    refactoringTask = Task.Run(
                        () => TryGetRefactoringSuggestedActionCategoryAsync(document, selection, options, linkedToken), linkedToken);
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
