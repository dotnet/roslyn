// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal sealed partial class ThreadingContext
    {
        private class DenyExecutionSynchronizationContext : SynchronizationContext
        {
            private readonly SynchronizationContext _underlyingContext;
            private readonly Thread _mainThread;
            private readonly StrongBox<ExceptionDispatchInfo> _failedTransfer;

            public DenyExecutionSynchronizationContext(SynchronizationContext underlyingContext)
                : this(underlyingContext, mainThread: null, failedTransfer: null)
            {
            }

            private DenyExecutionSynchronizationContext(SynchronizationContext underlyingContext, Thread mainThread, StrongBox<ExceptionDispatchInfo> failedTransfer)
            {
                _underlyingContext = underlyingContext;
                _mainThread = mainThread ?? new Thread(MainThreadStart);
                _failedTransfer = failedTransfer ?? new StrongBox<ExceptionDispatchInfo>();
            }

            internal SynchronizationContext UnderlyingContext => _underlyingContext;

            internal Thread MainThread => _mainThread;

            private void MainThreadStart() => throw new InvalidOperationException("This thread should never be started.");

            internal void ThrowIfSwitchOccurred()
            {
                if (_failedTransfer.Value == null)
                {
                    return;
                }

                _failedTransfer.Value.Throw();
            }

            public override void Post(SendOrPostCallback d, object state)
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
                }

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                (_underlyingContext ?? new SynchronizationContext()).Post(d, state);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            }

            public override void Send(SendOrPostCallback d, object state)
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
                }

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                (_underlyingContext ?? new SynchronizationContext()).Send(d, state);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            }

            public override SynchronizationContext CreateCopy()
            {
                return new DenyExecutionSynchronizationContext(_underlyingContext.CreateCopy(), _mainThread, _failedTransfer);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void ThrowFailedTransferExceptionForCapture()
            {
                throw new InvalidOperationException($"Code cannot switch to the main thread without configuring the {nameof(IThreadingContext)}.");
            }
        }
    }
}
