// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit.Abstractions;

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
        private readonly ITestOutputHelper _testOutputHelper;

        public VisualStudioInstance Instance { get; }

        internal VisualStudioInstanceContext(VisualStudioInstance instance, VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
        {
            Instance = instance;
            _instanceFactory = instanceFactory;
            _testOutputHelper = testOutputHelper;
        }

        public void Dispose()
        {
            try
            {
                Instance.CleanUp(_testOutputHelper);
                _instanceFactory.NotifyCurrentInstanceContextDisposed(canReuse: true, _testOutputHelper);
            }
            catch (Exception)
            {
                // If the cleanup process fails, we want to make sure the next test gets a new instance. However,
                // we still want to raise this exception to fail this test
                _instanceFactory.NotifyCurrentInstanceContextDisposed(canReuse: false, _testOutputHelper);
                throw;
            }
        }
    }
}
