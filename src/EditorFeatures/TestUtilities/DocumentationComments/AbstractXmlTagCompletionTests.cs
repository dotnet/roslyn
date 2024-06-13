// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
{
    [UseExportProvider]
    public abstract class AbstractXmlTagCompletionTests
    {
        private protected abstract IChainedCommandHandler<TypeCharCommandArgs> CreateCommandHandler(TestWorkspace testWorkspace);
        private protected abstract TestWorkspace CreateTestWorkspace(string initialMarkup);

        public void Verify(string initialMarkup, string expectedMarkup, char typeChar)
        {
            using var workspace = CreateTestWorkspace(initialMarkup);

            var testDocument = workspace.Documents.Single();
            var view = testDocument.GetTextView();
            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

            var commandHandler = CreateCommandHandler(workspace);

            var args = new TypeCharCommandArgs(view, view.TextBuffer, typeChar);
            var nextHandler = CreateInsertTextHandler(view, typeChar.ToString());

            commandHandler.ExecuteCommand(args, nextHandler, TestCommandExecutionContext.Create());
            MarkupTestFile.GetPosition(expectedMarkup, out var expectedCode, out int expectedPosition);

            Assert.Equal(expectedCode, view.TextSnapshot.GetText());

            var caretPosition = view.Caret.Position.BufferPosition.Position;
            Assert.True(expectedPosition == caretPosition,
                string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
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
    }
}
