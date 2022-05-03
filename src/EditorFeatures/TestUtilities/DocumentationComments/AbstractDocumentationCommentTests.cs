// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
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

        internal abstract ICommandHandler CreateCommandHandler(TestWorkspace workspace);

        protected abstract TestWorkspace CreateTestWorkspace(string code);

        protected void VerifyTypingCharacter(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true, string newLine = "\r\n")
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments, newLine: newLine,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new TypeCharCommandArgs(view, view.TextBuffer, DocumentationCommentCharacter);
                    var nextHandler = CreateInsertTextHandler(view, DocumentationCommentCharacter.ToString());

                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                });
        }

        protected void VerifyPressingEnter(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true,
            Action<TestWorkspace> setOptionsOpt = null)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
                setOptionsOpt: setOptionsOpt,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new ReturnKeyCommandArgs(view, view.TextBuffer);
                    var nextHandler = CreateInsertTextHandler(view, "\r\n");
                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                });
        }

        protected void VerifyInsertCommentCommand(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
                execute: (workspace, view, editorOperationsFactoryService) =>
                {
                    var commandHandler = CreateCommandHandler(workspace);

                    var commandArgs = new InsertCommentCommandArgs(view, view.TextBuffer);
                    Action nextHandler = delegate { };

                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
                });
        }

        protected void VerifyOpenLineAbove(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
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
                });
        }

        protected void VerifyOpenLineBelow(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
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
                });
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

        private void Verify(string initialMarkup, string expectedMarkup, bool useTabs, bool autoGenerateXmlDocComments,
            Action<TestWorkspace, IWpfTextView, IEditorOperationsFactoryService> execute,
            Action<TestWorkspace> setOptionsOpt = null, string newLine = "\r\n")
        {
            using (var workspace = CreateTestWorkspace(initialMarkup))
            {
                var testDocument = workspace.Documents.Single();
                workspace.GlobalOptions.SetGlobalOption(new OptionKey(DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, testDocument.Project.Language), autoGenerateXmlDocComments);

                var options = workspace.Options;

                options = options.WithChangedOption(FormattingOptions.UseTabs, testDocument.Project.Language, useTabs);
                options = options.WithChangedOption(FormattingOptions.NewLine, testDocument.Project.Language, newLine);
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(options));

                setOptionsOpt?.Invoke(workspace);

                Assert.True(testDocument.CursorPosition.HasValue, "No caret position set!");
                var startCaretPosition = testDocument.CursorPosition.Value;

                var view = testDocument.GetTextView();

                if (testDocument.SelectedSpans.Any())
                {
                    var selectedSpan = testDocument.SelectedSpans[0];

                    var isReversed = selectedSpan.Start == startCaretPosition
                        ? true
                        : false;

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
}
