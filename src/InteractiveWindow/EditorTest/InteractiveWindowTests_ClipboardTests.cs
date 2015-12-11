// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public partial class InteractiveWindowTests : IDisposable
    {
        [WpfFact]
        public void CopyStreamSelectionWithinInput()
        {
            _testClipboard.Clear();

            Window.InsertCode("1 + 2");
            Window.Operations.SelectAll();
            Window.Operations.Copy();
            VerifyClipboardData("1 + 2", 
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 1 + 2}", 
                "[{\"content\":\"1 + 2\",\"kind\":2}]");

            // Shrink the selection.
            var selection = Window.TextView.Selection;
            var span = selection.SelectedSpans[0];
            selection.Select(new SnapshotSpan(span.Snapshot, span.Start + 1, span.Length - 2), isReversed: false);

            Window.Operations.Copy();
            VerifyClipboardData(" + ",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2  + }",
                @"[{""content"":"" + "",""kind"":2}]");
        }

        [WpfFact]
        public void CopyStreamSelectionInputAndActivePrompt()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a stream selection as follows:
            // > |111
            // > 222|
            Window.Operations.SelectAll();
            Window.Operations.Copy();
            VerifyClipboardData("111\r\n> 222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111\\par > 222}",
                "[{\"content\":\"111\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\",\"kind\":2}]");
        }

        [WpfFact]
        public async Task CopyStreamSelectionInputAndOutput()
        {
            _testClipboard.Clear();

            await Submit(
@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine(o);",
@"1
2
3
").ConfigureAwait(true);
            Window.InsertCode("1 + 2");

            // make a stream selection as follows:
            // |> foreach (var o in new[] { 1, 2, 3 })
            // > System.Console.WriteLine(o);
            // 1
            // 2
            // 3
            // > 1 + 2|
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.Copy();
            VerifyClipboardData("> foreach (var o in new[] { 1, 2, 3 })\r\n> System.Console.WriteLine(o);\r\n1\r\n2\r\n3\r\n> 1 + 2",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > foreach (var o in new[] \\{ 1, 2, 3 \\})\\par > System.Console.WriteLine(o);\\par 1\\par 2\\par 3\\par > 1 + 2}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"foreach (var o in new[] { 1, 2, 3 })\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"System.Console.WriteLine(o);\\u000d\\u000a\",\"kind\":2},{\"content\":\"1\\u000d\\u000a2\\u000d\\u000a3\\u000d\\u000a\",\"kind\":1},{\"content\":\"> \",\"kind\":0},{\"content\":\"1 + 2\",\"kind\":2}]");

            // Shrink the selection.
            var selection = Window.TextView.Selection;
            var span = selection.SelectedSpans[0];
            selection.Select(new SnapshotSpan(span.Snapshot, span.Start + 3, span.Length - 6), isReversed: false);

            Window.Operations.Copy();
            VerifyClipboardData("oreach (var o in new[] { 1, 2, 3 })\r\n> System.Console.WriteLine(o);\r\n1\r\n2\r\n3\r\n> 1 ",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 oreach (var o in new[] \\{ 1, 2, 3 \\})\\par > System.Console.WriteLine(o);\\par 1\\par 2\\par 3\\par > 1 }",
                "[{\"content\":\"oreach (var o in new[] { 1, 2, 3 })\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"System.Console.WriteLine(o);\\u000d\\u000a\",\"kind\":2},{\"content\":\"1\\u000d\\u000a2\\u000d\\u000a3\\u000d\\u000a\",\"kind\":1},{\"content\":\"> \",\"kind\":0},{\"content\":\"1 \",\"kind\":2}]");
        }

        [WpfFact]
        public void CopyBoxSelectionWithinInput()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a box selection as follows:
            // > 11|1|
            // > 22|2|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            Window.Operations.Copy();
            VerifyClipboardData("1\r\n2\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 1\\par 2}",
                "[{\"content\":\"1\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"2\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4}]",
                expectedToBeBoxCopy: true);
        }

        [WpfFact]
        public void CopyBoxSelectionInputAndActivePrompt()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a box selection as follows:
            // |> 111|
            // |> 222|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(11);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            Window.Operations.Copy();
            VerifyClipboardData("> 111\r\n> 222\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par > 222}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4}]",
                expectedToBeBoxCopy: true);
        }

        [WpfFact]
        public async Task CopyBoxSelectionInputAndOutput()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            await Submit(
@"11111",
@"11111
").ConfigureAwait(true);

            Window.InsertCode("222");

            // make a box selection as follows:
            // 1111|1|
            // > 22|2|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            Window.Operations.Copy();
            VerifyClipboardData("1\r\n2\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 1\\par 2}",
                "[{\"content\":\"1\",\"kind\":1},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"2\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4}]",
                expectedToBeBoxCopy: true);
        }

        [WpfFact]
        public void CutStreamSelectionWithinInputThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");

            // make a stream selection as follows:
            // > |111|
            Window.Operations.SelectAll();

            Window.Operations.Cut();

            Assert.Equal("> ", GetTextFromCurrentSnapshot());
            Assert.True(Window.TextView.Selection.IsEmpty);

            VerifyClipboardData("111",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111}",
                "[{\"content\":\"111\",\"kind\":2}]");

            // undo 
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void CutStreamSelectionInputAndActivePromptThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a stream selection as follows:
            // > |111
            // > 222|
            Window.Operations.SelectAll();
            Window.Operations.Cut();
            
            Assert.Equal("> ", GetTextFromCurrentSnapshot());
            Assert.True(Window.TextView.Selection.IsEmpty);

            VerifyClipboardData("111\r\n> 222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111\\par > 222}",
                "[{\"content\":\"111\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\",\"kind\":2}]");

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task CutStreamSelectionInputAndOutput()
        {
            _testClipboard.Clear();

            await Submit(
@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();",
@"1
2
3
").ConfigureAwait(true);
            Window.InsertCode("1 + 2");

            // make a stream selection as follows:
            // |> foreach (var o in new[] { 1, 2, 3 })
            // > System.Console.WriteLine(o);
            // 1
            // 2
            // 3
            // > 1 + 2|
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.Cut();

            // expect nothing got deleted
            Assert.Equal("> foreach (var o in new[] { 1, 2, 3 })\r\n> System.Console.WriteLine();\r\n1\r\n2\r\n3\r\n> 1 + 2", 
                GetTextFromCurrentSnapshot());
            Assert.False(Window.TextView.Selection.IsEmpty);

            // everything got copied to clipboard
            VerifyClipboardData("> foreach (var o in new[] { 1, 2, 3 })\r\n> System.Console.WriteLine();\r\n1\r\n2\r\n3\r\n> 1 + 2",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > foreach (var o in new[] \\{ 1, 2, 3 \\})\\par > System.Console.WriteLine();\\par 1\\par 2\\par 3\\par > 1 + 2}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"foreach (var o in new[] { 1, 2, 3 })\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"System.Console.WriteLine();\\u000d\\u000a\",\"kind\":2},{\"content\":\"1\\u000d\\u000a2\\u000d\\u000a3\\u000d\\u000a\",\"kind\":1},{\"content\":\"> \",\"kind\":0},{\"content\":\"1 + 2\",\"kind\":2}]");
        }

        [WpfFact]
        public void CutBoxSelectionWithinInputThenUndo()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a box selection as follows:
            // > 11|1|
            // > 22|2|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            Window.Operations.Cut();

            // expected snapshot after cut:
            // > 11
            // > 22
            Assert.Equal("> 11\r\n> 22", GetTextFromCurrentSnapshot());
            Assert.True(IsEmptyBoxSelection());

            VerifyClipboardData("1\r\n2\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 1\\par 2}",
                "[{\"content\":\"1\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"2\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4}]",
                expectedToBeBoxCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void CutBoxSelectionInputAndActivePromptThenUndo()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a box selection as follows:
            // |> 111|
            // |> 222|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(11);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            Window.Operations.Cut();

            // expected snapshot after cut:
            // >
            // >  
            Assert.Equal("> \r\n> ", GetTextFromCurrentSnapshot());
            Assert.True(IsEmptyBoxSelection());

            VerifyClipboardData("> 111\r\n> 222\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par > 222}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4}]",
                expectedToBeBoxCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task CutBoxSelectionInputAndOutput()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            await Submit(
@"11111",
@"11111
").ConfigureAwait(true);

            Window.InsertCode("222");
            Window.Operations.BreakLine();
            Window.InsertCode("333");

            // make a box selection as follows:
            // 1111|1|
            // > 22|2|
            // > 33|3|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(13);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            // expected snapshot after cut:
            // 11111
            // > 222
            // > 333
            Window.Operations.Cut();

            Assert.Equal("> 11111\r\n11111\r\n> 222\r\n> 333", GetTextFromCurrentSnapshot());
            Assert.False(Window.TextView.Selection.IsEmpty);
            Assert.True(Window.TextView.Selection.Mode == Text.Editor.TextSelectionMode.Box);
            Assert.False(IsEmptyBoxSelection());

            VerifyClipboardData("1\r\n2\r\n3\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 1\\par 2\\par 3}",
                "[{\"content\":\"1\",\"kind\":1},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"2\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"3\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4}]",
                expectedToBeBoxCopy: true);
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

22
").ConfigureAwait(true);
            Window.InsertCode("1 + 2");

            // readonly buffer
            CopyNoSelectionAndVerify(0, 7, "> s +\r\n", 
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > s +\\par }",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"s +\\u000d\\u000a\",\"kind\":2}]");
            CopyNoSelectionAndVerify(7, 11, "> \r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > \\par }",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"\\u000d\\u000a\",\"kind\":2}]");
            CopyNoSelectionAndVerify(11, 17, ">  t\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 >  t\\par }",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\" t\\u000d\\u000a\",\"kind\":2}]");
            CopyNoSelectionAndVerify(17, 21, " 1\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2  1\\par }",
                "[{\"content\":\" 1\\u000d\\u000a\",\"kind\":1}]");
            CopyNoSelectionAndVerify(21, 23, "\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 \\par }",
                "[{\"content\":\"\\u000d\\u000a\",\"kind\":1}]");
            CopyNoSelectionAndVerify(23, 27, "22\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 22\\par }",
                "[{\"content\":\"22\\u000d\\u000a\",\"kind\":1}]");

            // editable buffer and active prompt
            CopyNoSelectionAndVerify(27, 34, "> 1 + 2",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 1 + 2}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"1 + 2\",\"kind\":2}]");
        }

        [WpfFact]
        public void CutNoSelectionInInputThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");

            // caret at"
            // > 11|1
            MoveCaretToPreviousPosition(1);
            Window.Operations.Cut();

            // expected snapshot after cut:
            // > 
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("> 111",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void CutNoSelectionInActivePromptThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");

            // caret at"
            // > 11|1
            MoveCaretToPreviousPosition(5);
            Window.Operations.Cut();

            // expected snapshot after cut:
            // > 
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("> 111",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task CutNoSelectionInReadOnly()
        {
            _testClipboard.Clear();
            await Submit(
@"111",
@"111
").ConfigureAwait(true);

            // caret at"
            // > 111
            // 11|1
            // > 
            MoveCaretToPreviousPosition(4);
            Window.Operations.Cut();

            // expected snapshot after cut:
            // > 
            Assert.Equal("> 111\r\n111\r\n> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("111\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111\\par }",
                "[{\"content\":\"111\\u000d\\u000a\",\"kind\":1}]",
                expectedToBeLineCopy: true);

            // caret in non-active prompt"
            // |> 111
            // 111
            // > 
            MoveCaretToPreviousPosition(8);
            Window.Operations.Cut();

            Assert.Equal("> 111\r\n111\r\n> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("> 111\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par }",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\\u000d\\u000a\",\"kind\":2}]",
                expectedToBeLineCopy: true);
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
                new BufferBlock((ReplSpanKind)10, "xyz")    // this is invalid ReplSpanKind value
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
        public void CutLineNoSelectionInInputThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");

            // caret at"
            // > 11|1
            MoveCaretToPreviousPosition(1);
            Window.Operations.CutLine();

            // expected snapshot after cut:
            // > 
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("> 111",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void CutLineNoSelectionInActivePromptThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");

            // caret at"
            // > 11|1
            MoveCaretToPreviousPosition(5);
            Window.Operations.CutLine();

            // expected snapshot after cut:
            // > 
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("> 111",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task CutLineNoSelectionInReadOnly()
        {
            _testClipboard.Clear();
            await Submit(
@"111",
@"111
").ConfigureAwait(true);

            // caret at"
            // > 111
            // 11|1
            // > 
            MoveCaretToPreviousPosition(4);
            Window.Operations.CutLine();

            // expected snapshot after cut:
            // > 
            Assert.Equal("> 111\r\n111\r\n> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("111\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111\\par }",
                "[{\"content\":\"111\\u000d\\u000a\",\"kind\":1}]",
                expectedToBeLineCopy: true);

            // caret in non-active prompt"
            // |> 111
            // 111
            // > 
            MoveCaretToPreviousPosition(8);
            Window.Operations.CutLine();

            Assert.Equal("> 111\r\n111\r\n> ", GetTextFromCurrentSnapshot());

            VerifyClipboardData("> 111\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par }",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\\u000d\\u000a\",\"kind\":2}]",
                expectedToBeLineCopy: true);
        }

        [WpfFact]
        public void CutLineStreamSelectionWithinInputThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // make a stream selection as follows:
            // > 1|11|
            // > 222
            MoveCaretToPreviousPosition(6);
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(2);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Stream;
            selection.Select(anchor, active);

            Window.Operations.CutLine();

            Assert.Equal("> 222", GetTextFromCurrentSnapshot());
            Assert.True(Window.TextView.Selection.IsEmpty);

            VerifyClipboardData("> 111\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par }",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\\u000d\\u000a\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void CutLineStreamSelectionInputAndActivePromptThenUndo()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // make a stream selection as follows:
            // > |111
            // >| 222
            MoveCaretToPreviousPosition(4);
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(5);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Stream;
            selection.Select(anchor, active);

            Window.Operations.CutLine();

            Assert.Equal("> ", GetTextFromCurrentSnapshot());
            Assert.True(Window.TextView.Selection.IsEmpty);

            VerifyClipboardData("> 111\r\n> 222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par > 222}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task CutLineStreamSelectionInputAndOutput()
        {
            _testClipboard.Clear();

            await Submit(
@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();",
@"1
2
3
").ConfigureAwait(true);
            Window.InsertCode("1 + 2");

            // make a stream selection as follows:
            // |> foreach (var o in new[] { 1, 2, 3 })
            // > System.Console.WriteLine(o);
            // 1
            // 2
            // 3
            // > 1 + 2|
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.CutLine();

            // expect nothing got deleted
            Assert.Equal("> foreach (var o in new[] { 1, 2, 3 })\r\n> System.Console.WriteLine();\r\n1\r\n2\r\n3\r\n> 1 + 2",
                GetTextFromCurrentSnapshot());
            Assert.False(Window.TextView.Selection.IsEmpty);

            // everything got copied to clipboard
            VerifyClipboardData("> foreach (var o in new[] { 1, 2, 3 })\r\n> System.Console.WriteLine();\r\n1\r\n2\r\n3\r\n> 1 + 2",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > foreach (var o in new[] \\{ 1, 2, 3 \\})\\par > System.Console.WriteLine();\\par 1\\par 2\\par 3\\par > 1 + 2}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"foreach (var o in new[] { 1, 2, 3 })\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"System.Console.WriteLine();\\u000d\\u000a\",\"kind\":2},{\"content\":\"1\\u000d\\u000a2\\u000d\\u000a3\\u000d\\u000a\",\"kind\":1},{\"content\":\"> \",\"kind\":0},{\"content\":\"1 + 2\",\"kind\":2}]",
                expectedToBeLineCopy: true);
        }

        [WpfFact]
        public void CutLineBoxSelectionWithinInputThenUndo()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a box selection as follows:
            // > 11|1|
            // > 22|2|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            Window.Operations.CutLine();

            // expected snapshot after cut line:
            // > 
            Assert.Equal("> ", GetTextFromCurrentSnapshot());
            Assert.True(selection.IsEmpty);

            VerifyClipboardData("> 111\r\n> 222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par > 222}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void CutLineBoxSelectionInputAndActivePromptThenUndo()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a box selection as follows:
            // |> 111|
            // |> 222|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(11);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            Window.Operations.CutLine();

            // expected snapshot after cut line:
            // >
            Assert.Equal("> ", GetTextFromCurrentSnapshot());
            Assert.True(selection.IsEmpty);

            VerifyClipboardData("> 111\r\n> 222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 > 111\\par > 222}",
                "[{\"content\":\"> \",\"kind\":0},{\"content\":\"111\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task CutLineBoxSelectionInputAndOutput()
        {
            _testClipboard.Clear();
            var caret = Window.TextView.Caret;

            await Submit(
@"11111",
@"11111
").ConfigureAwait(true);

            Window.InsertCode("222");
            Window.Operations.BreakLine();
            Window.InsertCode("333");

            // make a box selection as follows:
            // 1111|1|
            // > 22|2|
            // > 33|3|
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(13);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            // expected snapshot after cut-line:
            // 11111
            // > 222
            // > 333
            Window.Operations.CutLine();

            Assert.Equal("> 11111\r\n11111\r\n> 222\r\n> 333", GetTextFromCurrentSnapshot());
            Assert.False(Window.TextView.Selection.IsEmpty);
            Assert.True(Window.TextView.Selection.Mode == Text.Editor.TextSelectionMode.Box);
            Assert.False(IsEmptyBoxSelection());

            VerifyClipboardData("11111\r\n> 222\r\n> 333",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 11111\\par > 222\\par > 333}",
                "[{\"content\":\"11111\\u000d\\u000a\",\"kind\":1},{\"content\":\"> \",\"kind\":0},{\"content\":\"222\\u000d\\u000a\",\"kind\":2},{\"content\":\"> \",\"kind\":0},{\"content\":\"333\",\"kind\":2}]",
                expectedToBeLineCopy: true);
        }

        [WpfFact]
        public void PasteNoSelectionWithinInputThenUndo()
        {
            // paste text copied from stream selection
            Window.InsertCode("111");
            MoveCaretToPreviousPosition(1);

            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 11TextCopiedFromStreamSelection1", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());

            // paste text copied from copyline
            // > 11|1
            Window.Operations.ClearView();
            Window.InsertCode("111");
            MoveCaretToPreviousPosition(1);

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> TextCopiedFromLineSelection\r\n> 111", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());

            // paste text copied from box selection into a blank line
            // > |
            // > 111
            Window.Operations.ClearView();
            Window.Operations.BreakLine();
            Window.InsertCode("111");
            MoveCaretToPreviousPosition(6);

            CopyBoxToClipboard();
            Window.Operations.Paste();

            //TODO: Fix this
            //Assert.Equal("> BoxLine1\r\n> BoxLine2\r\n> 111", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> \r\n> 111", GetTextFromCurrentSnapshot());

            // paste text copied from box selection
            // > 1|11
            // > 222
            Window.Operations.ClearView();
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");
            MoveCaretToPreviousPosition(8);

            CopyBoxToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 1BoxLine111\r\n> 2BoxLine222", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void PasteNoSelectionInActivePromptThenUndo()
        {
            // >| 111
            Window.InsertCode("111");
            MoveCaretToPreviousPosition(4);

            // paste text copied from stream selection
            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> TextCopiedFromStreamSelection111", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());

            // paste text copied from copyline
            // |> 111
            Window.Operations.ClearView();
            Window.InsertCode("111");
            MoveCaretToPreviousPosition(5);

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> TextCopiedFromLineSelection\r\n> 111", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());

            // paste text copied from box selection
            // >| 111
            // > 222
            Window.Operations.ClearView();
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");
            MoveCaretToPreviousPosition(10);

            CopyBoxToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> BoxLine1111\r\n> BoxLine2222", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task PasteNoSelectioninReadOnly()
        {
            await Submit(
@"111",
@"111
").ConfigureAwait(true);

            // > 111
            // 11|1
            // > 
            MoveCaretToPreviousPosition(4);

            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> ", GetTextFromCurrentSnapshot());

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> ", GetTextFromCurrentSnapshot());

            CopyBoxToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> ", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void PasteStreamSelectionWithinInputThenUndo()
        {
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            Window.InsertCode("111");

            // make a stream selection as follows:
            // > 1|11|
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(2);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Stream;
            selection.Select(anchor, active);

            // paste text copied from stream selection
            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 1TextCopiedFromStreamSelection", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());

            // paste text copied from copyline
            Window.Operations.ClearView();
            Window.InsertCode("111");

            // make a stream selection as follows:
            // > 1|11|
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(2);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Stream;
            selection.Select(anchor, active);

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 1TextCopiedFromLineSelection\r\n> ", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());

            // paste text copied from box selection
            Window.Operations.ClearView();
            Window.InsertCode("111");

            // make a stream selection as follows:
            // > 1|11|
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(2);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Stream;
            selection.Select(anchor, active);

            CopyBoxToClipboard();
            Window.Operations.Paste();

            // TODO: Fix this, should be 
            // > 1BoxLine1
            // >  BoxLine2
            Assert.Equal("> 1BoxLine1\r\n>    BoxLine2", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void PasteStreamSelectionInputAndActivePromptThenUndo()
        {
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;
            
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a stream selection as follows:
            // > |111
            // > 222|
            Window.Operations.SelectAll();

            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> TextCopiedFromStreamSelection", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());

            // paste text copied from copyline
            Window.Operations.ClearView();
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            Window.Operations.SelectAll();

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> TextCopiedFromLineSelection\r\n> ", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());

            // paste text copied from box selection
            Window.Operations.ClearView();
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            Window.Operations.SelectAll();

            CopyBoxToClipboard();
            Window.Operations.Paste();

            // > BoxLine1
            // > BoxLine2
            Assert.Equal("> BoxLine1\r\n> BoxLine2\r\n> ", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task PasteStreamSelectionInputAndOutput()
        {
            await Submit(
@"111",
@"111
").ConfigureAwait(true);
            Window.InsertCode("222");

            Window.Operations.SelectAll();
            Window.Operations.SelectAll();

            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> 222", GetTextFromCurrentSnapshot());

            Window.TextView.Selection.Clear();
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> 222", GetTextFromCurrentSnapshot());

            Window.TextView.Selection.Clear();
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();

            CopyBoxToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void PasteBoxSelectionWithinInputThenUndo()
        {
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // stream copy
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make a box selection as follows:
            // > 1|1|1
            // > 2|2|2
            MoveCaretToPreviousPosition(1);
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 1TextCopiedFromStreamSelection1\r\n> 2TextCopiedFromStreamSelection2", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());

            // line copy
            Window.Operations.ClearView();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            MoveCaretToPreviousPosition(1);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 1TextCopiedFromLineSelection\r\n> 1\r\n> 2TextCopiedFromLineSelection\r\n> 2", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());

            // box copy
            Window.Operations.ClearView();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            MoveCaretToPreviousPosition(1);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyBoxToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 1BoxLine11\r\n> 2BoxLine22", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public void PasteBoxSelectionInputAndActivePromptThenUndo()
        {
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;
            
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make an empty box selection as follows:
            // >|| 111
            // >|| 222
            MoveCaretToPreviousPosition(4);
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> TextCopiedFromStreamSelection111\r\n> TextCopiedFromStreamSelection222", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());

            Window.Operations.ClearView();
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make an empty box selection as follows:
            // >|| 111
            // >|| 222
            MoveCaretToPreviousPosition(4);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> TextCopiedFromLineSelection\r\n> 111\r\n> TextCopiedFromLineSelection\r\n> 222", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());

            Window.Operations.ClearView();
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make an empty box selection as follows:
            // >|| 111
            // >|| 222
            MoveCaretToPreviousPosition(4);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyBoxToClipboard();
            Window.Operations.Paste();

            // TODO: fix , should be "> BoxLine1111\r\n> BoxLine2222"
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());

            Window.Operations.ClearView();
            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            // make an empty box selection as follows:
            // |> 1|11
            // |> 2|22
            MoveCaretToPreviousPosition(2);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(9);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyBoxToClipboard();
            Window.Operations.Paste();
            Assert.Equal("> BoxLine111\r\n> BoxLine222", GetTextFromCurrentSnapshot());

            // undo
            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> 111\r\n> 222", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task PasteBoxSelectionInputAndOutput()
        {
            await Submit(
@"111",
@"111
").ConfigureAwait(true);
            Window.InsertCode("222");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // make a stream selection as follows:
            // > 111
            // |111|
            // |> 2|22
            MoveCaretToPreviousPosition(2);
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyStreamToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> 222", GetTextFromCurrentSnapshot());

            Window.TextView.Selection.Clear();
            MoveCaretToPreviousPosition(2);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyLineToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> 222", GetTextFromCurrentSnapshot());

            Window.TextView.Selection.Clear();
            MoveCaretToPreviousPosition(2);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);

            CopyBoxToClipboard();
            Window.Operations.Paste();

            Assert.Equal("> 111\r\n111\r\n> 222", GetTextFromCurrentSnapshot());
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

        [WpfFact]
        public async Task CopyInputsFromCurrentLine()
        {
            _testClipboard.Clear();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");

            Window.Operations.CopyInputs();
            VerifyClipboardData("222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 222}",
                "[{\"content\":\"222\",\"kind\":2}]",
                expectedToBeLineCopy: true);


            // Move caret to:
            // > 1|11
            // > 222
            MoveCaretToPreviousPosition(7);
            Window.Operations.CopyInputs();
            VerifyClipboardData("111\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111\\par }",
                "[{\"content\":\"111\\u000d\\u000a\",\"kind\":2}]",
                expectedToBeLineCopy: true);

            _testClipboard.Clear();
            Window.Operations.ClearView();
            
            await Submit(
@"111",
@"111
").ConfigureAwait(true);

            Window.InsertCode("222");

            // Move caret to:
            // > 111
            // 1|11
            // > 222
            MoveCaretToPreviousPosition(7);
            Window.Operations.CopyInputs();
            VerifyClipboardData(null, null, null);
        }

        [WpfFact]
        public async Task CopyInputsFromSelection()
        {
            _testClipboard.Clear();

            await Submit(
@"111",
@"111
").ConfigureAwait(true);
            Window.InsertCode("222");

            // Make following stream selection:
            // |> 111
            // 111
            // > 222|
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.CopyInputs();
            VerifyClipboardData("111\r\n222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111\\par 222}",
                "[{\"content\":\"111\\u000d\\u000a\",\"kind\":2},{\"content\":\"222\",\"kind\":2}]");


            _testClipboard.Clear();
            Window.Operations.ClearView();

            Window.InsertCode("111");
            Window.Operations.BreakLine();
            Window.InsertCode("222");
            
            // Make a selection as follows:
            // |> 111
            // > 222|
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.CopyInputs();

            VerifyClipboardData("111\r\n222",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 111\\par 222}",
                "[{\"content\":\"111\\u000d\\u000a\",\"kind\":2},{\"content\":\"222\",\"kind\":2}]");

            _testClipboard.Clear();
            Window.TextView.Selection.Clear();

            var caret = Window.TextView.Caret;
            // Make a box selection as follows:
            // |> 1|11
            // |111|
            // |> 2|22
            MoveCaretToPreviousPosition(2);
            var selection = Window.TextView.Selection;
            var anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(13);
            var active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Box;
            selection.Select(anchor, active);
            Window.Operations.CopyInputs();

            VerifyClipboardData("1\r\n2\r\n",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 1\\par 2}",
                "[{\"content\":\"1\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4},{\"content\":\"2\",\"kind\":2},{\"content\":\"\\u000d\\u000a\",\"kind\":4}]",
                 expectedToBeBoxCopy: true);

            _testClipboard.Clear();
            Window.Operations.ClearView();

            await Submit(
@"111",
@"111
").ConfigureAwait(true);
            Window.InsertCode("222");

            // Make a stream selection as follows:
            // > 111
            // 1|11
            // > 22|2
            MoveCaretToPreviousPosition(1);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(7);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Stream;
            selection.Select(anchor, active);
            Window.Operations.CopyInputs();

            VerifyClipboardData("22",
                "{\\rtf\\ansi{\\fonttbl{\\f0 Consolas;}}{\\colortbl;\\red0\\green0\\blue0;\\red255\\green255\\blue255;}\\f0 \\fs24 \\cf1 \\cb2 \\highlight2 22}",
                "[{\"content\":\"22\",\"kind\":2}]");

            _testClipboard.Clear();
            Window.Operations.ClearView();

            await Submit(
@"111",
@"111
").ConfigureAwait(true);
            Window.InsertCode("222");

            // Make a stream selection as follows:
            // > 111
            // 1|11|
            // > 222
            MoveCaretToPreviousPosition(6);
            anchor = caret.Position.VirtualBufferPosition;
            MoveCaretToPreviousPosition(2);
            active = caret.Position.VirtualBufferPosition;
            selection.Mode = Text.Editor.TextSelectionMode.Stream;
            selection.Select(anchor, active);
            Window.Operations.CopyInputs();

            VerifyClipboardData(null, null, null);
        }

        /// <summary>
        /// Put text equivalent to copying from following selection to clipboard:
        /// |> TextCopiedFromStreamSelection| 
        /// </summary>
        private void CopyStreamToClipboard()
        {
            var blocks = new[]
            {
                new BufferBlock(ReplSpanKind.Prompt, "> "),
                new BufferBlock(ReplSpanKind.Input, "TextCopiedFromStreamSelection"),
            };
            
            CopyToClipboard(blocks, includeRepl: true);
        }

        /// <summary>
        /// Put text equivalent to line-copying from following selection to clipboard:
        /// > TextCopiedFromLineSel|ection
        /// > 
        /// </summary>
        private void CopyLineToClipboard()
        {
            var blocks = new[]
            {
                new BufferBlock(ReplSpanKind.Prompt, "> "),
                new BufferBlock(ReplSpanKind.Input, "TextCopiedFromLineSelection\r\n"),
            };

            CopyToClipboard(blocks, includeRepl: true, isLineCopy: true);
        }

        /// <summary>
        /// Put text equivalent to copying from following box selection to clipboard:
        /// > 1|1|1
        /// > 2|2|2 
        /// </summary>
        private void CopyBoxToClipboard()
        {
            var blocks = new[]
            {
                new BufferBlock(ReplSpanKind.Input, "BoxLine1"),
                new BufferBlock(ReplSpanKind.LineBreak, "\r\n"),
                new BufferBlock(ReplSpanKind.Input, "BoxLine2"),
                new BufferBlock(ReplSpanKind.LineBreak, "\r\n"),
            };

            CopyToClipboard(blocks, includeRepl: true, isBoxCopy: true);
        }

        private void CopyToClipboard(string text)
        {
            _testClipboard.Clear();
            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, text);
            data.SetData(DataFormats.StringFormat, text);
            _testClipboard.SetDataObject(data, false);
        }

        private void CopyToClipboard(BufferBlock[] blocks, bool includeRepl, bool isLineCopy = false, bool isBoxCopy = false)
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
            if (isLineCopy)
            {
                data.SetData(InteractiveWindow.ClipboardLineBasedCutCopyTag, true);
            }
            if (isBoxCopy)
            {
                data.SetData(InteractiveWindow.BoxSelectionCutCopyTag, true);
            }
            _testClipboard.SetDataObject(data, false);
        }

        private void VerifyClipboardData(string expectedText, string expectedRtf, string expectedRepl, bool expectedToBeLineCopy = false, bool expectedToBeBoxCopy = false)
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
                Assert.Equal(expectedRtf, actualRtf);
            }

            Assert.Equal(expectedToBeLineCopy, data?.GetDataPresent(InteractiveWindow.ClipboardLineBasedCutCopyTag) ?? false);
            Assert.Equal(expectedToBeBoxCopy, data?.GetDataPresent(InteractiveWindow.BoxSelectionCutCopyTag) ?? false);
            Assert.False(expectedToBeLineCopy && expectedToBeBoxCopy);
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
                VerifyClipboardData(expectedText, expectedRtf, expectedRepl, expectedToBeLineCopy: true);
            }
        }

        private void MoveCaretToPreviousPosition(int moves = 1)
        {
            var caret = Window.TextView.Caret;
            for (int i = 0; i< moves; ++i)
            {
                caret.MoveToPreviousCaretPosition();
            }
        }

        private void MoveCaretToNextPosition(int moves = 1)
        {
            var caret = Window.TextView.Caret;
            for (int i = 0; i < moves; ++i)
            {
                caret.MoveToNextCaretPosition();
            }
        }

        private bool IsEmptyBoxSelection()
        {
            return !Window.TextView.Selection.IsEmpty &&
                    Window.TextView.Selection.VirtualSelectedSpans.All(s => s.IsEmpty);
        }
    }
}
