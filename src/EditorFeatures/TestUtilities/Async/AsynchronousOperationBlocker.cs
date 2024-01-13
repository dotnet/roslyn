// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public sealed class AsynchronousOperationBlocker : IDisposable
    {
        private readonly ManualResetEvent _waitHandle;
        private readonly object _lockObj = new();
        private bool _blocking;
        private bool _disposed;

        public AsynchronousOperationBlocker()
        {
            _waitHandle = new ManualResetEvent(false);
            _blocking = true;
        }

        public bool IsBlockingOperations
        {
            get
            {
                lock (_lockObj)
                {
                    return _blocking;
                }
            }

            private set
            {
                lock (_lockObj)
                {
                    if (_blocking == value)
                    {
                        return;
                    }

                    _blocking = value;
                    if (!_disposed)
                    {
                        if (_blocking)
                        {
                            _waitHandle.Reset();
                        }
                        else
                        {
                            _waitHandle.Set();
                        }
                    }
                }
            }
        }

        public void BlockOperations()
            => this.IsBlockingOperations = true;

        public void UnblockOperations()
            => this.IsBlockingOperations = false;

        public bool WaitIfBlocked(TimeSpan timeout)
        {
            if (_disposed)
            {
                FailFast.Fail("Badness");
            }

            return _waitHandle.WaitOne(timeout);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _waitHandle.Dispose();
            }
        }
    }
}
