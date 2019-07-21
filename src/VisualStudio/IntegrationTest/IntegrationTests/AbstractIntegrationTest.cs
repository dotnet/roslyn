// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IAsyncLifetime, IDisposable
    {
        protected const string ProjectName = "TestProj";
        protected const string SolutionName = "TestSolution";

        private readonly MessageFilter _messageFilter;
        private readonly VisualStudioInstanceFactory _instanceFactory;
        private readonly ITestOutputHelper _testOutputHelper;
        private VisualStudioInstanceContext _visualStudioContext;

        protected AbstractIntegrationTest(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

            // Install a COM message filter to handle retry operations when the first attempt fails
            _messageFilter = RegisterMessageFilter();
            _instanceFactory = instanceFactory;
            _testOutputHelper = testOutputHelper;

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

        public VisualStudioInstance VisualStudio => _visualStudioContext?.Instance;

        public virtual async Task InitializeAsync()
        {
            try
            {
                _visualStudioContext = await _instanceFactory.GetNewOrUsedInstanceAsync(SharedIntegrationHostFixture.RequiredPackageIds).ConfigureAwait(false);
                _visualStudioContext.Instance.ActivateMainWindow();
            }
            catch
            {
                _messageFilter.Dispose();
                throw;
            }
        }

        /// <summary>
        /// This method implements <see cref="IAsyncLifetime.DisposeAsync"/>, and is used for releasing resources
        /// created by <see cref="IAsyncLifetime.InitializeAsync"/>. This method is only called if
        /// <see cref="InitializeAsync"/> completes successfully.
        /// </summary>
        public virtual Task DisposeAsync()
        {
            if (VisualStudio?.Editor.IsCompletionActive() ?? false)
            {
                // Make sure completion isn't visible.
                // 🐛 Only needed as a workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/801435
                VisualStudio.SendKeys.Send(VirtualKey.Escape);
            }

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
