// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public sealed class AsynchronousOperationBlocker : IDisposable
    {
        private readonly ManualResetEvent _waitHandle;
        private readonly object _lockObj;
        private bool _blocking;
        private bool _disposed;

        public AsynchronousOperationBlocker()
        {
            _waitHandle = new ManualResetEvent(false);
            _lockObj = new object();
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
        {
            this.IsBlockingOperations = true;
        }

        public void UnblockOperations()
        {
            this.IsBlockingOperations = false;
        }

        public bool WaitIfBlocked(TimeSpan timeout)
        {
            if (_disposed)
            {
                Environment.FailFast("Badness");
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
