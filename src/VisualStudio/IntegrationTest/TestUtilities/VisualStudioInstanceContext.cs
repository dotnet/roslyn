// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Represents a wrapper of <see cref="VisualStudioInstance"/> that is given to a specific test. This should
    /// be disposed by the test to ensure the test's actions are cleaned up during the test run so the instance is
    /// usable for the next test.
    /// </summary>
    public sealed class VisualStudioInstanceContext : IDisposable
    {
        private readonly VisualStudioInstanceFactory _instanceFactory;

        public VisualStudioInstance Instance { get; }

        internal VisualStudioInstanceContext(VisualStudioInstance instance, VisualStudioInstanceFactory instanceFactory)
        {
            Instance = instance;
            _instanceFactory = instanceFactory;
        }

        public void Dispose()
        {
            _instanceFactory.NotifyCurrentInstanceContextDisposed(canReuse: true);
        }
    }
}
