// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Text;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class InteractiveWindowTests : IDisposable
    {
        #region Helpers

        private InteractiveWindowTestHost _testHost;
        private List<InteractiveWindow.State> _states;

        public InteractiveWindowTests()
        {
            _states = new List<InteractiveWindow.State>();
            _testHost = new InteractiveWindowTestHost(_states.Add);
        }

        void IDisposable.Dispose()
        {
            _testHost.Dispose();
        }

        private IInteractiveWindow Window => _testHost.Window;

        private static IEnumerable<IInteractiveWindowCommand> MockCommands(params string[] commandNames)
        {
            foreach (var name in commandNames)
            {
                var mock = new Mock<IInteractiveWindowCommand>();
                mock.Setup(m => m.Names).Returns(new[] { name });
                yield return mock.Object;
            }
        }

        private static ITextSnapshot MockSnapshot(string content)
        {
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(m => m[It.IsAny<int>()]).Returns<int>(index => content[index]);
            snapshotMock.Setup(m => m.Length).Returns(content.Length);
            snapshotMock.Setup(m => m.GetText()).Returns(content);
            snapshotMock.Setup(m => m.GetText(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((start, length) => content.Substring(start, length));
            snapshotMock.Setup(m => m.GetText(It.IsAny<Span>())).Returns<Span>(span => content.Substring(span.Start, span.Length));
            return snapshotMock.Object;
        }

        #endregion

        [Fact]
        public void InteractiveWindow__CommandParsing()
        {
            var commandList = MockCommands("foo", "bar", "bz", "command1").ToArray();
            var commands = new Commands.Commands(null, "%", commandList);
            AssertEx.Equal(commands.GetCommands(), commandList);

            var cmdBar = commandList[1];
            Assert.Equal("bar", cmdBar.Names.First());

            Assert.Equal("%", commands.CommandPrefix);
            commands.CommandPrefix = "#";
            Assert.Equal("#", commands.CommandPrefix);

            ////                             111111
            ////                   0123456789012345
            var s1 = MockSnapshot("#bar arg1 arg2 ");

            SnapshotSpan prefixSpan, commandSpan, argsSpan;
            IInteractiveWindowCommand cmd;

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 0)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 1)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 2)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(2, commandSpan.End);
            Assert.Equal(2, argsSpan.Start);
            Assert.Equal(2, argsSpan.End);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 3)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(3, commandSpan.End);
            Assert.Equal(3, argsSpan.Start);
            Assert.Equal(3, argsSpan.End);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 4)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(4, commandSpan.End);
            Assert.Equal(4, argsSpan.Start);
            Assert.Equal(4, argsSpan.End);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 5)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(4, commandSpan.End);
            Assert.Equal(5, argsSpan.Start);
            Assert.Equal(5, argsSpan.End);

            cmd = commands.TryParseCommand(s1.GetExtent(), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(4, commandSpan.End);
            Assert.Equal(5, argsSpan.Start);
            Assert.Equal(14, argsSpan.End);

            ////                             
            ////                   0123456789
            var s2 = MockSnapshot("  #bar   ");
            cmd = commands.TryParseCommand(s2.GetExtent(), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(2, prefixSpan.Start);
            Assert.Equal(3, prefixSpan.End);
            Assert.Equal(3, commandSpan.Start);
            Assert.Equal(6, commandSpan.End);
            Assert.Equal(9, argsSpan.Start);
            Assert.Equal(9, argsSpan.End);

            ////                             111111
            ////                   0123456789012345
            var s3 = MockSnapshot("  #   bar  args");
            cmd = commands.TryParseCommand(s3.GetExtent(), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(2, prefixSpan.Start);
            Assert.Equal(3, prefixSpan.End);
            Assert.Equal(6, commandSpan.Start);
            Assert.Equal(9, commandSpan.End);
            Assert.Equal(11, argsSpan.Start);
            Assert.Equal(15, argsSpan.End);
        }

        [Fact]
        public void InteractiveWindow_GetCommands()
        {
            var interactiveCommands = new InteractiveCommandsFactory(null, null).CreateInteractiveCommands(
                Window,
                "#",
                _testHost.ExportProvider.GetExports<IInteractiveWindowCommand>().Select(x => x.Value).ToArray());

            var commands = interactiveCommands.GetCommands();

            Assert.NotEmpty(commands);
            Assert.Equal(2, commands.Where(n => n.Names.First() == "cls").Count());
            Assert.Equal(2, commands.Where(n => n.Names.Last() == "clear").Count());
            Assert.NotNull(commands.Where(n => n.Names.First() == "help").SingleOrDefault());
            Assert.NotNull(commands.Where(n => n.Names.First() == "reset").SingleOrDefault());
        }

        [WorkItem(3970, "https://github.com/dotnet/roslyn/issues/3970")]
        [Fact]
        public void ResetStateTransitions()
        {
            Window.Operations.ResetAsync().PumpingWait();
            Assert.Equal(_states, new[]
            {
                InteractiveWindow.State.Initializing,
                InteractiveWindow.State.WaitingForInput,
                InteractiveWindow.State.Resetting,
                InteractiveWindow.State.WaitingForInput,
            });
        }

        [Fact]
        public void DoubleInitialize()
        {
            try
            {
                Window.InitializeAsync().PumpingWait();
                Assert.True(false);
            }
            catch (AggregateException e)
            {
                Assert.IsType<InvalidOperationException>(e.InnerExceptions.Single());
            }
        }

        [Fact]
        public void AccessPropertiesOnUIThread()
        {
            foreach (var property in typeof(IInteractiveWindow).GetProperties())
            {
                Assert.Null(property.SetMethod);
                property.GetMethod.Invoke(Window, Array.Empty<object>());
            }

            Assert.Empty(typeof(IInteractiveWindowOperations).GetProperties());
        }

        [Fact]
        public void AccessPropertiesOnNonUIThread()
        {
            foreach (var property in typeof(IInteractiveWindow).GetProperties())
            {
                Assert.Null(property.SetMethod);
                Task.Run(() => property.GetMethod.Invoke(Window, Array.Empty<object>())).PumpingWait();
            }

            Assert.Empty(typeof(IInteractiveWindowOperations).GetProperties());
        }

        /// <remarks>
        /// Confirm that we are, in fact, running on a non-UI thread.
        /// </remarks>
        [Fact]
        public void NonUIThread()
        {
            Task.Run(() => Assert.False(((InteractiveWindow)Window).OnUIThread())).PumpingWait();
        }

        [Fact]
        public void CallCloseOnNonUIThread()
        {
            Task.Run(() => Window.Close()).PumpingWait();
        }

        [Fact]
        public void CallInsertCodeOnNonUIThread()
        {
            Task.Run(() => Window.InsertCode("1")).PumpingWait();
        }

        [Fact]
        public void CallSubmitAsyncOnNonUIThread()
        {
            Task.Run(() => Window.SubmitAsync(Array.Empty<string>()).GetAwaiter().GetResult()).PumpingWait();
        }

        [Fact]
        public void CallWriteOnNonUIThread()
        {
            Task.Run(() => Window.WriteLine("1")).PumpingWait();
            Task.Run(() => Window.Write("1")).PumpingWait();
            Task.Run(() => Window.WriteErrorLine("1")).PumpingWait();
            Task.Run(() => Window.WriteError("1")).PumpingWait();
        }

        [Fact]
        public void CallFlushOutputOnNonUIThread()
        {
            Window.Write("1"); // Something to flush.
            Task.Run(() => Window.FlushOutput()).PumpingWait();
        }

        [Fact]
        public void CallAddInputOnNonUIThread()
        {
            Task.Run(() => Window.AddInput("1")).PumpingWait();
        }

        /// <remarks>
        /// Call is blocking, so we can't write a simple non-failing test.
        /// </remarks>
        [Fact]
        public void CallReadStandardInputOnUIThread()
        {
            Assert.Throws<InvalidOperationException>(() => Window.ReadStandardInput());
        }

        [Fact]
        public void CallBackspaceOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to backspace.
            Task.Run(() => Window.Operations.Backspace()).PumpingWait();
        }

        [Fact]
        public void CallBreakLineOnNonUIThread()
        {
            Task.Run(() => Window.Operations.BreakLine()).PumpingWait();
        }

        [Fact]
        public void CallClearHistoryOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            Task.Run(() => Window.Operations.ClearHistory()).PumpingWait();
        }

        [Fact]
        public void CallClearViewOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to clear.
            Task.Run(() => Window.Operations.ClearView()).PumpingWait();
        }

        [Fact]
        public void CallHistoryNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            Task.Run(() => Window.Operations.HistoryNext()).PumpingWait();
        }

        [Fact]
        public void CallHistoryPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            Task.Run(() => Window.Operations.HistoryPrevious()).PumpingWait();
        }

        [Fact]
        public void CallHistorySearchNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            Task.Run(() => Window.Operations.HistorySearchNext()).PumpingWait();
        }

        [Fact]
        public void CallHistorySearchPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            Task.Run(() => Window.Operations.HistorySearchPrevious()).PumpingWait();
        }

        [Fact]
        public void CallHomeOnNonUIThread()
        {
            Window.Operations.BreakLine(); // Distinguish Home from End.
            Task.Run(() => Window.Operations.Home(true)).PumpingWait();
        }

        [Fact]
        public void CallEndOnNonUIThread()
        {
            Window.Operations.BreakLine(); // Distinguish Home from End.
            Task.Run(() => Window.Operations.End(true)).PumpingWait();
        }

        [Fact]
        public void ScrollToCursorOnHomeAndEndOnNonUIThread()
        {
            Window.InsertCode(new string('1', 512));    // a long input string 

            var textView = Window.TextView;

            Window.Operations.Home(false);
            Assert.True(textView.TextViewModel.IsPointInVisualBuffer(textView.Caret.Position.BufferPosition,
                                                                     textView.Caret.Position.Affinity));
            Window.Operations.End(false);
            Assert.True(textView.TextViewModel.IsPointInVisualBuffer(textView.Caret.Position.BufferPosition,
                                                                     textView.Caret.Position.Affinity));
        }

        [Fact]
        public void CallSelectAllOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to select.
            Task.Run(() => Window.Operations.SelectAll()).PumpingWait();
        }

        [Fact]
        public void CallPasteOnNonUIThread()
        {
            Task.Run(() => Window.Operations.Paste()).PumpingWait();
        }

        [Fact]
        public void CallCutOnNonUIThread()
        {
            Task.Run(() => Window.Operations.Cut()).PumpingWait();
        }

        [Fact]
        public void CallDeleteOnNonUIThread()
        {
            Task.Run(() => Window.Operations.Delete()).PumpingWait();
        }

        [Fact]
        public void CallReturnOnNonUIThread()
        {
            Task.Run(() => Window.Operations.Return()).PumpingWait();
        }

        [Fact]
        public void CallTrySubmitStandardInputOnNonUIThread()
        {
            Task.Run(() => Window.Operations.TrySubmitStandardInput()).PumpingWait();
        }

        [Fact]
        public void CallResetAsyncOnNonUIThread()
        {
            Task.Run(() => Window.Operations.ResetAsync()).PumpingWait();
        }
        
        [Fact]
        public void CallExecuteInputOnNonUIThread()
        {
            Task.Run(() => Window.Operations.ExecuteInput()).PumpingWait();
        }

        [Fact]
        public void CallCancelOnNonUIThread()
        {
            Task.Run(() => Window.Operations.Cancel()).PumpingWait();
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [Fact]
        public void TestIndentation1()
        {
            TestIndentation(indentSize: 1);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [Fact]
        public void TestIndentation2()
        {
            TestIndentation(indentSize: 2);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [Fact]
        public void TestIndentation3()
        {
            TestIndentation(indentSize: 3);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [Fact]
        public void TestIndentation4()
        {
            TestIndentation(indentSize: 4);
        }

        private void TestIndentation(int indentSize)
        {
            const int promptWidth = 2;

            _testHost.ExportProvider.GetExport<TestSmartIndentProvider>().Value.SmartIndent = new TestSmartIndent(
                promptWidth,
                promptWidth + indentSize,
                promptWidth
            );

            AssertCaretVirtualPosition(0, promptWidth);
            Window.InsertCode("{");
            AssertCaretVirtualPosition(0, promptWidth + 1);
            Window.Operations.BreakLine();
            AssertCaretVirtualPosition(1, promptWidth + indentSize);
            Window.InsertCode("Console.WriteLine();");
            Window.Operations.BreakLine();
            AssertCaretVirtualPosition(2, promptWidth);
            Window.InsertCode("}");
            AssertCaretVirtualPosition(2, promptWidth + 1);
        }

        private void AssertCaretVirtualPosition(int expectedLine, int expectedColumn)
        {
            ITextSnapshotLine actualLine;
            int actualColumn;
            Window.TextView.Caret.Position.VirtualBufferPosition.GetLineAndColumn(out actualLine, out actualColumn);
            Assert.Equal(expectedLine, actualLine.LineNumber);
            Assert.Equal(expectedColumn, actualColumn);
        }

        [Fact]
        public void ResetCommandArgumentParsing_Success()
        {
            bool initialize;
            Assert.True(ResetCommand.TryParseArguments("", out initialize));
            Assert.True(initialize);

            Assert.True(ResetCommand.TryParseArguments(" ", out initialize));
            Assert.True(initialize);

            Assert.True(ResetCommand.TryParseArguments("\r\n", out initialize));
            Assert.True(initialize);

            Assert.True(ResetCommand.TryParseArguments("noconfig", out initialize));
            Assert.False(initialize);

            Assert.True(ResetCommand.TryParseArguments(" noconfig ", out initialize));
            Assert.False(initialize);

            Assert.True(ResetCommand.TryParseArguments("\r\nnoconfig\r\n", out initialize));
            Assert.False(initialize);
        }

        [Fact]
        public void ResetCommandArgumentParsing_Failure()
        {
            bool initialize;
            Assert.False(ResetCommand.TryParseArguments("a", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfi", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfig1", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfig 1", out initialize));
            Assert.False(ResetCommand.TryParseArguments("1 noconfig", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfig\r\na", out initialize));
            Assert.False(ResetCommand.TryParseArguments("nOcOnfIg", out initialize));
        }

        [Fact]
        public void ResetCommandNoConfigClassification()
        {
            Assert.Empty(ResetCommand.GetNoConfigPositions(""));
            Assert.Empty(ResetCommand.GetNoConfigPositions("a"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("noconfi"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("noconfig1"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("1noconfig"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("1noconfig1"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("nOcOnfIg"));

            Assert.Equal(new[] { 0 }, ResetCommand.GetNoConfigPositions("noconfig"));
            Assert.Equal(new[] { 0 }, ResetCommand.GetNoConfigPositions("noconfig "));
            Assert.Equal(new[] { 1 }, ResetCommand.GetNoConfigPositions(" noconfig"));
            Assert.Equal(new[] { 1 }, ResetCommand.GetNoConfigPositions(" noconfig "));
            Assert.Equal(new[] { 2 }, ResetCommand.GetNoConfigPositions("\r\nnoconfig"));
            Assert.Equal(new[] { 0 }, ResetCommand.GetNoConfigPositions("noconfig\r\n"));
            Assert.Equal(new[] { 2 }, ResetCommand.GetNoConfigPositions("\r\nnoconfig\r\n"));
            Assert.Equal(new[] { 6 }, ResetCommand.GetNoConfigPositions("error noconfig"));

            Assert.Equal(new[] { 0, 9 }, ResetCommand.GetNoConfigPositions("noconfig noconfig"));
            Assert.Equal(new[] { 0, 15 }, ResetCommand.GetNoConfigPositions("noconfig error noconfig"));
        }

        [WorkItem(4755, "https://github.com/dotnet/roslyn/issues/4755")]
        [Fact]
        public void ReformatBraces()
        {
            var buffer = Window.CurrentLanguageBuffer;
            var snapshot = buffer.CurrentSnapshot;
            Assert.Equal(0, snapshot.Length);

            // Text before reformatting.
            snapshot = ApplyChanges(
                buffer,
                new TextChange(0, 0, "{ {\r\n } }"));

            // Text after reformatting.
            Assert.Equal(9, snapshot.Length);
            snapshot = ApplyChanges(
                buffer,
                new TextChange(1, 1, "\r\n    "),
                new TextChange(5, 1, "    "),
                new TextChange(7, 1, "\r\n"));

            // Text from language buffer.
            var actualText = snapshot.GetText();
            Assert.Equal("{\r\n    {\r\n    }\r\n}", actualText);

            // Text including prompts.
            buffer = Window.TextView.TextBuffer;
            snapshot = buffer.CurrentSnapshot;
            actualText = snapshot.GetText();
            Assert.Equal("> {\r\n>     {\r\n>     }\r\n> }", actualText);

            // Prompts should be read-only.
            var regions = buffer.GetReadOnlyExtents(new Span(0, snapshot.Length));
            AssertEx.SetEqual(regions,
                new Span(0, 2),
                new Span(5, 2),
                new Span(14, 2),
                new Span(23, 2));
        }

        [Fact]
        public void CopyWithinInput()
        {
            Clipboard.Clear();

            Window.InsertCode("1 + 2");
            Window.Operations.SelectAll();
            Window.Operations.Copy();
            VerifyClipboardData("1 + 2");

            // Shrink the selection.
            var selection = Window.TextView.Selection;
            var span = selection.SelectedSpans[0];
            selection.Select(new SnapshotSpan(span.Snapshot, span.Start + 1, span.Length - 2), isReversed: false);

            Window.Operations.Copy();
            VerifyClipboardData(" + ");
        }

        [Fact]
        public void CopyInputAndOutput()
        {
            Clipboard.Clear();

            Submit(
@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();",
@"1
2
3
");
            var caret = Window.TextView.Caret;
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.Copy();
            VerifyClipboardData(@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();
1
2
3
",
@"> foreach (var o in new[] \{ 1, 2, 3 \})\par > System.Console.WriteLine();\par 1\par 2\par 3\par > ");

            // Shrink the selection.
            var selection = Window.TextView.Selection;
            var span = selection.SelectedSpans[0];
            selection.Select(new SnapshotSpan(span.Snapshot, span.Start + 3, span.Length - 6), isReversed: false);

            Window.Operations.Copy();
            VerifyClipboardData(@"oreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();
1
2
3",
@"oreach (var o in new[] \{ 1, 2, 3 \})\par > System.Console.WriteLine();\par 1\par 2\par 3");
        }

        [Fact]
        public void CutWithinInput()
        {
            Clipboard.Clear();

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
                expectedRtf: null);
        }

        [Fact]
        public void CutInputAndOutput()
        {
            Clipboard.Clear();

            Submit(
@"foreach (var o in new[] { 1, 2, 3 })
System.Console.WriteLine();",
@"1
2
3
");
            var caret = Window.TextView.Caret;
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.Cut();
            VerifyClipboardData(null);
        }

        /// <summary>
        /// When there is no selection, copy
        /// should copy the current line.
        /// </summary>
        [Fact]
        public void CopyNoSelection()
        {
            Submit(
@"s +

 t",
@" 1

2 ");
            CopyNoSelectionAndVerify(0, 7, "s +\r\n", @"> s +\par ");
            CopyNoSelectionAndVerify(7, 11, "\r\n", @"> \par ");
            CopyNoSelectionAndVerify(11, 17, " t\r\n", @">  t\par ");
            CopyNoSelectionAndVerify(17, 21, " 1\r\n", @" 1\par ");
            CopyNoSelectionAndVerify(21, 23, "\r\n", @"\par ");
            CopyNoSelectionAndVerify(23, 28, "2 ", "2 > ");
        }

        private void CopyNoSelectionAndVerify(int start, int end, string expectedText, string expectedRtf)
        {
            var caret = Window.TextView.Caret;
            var snapshot = Window.TextView.TextBuffer.CurrentSnapshot;
            for (int i = start; i < end; i++)
            {
                Clipboard.Clear();
                caret.MoveTo(new SnapshotPoint(snapshot, i));
                Window.Operations.Copy();
                VerifyClipboardData(expectedText, expectedRtf);
            }
        }

        [Fact]
        public void CancelMultiLineInput()
        {
            ApplyChanges(
                Window.CurrentLanguageBuffer,
                new TextChange(0, 0, "{\r\n    {\r\n    }\r\n}"));

            // Text including prompts.
            var buffer = Window.TextView.TextBuffer;
            var snapshot = buffer.CurrentSnapshot;
            Assert.Equal("> {\r\n>     {\r\n>     }\r\n> }", snapshot.GetText());

            Task.Run(() => Window.Operations.Cancel()).PumpingWait();

            // Text after cancel.
            snapshot = buffer.CurrentSnapshot;
            Assert.Equal("> ", snapshot.GetText());
        }

        [Fact]
        public void SelectAllInHeader()
        {
            Window.WriteLine("Header");
            Window.FlushOutput();
            var fullText = Window.TextView.TextBuffer.CurrentSnapshot.GetText();
            Assert.Equal("Header\r\n> ", fullText);

            Window.TextView.Caret.MoveTo(new SnapshotPoint(Window.TextView.TextBuffer.CurrentSnapshot, 1));
            Window.Operations.SelectAll(); // Used to throw.

            // Everything is selected.
            Assert.Equal(new Span(0, fullText.Length), Window.TextView.Selection.SelectedSpans.Single().Span);
        }

        private void Submit(string submission, string output)
        {
            Task.Run(() => Window.SubmitAsync(new[] { submission })).PumpingWait();
            // TestInteractiveEngine.ExecuteCodeAsync() simply returns
            // success rather than executing the submission, so add the
            // expected output to the output buffer.
            var buffer = Window.OutputBuffer;
            using (var edit = buffer.CreateEdit())
            {
                edit.Replace(buffer.CurrentSnapshot.Length, 0, output);
                edit.Apply();
            }
        }

        private static void VerifyClipboardData(string expectedText)
        {
            VerifyClipboardData(expectedText, expectedText);
        }

        private static void VerifyClipboardData(string expectedText, string expectedRtf)
        {
            var data = Clipboard.GetDataObject();
            Assert.Equal(expectedText, data.GetData(DataFormats.StringFormat));
            Assert.Equal(expectedText, data.GetData(DataFormats.Text));
            Assert.Equal(expectedText, data.GetData(DataFormats.UnicodeText));
            var actualRtf = (string)data.GetData(DataFormats.Rtf);
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

        private struct TextChange
        {
            internal readonly int Start;
            internal readonly int Length;
            internal readonly string Text;

            internal TextChange(int start, int length, string text)
            {
                Start = start;
                Length = length;
                Text = text;
            }
        }

        private static ITextSnapshot ApplyChanges(ITextBuffer buffer, params TextChange[] changes)
        {
            using (var edit = buffer.CreateEdit())
            {
                foreach (var change in changes)
                {
                    edit.Replace(change.Start, change.Length, change.Text);
                }
                return edit.Apply();
            }
        }
    }

    internal static class OperationsExtensions
    {
        internal static void Copy(this IInteractiveWindowOperations operations)
        {
            ((IInteractiveWindowOperations2)operations).Copy();
        }
    }
}
