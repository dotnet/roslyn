// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal partial class AsynchronousOperationListener
    {
        internal class AsyncToken : IAsyncToken
        {
            private readonly AsynchronousOperationListener _listener;

            private bool _disposed;

            public AsyncToken(AsynchronousOperationListener listener)
            {
                _listener = listener;

                listener.Increment_NoLock();
            }

            public void Dispose()
            {
                using (_listener._gate.DisposableWait(CancellationToken.None))
                {
                    if (_disposed)
                    {
                        throw new InvalidOperationException("Double disposing of an async token");
                    }

                    _disposed = true;
                    _listener.Decrement_NoLock(this);
                }
            }
        }
    }
}
