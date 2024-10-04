// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
{
    [UseExportProvider]
    public abstract class AbstractDocumentationCommentTests
    {
        protected abstract char DocumentationCommentCharacter { get; }

        internal abstract ICommandHandler CreateCommandHandler(EditorTestWorkspace workspace);

        protected abstract EditorTestWorkspace CreateTestWorkspace(string code);

        internal void VerifyTypingCharacter(string initialMarkup, string expectedMarkup, bool useTabs = false, string newLine = "\r\n", bool trimTrailingWhiteSpace = false, OptionsCollection globalOptions = null)
        {
            Verify(initialMarkup, expectedMarkup,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new TypeCharCommandArgs(view, view.TextBuffer, DocumentationCommentCharacter);
                    var nextHandler = CreateInsertTextHandler(view, DocumentationCommentCharacter.ToString());

                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                },
                useTabs, newLine, trimTrailingWhiteSpace, globalOptions);
        }

        internal void VerifyPressingEnter(string initialMarkup, string expectedMarkup, bool useTabs = false, string newLine = "\r\n", bool trimTrailingWhiteSpace = false, OptionsCollection globalOptions = null)
        {
            Verify(initialMarkup, expectedMarkup,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new ReturnKeyCommandArgs(view, view.TextBuffer);
                    var nextHandler = CreateInsertTextHandler(view, "\r\n");
                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                },
                useTabs, newLine, trimTrailingWhiteSpace, globalOptions);
        }

        internal void VerifyInsertCommentCommand(string initialMarkup, string expectedMarkup, bool useTabs = false, string newLine = "\r\n", bool trimTrailingWhiteSpace = false, OptionsCollection globalOptions = null)
        {
            Verify(initialMarkup, expectedMarkup,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new InsertCommentCommandArgs(view, view.TextBuffer);
                    Action nextHandler = delegate { };

                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                },
                useTabs, newLine, trimTrailingWhiteSpace, globalOptions);
        }

        internal void VerifyOpenLineAbove(string initialMarkup, string expectedMarkup, bool useTabs = false, string newLine = "\r\n", bool trimTrailingWhiteSpace = false, OptionsCollection globalOptions = null)
        {
            Verify(initialMarkup, expectedMarkup,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new OpenLineAboveCommandArgs(view, view.TextBuffer);
                    void nextHandler()
                    {
                        var editorOperations = editorOperationsFactoryService.GetEditorOperations(view);
                        editorOperations.OpenLineAbove();
                    }

                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                },
                useTabs, newLine, trimTrailingWhiteSpace, globalOptions);
        }

        internal void VerifyOpenLineBelow(string initialMarkup, string expectedMarkup, bool useTabs = false, string newLine = "\r\n", bool trimTrailingWhiteSpace = false, OptionsCollection globalOptions = null)
        {
            Verify(initialMarkup, expectedMarkup,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new OpenLineBelowCommandArgs(view, view.TextBuffer);
                    void nextHandler()
                    {
                        var editorOperations = editorOperationsFactoryService.GetEditorOperations(view);
                        editorOperations.OpenLineBelow();
                    }

                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                },
                useTabs, newLine, trimTrailingWhiteSpace, globalOptions);
        }

        private static Action CreateInsertTextHandler(ITextView textView, string text)
        {
            return () =>
            {
                var caretPosition = textView.Caret.Position.BufferPosition;
                var newSpanshot = textView.TextBuffer.Insert(caretPosition, text);
                textView.Caret.MoveTo(new SnapshotPoint(newSpanshot, caretPosition + text.Length));
            };
        }

        private void Verify(
            string initialMarkup,
            string expectedMarkup,
            Action<EditorTestWorkspace, IWpfTextView, IEditorOperationsFactoryService> execute,
            bool useTabs,
            string newLine,
            bool trimTrailingWhiteSpace,
            OptionsCollection globalOptions)
        {
            using var workspace = CreateTestWorkspace(initialMarkup);
            var testDocument = workspace.Documents.Single();

            Assert.True(testDocument.CursorPosition.HasValue, "No caret position set!");
            var startCaretPosition = testDocument.CursorPosition.Value;

            var view = testDocument.GetTextView();

            globalOptions?.SetGlobalOptions(workspace.GlobalOptions);

            var optionsFactory = workspace.GetService<IEditorOptionsFactoryService>();
            var editorOptions = optionsFactory.GetOptions(view.TextBuffer);
            editorOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !useTabs);
            editorOptions.SetOptionValue(DefaultOptions.NewLineCharacterOptionId, newLine);
            view.Options.SetOptionValue(DefaultOptions.TrimTrailingWhiteSpaceOptionId, trimTrailingWhiteSpace);

            if (testDocument.SelectedSpans.Any())
            {
                var selectedSpan = testDocument.SelectedSpans[0];
                var isReversed = selectedSpan.Start == startCaretPosition;

                view.Selection.Select(new SnapshotSpan(view.TextSnapshot, selectedSpan.Start, selectedSpan.Length), isReversed);
            }

            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

            execute(
                workspace,
                view,
                workspace.GetService<IEditorOperationsFactoryService>());
            MarkupTestFile.GetPosition(expectedMarkup, out var expectedCode, out int _);

            var actual = view.TextSnapshot.GetText();
            Assert.Equal(expectedCode, actual);

            var endCaretPosition = view.Caret.Position.BufferPosition.Position;
            var actualWithCaret = actual.Insert(endCaretPosition, "$$");

            Assert.Equal(expectedMarkup, actualWithCaret);
        }
    }
}
