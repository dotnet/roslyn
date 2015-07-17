// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public void Dispose()
        {
            _testHost.Dispose();
        }

        public IInteractiveWindow Window
        {
            get
            {
                return _testHost.Window;
            }
        }

        public static IEnumerable<IInteractiveWindowCommand> MockCommands(params string[] commandNames)
        {
            foreach (var name in commandNames)
            {
                var mock = new Mock<IInteractiveWindowCommand>();
                mock.Setup(m => m.Names).Returns(new[] { name });
                yield return mock.Object;
            }
        }

        public static ITextSnapshot MockSnapshot(string content)
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
            // TODO (https://github.com/dotnet/roslyn/issues/3984): InsertCode is a no-op unless standard input is being collected.
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
            Window.Operations.BreakLine(); // Something to backspace.
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
            Window.Operations.BreakLine(); // Something to clear.
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
        public void CallSelectAllOnNonUIThread()
        {
            Window.Operations.BreakLine(); // Something to select.
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
    }
}
