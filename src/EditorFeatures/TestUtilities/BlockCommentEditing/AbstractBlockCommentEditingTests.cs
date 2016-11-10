﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.BlockCommentEditing
{
    public abstract class AbstractBlockCommentEditingTests
    {
        internal abstract ICommandHandler<ReturnKeyCommandArgs> CreateCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService);

        protected abstract Task<TestWorkspace> CreateTestWorkspaceAsync(string initialMarkup);

        protected async Task VerifyAsync(string initialMarkup, string expectedMarkup)
        {
            using (var workspace = await CreateTestWorkspaceAsync(initialMarkup))
            {
                var testDocument = workspace.Documents.Single();
                var view = testDocument.GetTextView();
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

                var commandHandler = CreateCommandHandler(workspace.GetService<ITextUndoHistoryRegistry>(), workspace.GetService<IEditorOperationsFactoryService>());

                var args = new ReturnKeyCommandArgs(view, view.TextBuffer);
                var nextHandler = CreateInsertTextHandler(view, "\r\n");

                commandHandler.ExecuteCommand(args, nextHandler);

                string expectedCode;
                int expectedPosition;
                MarkupTestFile.GetPosition(expectedMarkup, out expectedCode, out expectedPosition);

                Assert.Equal(expectedCode, view.TextSnapshot.GetText());

                var caretPosition = view.Caret.Position.BufferPosition.Position;
                Assert.True(expectedPosition == caretPosition,
                    string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
            }
        }

        protected Task VerifyTabsAsync(string initialMarkup, string expectedMarkup) => VerifyAsync(ReplaceTabTags(initialMarkup), ReplaceTabTags(expectedMarkup));

        private string ReplaceTabTags(string markup) => markup.Replace("<tab>", "\t");

        private Action CreateInsertTextHandler(ITextView textView, string text)
        {
            return () =>
            {
                var caretPosition = textView.Caret.Position.BufferPosition;
                var newSpanshot = textView.TextBuffer.Insert(caretPosition, text);
                textView.Caret.MoveTo(new SnapshotPoint(newSpanshot, (int)caretPosition + text.Length));
            };
        }
    }
}
