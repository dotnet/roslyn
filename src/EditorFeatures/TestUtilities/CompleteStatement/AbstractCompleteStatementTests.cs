// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CompleteStatement
{
    [UseExportProvider]
    public abstract class AbstractCompleteStatementTests
    {
        internal static char semicolon = ';';

        internal abstract ICommandHandler GetCommandHandler(TestWorkspace workspace);

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
        protected void VerifyNoSpecialSemicolonHandling(string initialMarkup)
        {
            var expected = initialMarkup.Contains("$$") ?
                initialMarkup.Replace("$$", ";$$") :
                initialMarkup.Replace("|]", ";$$|]");

            VerifyTypingSemicolon(initialMarkup, expected);
        }

        /// <summary>
        /// Verify that typing a semicolon at the location in <paramref name="initialMarkup"/> marked with <c>$$</c>
        /// produces the result in <paramref name="expectedMarkup"/>. The final caret location in
        /// <paramref name="expectedMarkup"/> is marked with <c>$$</c>.
        /// </summary>
        protected void VerifyTypingSemicolon(string initialMarkup, string expectedMarkup)
        {
            Verify(initialMarkup, expectedMarkup,
                execute: (view, workspace) =>
                {
                    var commandHandler = GetCommandHandler(workspace);

                    var commandArgs = new TypeCharCommandArgs(view, view.TextBuffer, semicolon);
                    var nextHandler = CreateInsertTextHandler(view, semicolon.ToString());

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

        private void Verify(string initialMarkup, string expectedMarkup,
            Action<IWpfTextView, TestWorkspace> execute,
            Action<TestWorkspace> setOptionsOpt = null)
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
