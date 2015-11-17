// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Text;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public partial class InteractiveWindowTests : IDisposable
    {
        [WpfFact]
        public void CopyWithinInput()
        {
            _testClipboard.Clear();

            Window.InsertCode("1 + 2");
            Window.Operations.SelectAll();
            Window.Operations.Copy();
            VerifyClipboardData("1 + 2", "1 + 2", @"[{""content"":""1 + 2"",""kind"":2}]");

            // Shrink the selection.
            var selection = Window.TextView.Selection;
            var span = selection.SelectedSpans[0];
            selection.Select(new SnapshotSpan(span.Snapshot, span.Start + 1, span.Length - 2), isReversed: false);

            Window.Operations.Copy();
            VerifyClipboardData(" + ", " + ", @"[{""content"":"" + "",""kind"":2}]");
        }

        [WpfFact]
        public async Task CopyInputAndOutput()
        {
            _testClipboard.Clear();

            await Submit(
@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();",
@"1
2
3
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.Copy();
            VerifyClipboardData(@"> foreach (var o in new[] { 1, 2, 3 })
> System.Console.WriteLine();
1
2
3
> ",
@"> foreach (var o in new[] \{ 1, 2, 3 \})\par > System.Console.WriteLine();\par 1\par 2\par 3\par > ",
@"[{""content"":""> "",""kind"":0},{""content"":""foreach (var o in new[] { 1, 2, 3 })\u000d\u000a"",""kind"":2},{""content"":""> "",""kind"":0},{""content"":""System.Console.WriteLine();\u000d\u000a"",""kind"":2},{""content"":""1\u000d\u000a2\u000d\u000a3\u000d\u000a"",""kind"":1},{""content"":""> "",""kind"":0}]");

            // Shrink the selection.
            var selection = Window.TextView.Selection;
            var span = selection.SelectedSpans[0];
            selection.Select(new SnapshotSpan(span.Snapshot, span.Start + 3, span.Length - 6), isReversed: false);

            Window.Operations.Copy();
            VerifyClipboardData(@"oreach (var o in new[] { 1, 2, 3 })
> System.Console.WriteLine();
1
2
3",
@"oreach (var o in new[] \{ 1, 2, 3 \})\par > System.Console.WriteLine();\par 1\par 2\par 3",
@"[{""content"":""oreach (var o in new[] { 1, 2, 3 })\u000d\u000a"",""kind"":2},{""content"":""> "",""kind"":0},{""content"":""System.Console.WriteLine();\u000d\u000a"",""kind"":2},{""content"":""1\u000d\u000a2\u000d\u000a3"",""kind"":1}]");
        }

        [WpfFact]
        public void CutWithinInput()
        {
            _testClipboard.Clear();

            Window.InsertCode("foreach (var o in new[] { 1, 2, 3 })");
            Window.Operations.BreakLine();
            Window.InsertCode("System.Console.WriteLine();");
            Window.Operations.BreakLine();

            var caret = Window.TextView.Caret;
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.SelectAll();
            // Shrink the selection.
            var selection = Window.TextView.Selection;
            var span = selection.SelectedSpans[0];
            selection.Select(new SnapshotSpan(span.Snapshot, span.Start + 3, span.Length - 6), isReversed: false);

            Window.Operations.Cut();
            VerifyClipboardData(
@"each (var o in new[] { 1, 2, 3 })
System.Console.WriteLine()",
                expectedRtf: null,
                expectedRepl: null);
        }

        [WpfFact]
        public async Task CutInputAndOutput()
        {
            _testClipboard.Clear();

            await Submit(
@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();",
@"1
2
3
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.Cut();
            VerifyClipboardData(null, null, null);
        }

        /// <summary>
        /// When there is no selection, copy
        /// should copy the current line.
        /// </summary>
        [WpfFact]
        public async Task CopyNoSelection()
        {
            await Submit(
@"s +

 t",
@" 1

2 ").ConfigureAwait(true);
            CopyNoSelectionAndVerify(0, 7, "> s +\r\n", @"> s +\par ", @"[{""content"":""> "",""kind"":0},{""content"":""s +\u000d\u000a"",""kind"":2}]");
            CopyNoSelectionAndVerify(7, 11, "> \r\n", @"> \par ", @"[{""content"":""> "",""kind"":0},{""content"":""\u000d\u000a"",""kind"":2}]");
            CopyNoSelectionAndVerify(11, 17, ">  t\r\n", @">  t\par ", @"[{""content"":""> "",""kind"":0},{""content"":"" t\u000d\u000a"",""kind"":2}]");
            CopyNoSelectionAndVerify(17, 21, " 1\r\n", @" 1\par ", @"[{""content"":"" 1\u000d\u000a"",""kind"":1}]");
            CopyNoSelectionAndVerify(21, 23, "\r\n", @"\par ", @"[{""content"":""\u000d\u000a"",""kind"":1}]");
            CopyNoSelectionAndVerify(23, 28, "2 > ", "2 > ", @"[{""content"":""2 "",""kind"":1},{""content"":""> "",""kind"":0}]");
        }

        [WpfFact]
        public void Paste()
        {
            var blocks = new[]
            {
                new BufferBlock(ReplSpanKind.Output, "a\r\nbc"),
                new BufferBlock(ReplSpanKind.Prompt, "> "),
                new BufferBlock(ReplSpanKind.Prompt, "< "),
                new BufferBlock(ReplSpanKind.Input, "12"),
                new BufferBlock(ReplSpanKind.StandardInput, "3"),
                new BufferBlock((ReplSpanKind)10, "xyz")
            };

            // Paste from text clipboard format.
            CopyToClipboard(blocks, includeRepl: false);
            Window.Operations.Paste();
            Assert.Equal("> a\r\n> bc> < 123xyz", GetTextFromCurrentSnapshot());

            Window.Operations.ClearView();
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            // Paste from custom clipboard format.
            CopyToClipboard(blocks, includeRepl: true);
            Window.Operations.Paste();
            Assert.Equal("> a\r\n> bc123", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void CopyAndPasteFullLineNoSelection()
        {
            _testClipboard.Clear();

            Window.InsertCode(
@"Print(1);
Print(2);");
            var caret = Window.TextView.Caret;
            for (int i = 0; i < 14; ++i)
            {
                caret.MoveToPreviousCaretPosition();
            }
            // the caret is here:
            // > Print(1|);       
            Window.Operations.Copy();
            VerifyClipboardData("> Print(1);\r\n",
                "\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > Print(1);\\par ",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"Print(1);\\u000d\\u000a\",\"kind\":2}]");

            Window.Operations.Paste();
            Assert.Equal("> Print(1);\r\n> Print(1);\r\n> Print(2);", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void JsonSerialization()
        {
            var expectedContent = new[]
            {
                new BufferBlock(ReplSpanKind.Prompt, "> "),
                new BufferBlock(ReplSpanKind.Input, "Hello"),
                new BufferBlock(ReplSpanKind.Prompt, ". "),
                new BufferBlock(ReplSpanKind.StandardInput, "world"),
                new BufferBlock(ReplSpanKind.Output, "Hello world"),
            };
            var actualJson = BufferBlock.Serialize(expectedContent);
            var expectedJson = @"[{""content"":""> "",""kind"":0},{""content"":""Hello"",""kind"":2},{""content"":"". "",""kind"":0},{""content"":""world"",""kind"":3},{""content"":""Hello world"",""kind"":1}]";
            Assert.Equal(expectedJson, actualJson);
            var actualContent = BufferBlock.Deserialize(actualJson);
            Assert.Equal(expectedContent.Length, actualContent.Length);
            for (int i = 0; i < expectedContent.Length; i++)
            {
                var expectedBuffer = expectedContent[i];
                var actualBuffer = actualContent[i];
                Assert.Equal(expectedBuffer.Kind, actualBuffer.Kind);
                Assert.Equal(expectedBuffer.Content, actualBuffer.Content);
            }
        }

        [WpfFact]
        public async Task CutWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("2");

            var caret = Window.TextView.Caret;
            _testClipboard.Clear();

            // Cut() with caret in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            await TaskRun(() => Window.Operations.Cut()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 2", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(1, 1);

            VerifyClipboardData(null, null, null);

            // Cut() with caret in active prompt
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(2, 0);
            await TaskRun(() => Window.Operations.Cut()).ConfigureAwait(true);

            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            VerifyClipboardData("2", expectedRtf: null, expectedRepl: null);
        }

        [WpfFact]
        public async Task CutWithSelectionInReadonlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("23");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;
            _testClipboard.Clear();

            // Cut() with selection in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            await TaskRun(() => Window.Operations.Cut()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());
            VerifyClipboardData(null, null, null);

            // Cut() with selection in active prompt, no-op
            selection.Clear();
            var start = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            var end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Cut()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());
            VerifyClipboardData(null, null, null);

            // Cut() with selection overlaps with editable buffer, 
            // Cut editable content and move caret to closest editable location 
            selection.Clear();
            caret.MoveToPreviousCaretPosition();
            start = caret.MoveToPreviousCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 3);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Cut()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 3", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            VerifyClipboardData("2", expectedRtf: null, expectedRepl: null);
        }

        [WpfFact]
        public async Task PasteWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("2");

            var caret = Window.TextView.Caret;

            _testClipboard.Clear();
            Window.Operations.Home(true);
            Window.Operations.Copy();
            VerifyClipboardData("2", @"\ansi{\fonttbl{\f0 Consolas;}}{\colortbl;\red0\green0\blue0;\red255\green255\blue255;}\f0 \fs24 \cf1 \cb2 \highlight2 2", @"[{""content"":""2"",""kind"":2}]");

            // Paste() with caret in readonly area, no-op 
            Window.TextView.Selection.Clear();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.Paste();
            Assert.Equal("> 1\r\n1\r\n> 2", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(1, 1);

            // Paste() with caret in active prompt
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(2, 0);
            Window.Operations.Paste();

            Assert.Equal("> 1\r\n1\r\n> 22", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 3);
        }

        [WpfFact]
        public async Task PasteWithSelectionInReadonlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("23");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            _testClipboard.Clear();
            Window.Operations.Home(true);
            Window.Operations.Copy();
            VerifyClipboardData("23", @"\ansi{\fonttbl{\f0 Consolas;}}{\colortbl;\red0\green0\blue0;\red255\green255\blue255;}\f0 \fs24 \cf1 \cb2 \highlight2 23", @"[{""content"":""23"",""kind"":2}]");

            // Paste() with selection in readonly area, no-op  
            selection.Clear();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            await TaskRun(() => Window.Operations.Paste()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Paste() with selection in active prompt, no-op
            selection.Clear();
            var start = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            var end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Paste()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Paste() with selection overlaps with editable buffer, 
            // Cut editable content, move caret to closest editable location and insert text
            selection.Clear();
            caret.MoveToPreviousCaretPosition();
            start = caret.MoveToPreviousCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 3);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Paste()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 233", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 4);
        }

        [WpfFact]
        public async Task CutLineWithOutSelection()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;
            _testClipboard.Clear();

            // CutLine with caret in readonly area
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();

            AssertCaretVirtualPosition(1, 1);
            Window.Operations.CutLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(1, 1);
            VerifyClipboardData(null, null, null);

            // CutLine with caret in active prompt
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");
            for (int i = 0; i < 11; ++i)
            {
                caret.MoveToPreviousCaretPosition();
            }

            AssertCaretVirtualPosition(2, 0);
            Window.Operations.CutLine();
            Assert.Equal("> 1\r\n1\r\n> ;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            VerifyClipboardData("int x\r\n", null, null);

            // CutLine with caret in editable area   
            caret.MoveToNextCaretPosition();

            Window.Operations.CutLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            VerifyClipboardData(";", null, null);
        }

        [WpfFact]
        public async Task CutLineWithSelection()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;
            _testClipboard.Clear();

            // CutLine with selection in readonly area  
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.SelectAll();
            Window.Operations.CutLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            VerifyClipboardData(null, null, null);

            // CutLine with selection in active prompt
            selection.Clear();
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");
            for (int i = 0; i < 11; ++i)
            {
                caret.MoveToPreviousCaretPosition();
            }

            selection.Select(caret.MoveToNextCaretPosition().VirtualBufferPosition, caret.MoveToNextCaretPosition().VirtualBufferPosition);
            Window.Operations.CutLine();
            Assert.Equal("> 1\r\n1\r\n> ;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);
            VerifyClipboardData("int x\r\n", null, null);

            // CutLine with selection in editable area   
            Window.InsertCode("int x");
            selection.Select(caret.MoveToPreviousCaretPosition().VirtualBufferPosition, caret.MoveToPreviousCaretPosition().VirtualBufferPosition);
            Window.Operations.CutLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);
            VerifyClipboardData("int x;", null, null);

            // CutLine with selection spans all areas     
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.CutLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);
            VerifyClipboardData("int x\r\n;", null, null);
        }

        [WorkItem(6054, "https://github.com/dotnet/roslyn/issues/6054")]
        [WpfFact]
        public void UndoMultiLinePaste()
        {
            CopyToClipboard(
@"1
2
3");

            // paste multi-line text
            Window.Operations.Paste();
            Assert.Equal("> 1\r\n> 2\r\n> 3", GetTextFromCurrentSnapshot());

            // undo paste
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            // redo paste
            ((InteractiveWindow)Window).Redo_TestOnly(1);
            Assert.Equal("> 1\r\n> 2\r\n> 3", GetTextFromCurrentSnapshot());


            CopyToClipboard(
@"4
5
6");
            // replace current text 
            Window.Operations.SelectAll();
            Window.Operations.Paste();
            Assert.Equal("> 4\r\n> 5\r\n> 6", GetTextFromCurrentSnapshot());

            // undo replace
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 1\r\n> 2\r\n> 3", GetTextFromCurrentSnapshot());

            // undo paste
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> ", GetTextFromCurrentSnapshot());
        }

        private void CopyToClipboard(string text)
        {
            _testClipboard.Clear();
            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, text);
            data.SetData(DataFormats.StringFormat, text);
            _testClipboard.SetDataObject(data, false);
        }

        private void CopyToClipboard(BufferBlock[] blocks, bool includeRepl)
        {
            _testClipboard.Clear();
            var data = new DataObject();
            var builder = new StringBuilder();
            foreach (var block in blocks)
            {
                builder.Append(block.Content);
            }
            var text = builder.ToString();
            data.SetData(DataFormats.UnicodeText, text);
            data.SetData(DataFormats.StringFormat, text);
            if (includeRepl)
            {
                data.SetData(InteractiveWindow.ClipboardFormat, BufferBlock.Serialize(blocks));
            }
            _testClipboard.SetDataObject(data, false);
        }

        private void VerifyClipboardData(string expectedText, string expectedRtf, string expectedRepl)
        {
            var data = _testClipboard.GetDataObject();
            Assert.Equal(expectedText, data?.GetData(DataFormats.StringFormat));
            Assert.Equal(expectedText, data?.GetData(DataFormats.Text));
            Assert.Equal(expectedText, data?.GetData(DataFormats.UnicodeText));
            Assert.Equal(expectedRepl, (string)data?.GetData(InteractiveWindow.ClipboardFormat));
            var actualRtf = (string)data?.GetData(DataFormats.Rtf);
            if (expectedRtf == null)
            {
                Assert.Null(actualRtf);
            }
            else
            {
                Assert.True(actualRtf.StartsWith(@"{\rtf"));
                Assert.True(actualRtf.EndsWith(expectedRtf + "}"));
            }
        }

        private void CopyNoSelectionAndVerify(int start, int end, string expectedText, string expectedRtf, string expectedRepl)
        {
            var caret = Window.TextView.Caret;
            var snapshot = Window.TextView.TextBuffer.CurrentSnapshot;
            for (int i = start; i < end; i++)
            {
                _testClipboard.Clear();
                caret.MoveTo(new SnapshotPoint(snapshot, i));
                Window.Operations.Copy();
                VerifyClipboardData(expectedText, expectedRtf, expectedRepl);
            }
        }
    }
}
