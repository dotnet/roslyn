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
    public class InteractiveWindowTests : IDisposable
    {
        #region Helpers

        private InteractiveWindowTestHost _testHost;
        private List<InteractiveWindow.State> _states;
        private readonly TestClipboard _testClipboard; 
        private readonly TaskFactory _factory = new TaskFactory(TaskScheduler.Default);

        public InteractiveWindowTests()
        {
            _states = new List<InteractiveWindow.State>();
            _testHost = new InteractiveWindowTestHost(_states.Add);
            _testClipboard = new TestClipboard();
            ((InteractiveWindow)Window).InteractiveWindowClipboard = _testClipboard;            
        }

        void IDisposable.Dispose()
        {
            _testHost.Dispose();
        }

        private IInteractiveWindow Window => _testHost.Window;                                                                                                                                       

        private Task TaskRun(Action action)
        {
            return _factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

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

        [WpfFact]
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

        [WpfFact]
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
        [WpfFact]
        public async Task ResetStateTransitions()
        {
            await Window.Operations.ResetAsync().ConfigureAwait(true);
            Assert.Equal(_states, new[]
            {
                InteractiveWindow.State.Initializing,
                InteractiveWindow.State.WaitingForInput,
                InteractiveWindow.State.Resetting,
                InteractiveWindow.State.WaitingForInput,
            });
        }

        [WpfFact]
        public async Task DoubleInitialize()
        {
            try
            {
                await Window.InitializeAsync().ConfigureAwait(true);
                Assert.True(false);
            }
            catch (InvalidOperationException)
            {

            }
        }

        [WpfFact]
        public void AccessPropertiesOnUIThread()
        {
            foreach (var property in typeof(IInteractiveWindow).GetProperties())
            {
                Assert.Null(property.SetMethod);
                property.GetMethod.Invoke(Window, Array.Empty<object>());
            }

            Assert.Empty(typeof(IInteractiveWindowOperations).GetProperties());
        }

        [WpfFact]
        public async Task AccessPropertiesOnNonUIThread()
        {
            foreach (var property in typeof(IInteractiveWindow).GetProperties())
            {
                Assert.Null(property.SetMethod);
                await TaskRun(() => property.GetMethod.Invoke(Window, Array.Empty<object>())).ConfigureAwait(true);
            }

            Assert.Empty(typeof(IInteractiveWindowOperations).GetProperties());
        }

        /// <remarks>
        /// Confirm that we are, in fact, running on a non-UI thread.
        /// </remarks>
        [WpfFact]
        public async Task NonUIThread()
        {
            await TaskRun(() => Assert.False(((InteractiveWindow)Window).OnUIThread())).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallCloseOnNonUIThread()
        {
            await TaskRun(() => Window.Close()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallInsertCodeOnNonUIThread()
        {
            await TaskRun(() => Window.InsertCode("1")).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallSubmitAsyncOnNonUIThread()
        {
            await TaskRun(() => Window.SubmitAsync(Array.Empty<string>()).GetAwaiter().GetResult()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallWriteOnNonUIThread()
        {
            await TaskRun(() => Window.WriteLine("1")).ConfigureAwait(true);
            await TaskRun(() => Window.Write("1")).ConfigureAwait(true);
            await TaskRun(() => Window.WriteErrorLine("1")).ConfigureAwait(true);
            await TaskRun(() => Window.WriteError("1")).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallFlushOutputOnNonUIThread()
        {
            Window.Write("1"); // Something to flush.
            await TaskRun(() => Window.FlushOutput()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallAddInputOnNonUIThread()
        {
            await TaskRun(() => Window.AddInput("1")).ConfigureAwait(true);
        }

        /// <remarks>
        /// Call is blocking, so we can't write a simple non-failing test.
        /// </remarks>
        [WpfFact]
        public void CallReadStandardInputOnUIThread()
        {
            Assert.Throws<InvalidOperationException>(() => Window.ReadStandardInput());
        }

        [WpfFact]
        public async Task CallBackspaceOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to backspace.
            await TaskRun(() => Window.Operations.Backspace()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallBreakLineOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.BreakLine()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallClearHistoryOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.ClearHistory()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallClearViewOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to clear.
            await TaskRun(() => Window.Operations.ClearView()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistoryNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistoryNext()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistoryPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistoryPrevious()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistorySearchNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistorySearchNext()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistorySearchPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistorySearchPrevious()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHomeOnNonUIThread()
        {
            Window.Operations.BreakLine(); // Distinguish Home from End.
            await TaskRun(() => Window.Operations.Home(true)).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallEndOnNonUIThread()
        {
            Window.Operations.BreakLine(); // Distinguish Home from End.
            await TaskRun(() => Window.Operations.End(true)).ConfigureAwait(true);
        }

        [WpfFact]
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

        [WpfFact]
        public async Task CallSelectAllOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to select.
            await TaskRun(() => Window.Operations.SelectAll()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallPasteOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Paste()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallCutOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Cut()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallDeleteOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Delete()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallReturnOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Return()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallTrySubmitStandardInputOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.TrySubmitStandardInput()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallResetAsyncOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.ResetAsync()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallExecuteInputOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.ExecuteInput()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallCancelOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Cancel()).ConfigureAwait(true);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
        public void TestIndentation1()
        {
            TestIndentation(indentSize: 1);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
        public void TestIndentation2()
        {
            TestIndentation(indentSize: 2);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
        public void TestIndentation3()
        {
            TestIndentation(indentSize: 3);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
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
		
        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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
        [WpfFact]
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
        public void JsonSerialization()
        {
            var expectedContent = new []
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
        public async Task CancelMultiLineInput()
        {
            ApplyChanges(
                Window.CurrentLanguageBuffer,
                new TextChange(0, 0, "{\r\n    {\r\n    }\r\n}"));

            // Text including prompts.
            var buffer = Window.TextView.TextBuffer;
            var snapshot = buffer.CurrentSnapshot;
            Assert.Equal("> {\r\n>     {\r\n>     }\r\n> }", snapshot.GetText());

            await TaskRun(() => Window.Operations.Cancel()).ConfigureAwait(true);

            // Text after cancel.
            snapshot = buffer.CurrentSnapshot;
            Assert.Equal("> ", snapshot.GetText());
        }

        [WpfFact]
        public void SelectAllInHeader()
        {
            Window.WriteLine("Header");
            Window.FlushOutput();
            var fullText = GetTextFromCurrentSnapshot();
            Assert.Equal("Header\r\n> ", fullText);

            Window.TextView.Caret.MoveTo(new SnapshotPoint(Window.TextView.TextBuffer.CurrentSnapshot, 1));
            Window.Operations.SelectAll(); // Used to throw.

            // Everything is selected.
            Assert.Equal(new Span(0, fullText.Length), Window.TextView.Selection.SelectedSpans.Single().Span);
        }

        [WpfFact]
        public async Task DeleteWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("2");                                              

            var caret = Window.TextView.Caret;

            // with empty selection, Delete() only handles caret movement,
            // so we can only test caret location. 

            // Delete() with caret in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            await TaskRun(() => Window.Operations.Delete()).ConfigureAwait(true);
            AssertCaretVirtualPosition(1, 1);

            // Delete() with caret in active prompt, move caret to 
            // closest editable buffer
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(2, 0);
            await TaskRun(() => Window.Operations.Delete()).ConfigureAwait(true);
            AssertCaretVirtualPosition(2, 2);
        }
        
        [WpfFact]
        public async Task DeleteWithSelectionInReadonlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("23");

            var caret = Window.TextView.Caret;                                   
            var selection = Window.TextView.Selection; 

            // Delete() with selection in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            await TaskRun(() => Window.Operations.Delete()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Delete() with selection in active prompt, no-op
            selection.Clear(); 
            var start = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            var end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Delete()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Delete() with selection overlaps with editable buffer, 
            // delete editable content and move caret to closest editable location 
            selection.Clear();       
            caret.MoveToPreviousCaretPosition();
            start = caret.MoveToPreviousCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            end = caret.MoveToNextCaretPosition().VirtualBufferPosition; 
            AssertCaretVirtualPosition(2, 3);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Delete()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 3", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
        }

        [WpfFact]
        public async Task BackspaceWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");

            var caret = Window.TextView.Caret;

            // Backspace() with caret in readonly area, no-op
            Window.Operations.Home(false);
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();  
            AssertCaretVirtualPosition(1, 1);

            await TaskRun(() => Window.Operations.Backspace()).ConfigureAwait(true);
            AssertCaretVirtualPosition(1, 1);
            Assert.Equal("> 1\r\n1\r\n> int x\r\n> ;", GetTextFromCurrentSnapshot());

            // Backspace() with caret in 2nd active prompt, move caret to 
            // closest editable buffer then delete previous character (breakline)        
            caret.MoveToNextCaretPosition();
            Window.Operations.End(false);
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(3, 1);

            await TaskRun(() => Window.Operations.Backspace()).ConfigureAwait(true);
            AssertCaretVirtualPosition(2, 7);
            Assert.Equal("> 1\r\n1\r\n> int x;", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task BackspaceWithSelectionInReadonlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // Backspace() with selection in readonly area, no-op      
            Window.Operations.Home(false);
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            await TaskRun(() => Window.Operations.Backspace()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> int x\r\n> ;", GetTextFromCurrentSnapshot());

            // Backspace() with selection in active prompt, no-op
            selection.Clear();
            var start = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            var end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Backspace()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> int x\r\n> ;", GetTextFromCurrentSnapshot());

            // Backspace() with selection overlaps with editable buffer
            selection.Clear();
            Window.Operations.End(false);
            start = caret.Position.VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            end = caret.MoveToNextCaretPosition().VirtualBufferPosition; 
            AssertCaretVirtualPosition(3, 2);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Backspace()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> int x;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 7);
        }

        [WpfFact]
        public async Task ReturnWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;      

            // Return() with caret in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();   
            AssertCaretVirtualPosition(1, 1);

            await TaskRun(() => Window.Operations.Return()).ConfigureAwait(true);
            AssertCaretVirtualPosition(1, 1);

            // Return() with caret in active prompt, move caret to 
            // closest editable buffer first
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(2, 0);

            await TaskRun(() => Window.Operations.Return()).ConfigureAwait(true);
            AssertCaretVirtualPosition(3, 2);
        }

        [WpfFact]
        public async Task ReturnWithSelectionInReadonlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("23");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // Return() with selection in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            await TaskRun(() => Window.Operations.Return()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Return() with selection in active prompt, no-op
            selection.Clear();
            var start = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            var end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Return()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Delete() with selection overlaps with editable buffer, 
            // delete editable content and move caret to closest editable location and insert a return
            selection.Clear();
            caret.MoveToPreviousCaretPosition();
            start = caret.MoveToPreviousCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 3);

            selection.Select(start, end);

            await TaskRun(() => Window.Operations.Return()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> \r\n> 3", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(3, 2);
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

            await TaskRun(() => Window.Operations.Paste()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> 2", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(1, 1);

            // Paste() with caret in active prompt
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(2, 0);                                                                                                                     
            await TaskRun(() => Window.Operations.Paste()).ConfigureAwait(true);

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
        public async Task DeleteLineWithOutSelection()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);                                                                                                                        
            var caret = Window.TextView.Caret;                               

            // DeleteLine with caret in readonly area
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();

            AssertCaretVirtualPosition(1, 1);
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(1, 1);

            // DeleteLine with caret in active prompt
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
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);

            // DeleteLine with caret in editable area   
            caret.MoveToNextCaretPosition();

            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
        }

        [WpfFact]
        public async Task DeleteLineWithSelection()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // DeleteLine with selection in readonly area  
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            await TaskRun(() => Window.Operations.SelectAll()).ConfigureAwait(true);
            await TaskRun(() => Window.Operations.DeleteLine()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());

            // DeleteLine with selection in active prompt
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
            await TaskRun(() => Window.Operations.DeleteLine()).ConfigureAwait(true);
            Assert.Equal("> 1\r\n1\r\n> ;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);

            // DeleteLine with selection in editable area   
            Window.InsertCode("int x");
            selection.Select(caret.MoveToPreviousCaretPosition().VirtualBufferPosition, caret.MoveToPreviousCaretPosition().VirtualBufferPosition);
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);

            // DeleteLine with selection spans all areas     
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);
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

        [WpfFact]
        public async Task SubmitAsyncNone()
        {
            await SubmitAsync().ConfigureAwait(true);
        }

        [WpfFact]
        public async Task SubmitAsyncSingle()
        {
            await SubmitAsync("1").ConfigureAwait(true);
        }

        [WorkItem(5964)]
        [WpfFact]
        public async Task SubmitAsyncMultiple()
        {
            await SubmitAsync("1", "2", "1 + 2").ConfigureAwait(true);
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

        private async Task SubmitAsync(params string[] submissions)
        {
            var actualSubmissions = new List<string>();
            var evaluator = _testHost.Evaluator;
            EventHandler<string> onExecute = (_, s) => actualSubmissions.Add(s.TrimEnd());

            evaluator.OnExecute += onExecute;
            await TaskRun(() => Window.SubmitAsync(submissions)).ConfigureAwait(true);
            evaluator.OnExecute -= onExecute;

            AssertEx.Equal(submissions, actualSubmissions);
        }

        private string GetTextFromCurrentSnapshot()
        {
            return Window.TextView.TextBuffer.CurrentSnapshot.GetText();
        }    

        private async Task Submit(string submission, string output)
        {
            await TaskRun(() => Window.SubmitAsync(new[] { submission })).ConfigureAwait(true);
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

        internal static void DeleteLine(this IInteractiveWindowOperations operations)
        {
            ((IInteractiveWindowOperations2)operations).DeleteLine();
        }

        internal static void CutLine(this IInteractiveWindowOperations operations)
        {
            ((IInteractiveWindowOperations2)operations).CutLine();
        }
    }
}
