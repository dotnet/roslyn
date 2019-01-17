// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Test.Apex;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractIntegrationTest : ApexTest, IDisposable
    {
        protected readonly string ProjectName = "TestProj";
        protected readonly string SolutionName = "TestSolution";

        private readonly MessageFilter _messageFilter;
        private readonly VisualStudioInstanceFactory _instanceFactory;
        private VisualStudioInstanceContext _visualStudioContext;

        protected AbstractIntegrationTest()
        {
            Assert.AreEqual(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

            // Install a COM message filter to handle retry operations when the first attempt fails
            _messageFilter = RegisterMessageFilter();
            _instanceFactory = new VisualStudioInstanceFactory();

            try
            {
                Helper.Automation.TransactionTimeout = 20000;
            }
            catch
            {
                _messageFilter.Dispose();
                _messageFilter = null;
                throw;
            }
        }

        public VisualStudioInstance VisualStudioInstance { get; private set; }

        [TestInitialize]
        public virtual async Task InitializeAsync()
        {
            try
            {
                _visualStudioContext = await _instanceFactory.GetNewOrUsedInstanceAsync(this.Operations, SharedIntegrationHostFixture.RequiredPackageIds).ConfigureAwait(false);
                _visualStudioContext.Instance.ActivateMainWindow();
            }
            catch
            {
                _messageFilter.Dispose();
                throw;
            }
        }

        [TestCleanup]
        public virtual Task DisposeAsync()
        {
            _visualStudioContext.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual MessageFilter RegisterMessageFilter()
            => new MessageFilter();

        protected void Wait(double seconds)
        {
            var timeout = TimeSpan.FromMilliseconds(seconds * 1000);
            Thread.Sleep(timeout);
        }

        /// <summary>
        /// This method provides the implementation for <see cref="IDisposable.Dispose"/>. This method via the
        /// <see cref="IDisposable"/> interface (i.e. <paramref name="disposing"/> is <see langword="true"/>) if the
        /// constructor completes successfully. The <see cref="InitializeAsync"/> may or may not have completed
        /// successfully.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageFilter.Dispose();
            }
        }

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);
    }
}
