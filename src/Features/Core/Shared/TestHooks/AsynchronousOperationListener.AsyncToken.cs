// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal partial class AsynchronousOperationListener
    {
        protected internal class AsyncToken : IAsyncToken
        {
            private readonly AsynchronousOperationListener listener;

            private bool disposed;

            public AsyncToken(AsynchronousOperationListener listener)
            {
                this.listener = listener;

                listener.Increment();
            }

            public void Dispose()
            {
                lock (listener.gate)
                {
                    if (disposed)
                    {
                        throw new InvalidOperationException("Double disposing of an async token");
                    }

                    disposed = true;
                    listener.Decrement(this);
                }
            }
        }
    }
}
