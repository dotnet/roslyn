// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Data.SqlTypes;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    [UseExportProvider]
    public abstract class StringCopyPasteCommandHandlerTests
    {
        internal sealed class StringCopyPasteTestState : AbstractCommandHandlerTestState
        {
            [ExportWorkspaceService(typeof(IStringCopyPasteService), ServiceLayer.Host), Shared]
            [PartNotDiscoverable]
            private class MockStringCopyPasteService : IStringCopyPasteService
            {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public MockStringCopyPasteService()
                {
                }

                public bool TryGetClipboardSequenceNumber(out int sequenceNumber)
                {
                    sequenceNumber = 0;
                    return true;
                }
            }

            private static readonly TestComposition s_compositionWithUnknownCopy =
                EditorTestCompositions.EditorFeaturesWpf
                    .AddParts(typeof(StringCopyPasteCommandHandler))
                    .AddExcludedPartTypes(typeof(WpfStringCopyPasteService));
            private static readonly TestComposition s_compositionWithKnownCopy =
                s_compositionWithUnknownCopy.AddParts(typeof(MockStringCopyPasteService));

            private readonly StringCopyPasteCommandHandler _commandHandler;

            public StringCopyPasteTestState(XElement workspaceElement, bool unknownCopy)
                : base(workspaceElement, unknownCopy ? s_compositionWithUnknownCopy : s_compositionWithKnownCopy)
            {
                _commandHandler = (StringCopyPasteCommandHandler)GetExportedValues<ICommandHandler>().
                    Single(c => c is StringCopyPasteCommandHandler);
            }

            public static StringCopyPasteTestState CreateTestState(string? copyFileMarkup, string pasteFileMarkup)
                => new(GetWorkspaceXml(copyFileMarkup, pasteFileMarkup), unknownCopy: copyFileMarkup == null);

            public static XElement GetWorkspaceXml(string? copyFileMarkup, string pasteFileMarkup)
                => XElement.Parse(($@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document Markup=""SpansOnly"">{pasteFileMarkup}</Document>
    </Project>
    {(copyFileMarkup == null ? "" : $@"
    <Project Language=""C#"" CommonReferences=""true"">
        <Document Markup=""SpansOnly"">{copyFileMarkup}</Document>
    </Project>")}
</Workspace>"));

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
                expectedMarkup = expectedMarkup.Replace("$", "\uD7FF");

                MarkupTestFile.GetPositionAndSpan(expectedMarkup, out expected, out int? cursorPosition, out var caretSpan);
                Contract.ThrowIfTrue(cursorPosition != null);

                expected = expected.Replace("\uD7FF", "$");

                Assert.True(caretSpan.HasValue && caretSpan.Value.IsEmpty);
                caretPosition = caretSpan!.Value.Start;
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

                var selections = copySpans.Select(s => new Selection(s.ToSnapshotSpan(textBuffer.CurrentSnapshot))).ToArray();
                broker.SetSelectionRange(selections, selections.First());
            }
        }
    }
}
