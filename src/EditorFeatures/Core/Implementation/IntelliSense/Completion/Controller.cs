﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
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
        private readonly ImmutableHashSet<char> _autoBraceCompletionChars;
        private readonly bool _isDebugger;
        private readonly bool _isImmediateWindow;
        private readonly ImmutableHashSet<string> _roles;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession> presenter,
            IAsynchronousOperationListener asyncListener,
            ImmutableHashSet<char> autoBraceCompletionChars,
            bool isDebugger,
            bool isImmediateWindow)
            : base(textView, subjectBuffer, presenter, asyncListener, null, "Completion")
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _undoHistoryRegistry = undoHistoryRegistry;
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
            IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession> presenter,
            IAsynchronousOperationListener asyncListener,
            ImmutableHashSet<char> autoBraceCompletionChars)
        {
            var debuggerTextView = textView as IDebuggerTextView;
            var isDebugger = debuggerTextView != null;
            var isImmediateWindow = isDebugger && debuggerTextView.IsImmediateWindow;

            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_controllerPropertyKey,
                (v, b) => new Controller(
                    textView, subjectBuffer, editorOperationsFactoryService, undoHistoryRegistry, 
                    presenter, asyncListener, autoBraceCompletionChars, isDebugger, isImmediateWindow));
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

        private bool ShouldBlockForCompletionItems()
        {
            var service = GetCompletionService();
            var options = GetOptions();
            if (service == null || options == null)
            {
                return true;
            }

            return options.GetOption(CompletionOptions.BlockForCompletionItems, service.Language);
        }

        private Model WaitForModel()
        {
            this.AssertIsForeground();

            var shouldBlock = ShouldBlockForCompletionItems();
            var model = sessionOpt.WaitForModel_DoNotCallDirectly(shouldBlock);
            if (model == null && !shouldBlock)
            {
                // We didn't get a model back, and we're a language that doesn't want to block
                // when this happens.  Essentially, the user typed something like a commit 
                // character before we got any results back.  In this case, because we're not
                // willing to block, we just stop everything that we're doing and return to 
                // the non-active state.
                DismissSessionIfActive();
            }

            return model;
        }

        internal override void OnModelUpdated(Model modelOpt)
        {
            AssertIsForeground();
            if (modelOpt == null)
            {
                this.DismissSessionIfActive();
            }
            else
            {
                var selectedItem = modelOpt.SelectedItemOpt;
                var viewSpan = selectedItem == null ? (ViewTextSpan?)null : modelOpt.GetViewBufferSpan(selectedItem.Span);
                var triggerSpan = viewSpan == null 
                    ? null
                    : modelOpt.GetCurrentSpanInSnapshot(viewSpan.Value, this.TextView.TextSnapshot)
                              .CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

                sessionOpt.PresenterSession.PresentItems(
                    triggerSpan, modelOpt.FilteredItems, selectedItem,
                    modelOpt.SuggestionModeItem, modelOpt.UseSuggestionMode,
                    modelOpt.IsSoftSelection, modelOpt.CompletionItemFilters, modelOpt.FilterText);
            }
        }

        private bool StartNewModelComputation(
            CompletionService completionService,
            CompletionTrigger trigger)
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
            sessionOpt.FilterModel(trigger.GetFilterReason(), filterState: null);

            return true;
        }

        private CompletionService GetCompletionService()
        {
            if (!Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out var workspace))
            {
                return null;
            }

            return workspace.Services.GetLanguageServices(this.SubjectBuffer).GetService<CompletionService>();
        }

        private OptionSet GetOptions()
        {
            AssertIsForeground();
            if (!Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out var workspace))
            {
                return null;
            }

            return _isDebugger
                ? workspace.Options.WithDebuggerCompletionOptions()
                : workspace.Options;
        }

        private void CommitItem(CompletionItem item)
        {
            AssertIsForeground();

            // We should not be getting called if we didn't even have a computation running.
            Contract.ThrowIfNull(this.sessionOpt);
            Contract.ThrowIfNull(this.sessionOpt.Computation.InitialUnfilteredModel);

            var model = sessionOpt.InitialUnfilteredModel;

            // If the selected item is the builder, there's not actually any work to do to commit
            if (item != model.SuggestionModeItem)
            {
                this.CommitOnNonTypeChar(item, this.sessionOpt.Computation.InitialUnfilteredModel);
            }

            // Make sure we're always dismissed after any commit request.
            this.DismissSessionIfActive();
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
