// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
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
        private readonly IEnumerable<Lazy<CompletionListProvider, OrderableLanguageAndRoleMetadata>> _allCompletionProviders;
        private readonly ImmutableHashSet<char> _autoBraceCompletionChars;
        private readonly bool _isDebugger;
        private readonly bool _isImmediateWindow;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IEnumerable<Lazy<CompletionListProvider, OrderableLanguageAndRoleMetadata>> allCompletionProviders,
            ImmutableHashSet<char> autoBraceCompletionChars,
            bool isDebugger,
            bool isImmediateWindow)
            : base(textView, subjectBuffer, presenter, asyncListener, null, "Completion")
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _undoHistoryRegistry = undoHistoryRegistry;
            _allCompletionProviders = allCompletionProviders;
            _autoBraceCompletionChars = autoBraceCompletionChars;
            _isDebugger = isDebugger;
            _isImmediateWindow = isImmediateWindow;
        }

        internal static Controller GetInstance(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IEnumerable<Lazy<CompletionListProvider, OrderableLanguageAndRoleMetadata>> allCompletionProviders,
            ImmutableHashSet<char> autoBraceCompletionChars)
        {
            var debuggerTextView = textView as IDebuggerTextView;
            var isDebugger = debuggerTextView != null;
            var isImmediateWindow = isDebugger && debuggerTextView.IsImmediateWindow;

            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_controllerPropertyKey,
                (v, b) => new Controller(textView, subjectBuffer, editorOperationsFactoryService, undoHistoryRegistry,
                    presenter, asyncListener,
                    allCompletionProviders, autoBraceCompletionChars,
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
                var viewSpan = selectedItem == null ? (ViewTextSpan?)null : modelOpt.GetSubjectBufferFilterSpanInViewBuffer(selectedItem.FilterSpan);
                var triggerSpan = viewSpan == null ? null : modelOpt.GetCurrentSpanInSnapshot(viewSpan.Value, this.TextView.TextSnapshot)
                                          .CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

                sessionOpt.PresenterSession.PresentItems(
                    triggerSpan, modelOpt.FilteredItems, selectedItem, modelOpt.Builder,
                    this.SubjectBuffer.GetOption(EditorCompletionOptions.UseSuggestionMode), 
                    modelOpt.IsSoftSelection, modelOpt.CompletionItemFilters, modelOpt.CompletionItemToFilterText);
            }
        }

        private bool StartNewModelComputation(ICompletionService completionService, bool filterItems, bool dismissIfEmptyAllowed = true)
        {
            return StartNewModelComputation(
                completionService,
                CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), filterItems, dismissIfEmptyAllowed);
        }

        private bool StartNewModelComputation(ICompletionService completionService, CompletionTriggerInfo triggerInfo, bool filterItems, bool dismissIfEmptyAllowed = true)
        {
            AssertIsForeground();
            Contract.ThrowIfTrue(sessionOpt != null);

            if (this.TextView.Selection.Mode == TextSelectionMode.Box)
            {
                Trace.WriteLine("Box selection, cannot have completion");

                // No completion with multiple selection
                return false;
            }

            // The caret may no longer be mappable into our subject buffer.
            var caret = TextView.GetCaretPoint(SubjectBuffer);
            if (!caret.HasValue)
            {
                Trace.WriteLine("Caret is not mappable to subject buffer, cannot have completion");

                return false;
            }


            if (this.TextView.Caret.Position.VirtualBufferPosition.IsInVirtualSpace)
            {
                // Convert any virtual whitespace to real whitespace by doing an empty edit at the caret position.
                _editorOperationsFactoryService.GetEditorOperations(TextView).InsertText("");
            }

            var computation = new ModelComputation<Model>(this, PrioritizedTaskScheduler.AboveNormalInstance);

            this.sessionOpt = new Session(this, computation, GetCompletionRules(), Presenter.CreateSession(TextView, SubjectBuffer, null));

            var completionProviders = triggerInfo.TriggerReason == CompletionTriggerReason.Snippets
                ? GetSnippetCompletionProviders()
                : GetCompletionProviders();

            sessionOpt.ComputeModel(completionService, triggerInfo, GetOptions(), completionProviders);

            var filterReason = triggerInfo.TriggerReason == CompletionTriggerReason.BackspaceOrDeleteCommand
                ? CompletionFilterReason.BackspaceOrDelete
                : CompletionFilterReason.TypeChar;

            if (filterItems)
            {
                sessionOpt.FilterModel(filterReason, dismissIfEmptyAllowed: dismissIfEmptyAllowed);
            }
            else
            {
                sessionOpt.IdentifyBestMatchAndFilterToAllItems(filterReason, dismissIfEmptyAllowed: dismissIfEmptyAllowed);
            }

            return true;
        }

        private ICompletionService GetCompletionService()
        {
            AssertIsForeground();

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out workspace))
            {
                Trace.WriteLine("Failed to get a workspace, cannot have a completion session.");
                return null;
            }

            return workspace.Services.GetLanguageServices(this.SubjectBuffer).GetService<ICompletionService>();
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

        private void CommitItem(CompletionItem item)
        {
            AssertIsForeground();

            item = Controller.GetExternallyUsableCompletionItem(item);

            // We should not be getting called if we didn't even have a computation running.
            Contract.ThrowIfNull(this.sessionOpt);
            Contract.ThrowIfNull(this.sessionOpt.Computation.InitialUnfilteredModel);

            // If the selected item is the builder, there's not actually any work to do to commit
            if (item.IsBuilder)
            {
                this.StopModelComputation();
                return;
            }

            var textChange = GetCompletionRules().GetTextChange(item);
            this.Commit(item, textChange, this.sessionOpt.Computation.InitialUnfilteredModel, null);
        }

        /// <summary>
        /// The Model sometimes replaces CompletionItems with DescriptionModifyingCompletionItems.
        /// We need to ensure that all internal actions continue to use the 
        /// DescriptionModifyingCompletionItems and that external actions are given the original
        /// CompletionItems.
        /// </summary>
        private static CompletionItem GetExternallyUsableCompletionItem(CompletionItem item)
        {
            var displayItem = item as DescriptionModifyingCompletionItem;
            return displayItem != null ? displayItem.CompletionItem : item;
        }
    }
}
