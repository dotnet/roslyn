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
using Microsoft.VisualStudio.Text.Operations;
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

                {
                    MarkupTestFile.GetPosition(expectedMarkup, out var expected, out int caretPosition);
                    var finalText = this.SubjectBuffer.CurrentSnapshot.GetText();

                    Assert.Equal(expected, finalText);
                    Assert.Equal(caretPosition, this.TextView.Caret.Position.BufferPosition.Position);
                }

                this.SendUndo();

                {
                    MarkupTestFile.GetPosition(afterUndoMarkup, out var expected, out int caretPosition);
                    var finalText = this.SubjectBuffer.CurrentSnapshot.GetText();

                    Assert.Equal(expected, finalText);
                    Assert.Equal(caretPosition, this.TextView.Caret.Position.BufferPosition.Position);
                }
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

        private static void TestCopyPaste(string markup, string expectedMarkup, string afterUndo)
        {
            using var state = StringCopyPasteTestState.CreateTestState(markup);

            state.TestCopyPaste(expectedMarkup, pasteText: null, afterUndo);
        }

        private static void TestPasteOnly(string pasteText, string markup, string expectedMarkup, string afterUndo)
        {
            using var state = StringCopyPasteTestState.CreateTestState(markup);

            state.TestCopyPaste(expectedMarkup, pasteText, afterUndo);
        }

        #region Paste from external source into normal string

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalString1()
        {
            TestPasteOnly(
                pasteText: "\n",
                @"var x = ""$$""",
                @"var x = ""\n$$""",
                afterUndo: "var x = \"\n$$\"");
        }

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalString2()
        {
            TestPasteOnly(
                pasteText: "\r\n",
                @"var x = ""$$""",
                @"var x = ""\r\n$$""",
                afterUndo: "var x = \"\r\n$$\"");
        }

        [WpfFact]
        public void TestPasteExternalTabIntoNormalString1()
        {
            TestPasteOnly(
                pasteText: "\t",
                @"var x = ""$$""",
                @"var x = ""\t$$""",
                afterUndo: "var x = \"\t$$\"");
        }

        [WpfFact]
        public void TestPasteExternalSingleQuoteIntoNormalString()
        {
            TestPasteOnly(
                pasteText: "'",
                @"var x = ""$$""",
                @"var x = ""'$$""",
                afterUndo: "var x = \"$$\"");
        }

        [WpfFact]
        public void TestPasteExternalDoubleQuoteIntoNormalString()
        {
            TestPasteOnly(
                pasteText: "\"",
                @"var x = ""$$""",
                @"var x = ""\""$$""",
                afterUndo: "var x = \"\"$$\"");
        }

        [WpfFact]
        public void TestPasteExternalComplexStringIntoNormalString()
        {
            TestPasteOnly(
                pasteText: "\t\"\"\t",
                @"var x = ""$$""",
                @"var x = ""\t\""\""\t$$""",
                afterUndo: "var x = \"\t\"\"\t$$\"");
        }

        [WpfFact]
        public void TestPasteExternalNormalTextIntoNormalString()
        {
            TestPasteOnly(
                pasteText: "abc",
                @"var x = ""$$""",
                @"var x = ""abc$$""",
                afterUndo: @"var x = ""$$""");
        }

        #endregion

        #region Paste from external source into multi-line raw string

        [WpfFact]
        public void TestPasteExternalNewLineIntoMultiLineRawString1()
        {
            TestPasteOnly(
                pasteText: "\n",
@"var x = """"""
    $$
    """"""",
@"var x = """"""
    
    $$
    """"""",
                afterUndo:
@"var x = """"""
    \n$$
    """"""");
        }

        [WpfFact]
        public void TestPasteExternalNewLineIntoMultiLineRawString2()
        {
            TestPasteOnly(
                pasteText: "\r\n",
@"var x = """"""
    $$
    """"""",
@"var x = ""\r\n$$""",
                afterUndo: "var x = \"\r\n$$\"");
        }

        [WpfFact]
        public void TestPasteExternalTabIntoMultiLineRawString1()
        {
            TestPasteOnly(
                pasteText: "\t",
                @"var x = ""$$""",
                @"var x = ""\t$$""",
                afterUndo: "var x = \"\t$$\"");
        }

        [WpfFact]
        public void TestPasteExternalSingleQuoteIntoMultiLineRawString()
        {
            TestPasteOnly(
                pasteText: "'",
@"var x = """"""
    $$
    """"""",
@"var x = """"""
    '$$
    """"""",
                afterUndo:
@"var x = """"""
    $$
    """"""");
        }

        [WpfFact]
        public void TestPasteExternalDoubleQuoteIntoMultiLineRawString()
        {
            TestPasteOnly(
                pasteText: "\"",
@"var x = """"""
    $$
    """"""",
@"var x = """"""
    ""$$
    """"""",
                afterUndo:
@"var x = """"""
    $$
    """"""");
        }

        [WpfFact]
        public void TestPasteExternalTripleQuoteIntoMultiLineRawString()
        {
            TestPasteOnly(
                pasteText: "\"\"\"",
@"var x = """"""
    $$
    """"""",
@"var x = """"""""
    """"""$$
    """"""""",
                afterUndo:
@"var x = """"""
    """"""$$
    """"""");
        }

        [WpfFact]
        public void TestPasteExternalQuadrupleQuoteIntoMultiLineRawString()
        {
            TestPasteOnly(
                pasteText: "\"\"\"\"",
@"var x = """"""
    $$
    """"""",
@"var x = """"""""""
    """"""""$$
    """"""""""",
                afterUndo:
@"var x = """"""
    """"""""$$
    """"""");
        }

        [WpfFact]
        public void TestPasteExternalComplexStringIntoMultiLineRawString()
        {
            TestPasteOnly(
                pasteText: "  \"\"  ",
@"var x = """"""
    $$
    """"""",
@"var x = """"""
    """"  $$
    """"""",
                afterUndo:
@"var x = """"""
      """"  $$
    """"""");
        }

        [WpfFact]
        public void TestPasteExternalNormalTextIntoMultiLineRawString()
        {
            TestPasteOnly(
                pasteText: "abc",
@"var x = """"""
    $$
    """"""",
@"var x = """"""
    abc$$
    """"""",
                afterUndo:
@"var x = """"""
    $$
    """"""");
        }

        #endregion

        #region Paste from external source into normal interpolated string

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalInterpolatedString1()
        {
            TestPasteOnly(
                pasteText: "\n",
                @"var x = $""$$""",
                @"var x = $""\n$$""",
                afterUndo: "var x = $\"\n$$\"");
        }

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalInterpolatedString2()
        {
            TestPasteOnly(
                pasteText: "\r\n",
                @"var x = $""$$""",
                @"var x = $""\r\n$$""",
                afterUndo: "var x = $\"\r\n$$\"");
        }

        [WpfFact]
        public void TestPasteExternalTabIntoNormalInterpolatedString1()
        {
            TestPasteOnly(
                pasteText: "\t",
                @"var x = $""$$""",
                @"var x = $""\t$$""",
                afterUndo: "var x = $\"\t$$\"");
        }

        [WpfFact]
        public void TestPasteExternalSingleQuoteIntoNormalInterpolatedString()
        {
            TestPasteOnly(
                pasteText: "'",
                @"var x = $""$$""",
                @"var x = $""'$$""",
                afterUndo: "var x = $\"$$\"");
        }

        [WpfFact]
        public void TestPasteExternalDoubleQuoteIntoNormalInterpolatedString()
        {
            TestPasteOnly(
                pasteText: "\"",
                @"var x = $""$$""",
                @"var x = $""\""$$""",
                afterUndo: "var x = $\"\"$$\"");
        }

        [WpfFact]
        public void TestPasteExternalComplexStringIntoNormalInterpolatedString()
        {
            TestPasteOnly(
                pasteText: "\t\"\"\t",
                @"var x = $""$$""",
                @"var x = $""\t\""\""\t$$""",
                afterUndo: "var x = $\"\t\"\"\t$$\"");
        }

        [WpfFact]
        public void TestPasteExternalNormalTextIntoNormalInterpolatedString()
        {
            TestPasteOnly(
                pasteText: "abc",
                @"var x = $""$$""",
                @"var x = $""abc$$""",
                afterUndo: @"var x = $""$$""");
        }

        #endregion

        #region Paste from external source into normal interpolated string before hole

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteOnly(
                pasteText: "\n",
                @"var x = $""$${0}""",
                @"var x = $""\n$${0}""",
                afterUndo: "var x = $\"\n$${0}\"");
        }

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalInterpolatedStringBeforeHole2()
        {
            TestPasteOnly(
                pasteText: "\r\n",
                @"var x = $""$${0}""",
                @"var x = $""\r\n$${0}""",
                afterUndo: "var x = $\"\r\n$${0}\"");
        }

        [WpfFact]
        public void TestPasteExternalTabIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteOnly(
                pasteText: "\t",
                @"var x = $""$${0}""",
                @"var x = $""\t$${0}""",
                afterUndo: "var x = $\"\t$${0}\"");
        }

        [WpfFact]
        public void TestPasteExternalSingleQuoteIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteOnly(
                pasteText: "'",
                @"var x = $""$${0}""",
                @"var x = $""'$${0}""",
                afterUndo: "var x = $\"$${0}\"");
        }

        [WpfFact]
        public void TestPasteExternalDoubleQuoteIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteOnly(
                pasteText: "\"",
                @"var x = $""$${0}""",
                @"var x = $""\""$${0}""",
                afterUndo: "var x = $\"\"$${0}\"");
        }

        [WpfFact]
        public void TestPasteExternalComplexStringIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteOnly(
                pasteText: "\t\"\"\t",
                @"var x = $""$${0}""",
                @"var x = $""\t\""\""\t$${0}""",
                afterUndo: "var x = $\"\t\"\"\t$${0}\"");
        }

        [WpfFact]
        public void TestPasteExternalNormalTextIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteOnly(
                pasteText: "abc",
                @"var x = $""$${0}""",
                @"var x = $""abc$${0}""",
                afterUndo: @"var x = $""$${0}""");
        }

        #endregion

        #region Paste from external source into normal interpolated string after hole

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteOnly(
                pasteText: "\n",
                @"var x = $""{0}$$""",
                @"var x = $""{0}\n$$""",
                afterUndo: "var x = $\"{0}\n$$\"");
        }

        [WpfFact]
        public void TestPasteExternalNewLineIntoNormalInterpolatedStringAfterHole2()
        {
            TestPasteOnly(
                pasteText: "\r\n",
                @"var x = $""{0}$$""",
                @"var x = $""{0}\r\n$$""",
                afterUndo: "var x = $\"{0}\r\n$$\"");
        }

        [WpfFact]
        public void TestPasteExternalTabIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteOnly(
                pasteText: "\t",
                @"var x = $""{0}$$""",
                @"var x = $""{0}\t$$""",
                afterUndo: "var x = $\"{0}\t$$\"");
        }

        [WpfFact]
        public void TestPasteExternalSingleQuoteIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteOnly(
                pasteText: "'",
                @"var x = $""{0}$$""",
                @"var x = $""{0}'$$""",
                afterUndo: "var x = $\"{0}$$\"");
        }

        [WpfFact]
        public void TestPasteExternalDoubleQuoteIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteOnly(
                pasteText: "\"",
                @"var x = $""{0}$$""",
                @"var x = $""{0}\""$$""",
                afterUndo: "var x = $\"{0}\"$$\"");
        }

        [WpfFact]
        public void TestPasteExternalComplexStringIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteOnly(
                pasteText: "\t\"\"\t",
                @"var x = $""{0}$$""",
                @"var x = $""{0}\t\""\""\t$$""",
                afterUndo: "var x = $\"{0}\t\"\"\t$$\"");
        }

        [WpfFact]
        public void TestPasteExternalNormalTextIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteOnly(
                pasteText: "abc",
                @"var x = $""{0}$$""",
                @"var x = $""{0}abc$$""",
                afterUndo: @"var x = $""{0}$$""");
        }

        #endregion

        #region Paste from external source into verbatim string

        [WpfFact]
        public void TestPasteExternalNewLineIntoVerbatimString1()
        {
            TestPasteOnly(
                pasteText: "\n",
                @"var x = @""$$""",
                "var x = @\"\n$$\"",
                afterUndo: @"var x = @""$$""");
        }

        [WpfFact]
        public void TestPasteExternalNewLineIntoVerbatimString2()
        {
            TestPasteOnly(
                pasteText: "\r\n",
                @"var x = @""$$""",
                "var x = @\"\r\n$$\"",
                afterUndo: @"var x = @""$$""");
        }

        [WpfFact]
        public void TestPasteExternalTabIntoVerbatimString1()
        {
            TestPasteOnly(
                pasteText: "\t",
                @"var x = @""$$""",
                "var x = @\"\t$$\"",
                afterUndo: @"var x = @""$$""");
        }

        [WpfFact]
        public void TestPasteExternalSingleQuoteIntoVerbatimString()
        {
            TestPasteOnly(
                pasteText: "'",
                @"var x = @""$$""",
                @"var x = @""'$$""",
                afterUndo: @"var x = @""$$""");
        }

        [WpfFact]
        public void TestPasteExternalDoubleQuoteIntoVerbatimString()
        {
            TestPasteOnly(
                pasteText: "\"",
                @"var x = @""$$""",
                @"var x = @""""$$""""",
                afterUndo: @"var x = @""""$$""");
        }

        [WpfFact]
        public void TestPasteExternalComplexStringIntoVerbatimString()
        {
            TestPasteOnly(
                pasteText: "\t\"\"\t",
                @"var x = @""$$""",
                "var x = @\"\t\"\"\t$$\"",
                afterUndo: @"var x = @""$$""");
        }

        [WpfFact]
        public void TestPasteExternalNormalTextIntoVerbatimString()
        {
            TestPasteOnly(
                pasteText: "abc",
                @"var x = @""$$""",
                @"var x = @""abc$$""",
                afterUndo: @"var x = @""$$""");
        }

        #endregion
    }
}
