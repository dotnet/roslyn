// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IAsyncLifetime, IDisposable
    {
        protected readonly string ProjectName = "TestProj";
        protected readonly string SolutionName = "TestSolution";

        private readonly CancellationTokenSource _watchdogCompletionTokenSource;

        private readonly MessageFilter _messageFilter;
        private readonly VisualStudioInstanceFactory _instanceFactory;
        private VisualStudioInstanceContext _visualStudioContext;

        protected AbstractIntegrationTest(VisualStudioInstanceFactory instanceFactory)
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

            _watchdogCompletionTokenSource = new CancellationTokenSource();

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
            var testName = CaptureTestNameAttribute.CurrentName;

            try
            {
                _visualStudioContext = await _instanceFactory.GetNewOrUsedInstanceAsync(VisualStudioInstanceFactory.RequiredPackageIds).ConfigureAwait(false);

                WatchdogAsync().Forget();
            }
            catch
            {
                _messageFilter.Dispose();
                throw;
            }

            return;

            // Local function
            async Task WatchdogAsync()
            {
                await Task.Delay(TimeSpan.FromMilliseconds(3 * Helper.HangMitigatingTimeout.TotalMilliseconds), _watchdogCompletionTokenSource.Token).ConfigureAwait(false);

                var ex = new Exception($"Terminating test '{GetType().Name}.{testName}' run due to unrecoverable test timeout.");
                InProcessIdeTestAssemblyRunner.SaveScreenshot(ex);

                if (!Debugger.IsAttached)
                {
                    Environment.FailFast(ex.Message);
                }
            }
        }

        /// <summary>
        /// This method implements <see cref="IAsyncLifetime.DisposeAsync"/>, and is used for releasing resources
        /// created by <see cref="IAsyncLifetime.InitializeAsync"/>. This method is only called if
        /// <see cref="InitializeAsync"/> completes successfully.
        /// </summary>
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
                _watchdogCompletionTokenSource.Cancel();
                _messageFilter.Dispose();
            }
        }
    }
}
