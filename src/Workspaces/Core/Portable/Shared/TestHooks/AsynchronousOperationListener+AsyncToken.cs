// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.TestHooks;

internal sealed partial class AsynchronousOperationListener
{
    internal class AsyncToken : IAsyncToken
    {
        private bool _disposed;

        public AsyncToken(AsynchronousOperationListener listener)
        {
            Listener = listener;

            listener.Increment_NoLock();
        }

        public AsynchronousOperationListener Listener { get; }

        public bool IsNull
            => false;

        public void Dispose()
        {
            using (Listener._gate.DisposableWait(CancellationToken.None))
            {
                if (_disposed)
                {
                    throw new InvalidOperationException("Double disposing of an async token");
                }

                _disposed = true;
                Listener.Decrement_NoLock(this);
            }
        }
    }
}
