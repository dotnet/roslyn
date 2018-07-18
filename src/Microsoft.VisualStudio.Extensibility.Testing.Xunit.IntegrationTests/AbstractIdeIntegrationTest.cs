// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests
{
    using System;
    using System.Threading.Tasks;
    using global::Xunit;
    using global::Xunit.Harness;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    public abstract class AbstractIdeIntegrationTest : IAsyncLifetime, IDisposable
    {
        private JoinableTaskContext _joinableTaskContext;
        private JoinableTaskCollection _joinableTaskCollection;
        private JoinableTaskFactory _joinableTaskFactory;

        protected AbstractIdeIntegrationTest()
        {
            if (GlobalServiceProvider.ServiceProvider.GetService(typeof(SVsTaskSchedulerService)) is IVsTaskSchedulerService2 taskSchedulerService)
            {
                JoinableTaskContext = (JoinableTaskContext)taskSchedulerService.GetAsyncTaskContext();
            }
            else
            {
                JoinableTaskContext = new JoinableTaskContext();
            }
        }

        protected JoinableTaskContext JoinableTaskContext
        {
            get
            {
                return _joinableTaskContext ?? throw new InvalidOperationException();
            }

            private set
            {
                if (value == _joinableTaskContext)
                {
                    return;
                }

                if (value is null)
                {
                    _joinableTaskContext = null;
                    _joinableTaskCollection = null;
                    _joinableTaskFactory = null;
                }
                else
                {
                    _joinableTaskContext = value;
                    _joinableTaskCollection = value.CreateCollection();
                    _joinableTaskFactory = value.CreateFactory(_joinableTaskCollection);
                }
            }
        }

        protected JoinableTaskFactory JoinableTaskFactory => _joinableTaskFactory ?? throw new InvalidOperationException();

        protected IServiceProvider ServiceProvider => GlobalServiceProvider.ServiceProvider;

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual async Task DisposeAsync()
        {
            await _joinableTaskCollection.JoinTillEmptyAsync();
            JoinableTaskContext = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
