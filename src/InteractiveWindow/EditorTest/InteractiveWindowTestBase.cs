// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;    
using System.Threading;
using System.Threading.Tasks;                               
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public abstract class InteractiveWindowTestBase : IDisposable
    {
        private readonly TaskFactory _factory = new TaskFactory(TaskScheduler.Default);

        internal abstract InteractiveWindowTestHost TestHost { get; }

        internal IInteractiveWindow Window => TestHost.Window;      

        internal void AssertCaretVirtualPosition(int expectedLine, int expectedColumn)
        {
            ITextSnapshotLine actualLine;
            int actualColumn;
            Window.TextView.Caret.Position.VirtualBufferPosition.GetLineAndColumn(out actualLine, out actualColumn);
            Assert.Equal(expectedLine, actualLine.LineNumber);
            Assert.Equal(expectedColumn, actualColumn);
        }

        internal string GetTextFromCurrentSnapshot()
        {
            return Window.TextView.TextBuffer.CurrentSnapshot.GetText();
        }

        internal Task TaskRun(Action action)
        {
            return _factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        internal void Submit(string submission, string output)
        {
            TaskRun(() => Window.SubmitAsync(new[] { submission })).PumpingWait();
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

        public abstract void Dispose();
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
