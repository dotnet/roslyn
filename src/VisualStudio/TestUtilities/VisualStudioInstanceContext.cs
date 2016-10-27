// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>
    /// Represents a wrapper of <see cref="VisualStudioInstance"/> that is given to a specific test. This should
    /// be disposed by the test to ensure the test's actions are cleaned up during the test run so the instance is
    /// usable for the next test.
    /// </summary>
    public sealed class VisualStudioInstanceContext : IDisposable
    {
        public VisualStudioInstance Instance { get; }
        private readonly VisualStudioInstanceFactory _instanceFactory;

        internal VisualStudioInstanceContext(VisualStudioInstance instance, VisualStudioInstanceFactory instanceFactory)
        {
            this.Instance = instance;
            _instanceFactory = instanceFactory;
        }

        public void Dispose()
        {
            try
            {
                this.Instance.CleanUp();
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
