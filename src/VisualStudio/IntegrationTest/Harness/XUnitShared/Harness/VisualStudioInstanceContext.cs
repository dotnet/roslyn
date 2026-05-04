// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;

    /// <summary>
    /// Represents a wrapper of <see cref="VisualStudioInstance"/> that is given to a specific test. This should
    /// be disposed by the test to ensure the test's actions are cleaned up during the test run so the instance is
    /// usable for the next test.
    /// </summary>
    internal sealed class VisualStudioInstanceContext : IDisposable
    {
        private readonly VisualStudioInstanceFactory _instanceFactory;

        internal VisualStudioInstanceContext(VisualStudioInstance instance, VisualStudioInstanceFactory instanceFactory)
        {
            Instance = instance;
            _instanceFactory = instanceFactory;
        }

        public VisualStudioInstance Instance
        {
            get;
        }

        public void Dispose()
        {
            try
            {
                Instance.CleanUp();
                _instanceFactory.NotifyCurrentInstanceContextDisposed(canReuse: true);
            }
            catch (Exception)
            {
                // If the cleanup process fails, we want to make sure the next test gets a new instance. However,
                // we still want to raise this exception to fail this test
                _instanceFactory.NotifyCurrentInstanceContextDisposed(canReuse: false);
                throw;
            }
        }
    }
}
