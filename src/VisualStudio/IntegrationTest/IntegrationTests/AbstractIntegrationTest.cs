// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private VisualStudioInstanceContext _visualStudioContext;

        protected AbstractIntegrationTest(VisualStudioInstanceFactory instanceFactory)
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

            // Install a COM message filter to handle retry operations when the first attempt fails
            _messageFilter = RegisterMessageFilter();
            _instanceFactory = instanceFactory;

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

        protected virtual MessageFilter RegisterMessageFilter()
            => new MessageFilter();

        protected void Wait(double seconds)
        {
            var timeout = TimeSpan.FromMilliseconds(seconds * 1000);
            Thread.Sleep(timeout);
        }

        /// <summary>
        /// This method provides the implementation for <see cref="IDisposable.Dispose"/>.
        /// This method is called via the <see cref="IDisposable"/> interface if the constructor completes successfully.
        /// The <see cref="InitializeAsync"/> may or may not have completed successfully.
        /// </summary>
        public virtual void Dispose()
        {
            _messageFilter.Dispose();
        }

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);
    }
}
