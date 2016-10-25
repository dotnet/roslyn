// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller :
        AbstractController<Controller.Session, Model, ICompletionPresenterSession, ICompletionSession>,
        ICommandHandler<TabKeyCommandArgs>,
        ICommandHandler<ToggleCompletionModeCommandArgs>,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<InvokeCompletionListCommandArgs>,
        ICommandHandler<CommitUniqueCompletionListItemCommandArgs>,
        ICommandHandler<PageUpKeyCommandArgs>,
        ICommandHandler<PageDownKeyCommandArgs>,
        ICommandHandler<CutCommandArgs>,
        ICommandHandler<PasteCommandArgs>,
        ICommandHandler<BackspaceKeyCommandArgs>,
        ICommandHandler<InsertSnippetCommandArgs>,
        ICommandHandler<SurroundWithCommandArgs>,
        ICommandHandler<AutomaticLineEnderCommandArgs>,
        ICommandHandler<SaveCommandArgs>,
        ICommandHandler<DeleteKeyCommandArgs>,
        ICommandHandler<SelectAllCommandArgs>
    {
        private static readonly object s_controllerPropertyKey = new object();

        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IWaitIndicator _waitIndicator;
        private readonly ImmutableHashSet<char> _autoBraceCompletionChars;
        private readonly bool _isDebugger;
        private readonly bool _isImmediateWindow;
        private readonly ImmutableHashSet<string> _roles;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IWaitIndicator waitIndicator,
            IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession> presenter,
            IAsynchronousOperationListener asyncListener,
            ImmutableHashSet<char> autoBraceCompletionChars,
            bool isDebugger,
            bool isImmediateWindow)
            : base(textView, subjectBuffer, presenter, asyncListener, null, "Completion")
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _undoHistoryRegistry = undoHistoryRegistry;
            _waitIndicator = waitIndicator;
            _autoBraceCompletionChars = autoBraceCompletionChars;
            _isDebugger = isDebugger;
            _isImmediateWindow = isImmediateWindow;
            _roles = textView.Roles.ToImmutableHashSet();
        }

        internal static Controller GetInstance(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IWaitIndicator waitIndicator,
            IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession> presenter,
            IAsynchronousOperationListener asyncListener,
            ImmutableHashSet<char> autoBraceCompletionChars)
        {
            var debuggerTextView = textView as IDebuggerTextView;
            var isDebugger = debuggerTextView != null;
            var isImmediateWindow = isDebugger && debuggerTextView.IsImmediateWindow;

            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_controllerPropertyKey,
                (v, b) => new Controller(textView, subjectBuffer, editorOperationsFactoryService, undoHistoryRegistry, waitIndicator,
                    presenter, asyncListener,
                    autoBraceCompletionChars,
                    isDebugger, isImmediateWindow));
        }

        internal bool WaitForComputation()
        {
            if (sessionOpt == null)
            {
                return false;
            }

            var model = sessionOpt.WaitForModel();

            return model != null;
        }

        private SnapshotPoint GetCaretPointInViewBuffer()
        {
            AssertIsForeground();
            return this.TextView.Caret.Position.BufferPosition;
        }

        private SnapshotPoint GetCaretPointInSubjectBuffer()
        {
            AssertIsForeground();
            return this.TextView.BufferGraph.MapUpOrDownToBuffer(this.TextView.Caret.Position.BufferPosition, this.SubjectBuffer).GetValueOrDefault();
        }

        internal override void OnModelUpdated(Model modelOpt)
        {
            AssertIsForeground();
            if (modelOpt == null)
            {
                this.StopModelComputation();
            }
            else
            {
                var selectedItem = modelOpt.SelectedItem;
                var viewSpan = selectedItem == null ? (ViewTextSpan?)null : modelOpt.GetViewBufferSpan(selectedItem.Item.Span);
                var triggerSpan = viewSpan == null ? null : modelOpt.GetCurrentSpanInSnapshot(viewSpan.Value, this.TextView.TextSnapshot)
                                          .CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

                sessionOpt.PresenterSession.PresentItems(
                    triggerSpan, modelOpt.FilteredItems, selectedItem, modelOpt.SuggestionModeItem,
                    this.SubjectBuffer.GetFeatureOnOffOption(EditorCompletionOptions.UseSuggestionMode),
                    modelOpt.IsSoftSelection, modelOpt.CompletionItemFilters, modelOpt.FilterText);
            }
        }

        private bool StartNewModelComputation(
            CompletionService completionService, bool filterItems, bool dismissIfEmptyAllowed)
        {
            return StartNewModelComputation(
                completionService, CompletionTrigger.Default, filterItems, dismissIfEmptyAllowed);
        }

        private bool StartNewModelComputation(
            CompletionService completionService,
            CompletionTrigger trigger,
            bool filterItems,
            bool dismissIfEmptyAllowed)
        {
            AssertIsForeground();
            Contract.ThrowIfTrue(sessionOpt != null);

            if (completionService == null)
            {
                return false;
            }

            if (this.TextView.Selection.Mode == TextSelectionMode.Box)
            {
                // No completion with multiple selection
                return false;
            }

            // The caret may no longer be mappable into our subject buffer.
            var caret = TextView.GetCaretPoint(SubjectBuffer);
            if (!caret.HasValue)
            {
                return false;
            }

            if (this.TextView.Caret.Position.VirtualBufferPosition.IsInVirtualSpace)
            {
                // Convert any virtual whitespace to real whitespace by doing an empty edit at the caret position.
                _editorOperationsFactoryService.GetEditorOperations(TextView).InsertText("");
            }

            var computation = new ModelComputation<Model>(this, PrioritizedTaskScheduler.AboveNormalInstance);

            this.sessionOpt = new Session(this, computation, Presenter.CreateSession(TextView, SubjectBuffer, null));

            sessionOpt.ComputeModel(completionService, trigger, _roles, GetOptions());

            var filterReason = trigger.Kind == CompletionTriggerKind.Deletion
                ? CompletionFilterReason.BackspaceOrDelete
                : trigger.Kind == CompletionTriggerKind.Other
                    ? CompletionFilterReason.Other
                    : CompletionFilterReason.TypeChar;

            FilterToSomeOrAllItems(filterItems, dismissIfEmptyAllowed, filterReason);

            return true;
        }

        private void FilterToSomeOrAllItems(bool filterItems, bool dismissIfEmptyAllowed, CompletionFilterReason filterReason)
        {
            if (filterItems)
            {
                sessionOpt.FilterModel(
                    filterReason,
                    recheckCaretPosition: false,
                    dismissIfEmptyAllowed: dismissIfEmptyAllowed,
                    filterState: null);
            }
            else
            {
                sessionOpt.IdentifyBestMatchAndFilterToAllItems(
                    filterReason,
                    recheckCaretPosition: false,
                    dismissIfEmptyAllowed: dismissIfEmptyAllowed);
            }
        }

        private CompletionService GetCompletionService()
        {
            Workspace workspace;
            if (!Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out workspace))
            {
                return null;
            }

            return workspace.Services.GetLanguageServices(this.SubjectBuffer).GetService<CompletionService>();
        }

        private OptionSet GetOptions()
        {
            AssertIsForeground();

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out workspace))
            {
                return null;
            }

            return _isDebugger
                ? workspace.Options.WithDebuggerCompletionOptions()
                : workspace.Options;
        }

        private void CommitItem(PresentationItem item)
        {
            AssertIsForeground();

            // We should not be getting called if we didn't even have a computation running.
            Contract.ThrowIfNull(this.sessionOpt);
            Contract.ThrowIfNull(this.sessionOpt.Computation.InitialUnfilteredModel);

            // If the selected item is the builder, there's not actually any work to do to commit
            if (item.IsSuggestionModeItem)
            {
                this.StopModelComputation();
                return;
            }

            this.CommitOnNonTypeChar(item, this.sessionOpt.Computation.InitialUnfilteredModel);
        }

        private const int MaxMRUSize = 10;
        private ImmutableArray<string> _recentItems = ImmutableArray<string>.Empty;

        public void MakeMostRecentItem(string item)
        {
            bool updated = false;

            while (!updated)
            {
                var oldItems = _recentItems;

                // We need to remove the item if it's already in the list.
                var newItems = oldItems.Remove(item);

                // If we're at capacity, we need to remove the least recent item.
                if (newItems.Length == MaxMRUSize)
                {
                    newItems = newItems.RemoveAt(0);
                }

                newItems = newItems.Add(item);

                updated = ImmutableInterlocked.InterlockedCompareExchange(ref _recentItems, newItems, oldItems) == oldItems;
            }
        }

        public ImmutableArray<string> GetRecentItems()
        {
            return _recentItems;
        }
    }
}