// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Completion
{
    internal class AbstractCompletionCommandHandlerTestState : IDisposable
    {
        private readonly TestWorkspace _workspace;
        private readonly ITextView _view;
        private readonly IEditorOperations _editorOperations;

        public ICommandHandler CompletionCommandHandler { get; protected set; }

        public AbstractCompletionCommandHandlerTestState(XElement workspaceElement)
        {
            _workspace = TestWorkspaceFactory.CreateWorkspace(workspaceElement);
            _view = _workspace.Documents.Single().GetTextView();

            int caretPosition = _workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition.Value;
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, caretPosition));

            _editorOperations = _workspace.GetService<IEditorOperationsFactoryService>().GetEditorOperations(_view);
        }

        public T GetService<T>()
        {
            return _workspace.GetService<T>();
        }

        public void SendBackspace()
        {
            var backspaceHandler = (ICommandHandler<BackspaceKeyCommandArgs>)CompletionCommandHandler;
            backspaceHandler.ExecuteCommand(new BackspaceKeyCommandArgs(_view, _view.TextBuffer), () => _editorOperations.Backspace());
        }

        public void SendTypeChars(string typeChars)
        {
            var typeCharHandler = (ICommandHandler<TypeCharCommandArgs>)CompletionCommandHandler;
            foreach (var ch in typeChars)
            {
                var typeCharArgs = new TypeCharCommandArgs(_view, _view.TextBuffer, ch);
                typeCharHandler.ExecuteCommand(typeCharArgs, () => _editorOperations.InsertText(ch.ToString()));
            }
        }

        public string GetLineTextFromCaretPosition()
        {
            int caretPosition = _workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition.Value;
            return _view.TextBuffer.CurrentSnapshot.GetLineFromPosition(caretPosition).GetText();
        }

        public void AssertNoSession()
        {
            var broker = _workspace.GetService<ICompletionBroker>();
            Assert.Empty(broker.GetSessions(_view));
        }

        public void AssertSession()
        {
            var broker = _workspace.GetService<ICompletionBroker>();
            Assert.NotEmpty(broker.GetSessions(_view));
        }

        public void Dispose()
        {
            // HACK: It seems that if we close the view before dismissing sessions, the editor blows
            // up internally.
            _workspace.GetService<ICompletionBroker>().DismissAllSessions(_view);
            _workspace.Dispose();
        }
    }
}
