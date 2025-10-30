// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

[UseExportProvider]
public abstract class AbstractTypingCommandHandlerTest<TCommandArgs> where TCommandArgs : CommandArgs
{
    internal abstract ICommandHandler<TCommandArgs> GetCommandHandler(EditorTestWorkspace workspace);

    protected abstract EditorTestWorkspace CreateTestWorkspace(string initialMarkup);

    protected abstract (TCommandArgs, string insertionText) CreateCommandArgs(ITextView textView, ITextBuffer textBuffer);

    protected void Verify(string initialMarkup, string expectedMarkup, Action<EditorTestWorkspace> initializeWorkspace = null)
    {
        using var workspace = CreateTestWorkspace(initialMarkup);
        initializeWorkspace?.Invoke(workspace);

        var testDocument = workspace.Documents.Single();
        var view = testDocument.GetTextView();
        view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

        var commandHandler = GetCommandHandler(workspace);

        var (args, insertionText) = CreateCommandArgs(view, view.TextBuffer);
        var nextHandler = CreateInsertTextHandler(view, insertionText);

        if (!commandHandler.ExecuteCommand(args, TestCommandExecutionContext.Create()))
        {
            nextHandler();
        }

        MarkupTestFile.GetPosition(expectedMarkup, out var expectedCode, out int expectedPosition);

        Assert.Equal(expectedCode, view.TextSnapshot.GetText());

        var caretPosition = view.Caret.Position.BufferPosition.Position;
        Assert.True(expectedPosition == caretPosition,
            string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
    }

    protected void VerifyTabs(string initialMarkup, string expectedMarkup)
        => Verify(ReplaceTabTags(initialMarkup), ReplaceTabTags(expectedMarkup));

    private static string ReplaceTabTags(string markup) => markup.Replace("<tab>", "\t");

    private static Action CreateInsertTextHandler(ITextView textView, string text)
    {
        return () =>
        {
            var caretPosition = textView.Caret.Position.BufferPosition;
            var newSpanshot = textView.TextBuffer.Insert(caretPosition, text);
            textView.Caret.MoveTo(new SnapshotPoint(newSpanshot, (int)caretPosition + text.Length));
        };
    }
}
