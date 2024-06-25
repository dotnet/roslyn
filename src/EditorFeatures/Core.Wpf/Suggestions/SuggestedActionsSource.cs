// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using IUIThreadOperationContext = Microsoft.VisualStudio.Utilities.IUIThreadOperationContext;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        private sealed partial class SuggestedActionsSource : ISuggestedActionsSource3
        {
            private readonly ISuggestedActionCategoryRegistryService _suggestedActionCategoryRegistry;

            private readonly ReferenceCountedDisposable<State> _state;
            private readonly IAsynchronousOperationListener _listener;

            public event EventHandler<EventArgs>? SuggestedActionsChanged { add { } remove { } }

            private readonly IThreadingContext _threadingContext;
            public readonly IGlobalOptionService GlobalOptions;

            public SuggestedActionsSource(
                IThreadingContext threadingContext,
                IGlobalOptionService globalOptions,
                SuggestedActionsSourceProvider owner,
                ITextView textView,
                ITextBuffer textBuffer,
                ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistry,
                IAsynchronousOperationListener listener)
            {
                _threadingContext = threadingContext;
                GlobalOptions = globalOptions;

                _suggestedActionCategoryRegistry = suggestedActionCategoryRegistry;
                _state = new ReferenceCountedDisposable<State>(new State(this, owner, textView, textBuffer));

                _state.Target.TextView.Closed += OnTextViewClosed;
                _listener = listener;
            }

            public void Dispose()
                => _state.Dispose();

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
                => null;

            public IEnumerable<SuggestedActionSet>? GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                IUIThreadOperationContext operationContext)
                => null;

            public Task<bool> HasSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                // We implement GetSuggestedActionCategoriesAsync so this should not be called
                throw new NotImplementedException($"We implement {nameof(GetSuggestedActionCategoriesAsync)}. This should not be called.");
            }

            private TextSpan? TryGetCodeRefactoringSelection(ReferenceCountedDisposable<State> state, SnapshotSpan range)
            {
                _threadingContext.ThrowIfNotOnUIThread();

                var selectedSpans = state.Target.TextView.Selection.SelectedSpans
                    .SelectMany(ss => state.Target.TextView.BufferGraph.MapDownToBuffer(ss, SpanTrackingMode.EdgeExclusive, state.Target.SubjectBuffer))
                    .Where(ss => !state.Target.TextView.IsReadOnlyOnSurfaceBuffer(ss))
                    .ToList();

                // We only support refactorings when there is a single selection in the document.
                if (selectedSpans.Count != 1)
                    return null;

                var translatedSpan = selectedSpans[0].TranslateTo(range.Snapshot, SpanTrackingMode.EdgeInclusive);

                // We only support refactorings when selected span intersects with the span that the light bulb is asking for.
                if (!translatedSpan.IntersectsWith(range))
                    return null;

                return translatedSpan.Span.ToTextSpan();
            }

            private void OnTextViewClosed(object sender, EventArgs e)
                => Dispose();

            public async Task<ISuggestedActionCategorySet?> GetSuggestedActionCategoriesAsync(
                ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                // This function gets called immediately after operations like scrolling.  We want to wait just a small
                // amount to ensure that we don't immediately start consuming CPU/memory which then impedes the very
                // action the user is trying to perform.  To accomplish this, we wait 100ms.  That's longer than normal
                // keyboard repeat rates (usually around 30ms), but short enough that it's not noticeable to the user.
                await Task.Delay(100, cancellationToken).NoThrowAwaitable();
                if (cancellationToken.IsCancellationRequested)
                    return null;

                using var state = _state.TryAddReference();
                if (state is null)
                    return null;

                // Make sure the range is from the same buffer that this source was created for.
                Contract.ThrowIfFalse(
                    range.Snapshot.TextBuffer.Equals(state.Target.SubjectBuffer),
                    $"Invalid text buffer passed to {nameof(HasSuggestedActionsAsync)}");

                var workspace = state.Target.Workspace;
                if (workspace == null)
                    return null;

                // never show light bulb if solution is not fully loaded yet
                if (!await workspace.Services.GetRequiredService<IWorkspaceStatusService>().IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false))
                    return null;

                cancellationToken.ThrowIfCancellationRequested();

                using var asyncToken = state.Target.Owner.OperationListener.BeginAsyncOperation(nameof(GetSuggestedActionCategoriesAsync));
                var document = range.Snapshot.GetOpenTextDocumentInCurrentContextWithChanges();
                if (document == null)
                    return null;

                var fallbackOptions = GlobalOptions.GetCodeActionOptionsProvider();

                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                // Assign over cancellation token so no one accidentally uses the wrong token.
                cancellationToken = linkedTokenSource.Token;

                // Kick off the work to get errors.
                var errorTask = GetFixLevelAsync(document, range, fallbackOptions, cancellationToken);

                // Make a quick jump back to the UI thread to get the user's selection, then go back to the thread pool..
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

                var selection = TryGetCodeRefactoringSelection(state, range);
                await TaskScheduler.Default;

                // If we have a selection, kick off the work to get refactorings concurrently with the above work to get errors.
                var refactoringTask = selection != null
                    ? TryGetRefactoringSuggestedActionCategoryAsync(document, selection, fallbackOptions, cancellationToken)
                    : SpecializedTasks.Null<string>();

                // If we happen to get the result of the error task before the refactoring task,
                // and that result is non-null, we can just cancel the refactoring task.
                var result = await errorTask.ConfigureAwait(false) ?? await refactoringTask.ConfigureAwait(false);
                linkedTokenSource.Cancel();

                return result == null
                    ? null
                    : _suggestedActionCategoryRegistry.CreateSuggestedActionCategorySet(result);
            }

            private async Task<string?> GetFixLevelAsync(
                TextDocument document,
                SnapshotSpan range,
                CodeActionOptionsProvider fallbackOptions,
                CancellationToken cancellationToken)
            {
                // Ensure we yield the thread that called into us, allowing it to continue onwards.
                await TaskScheduler.Default.SwitchTo(alwaysYield: true);

                // Ensure we can get the snapshot of our state.  If not, we were disposed between this task being
                // created, and eventually run.
                using var state = _state.TryAddReference();
                if (state is null)
                    return null;

                var lowPriorityAnalyzerData = new SuggestedActionPriorityProvider.LowPriorityAnalyzersAndDiagnosticIds();

                foreach (var order in Orderings)
                {
                    var priority = TryGetPriority(order);
                    Contract.ThrowIfNull(priority);
                    var priorityProvider = new SuggestedActionPriorityProvider(priority.Value, lowPriorityAnalyzerData);

                    var result = await GetFixCategoryAsync(priorityProvider).ConfigureAwait(false);
                    if (result != null)
                        return result;
                }

                return null;

                async Task<string?> GetFixCategoryAsync(ICodeActionRequestPriorityProvider priorityProvider)
                {
                    if (state.Target.Owner._codeFixService != null &&
                        state.Target.SubjectBuffer.SupportsCodeFixes())
                    {
                        var result = await state.Target.Owner._codeFixService.GetMostSevereFixAsync(
                            document, range.Span.ToTextSpan(), priorityProvider, fallbackOptions, cancellationToken).ConfigureAwait(false);

                        if (result.HasFix)
                        {
                            Logger.Log(FunctionId.SuggestedActions_HasSuggestedActionsAsync);
                            return result.CodeFixCollection.FirstDiagnostic.Severity switch
                            {

                                DiagnosticSeverity.Hidden or DiagnosticSeverity.Info or DiagnosticSeverity.Warning => PredefinedSuggestedActionCategoryNames.CodeFix,
                                DiagnosticSeverity.Error => PredefinedSuggestedActionCategoryNames.ErrorFix,
                                _ => throw ExceptionUtilities.Unreachable(),
                            };
                        }

                        if (!result.UpToDate)
                            return null;
                    }

                    return null;
                }
            }

            private async Task<string?> TryGetRefactoringSuggestedActionCategoryAsync(
                TextDocument document,
                TextSpan? selection,
                CodeActionOptionsProvider fallbackOptions,
                CancellationToken cancellationToken)
            {
                // Ensure we yield the thread that called into us, allowing it to continue onwards.
                await TaskScheduler.Default.SwitchTo(alwaysYield: true);

                // Ensure we can get the snapshot of our state.  If not, we were disposed between this task being
                // created, and eventually run.
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
                            document, selection.Value, fallbackOptions, cancellationToken).ConfigureAwait(false))
                    {
                        return PredefinedSuggestedActionCategoryNames.Refactoring;
                    }
                }

                return null;
            }
        }
    }
}
