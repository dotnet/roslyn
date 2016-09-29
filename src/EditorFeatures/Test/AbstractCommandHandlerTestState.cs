// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading.Tasks;

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
            ComposableCatalog extraParts = null,
            bool useMinimumCatalog = false,
            string workspaceKind = null)
            : this(workspaceElement, GetExportProvider(useMinimumCatalog, extraParts), workspaceKind)
        {
        }

        /// <summary>
        /// This can use input files with an (optionally) annotated span 'Selection' and a cursor position ($$),
        /// and use it to create a selected span in the TextView.
        /// 
        /// For instance, the following will create a TextView that has a multiline selection with the cursor at the end.
        /// 
        /// Sub Foo
        ///     {|Selection:SomeMethodCall()
        ///     AnotherMethodCall()$$|}
        /// End Sub
        ///
        /// You can use multiple selection spans to create box selections.
        ///
        /// Sub Foo
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

            IList<Text.TextSpan> selectionSpanList;

            if (cursorDocument.AnnotatedSpans.TryGetValue("Selection", out selectionSpanList))
            {
                var firstSpan = selectionSpanList.First();
                var lastSpan = selectionSpanList.Last();
                var cursorPosition = cursorDocument.CursorPosition.Value;

                Assert.True(cursorPosition == firstSpan.Start || cursorPosition == firstSpan.End
                            || cursorPosition == lastSpan.Start || cursorPosition == lastSpan.End,
                    "cursorPosition wasn't at an endpoint of the 'Selection' annotated span");

                _textView.Selection.Mode = selectionSpanList.Count > 1
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

        private static ExportProvider GetExportProvider(bool useMinimumCatalog, ComposableCatalog extraParts)
        {
            var baseCatalog = useMinimumCatalog
                ? TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic
                : TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic;

            if (extraParts == null)
            {
                return MinimalTestExportProvider.CreateExportProvider(baseCatalog);
            }

            return MinimalTestExportProvider.CreateExportProvider(baseCatalog.WithParts(extraParts));
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
        public void SendBackspace()
        {
            EditorOperations.Backspace();
        }

        public void SendDelete()
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

        public void SendDeleteWordToLeft()
        {
            EditorOperations.DeleteWordToLeft();
        }

        public void SendUndo(int count = 1)
        {
            var history = UndoHistoryRegistry.GetHistory(SubjectBuffer);
            history.Undo(count);
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
            var caretPosition = Workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition.Value;
            return SubjectBuffer.CurrentSnapshot.GetLineFromPosition(caretPosition).GetText();
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
            var waiters = Workspace.ExportProvider.GetExportedValues<IAsynchronousOperationWaiter>();
            Assert.False(waiters.Any(x => x.HasPendingWork), "IAsyncTokens unexpectedly alive. Call WaitForAsynchronousOperationsAsync before this method");
        }

        public async Task WaitForAsynchronousOperationsAsync()
        {
            var waiters = Workspace.ExportProvider.GetExportedValues<IAsynchronousOperationWaiter>();
            await waiters.WaitAllAsync();
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
        public void SendBackspace(Action<BackspaceKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new BackspaceKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendDelete(Action<DeleteKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new DeleteKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendWordDeleteToStart(Action<WordDeleteToStartCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new WordDeleteToStartCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendEscape(Action<EscapeKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new EscapeKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendUpKey(Action<UpKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new UpKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendDownKey(Action<DownKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new DownKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendTab(Action<TabKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new TabKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendBackTab(Action<BackTabKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new BackTabKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendReturn(Action<ReturnKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new ReturnKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendPageUp(Action<PageUpKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new PageUpKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendPageDown(Action<PageDownKeyCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new PageDownKeyCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendCut(Action<CutCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new CutCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendPaste(Action<PasteCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new PasteCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendInvokeCompletionList(Action<InvokeCompletionListCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new InvokeCompletionListCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendCommitUniqueCompletionListItem(Action<CommitUniqueCompletionListItemCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new CommitUniqueCompletionListItemCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendInsertSnippetCommand(Action<InsertSnippetCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new InsertSnippetCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendSurroundWithCommand(Action<SurroundWithCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new SurroundWithCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendInvokeSignatureHelp(Action<InvokeSignatureHelpCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new InvokeSignatureHelpCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendTypeChar(char typeChar, Action<TypeCharCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new TypeCharCommandArgs(TextView, SubjectBuffer, typeChar), nextHandler);
        }

        public void SendTypeChars(string typeChars, Action<TypeCharCommandArgs, Action> commandHandler)
        {
            foreach (var ch in typeChars)
            {
                var localCh = ch;
                SendTypeChar(ch, commandHandler, () => EditorOperations.InsertText(localCh.ToString()));
            }
        }

        public void SendSave(Action<SaveCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new SaveCommandArgs(TextView, SubjectBuffer), nextHandler);
        }

        public void SendSelectAll(Action<SelectAllCommandArgs, Action> commandHandler, Action nextHandler)
        {
            commandHandler(new SelectAllCommandArgs(TextView, SubjectBuffer), nextHandler);
        }
        #endregion
    }
}
