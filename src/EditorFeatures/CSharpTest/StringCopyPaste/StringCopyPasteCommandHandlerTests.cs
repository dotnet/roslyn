// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    [UseExportProvider]
    public abstract class StringCopyPasteCommandHandlerTests
    {
        internal sealed class StringCopyPasteTestState : AbstractCommandHandlerTestState
        {
            private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeaturesWpf.AddParts(
                typeof(StringCopyPasteCommandHandler));

            private readonly StringCopyPasteCommandHandler _commandHandler;

            public StringCopyPasteTestState(XElement workspaceElement)
                : base(workspaceElement, s_composition)
            {
                _commandHandler = (StringCopyPasteCommandHandler)GetExportedValues<ICommandHandler>().
                    Single(c => c is StringCopyPasteCommandHandler);
            }

            public static StringCopyPasteTestState CreateTestState(string markup)
                => new(GetWorkspaceXml(markup));

            public static XElement GetWorkspaceXml(string markup)
                => markup.Contains("<Workspace>")
                    ? XElement.Parse(markup)
                    : XElement.Parse($@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document Markup=""SpansOnly"">{markup}</Document>
    </Project>
</Workspace>");

            internal void AssertCodeIs(string expectedCode)
            {
                TestFileMarkupParser.GetPositionAndSpans(
                    expectedCode, out var massaged, out int? caretPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
                Assert.Equal(massaged, TextView.TextSnapshot.GetText());
                Assert.Equal(caretPosition, TextView.Caret.Position.BufferPosition.Position);

                var virtualSpaces = spans.SingleOrDefault(kvp => kvp.Key.StartsWith("VirtualSpaces#"));
                if (virtualSpaces.Key != null)
                {
                    var virtualOffset = int.Parse(virtualSpaces.Key.Substring("VirtualSpaces-".Length));
                    Assert.True(TextView.Caret.InVirtualSpace);
                    Assert.Equal(virtualOffset, TextView.Caret.Position.VirtualBufferPosition.VirtualSpaces);
                }
            }

            public void TestCopyPaste(string expectedMarkup, string? pasteText, string afterUndoMarkup)
            {
                var workspace = this.Workspace;

                var copyDocument = this.Workspace.Documents.FirstOrDefault(d => d.AnnotatedSpans.ContainsKey("Copy"));
                if (copyDocument != null)
                {
                    Assert.Null(pasteText);
                    var copySpans = copyDocument.AnnotatedSpans["Copy"];

                    SetSelection(copyDocument, copySpans, out var copyTextView, out var copyTextBuffer);

                    _commandHandler.ExecuteCommand(
                        new CopyCommandArgs(copyTextView, copyTextBuffer), () =>
                        {
                            var copyEditorOperations = GetService<IEditorOperationsFactoryService>().GetEditorOperations(copyTextView);
                            Assert.True(copyEditorOperations.CopySelection());
                        }, TestCommandExecutionContext.Create());
                }
                else
                {
                    // If we don't have a file to copy text from, then the paste text must be explicitly provided.");
                    Assert.NotNull(pasteText);
                }

                if (pasteText == null)
                {
                    // if the user didn't supply text to paste, then just paste in what we put in the clipboard above.
                    _commandHandler.ExecuteCommand(
                        new PasteCommandArgs(this.TextView, this.SubjectBuffer), () => EditorOperations.Paste(), TestCommandExecutionContext.Create());
                }
                else
                {
                    // otherwise, this is a test of text coming in from another source.  Do the edit manually.
                    _commandHandler.ExecuteCommand(
                        new PasteCommandArgs(this.TextView, this.SubjectBuffer), () =>
                        {
                            EditorOperations.ReplaceSelection(pasteText);
                        }, TestCommandExecutionContext.Create());
                }

                ValidateBefore(expectedMarkup);

                this.SendUndo();

                ValidateAfter(afterUndoMarkup);
            }

            private void ValidateBefore(string expectedMarkup)
            {
                GetCodeAndCaretPosition(expectedMarkup, out var expected, out var caretPosition);
                var finalText = this.SubjectBuffer.CurrentSnapshot.GetText();

                Assert.Equal(expected, finalText);
                Assert.Equal(caretPosition, this.TextView.Caret.Position.BufferPosition.Position);
            }

            private static void GetCodeAndCaretPosition(string expectedMarkup, out string expected, out int caretPosition)
            {
                // Used so test can contain `$$` (for raw interpolations) without us thinking that it is an actual caret
                // position
                const string NON_TEST_CHARACTER = "\uD7FF";

                expectedMarkup = expectedMarkup.Replace("$", NON_TEST_CHARACTER);

                TestFileMarkupParser.GetSpan(expectedMarkup, out expected, out var caretSpan);

                expected = expected.Replace(NON_TEST_CHARACTER, "$");

                caretPosition = caretSpan.Start;
            }

            private void ValidateAfter(string afterUndoMarkup)
            {
                GetCodeAndCaretPosition(afterUndoMarkup, out var expected, out var caretPosition);
                var finalText = this.SubjectBuffer.CurrentSnapshot.GetText();

                Assert.Equal(expected, finalText);
                Assert.Equal(caretPosition, this.TextView.Caret.Position.BufferPosition.Position);
            }

            private static void SetSelection(
                TestHostDocument document,
                ImmutableArray<TextSpan> copySpans, out IWpfTextView textView, out ITextBuffer2 textBuffer2)
            {
                textView = document.GetTextView();
                var textBuffer = document.GetTextBuffer();
                textBuffer2 = textBuffer;
                var broker = textView.GetMultiSelectionBroker();

                broker.AddSelectionRange(copySpans.Select(s => new Selection(s.ToSnapshotSpan(textBuffer.CurrentSnapshot))));
            }
        }

#if false
        private static void TestCopyPaste(string markup, string expectedMarkup, string afterUndo)
        {
            using var state = StringCopyPasteTestState.CreateTestState(markup);

            state.TestCopyPaste(expectedMarkup, pasteText: null, afterUndo);
        }
#endif
    }
}
