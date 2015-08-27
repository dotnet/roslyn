// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
{
    public abstract class AbstractDocumentationCommentTests
    {
        protected abstract char DocumentationCommentCharacter { get; }

        internal abstract ICommandHandler CreateCommandHandler(IWaitIndicator waitIndicator, ITextUndoHistoryRegistry undoHistoryRegistry, IEditorOperationsFactoryService editorOperationsFactoryService, IAsyncCompletionService completionService);
        protected abstract TestWorkspace CreateTestWorkspace(string code);

        protected void VerifyTypingCharacter(string initialMarkup, string expectedMarkup, bool useTabs = false)
        {
            Verify(initialMarkup, expectedMarkup, useTabs,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService, completionService) as ICommandHandler<TypeCharCommandArgs>;

                    var commandArgs = new TypeCharCommandArgs(view, view.TextBuffer, DocumentationCommentCharacter);
                    var nextHandler = CreateInsertTextHandler(view, DocumentationCommentCharacter.ToString());

                    commandHandler.ExecuteCommand(commandArgs, nextHandler);
                });
        }

        protected void VerifyPressingEnter(string initialMarkup, string expectedMarkup, bool useTabs = false)
        {
            Verify(initialMarkup, expectedMarkup, useTabs,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService, completionService) as ICommandHandler<ReturnKeyCommandArgs>;

                    var commandArgs = new ReturnKeyCommandArgs(view, view.TextBuffer);
                    var nextHandler = CreateInsertTextHandler(view, "\r\n");

                    commandHandler.ExecuteCommand(commandArgs, nextHandler);
                });
        }

        protected void VerifyInsertCommentCommand(string initialMarkup, string expectedMarkup, bool useTabs = false)
        {
            Verify(initialMarkup, expectedMarkup, useTabs,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService, completionService) as ICommandHandler<InsertCommentCommandArgs>;

                    var commandArgs = new InsertCommentCommandArgs(view, view.TextBuffer);
                    Action nextHandler = delegate { };

                    commandHandler.ExecuteCommand(commandArgs, nextHandler);
                });
        }

        protected void VerifyOpenLineAbove(string initialMarkup, string expectedMarkup, bool useTabs = false)
        {
            Verify(initialMarkup, expectedMarkup, useTabs,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService, completionService) as ICommandHandler<OpenLineAboveCommandArgs>;

                    var commandArgs = new OpenLineAboveCommandArgs(view, view.TextBuffer);
                    Action nextHandler = () =>
                    {
                        var editorOperations = editorOperationsFactoryService.GetEditorOperations(view);
                        editorOperations.OpenLineAbove();
                    };

                    commandHandler.ExecuteCommand(commandArgs, nextHandler);
                });
        }

        protected void VerifyOpenLineBelow(string initialMarkup, string expectedMarkup, bool useTabs = false)
        {
            Verify(initialMarkup, expectedMarkup, useTabs,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService, completionService) as ICommandHandler<OpenLineBelowCommandArgs>;

                    var commandArgs = new OpenLineBelowCommandArgs(view, view.TextBuffer);
                    Action nextHandler = () =>
                    {
                        var editorOperations = editorOperationsFactoryService.GetEditorOperations(view);
                        editorOperations.OpenLineBelow();
                    };

                    commandHandler.ExecuteCommand(commandArgs, nextHandler);
                });
        }

        private Action CreateInsertTextHandler(ITextView textView, string text)
        {
            return () =>
            {
                var caretPosition = textView.Caret.Position.BufferPosition;
                var newSpanshot = textView.TextBuffer.Insert(caretPosition, text);
                textView.Caret.MoveTo(new SnapshotPoint(newSpanshot, caretPosition + text.Length));
            };
        }

        private void Verify(string initialMarkup, string expectedMarkup, bool useTabs, Action<IWpfTextView, ITextUndoHistoryRegistry, IEditorOperationsFactoryService, IAsyncCompletionService> execute)
        {
            using (var workspace = CreateTestWorkspace(initialMarkup))
            {
                var testDocument = workspace.Documents.Single();
                var view = testDocument.GetTextView();
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

                if (useTabs)
                {
                    var optionService = workspace.Services.GetService<IOptionService>();
                    optionService.SetOptions(optionService.GetOptions().WithChangedOption(FormattingOptions.UseTabs, testDocument.Project.Language, useTabs));
                }

                execute(
                    view,
                    workspace.GetService<ITextUndoHistoryRegistry>(),
                    workspace.GetService<IEditorOperationsFactoryService>(),
                    workspace.GetService<IAsyncCompletionService>());

                string expectedCode;
                int expectedPosition;
                MarkupTestFile.GetPosition(expectedMarkup, out expectedCode, out expectedPosition);

                Assert.Equal(expectedCode, view.TextSnapshot.GetText());

                var caretPosition = view.Caret.Position.BufferPosition.Position;
                Assert.True(expectedPosition == caretPosition,
                    string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
            }
        }
    }
}
