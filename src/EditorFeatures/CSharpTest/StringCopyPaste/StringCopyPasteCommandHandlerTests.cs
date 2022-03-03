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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    [UseExportProvider]
    public class StringCopyPasteCommandHandlerTests
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
                    : XElement.Parse(string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>{0}</Document>
    </Project>
</Workspace>", markup));

            internal void AssertCodeIs(string expectedCode)
            {
                MarkupTestFile.GetPositionAndSpans(expectedCode, out var massaged, out int? caretPosition, out var spans);
                Assert.Equal(massaged, TextView.TextSnapshot.GetText());
                Assert.Equal(caretPosition!.Value, TextView.Caret.Position.BufferPosition.Position);

                var virtualSpaces = spans.SingleOrDefault(kvp => kvp.Key.StartsWith("VirtualSpaces#"));
                if (virtualSpaces.Key != null)
                {
                    var virtualOffset = int.Parse(virtualSpaces.Key.Substring("VirtualSpaces-".Length));
                    Assert.True(TextView.Caret.InVirtualSpace);
                    Assert.Equal(virtualOffset, TextView.Caret.Position.VirtualBufferPosition.VirtualSpaces);
                }
            }

            private void TestCopyPasteWorker(string markup, string expectedMarkup, string? pasteText)
            {
                using var state = CreateTestState(markup);
                var workspace = state.Workspace;

                var copyDocument = state.Workspace.Documents.FirstOrDefault(d => d.AnnotatedSpans.ContainsKey("Copy"));
                if (copyDocument != null)
                {
                    Assert.Null(pasteText);
                    var copySpans = copyDocument.AnnotatedSpans["Copy"];

                    SetSelection(copyDocument, copySpans, out var copyTextView, out var copyTextBuffer);

                    _commandHandler.ExecuteCommand(
                        new CopyCommandArgs(copyTextView, copyTextBuffer), () => EditorOperations.CopySelection(), TestCommandExecutionContext.Create());
                }
                else
                {
                    // If we don't have a file to copy text from, then the paste text must be explicitly provided.");
                    Assert.NotNull(pasteText);
                }

                var pasteDocument = state.Workspace.Documents.Single(d => d.AnnotatedSpans.ContainsKey("Paste"));
                var pasteSpans = pasteDocument.AnnotatedSpans["Paste"];
                SetSelection(pasteDocument, pasteSpans, out var pasteTextView, out var pasteTextBuffer);

                if (pasteText == null)
                {
                    // if the user didn't supply text to paste, then just paste in what we put in the clipboard above.
                    _commandHandler.ExecuteCommand(
                        new PasteCommandArgs(pasteTextView, pasteTextBuffer), () => EditorOperations.Paste(), TestCommandExecutionContext.Create());
                }
                else
                {
                    // otherwise, this is a test of text coming in from another source.  Do the edit manually.
                    _commandHandler.ExecuteCommand(
                        new PasteCommandArgs(pasteTextView, pasteTextBuffer), () =>
                        {
                            EditorOperations.ReplaceSelection(pasteText);
                        }, TestCommandExecutionContext.Create());
                }

                MarkupTestFile.GetPosition(expectedMarkup, out var expected, out int caretPosition);
                var finalText = pasteTextBuffer.CurrentSnapshot.GetText();

                Assert.Equal(expected, finalText);
                Assert.Equal(caretPosition, pasteTextView.Caret.Position.BufferPosition.Position);
            }

            public void TestCopyPaste(string code, string expected)
            {
                TestCopyPasteWorker(code, expected, pasteText: null);
            }

            public void TestPasteOnly(string code, string expected, string pasteText)
            {
                TestCopyPasteWorker(code, expected, pasteText);
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

        [WpfFact]
        public void TestTypeQuoteEmptyFile()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"$$");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"""$$");
        }
    }
}
