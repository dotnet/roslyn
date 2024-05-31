// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste;

[UseExportProvider]
public abstract class StringCopyPasteCommandHandlerTests
{
    internal sealed class StringCopyPasteTestState : AbstractCommandHandlerTestState
    {
        private static readonly TestComposition s_composition =
            EditorTestCompositions.EditorFeaturesWpf
                .AddParts(typeof(StringCopyPasteCommandHandler));

        private static readonly TestComposition s_compositionWithMockCopyPasteService =
            EditorTestCompositions.EditorFeaturesWpf
                .RemoveExcludedPartTypes(typeof(WpfStringCopyPasteService))
                .AddParts(typeof(TestStringCopyPasteService))
                .AddParts(typeof(StringCopyPasteCommandHandler));

        public readonly StringCopyPasteCommandHandler CommandHandler;

        public StringCopyPasteTestState(XElement workspaceElement, bool mockCopyPasteService)
            : base(workspaceElement, mockCopyPasteService
                  ? s_compositionWithMockCopyPasteService
                  : s_composition)
        {
            CommandHandler = (StringCopyPasteCommandHandler)GetExportedValues<ICommandHandler>().
                Single(c => c is StringCopyPasteCommandHandler);
        }

        public static StringCopyPasteTestState CreateTestState(string? copyFileMarkup, string pasteFileMarkup, bool mockCopyPasteService)
            => new(GetWorkspaceXml(copyFileMarkup, pasteFileMarkup), mockCopyPasteService);

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
            TestFileMarkupParser.GetPositionAndSpans(
                expectedCode, out var massaged, out int? caretPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(massaged, TextView.TextSnapshot.GetText());
            Assert.Equal(caretPosition, TextView.Caret.Position.BufferPosition.Position);

            var virtualSpaces = spans.SingleOrDefault(kvp => kvp.Key.StartsWith("VirtualSpaces#"));
            if (virtualSpaces.Key != null)
            {
                var virtualOffset = int.Parse(virtualSpaces.Key["VirtualSpaces-".Length..]);
                Assert.True(TextView.Caret.InVirtualSpace);
                Assert.Equal(virtualOffset, TextView.Caret.Position.VirtualBufferPosition.VirtualSpaces);
            }
        }

        public void TestCopyPaste(string expectedMarkup, string? pasteText, bool pasteTextIsKnown, string afterUndoMarkup)
        {
            var workspace = this.Workspace;

            // Ensure we clear out the clipboard so that a prior copy/paste doesn't corrupt the test.
            var service = workspace.Services.GetRequiredService<IStringCopyPasteService>() as TestStringCopyPasteService;
            service?.TrySetClipboardData(StringCopyPasteCommandHandler.KeyAndVersion, "");

            var copyDocument = this.Workspace.Documents.FirstOrDefault(d => d.AnnotatedSpans.ContainsKey("Copy"));
            if (copyDocument != null)
            {
                Assert.Null(pasteText);
                var copySpans = copyDocument.AnnotatedSpans["Copy"];

                SetSelection(copyDocument, copySpans, out var copyTextView, out var copyTextBuffer);

                CommandHandler.ExecuteCommand(
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
                CommandHandler.ExecuteCommand(
                    new PasteCommandArgs(this.TextView, this.SubjectBuffer), () => EditorOperations.Paste(), TestCommandExecutionContext.Create());
            }
            else
            {
                // otherwise, this is a test of text coming in from another source.  Do the edit manually.

                if (pasteTextIsKnown)
                {
                    // we were given text to directly place on the clipboard without needing to do a copy.
                    Contract.ThrowIfNull(pasteText);
                    var json = new StringCopyPasteData(ImmutableArray.Create(StringCopyPasteContent.ForText(pasteText))).ToJson();
                    Contract.ThrowIfNull(json);
                    service!.TrySetClipboardData(StringCopyPasteCommandHandler.KeyAndVersion, json);
                }

                CommandHandler.ExecuteCommand(
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
            EditorTestHostDocument document,
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
