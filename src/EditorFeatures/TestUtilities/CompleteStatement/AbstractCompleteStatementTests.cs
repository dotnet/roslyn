// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using VSCommanding = Microsoft.VisualStudio.Commanding;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CompleteStatement
{
    [UseExportProvider]
    public abstract class AbstractCompleteStatementTests
    {
        internal static char semicolon = ';';

        internal abstract VSCommanding.ICommandHandler GetCommandHandler(TestWorkspace workspace);

        protected abstract TestWorkspace CreateTestWorkspace(string code);

        /// <summary>
        /// Verify that typing a semicolon at the location in <paramref name="initialMarkup"/> 
        /// marked with 
        ///     - <c>$$</c> for caret position
        ///     - or, <c>[|</c> and <c>|]</c> for selected span
        /// does not perform any special "complete statement" operations, e.g. inserting missing 
        /// delimiters or moving the caret prior to the semicolon character insertion. In other words, 
        /// statement completion does not impact typing behavior for the case.
        /// </summary>
        protected void VerifyNoSpecialSemicolonHandling(string initialMarkup, string newLine = "\r\n")
        {
            var expected = initialMarkup.Contains("$$") ?
                initialMarkup.Replace("$$", ";$$") :
                initialMarkup.Replace("|]", ";$$|]");

            VerifyTypingSemicolon(initialMarkup, expected, newLine);
        }

        /// <summary>
        /// Verify that typing a semicolon at the location in <paramref name="initialMarkup"/> marked with <c>$$</c>
        /// produces the result in <paramref name="expectedMarkup"/>. The final caret location in
        /// <paramref name="expectedMarkup"/> is marked with <c>$$</c>.
        /// </summary>
        protected void VerifyTypingSemicolon(string initialMarkup, string expectedMarkup, string newLine = "\r\n")
        {
            Verify(initialMarkup, expectedMarkup, newLine: newLine,
                execute: (view, workspace) =>
                {
                    var commandHandler = GetCommandHandler(workspace);

                    var commandArgs = new TypeCharCommandArgs(view, view.TextBuffer, semicolon);
                    var nextHandler = CreateInsertTextHandler(view, semicolon.ToString());

                    commandHandler.ExecuteCommand(commandArgs, nextHandler, TestCommandExecutionContext.Create());
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

        private void Verify(string initialMarkup, string expectedMarkup,
            Action<IWpfTextView, TestWorkspace> execute,
            Action<TestWorkspace> setOptionsOpt = null, string newLine = "\r\n")
        {
            using (var workspace = CreateTestWorkspace(initialMarkup))
            {
                var testDocument = workspace.Documents.Single();

                Assert.True(testDocument.CursorPosition.HasValue || testDocument.SelectedSpans.Any(), "No caret position or selected spans are set!");
                var startCaretPosition = testDocument.CursorPosition ?? testDocument.SelectedSpans.Last().End;

                var view = testDocument.GetTextView();

                if (testDocument.SelectedSpans.Any())
                {
                    var selectedSpan = testDocument.SelectedSpans[0];

                    var isReversed = selectedSpan.Start == startCaretPosition;

                    view.Selection.Select(new SnapshotSpan(view.TextSnapshot, selectedSpan.Start, selectedSpan.Length), isReversed);
                }

                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, startCaretPosition));

                setOptionsOpt?.Invoke(workspace);

                execute(view, workspace);
                MarkupTestFile.GetPosition(expectedMarkup, out var expectedCode, out int expectedPosition);

                Assert.Equal(expectedCode, view.TextSnapshot.GetText());

                var endCaretPosition = view.Caret.Position.BufferPosition.Position;
                Assert.True(expectedPosition == endCaretPosition,
                    string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, endCaretPosition));
            }
        }
    }
}
