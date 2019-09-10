// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    internal abstract class AbstractCommandHandlerTestState : IDisposable
    {
        public readonly TestWorkspace Workspace;
        public readonly IEditorOperations EditorOperations;
        public readonly ITextUndoHistoryRegistry UndoHistoryRegistry;
        private readonly ITextView _textView;
        private readonly ITextBuffer _subjectBuffer;

        public AbstractCommandHandlerTestState(
            XElement workspaceElement,
            IList<Type> excludedTypes = null,
            ComposableCatalog extraParts = null,
            string workspaceKind = null)
            : this(workspaceElement, GetExportProvider(excludedTypes, extraParts), workspaceKind)
        {
        }

        /// <summary>
        /// This can use input files with an (optionally) annotated span 'Selection' and a cursor position ($$),
        /// and use it to create a selected span in the TextView.
        /// 
        /// For instance, the following will create a TextView that has a multiline selection with the cursor at the end.
        /// 
        /// Sub Goo
        ///     {|Selection:SomeMethodCall()
        ///     AnotherMethodCall()$$|}
        /// End Sub
        ///
        /// You can use multiple selection spans to create box selections.
        ///
        /// Sub Goo
        ///     {|Selection:$$box|}11111
        ///     {|Selection:sel|}111
        ///     {|Selection:ect|}1
        ///     {|Selection:ion|}1111111
        /// End Sub
        /// </summary>
        public AbstractCommandHandlerTestState(
            XElement workspaceElement,
            ExportProvider exportProvider,
            string workspaceKind)
        {
            this.Workspace = TestWorkspace.CreateWorkspace(
                workspaceElement,
                exportProvider: exportProvider,
                workspaceKind: workspaceKind);

            var cursorDocument = this.Workspace.Documents.First(d => d.CursorPosition.HasValue);
            _textView = cursorDocument.GetTextView();
            _subjectBuffer = cursorDocument.GetTextBuffer();

            if (cursorDocument.AnnotatedSpans.TryGetValue("Selection", out var selectionSpanList))
            {
                var firstSpan = selectionSpanList.First();
                var lastSpan = selectionSpanList.Last();
                var cursorPosition = cursorDocument.CursorPosition.Value;

                Assert.True(cursorPosition == firstSpan.Start || cursorPosition == firstSpan.End
                            || cursorPosition == lastSpan.Start || cursorPosition == lastSpan.End,
                    "cursorPosition wasn't at an endpoint of the 'Selection' annotated span");

                _textView.Selection.Mode = selectionSpanList.Length > 1
                    ? TextSelectionMode.Box
                    : TextSelectionMode.Stream;

                SnapshotPoint boxSelectionStart, boxSelectionEnd;
                bool isReversed;

                if (cursorPosition == firstSpan.Start || cursorPosition == lastSpan.End)
                {
                    // Top-left and bottom-right corners used as anchor points.
                    boxSelectionStart = new SnapshotPoint(_subjectBuffer.CurrentSnapshot, firstSpan.Start);
                    boxSelectionEnd = new SnapshotPoint(_subjectBuffer.CurrentSnapshot, lastSpan.End);
                    isReversed = cursorPosition == firstSpan.Start;
                }
                else
                {
                    // Top-right and bottom-left corners used as anchor points.
                    boxSelectionStart = new SnapshotPoint(_subjectBuffer.CurrentSnapshot, firstSpan.End);
                    boxSelectionEnd = new SnapshotPoint(_subjectBuffer.CurrentSnapshot, lastSpan.Start);
                    isReversed = cursorPosition == firstSpan.End;
                }

                _textView.Selection.Select(
                        new SnapshotSpan(boxSelectionStart, boxSelectionEnd),
                        isReversed: isReversed);
            }
            else
            {
                _textView.Caret.MoveTo(
                    new SnapshotPoint(
                        _textView.TextBuffer.CurrentSnapshot,
                        cursorDocument.CursorPosition.Value));
            }

            this.EditorOperations = GetService<IEditorOperationsFactoryService>().GetEditorOperations(_textView);
            this.UndoHistoryRegistry = GetService<ITextUndoHistoryRegistry>();
        }

        public void Dispose()
        {
            Workspace.Dispose();
        }

        public T GetService<T>()
        {
            return Workspace.GetService<T>();
        }

        internal static ExportProvider GetExportProvider(IList<Type> excludedTypes, ComposableCatalog extraParts)
        {
            excludedTypes = excludedTypes ?? Type.EmptyTypes;

            if (excludedTypes.Count == 0 && (extraParts == null || extraParts.Parts.Count == 0))
            {
                return TestExportProvider.ExportProviderFactoryWithCSharpAndVisualBasic.CreateExportProvider();
            }

            var baseCatalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic;

            var filteredCatalog = baseCatalog.WithoutPartsOfTypes(excludedTypes);

            return ExportProviderCache.GetOrCreateExportProviderFactory(filteredCatalog.WithParts(extraParts)).CreateExportProvider();
        }

        public virtual ITextView TextView
        {
            get { return _textView; }
        }

        public virtual ITextBuffer SubjectBuffer
        {
            get { return _subjectBuffer; }
        }

        #region MEF
        public Lazy<TExport, TMetadata> GetExport<TExport, TMetadata>()
        {
            return (Lazy<TExport, TMetadata>)(object)Workspace.ExportProvider.GetExport<TExport, TMetadata>();
        }

        public IEnumerable<Lazy<TExport, TMetadata>> GetExports<TExport, TMetadata>()
        {
            return Workspace.ExportProvider.GetExports<TExport, TMetadata>();
        }

        public T GetExportedValue<T>()
        {
            return Workspace.ExportProvider.GetExportedValue<T>();
        }

        public IEnumerable<T> GetExportedValues<T>()
        {
            return Workspace.ExportProvider.GetExportedValues<T>();
        }

        protected static IEnumerable<Lazy<TProvider, OrderableLanguageMetadata>> CreateLazyProviders<TProvider>(
            TProvider[] providers,
            string languageName)
        {
            if (providers == null)
            {
                return Array.Empty<Lazy<TProvider, OrderableLanguageMetadata>>();
            }

            return providers.Select(p =>
                new Lazy<TProvider, OrderableLanguageMetadata>(
                    () => p,
                    new OrderableLanguageMetadata(
                        new Dictionary<string, object> {
                            {"Language", languageName },
                            {"Name", string.Empty }}),
                    true));
        }

        protected static IEnumerable<Lazy<TProvider, OrderableLanguageAndRoleMetadata>> CreateLazyProviders<TProvider>(
            TProvider[] providers,
            string languageName,
            string[] roles)
        {
            if (providers == null)
            {
                return Array.Empty<Lazy<TProvider, OrderableLanguageAndRoleMetadata>>();
            }

            return providers.Select(p =>
                new Lazy<TProvider, OrderableLanguageAndRoleMetadata>(
                    () => p,
                    new OrderableLanguageAndRoleMetadata(
                        new Dictionary<string, object> {
                            {"Language", languageName },
                            {"Name", string.Empty },
                            {"Roles", roles }}),
                    true));
        }
        #endregion

        #region editor related operation
        public virtual void SendBackspace()
        {
            EditorOperations.Backspace();
        }

        public virtual void SendDelete()
        {
            EditorOperations.Delete();
        }

        public void SendRightKey(bool extendSelection = false)
        {
            EditorOperations.MoveToNextCharacter(extendSelection);
        }

        public void SendLeftKey(bool extendSelection = false)
        {
            EditorOperations.MoveToPreviousCharacter(extendSelection);
        }

        public void SendMoveToPreviousCharacter(bool extendSelection = false)
        {
            EditorOperations.MoveToPreviousCharacter(extendSelection);
        }

        public virtual void SendDeleteWordToLeft()
        {
            EditorOperations.DeleteWordToLeft();
        }

        public void SendUndo(int count = 1)
        {
            var history = UndoHistoryRegistry.GetHistory(SubjectBuffer);
            history.Undo(count);
        }

        public void SelectAndMoveCaret(int offset)
        {
            var currentCaret = GetCaretPoint();
            EditorOperations.SelectAndMoveCaret(
                new VirtualSnapshotPoint(SubjectBuffer.CurrentSnapshot, currentCaret.BufferPosition.Position),
                new VirtualSnapshotPoint(SubjectBuffer.CurrentSnapshot, currentCaret.BufferPosition.Position + offset));
        }
        #endregion

        #region test/information/verification
        public ITextSnapshotLine GetLineFromCurrentCaretPosition()
        {
            var caretPosition = GetCaretPoint();
            return SubjectBuffer.CurrentSnapshot.GetLineFromPosition(caretPosition.BufferPosition);
        }

        public string GetLineTextFromCaretPosition()
        {
            var caretPosition = GetCaretPoint();
            return caretPosition.BufferPosition.GetContainingLine().GetText();
        }

        public (string TextBeforeCaret, string TextAfterCaret) GetLineTextAroundCaretPosition()
        {
            int bufferCaretPosition = GetCaretPoint().BufferPosition;
            var line = SubjectBuffer.CurrentSnapshot.GetLineFromPosition(bufferCaretPosition);
            var lineCaretPosition = bufferCaretPosition - line.Start.Position;

            var text = line.GetText();
            var textBeforeCaret = text.Substring(0, lineCaretPosition);
            var textAfterCaret = text.Substring(lineCaretPosition, text.Length - lineCaretPosition);

            return (textBeforeCaret, textAfterCaret);
        }

        public string GetDocumentText()
        {
            return SubjectBuffer.CurrentSnapshot.GetText();
        }

        public CaretPosition GetCaretPoint()
        {
            return TextView.Caret.Position;
        }

        /// <summary>
        /// Used in synchronous methods to ensure all outstanding <see cref="IAsyncToken"/> work has been
        /// completed.
        /// </summary>
        public void AssertNoAsynchronousOperationsRunning()
        {
            var provider = Workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            Assert.False(provider.HasPendingWaiter(FeatureAttribute.EventHookup, FeatureAttribute.CompletionSet, FeatureAttribute.SignatureHelp), "IAsyncTokens unexpectedly alive. Call WaitForAsynchronousOperationsAsync before this method");
        }

        // Cannot make it void even for the modern completion for now
        // because it is used by SignatureHelp for both Modern and Legacy completions.
        public async Task WaitForAsynchronousOperationsAsync()
        {
            var provider = Workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            await provider.WaitAllDispatcherOperationAndTasksAsync(FeatureAttribute.EventHookup, FeatureAttribute.CompletionSet, FeatureAttribute.SignatureHelp);
        }

        public void AssertMatchesTextStartingAtLine(int line, string text)
        {
            var lines = text.Split('\r');
            foreach (var expectedLine in lines)
            {
                Assert.Equal(expectedLine.Trim(), SubjectBuffer.CurrentSnapshot.GetLineFromLineNumber(line).GetText().Trim());
                line += 1;
            }
        }
        #endregion

        #region command handler
        public void SendBackspace(Action<BackspaceKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new BackspaceKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendDelete(Action<DeleteKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new DeleteKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendWordDeleteToStart(Action<WordDeleteToStartCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new WordDeleteToStartCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendEscape(Action<EscapeKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new EscapeKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public bool SendEscape(Func<EscapeKeyCommandArgs, CommandExecutionContext, bool> commandHandler)
        {
            return commandHandler(new EscapeKeyCommandArgs(TextView, SubjectBuffer), TestCommandExecutionContext.Create());
        }

        public void SendUpKey(Action<UpKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new UpKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendDownKey(Action<DownKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new DownKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendTab(Action<TabKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new TabKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public bool SendTab(Func<TabKeyCommandArgs, CommandExecutionContext, bool> commandHandler)
        {
            return commandHandler(new TabKeyCommandArgs(TextView, SubjectBuffer), TestCommandExecutionContext.Create());
        }

        public void SendBackTab(Action<BackTabKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new BackTabKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public bool SendBackTab(Func<BackTabKeyCommandArgs, CommandExecutionContext, bool> commandHandler)
        {
            return commandHandler(new BackTabKeyCommandArgs(TextView, SubjectBuffer), TestCommandExecutionContext.Create());
        }

        public void SendReturn(Action<ReturnKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new ReturnKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public bool SendReturn(Func<ReturnKeyCommandArgs, CommandExecutionContext, bool> commandHandler)
        {
            return commandHandler(new ReturnKeyCommandArgs(TextView, SubjectBuffer), TestCommandExecutionContext.Create());
        }

        public void SendPageUp(Action<PageUpKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new PageUpKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendPageDown(Action<PageDownKeyCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new PageDownKeyCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendCut(Action<CutCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new CutCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendPaste(Action<PasteCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new PasteCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendInvokeCompletionList(Action<InvokeCompletionListCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new InvokeCompletionListCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendCommitUniqueCompletionListItem(Action<CommitUniqueCompletionListItemCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new CommitUniqueCompletionListItemCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendInsertSnippetCommand(Action<InsertSnippetCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new InsertSnippetCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public bool SendInsertSnippetCommand(Func<InsertSnippetCommandArgs, CommandExecutionContext, bool> commandHandler)
        {
            return commandHandler(new InsertSnippetCommandArgs(TextView, SubjectBuffer), TestCommandExecutionContext.Create());
        }

        public void SendSurroundWithCommand(Action<SurroundWithCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new SurroundWithCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public bool SendSurroundWithCommand(Func<SurroundWithCommandArgs, CommandExecutionContext, bool> commandHandler)
        {
            return commandHandler(new SurroundWithCommandArgs(TextView, SubjectBuffer), TestCommandExecutionContext.Create());
        }

        public void SendInvokeSignatureHelp(Action<InvokeSignatureHelpCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new InvokeSignatureHelpCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendTypeChar(char typeChar, Action<TypeCharCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new TypeCharCommandArgs(TextView, SubjectBuffer, typeChar), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendTypeChars(string typeChars, Action<TypeCharCommandArgs, Action, CommandExecutionContext> commandHandler)
        {
            foreach (var ch in typeChars)
            {
                var localCh = ch;
                SendTypeChar(ch, commandHandler, () => EditorOperations.InsertText(localCh.ToString()));
            }
        }

        public void SendSave(Action<SaveCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new SaveCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendSelectAll(Action<SelectAllCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new SelectAllCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }

        public void SendToggleCompletionMode(Action<ToggleCompletionModeCommandArgs, Action, CommandExecutionContext> commandHandler, Action nextHandler)
        {
            commandHandler(new ToggleCompletionModeCommandArgs(TextView, SubjectBuffer), nextHandler, TestCommandExecutionContext.Create());
        }
        #endregion
    }
}
