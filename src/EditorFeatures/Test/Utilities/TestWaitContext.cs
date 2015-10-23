// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    public sealed class TestWaitContext : IWaitContext
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _maxUpdates;
        private int _updates;

        public TestWaitContext(int maxUpdates)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _maxUpdates = maxUpdates;
        }

        public int Updates
        {
            get { return _updates; }
        }

        public CancellationToken CancellationToken
        {
            get { return _cancellationTokenSource.Token; }
        }

        public void UpdateProgress()
        {
            var result = Interlocked.Increment(ref _updates);
            if (result > _maxUpdates)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public bool AllowCancel
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public string Message
        {
            get
            {
                return "";
            }

            set
            {
            }
        }

        public void Dispose()
        {
        }
    }
}
