// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal partial class AsynchronousOperationListener
    {
        protected internal class AsyncToken : IAsyncToken
        {
            private readonly AsynchronousOperationListener _listener;

            private bool _disposed;

            public AsyncToken(AsynchronousOperationListener listener)
            {
                _listener = listener;

                listener.Increment();
            }

            public void Dispose()
            {
                lock (_listener._gate)
                {
                    if (_disposed)
                    {
                        throw new InvalidOperationException("Double disposing of an async token");
                    }

                    _disposed = true;
                    _listener.Decrement(this);
                }
            }
        }
    }
}
