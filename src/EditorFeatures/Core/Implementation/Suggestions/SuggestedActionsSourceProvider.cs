// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.SuggestionSupport;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.CSharpContentType)]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.VisualBasicContentType)]
    [VisualStudio.Utilities.Name("Roslyn Code Fix")]
    [VisualStudio.Utilities.Order]
    internal class SuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        private static readonly Guid s_CSharpSourceGuid = new Guid("b967fea8-e2c3-4984-87d4-71a38f49e16a");
        private static readonly Guid s_visualBasicSourceGuid = new Guid("4de30e93-3e0c-40c2-a4ba-1124da4539f6");

        private const int InvalidSolutionVersion = -1;

        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeActionEditHandlerService _editHandler;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public SuggestedActionsSourceProvider(
            ICodeRefactoringService codeRefactoringService,
            IDiagnosticAnalyzerService diagnosticService,
            ICodeFixService codeFixService,
            ICodeActionEditHandlerService editHandler,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _codeRefactoringService = codeRefactoringService;
            _diagnosticService = diagnosticService;
            _codeFixService = codeFixService;
            _editHandler = editHandler;
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.LightBulb);
        }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(textBuffer);

            return new Source(this, textView, textBuffer);
        }

        private class Source : ForegroundThreadAffinitizedObject, ISuggestedActionsSource
        {
            // state that will be only reset when source is disposed.
            private SuggestedActionsSourceProvider _owner;
            private ITextView _textView;
            private ITextBuffer _subjectBuffer;
            private WorkspaceRegistration _registration;

            // mutable state
            private Workspace _workspace;
            private int _lastSolutionVersionReported;

            public Source(SuggestedActionsSourceProvider owner, ITextView textView, ITextBuffer textBuffer)
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

            public event EventHandler<EventArgs> SuggestedActionsChanged;

            public bool TryGetTelemetryId(out Guid telemetryId)
            {
                telemetryId = default(Guid);

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
                    default:
                        return false;
                }
            }

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                AssertIsForeground();

                using (Logger.LogBlock(FunctionId.SuggestedActions_GetSuggestedActions, cancellationToken))
                {
                    var documentAndSnapshot = GetMatchingDocumentAndSnapshotAsync(range.Snapshot, cancellationToken).WaitAndGetResult(cancellationToken);
                    if (!documentAndSnapshot.HasValue)
                    {
                        // this is here to fail test and see why it is failed.
                        System.Diagnostics.Trace.WriteLine("given range is not current");
                        return null;
                    }

                    var document = documentAndSnapshot.Value.Item1;
                    var snapshot = documentAndSnapshot.Value.Item2;

                    var workspace = document.Project.Solution.Workspace;
                    var optionService = workspace.Services.GetService<IOptionService>();
                    var supportSuggestion = workspace.Services.GetService<IDocumentSupportsSuggestionService>();

                    IEnumerable<SuggestedActionSet> result = null;
                    if (supportSuggestion.SupportsCodeFixes(document) && requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                    {
                        var suggestions = Task.Run(
                            async () => await _owner._codeFixService.GetFixesAsync(document, range.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false), cancellationToken).WaitAndGetResult(cancellationToken);

                        result = OrganizeFixes(workspace, suggestions);
                    }

                    if (supportSuggestion.SupportsRefactorings(document))
                    {
                        var refactoringResult = GetRefactorings(requestedActionCategories, document, snapshot, workspace, optionService, cancellationToken);

                        result = result == null ? refactoringResult :
                                    refactoringResult == null ? result :
                                        result.Concat(refactoringResult);
                    }

                    return result;
                }
            }

            /// <summary>
            /// Arrange fixes into groups based on the issue (diagnostic being fixed) and prioritize these groups.
            /// </summary>
            private IEnumerable<SuggestedActionSet> OrganizeFixes(Workspace workspace, IEnumerable<CodeFixCollection> fixCollections)
            {
                var map = ImmutableDictionary.CreateBuilder<Diagnostic, IList<SuggestedAction>>();
                var order = ImmutableArray.CreateBuilder<Diagnostic>();

                // First group fixes by issue (diagnostic).
                GroupFixes(workspace, fixCollections, map, order);

                // Then prioritize between the groups.
                return PrioritizeFixGroups(map.ToImmutable(), order.ToImmutable());
            }

            /// <summary>
            /// Groups fixes by the diagnostic being addressed by each fix.
            /// </summary>
            private void GroupFixes(Workspace workspace, IEnumerable<CodeFixCollection> fixCollections, IDictionary<Diagnostic, IList<SuggestedAction>> map, IList<Diagnostic> order)
            {
                foreach (var fixCollection in fixCollections)
                {
                    var fixes = fixCollection.Fixes;
                    var fixCount = fixes.Length;

                    foreach (var fix in fixes)
                    {
                        // Suppression fixes are handled below.
                        if (!(fix.Action is SuppressionCodeAction))
                        {
                            var fixAllSuggestedActionSet =
                                CodeFixSuggestedAction.GetFixAllSuggestedActionSet(fix.Action, fixCount, fixCollection.FixAllContext,
                                    workspace, _subjectBuffer, _owner._editHandler);

                            var suggestedAction = new CodeFixSuggestedAction(workspace, _subjectBuffer, _owner._editHandler,
                                fix, fixCollection.Provider, fixAllSuggestedActionSet);

                            AddFix(fix, suggestedAction, map, order);
                        }
                    }

                    // Add suppression fixes to the end of a given SuggestedActionSet so that they always show up last in a group.
                    foreach (var fix in fixes)
                    {
                        if (fix.Action is SuppressionCodeAction)
                        {
                            var suggestedAction = new SuppressionSuggestedAction(workspace, _subjectBuffer, _owner._editHandler,
                                fix, fixCollection.Provider);

                            AddFix(fix, suggestedAction, map, order);
                        }
                    }
                }
            }

            private static void AddFix(CodeFix fix, SuggestedAction suggestedAction, IDictionary<Diagnostic, IList<SuggestedAction>> map, IList<Diagnostic> order)
            {
                var diag = fix.PrimaryDiagnostic;

                if (!map.ContainsKey(diag))
                {
                    // Remember the order of the keys for the 'map' dictionary.
                    order.Add(diag);
                    map[diag] = ImmutableArray.CreateBuilder<SuggestedAction>();
                }

                map[diag].Add(suggestedAction);
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
            private static IEnumerable<SuggestedActionSet> PrioritizeFixGroups(IDictionary<Diagnostic, IList<SuggestedAction>> map, IList<Diagnostic> order)
            {
                var sets = ImmutableArray.CreateBuilder<SuggestedActionSet>();

                foreach (var diag in order)
                {
                    var fixes = map[diag];

                    var priority = fixes.All(s => s is SuppressionSuggestedAction) ? SuggestedActionSetPriority.None : SuggestedActionSetPriority.Medium;
                    var applicableToSpan = new Span(diag.Location.SourceSpan.Start, diag.Location.SourceSpan.Length);

                    sets.Add(new SuggestedActionSet(fixes, priority, applicableToSpan));
                }

                return sets.ToImmutable();
            }

            private IEnumerable<SuggestedActionSet> GetRefactorings(
                ISuggestedActionCategorySet requestedActionCategories,
                Document document,
                ITextSnapshot snapshot,
                Workspace workspace,
                IOptionService optionService,
                CancellationToken cancellationToken)
            {
                // For Dev14 Preview, we also want to show refactorings in the CodeFix list when users press Ctrl+.
                if (requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring) ||
                    requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.CodeFix))
                {
                    var refactoringsOn = optionService.GetOption(EditorComponentOnOffOptions.CodeRefactorings);
                    if (!refactoringsOn)
                    {
                        return null;
                    }

                    // Get the selection while on the UI thread.
                    var selection = GetCodeRefactoringSelection(snapshot);
                    if (!selection.HasValue)
                    {
                        // this is here to fail test and see why it is failed.
                        System.Diagnostics.Trace.WriteLine("given range is not current");
                        return null;
                    }

                    var refactorings = Task.Run(
                        async () => await _owner._codeRefactoringService.GetRefactoringsAsync(document, selection.Value, cancellationToken).ConfigureAwait(false), cancellationToken).WaitAndGetResult(cancellationToken);

                    return refactorings.Select(r => OrganizeRefactorings(workspace, r));
                }

                return null;
            }

            /// <summary>
            /// Arrange refactorings into groups.
            /// </summary>
            /// <remarks>
            /// Refactorings are returned in priority order determined based on <see cref="ExtensionOrderAttribute"/>.
            /// Priority for all <see cref="SuggestedActionSet"/>s containing refactorings is set to <see cref="SuggestedActionSetPriority.Low"/>
            /// and should show up after fixes but before suppression fixes in the light bulb menu.
            /// </remarks>
            private SuggestedActionSet OrganizeRefactorings(Workspace workspace, CodeRefactoring refactoring)
            {
                var refactoringSuggestedActions = ImmutableArray.CreateBuilder<SuggestedAction>();

                foreach (var a in refactoring.Actions)
                {
                    refactoringSuggestedActions.Add(
                        new CodeRefactoringSuggestedAction(
                            workspace, _subjectBuffer, _owner._editHandler, a, refactoring.Provider));
                }

                return new SuggestedActionSet(refactoringSuggestedActions.ToImmutable(), SuggestedActionSetPriority.Low);
            }

            public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                var view = _textView;
                var buffer = _subjectBuffer;
                var provider = _owner;

                if (view == null || buffer == null || provider == null)
                {
                    return false;
                }

                using (var asyncToken = provider._listener.BeginAsyncOperation("HasSuggesetedActionsAsync"))
                {
                    var result = await HasSuggesetedActionCoreAsync(view, buffer, provider._codeFixService, range, cancellationToken).ConfigureAwait(false);
                    if (!result.HasValue)
                    {
                        return false;
                    }

                    if (result.Value.HasFix)
                    {
                        Logger.Log(FunctionId.SuggestedActions_HasSuggestedActionsAsync);
                        return true;
                    }

                    if (result.Value.PartialResult)
                    {
                        // reset solution version number so that we can raise suggested action changed event
                        Volatile.Write(ref _lastSolutionVersionReported, InvalidSolutionVersion);
                        return false;
                    }

                    return false;
                }
            }

            private static async Task<FirstDiagnosticResult?> HasSuggesetedActionCoreAsync(
                ITextView view, ITextBuffer buffer, ICodeFixService service, SnapshotSpan range, CancellationToken cancellationToken)
            {
                var documentAndSnapshot = await GetMatchingDocumentAndSnapshotAsync(range.Snapshot, cancellationToken).ConfigureAwait(false);
                if (!documentAndSnapshot.HasValue)
                {
                    return null;
                }

                var document = documentAndSnapshot.Value.Item1;

                // make sure current document support codefix
                var supportCodeFix = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsSuggestionService>();
                if (!supportCodeFix.SupportsCodeFixes(document))
                {
                    return null;
                }

                return await service.GetFirstDiagnosticWithFixAsync(document, range.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            }

            private TextSpan? GetCodeRefactoringSelection(ITextSnapshot snapshot)
            {
                var selectedSpans = _textView.Selection.SelectedSpans
                    .SelectMany(ss => _textView.BufferGraph.MapDownToBuffer(ss, SpanTrackingMode.EdgeExclusive, _subjectBuffer))
                    .Where(ss => !_textView.IsReadOnlyOnSurfaceBuffer(ss))
                    .ToList();

                // We only support refactorings when there is a single selection in the document.
                if (selectedSpans.Count != 1)
                {
                    return null;
                }

                var selectedSpan = selectedSpans[0];
                return selectedSpan.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive).Span.ToTextSpan();
            }

            private static async Task<ValueTuple<Document, ITextSnapshot>?> GetMatchingDocumentAndSnapshotAsync(ITextSnapshot givenSnapshot, CancellationToken cancellationToken)
            {
                var buffer = givenSnapshot.TextBuffer;
                if (buffer == null)
                {
                    return null;
                }

                var workspace = buffer.GetWorkspace();
                if (workspace == null)
                {
                    return null;
                }

                var documentId = workspace.GetDocumentIdInCurrentContext(buffer.AsTextContainer());
                if (documentId == null)
                {
                    return null;
                }

                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    return null;
                }

                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var snapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                if (snapshot == null || snapshot.Version.ReiteratedVersionNumber != givenSnapshot.Version.ReiteratedVersionNumber)
                {
                    return null;
                }

                return ValueTuple.Create(document, snapshot);
            }

            private void OnTextViewClosed(object sender, EventArgs e)
            {
                Dispose();
            }

            private void OnWorkspaceChanged(object sender, EventArgs e)
            {
                // REVIEW: this event should give both old and new workspace as argument so that
                // one doesnt need to hold onto workspace in field.

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

            private void OnActiveContextChanged(object sender, DocumentEventArgs e)
            {
                // REVIEW: it would be nice for changed event to pass in both old and new document.
                OnSuggestedActionsChanged(e.Document.Project.Solution.Workspace, e.Document.Id, e.Document.Project.Solution.WorkspaceVersion);
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
                if (_subjectBuffer == null)
                {
                    return;
                }

                var workspace = _subjectBuffer.GetWorkspace();

                // workspace is not ready, nothing to do.
                if (workspace == null || workspace != currentWorkspace)
                {
                    return;
                }

                if (currentDocumentId != workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer()) ||
                    solutionVersion == Volatile.Read(ref _lastSolutionVersionReported))
                {
                    return;
                }

                // make sure we only raise event once for same solution version.
                // light bulb controller will call us back to find out new information
                var changed = this.SuggestedActionsChanged;
                if (changed != null)
                {
                    changed(this, EventArgs.Empty);
                }

                Volatile.Write(ref _lastSolutionVersionReported, solutionVersion);
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    var updateSource = (IDiagnosticUpdateSource)_owner._diagnosticService;
                    updateSource.DiagnosticsUpdated -= OnDiagnosticsUpdated;
                    _owner = null;
                }

                if (_workspace != null)
                {
                    _workspace.DocumentActiveContextChanged -= OnActiveContextChanged;
                    _workspace = null;
                }

                if (_registration != null)
                {
                    _registration.WorkspaceChanged -= OnWorkspaceChanged;
                    _registration = null;
                }

                if (_textView != null)
                {
                    _textView.Closed -= OnTextViewClosed;
                    _textView = null;
                }

                if (_subjectBuffer != null)
                {
                    _subjectBuffer = null;
                }
            }
        }
    }
}
