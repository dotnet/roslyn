// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public class EnsureInvariantCulture : IDisposable
    {
        private readonly CultureInfo _threadCulture;
        private readonly int _threadId;

        public EnsureInvariantCulture()
        {
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _threadCulture = CultureInfo.CurrentCulture;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose()
        {
            Debug.Assert(_threadId == Thread.CurrentThread.ManagedThreadId);

            if (_threadId == Thread.CurrentThread.ManagedThreadId)
            {
                CultureInfo.CurrentCulture = _threadCulture;
            }
        }
    }
}
