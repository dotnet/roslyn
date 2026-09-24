// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal sealed partial class TestExportJoinableTaskContext
{
    /// <summary>
    /// Defines a <see cref="SynchronizationContext"/> for use in cases where the synchronization context should not
    /// be used for execution of code. Attempting to execute code through this synchronization context will record
    /// an exception (with stack trace) for the first occurrence, which can be re-thrown by calling
    /// <see cref="ThrowIfSwitchOccurred"/> at an appropriate time.
    /// </summary>
    /// <remarks>
    /// <para>This synchronization context is used in cases where code expects a synchronization context with
    /// specific properties (e.g. code is executed on a "main thread"), but no synchronization context with those
    /// properties is available. Due to the positioning of synchronization contexts within asynchronous code
    /// patterns, detection of misused synchronization contexts without crashes is often challenging. Test cases can
    /// use this synchronization context in place of a mock to capture </para>
    ///
    /// <note type="important">
    /// <para>This synchronization context will not directly block the execution of scheduled tasks, and will fall
    /// back to execution on an underlying synchronization context (typically the thread pool). Tests using this
    /// synchronization context should verify the synchronization context was not used by calling
    /// <see cref="ThrowIfSwitchOccurred"/> at the end of the test.</para>
    /// </note>
    /// </remarks>
    internal sealed class DenyExecutionSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// Records the first case where the synchronization context was used for scheduling an operation.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="StrongBox{T}"/> wrapper allows copies of the synchronization context (created by
        /// <see cref="CreateCopy"/>) to record information about incorrect synchronization context in the original
        /// synchronization context from which it was created.</para>
        /// </remarks>
        private readonly StrongBox<ExceptionDispatchInfo> _failedTransfer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DenyExecutionSynchronizationContext"/> class.
        /// </summary>
        /// <param name="underlyingContext">The fallback synchronization context to use for scheduling operations
        /// posted to this synchronization context.</param>
        public DenyExecutionSynchronizationContext(SynchronizationContext? underlyingContext)
            : this(underlyingContext, mainThread: null, failedTransfer: null)
        {
        }

        private DenyExecutionSynchronizationContext(SynchronizationContext? underlyingContext, Thread? mainThread, StrongBox<ExceptionDispatchInfo>? failedTransfer)
        {
            UnderlyingContext = underlyingContext ?? new SynchronizationContext();
            MainThread = mainThread ?? new Thread(MainThreadStart);
            _failedTransfer = failedTransfer ?? new StrongBox<ExceptionDispatchInfo>();
        }

        internal event EventHandler? InvalidSwitch;

        private SynchronizationContext UnderlyingContext
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="Thread"/> to treat as the main thread.
        /// </summary>
        /// <remarks>
        /// <para>This thread will never be started, and will never have work scheduled for execution. The value is
        /// used for checking if <see cref="Thread.CurrentThread"/> matches a known "main thread", and ensures that
        /// the comparison will always result in a failure (not currently on the main thread).</para>
        /// </remarks>
        internal Thread MainThread
        {
            get;
        }

        private void MainThreadStart() => throw ExceptionUtilities.Unreachable();

        /// <summary>
        /// Verifies that the current synchronization context has not been used for scheduling work. If the
        /// synchronization was used, an exception is thrown with information about the first such case.
        /// </summary>
        internal void ThrowIfSwitchOccurred()
        {
            if (_failedTransfer.Value == null)
            {
                return;
            }

            _failedTransfer.Value.Throw();
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            try
            {
                if (_failedTransfer.Value == null)
                {
                    ThrowFailedTransferExceptionForCapture();
                }
            }
            catch (InvalidOperationException e)
            {
                _failedTransfer.Value = ExceptionDispatchInfo.Capture(e);
                InvalidSwitch?.Invoke(this, EventArgs.Empty);
            }

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            UnderlyingContext.Post(d, state);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            try
            {
                if (_failedTransfer.Value == null)
                {
                    ThrowFailedTransferExceptionForCapture();
                }
            }
            catch (InvalidOperationException e)
            {
                _failedTransfer.Value = ExceptionDispatchInfo.Capture(e);
                InvalidSwitch?.Invoke(this, EventArgs.Empty);
            }

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            UnderlyingContext.Send(d, state);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
        }

        public override SynchronizationContext CreateCopy()
            => new DenyExecutionSynchronizationContext(UnderlyingContext.CreateCopy(), MainThread, _failedTransfer);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFailedTransferExceptionForCapture()
            => throw new InvalidOperationException($"Code cannot switch to the main thread without configuring IThreadingContext.");
    }
}
