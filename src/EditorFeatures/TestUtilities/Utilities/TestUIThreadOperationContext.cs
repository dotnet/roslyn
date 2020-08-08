// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    internal class TestUIThreadOperationContext : AbstractUIThreadOperationContext
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
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
            => UpdateProgress();

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
