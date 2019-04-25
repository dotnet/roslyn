// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    internal class TestUIThreadOperationContext : AbstractUIThreadOperationContext
    {
        CancellationTokenSource _cancellationTokenSource;
        private readonly int _maxUpdates;
        private int _updates;

        public TestUIThreadOperationContext(int maxUpdates)
            : base(allowCancellation: false, defaultDescription: "")
        {
            _maxUpdates = maxUpdates;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public TestUIThreadOperationContext()
            : this(int.MaxValue)
        {
        }

        public int Updates
        {
            get { return _updates; }
        }

        public override CancellationToken UserCancellationToken
        {
            get { return _cancellationTokenSource.Token; }
        }

        protected override void OnScopeProgressChanged(IUIThreadOperationScope changedScope)
        {
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            var result = Interlocked.Increment(ref _updates);
            if (result > _maxUpdates)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
