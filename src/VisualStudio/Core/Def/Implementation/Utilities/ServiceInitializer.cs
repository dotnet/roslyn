// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class ServiceInitializer<TInterface, TService> where TInterface : class
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

        private readonly Shell.IAsyncServiceProvider _serviceProvider;
        private readonly bool _uiThreadRequired;

        private Func<TInterface, Task> _initializer;
        private TInterface _service;

        public ServiceInitializer(Shell.IAsyncServiceProvider serviceProvider, Func<TInterface, Task> initializer, bool uiThreadRequired)
        {
            _serviceProvider = serviceProvider;
            _initializer = initializer;
            _uiThreadRequired = uiThreadRequired;
        }

        public TInterface GetService(CancellationToken cancellationToken)
        {
            if (_service == null)
            {
                ThreadHelper.JoinableTaskFactory.Run(() => GetServiceAsync(cancellationToken));
            }

            return _service;
        }

        public async Task<TInterface> GetServiceAsync(CancellationToken cancellationToken)
        {
            // if we are called from UI thraed and UI thread is required, keep context through whole method.
            // if we are called from non UI thread, but UI thread is required, lock is acquired on non UI thread, but others run on UI thread
            // if we are called from UI thread, but UI thread is not required, we don't need to keep the context
            // if we are called from non UI thread, but UI thread is not required, we don't need to keep the context
            var continueOnCapturedContext = ThreadHelper.JoinableTaskContext.IsOnMainThread && _uiThreadRequired;

            Func<TInterface, Task> initializer = null;
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext))
            {
                if (_service != null)
                {
                    return _service;
                }

                initializer = _initializer;
                _initializer = null;
            }

            if (_uiThreadRequired)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }

            var service = (TInterface)await _serviceProvider.GetServiceAsync(typeof(TService)).ConfigureAwait(_uiThreadRequired);

            if (initializer != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // initializer is not cancellable. otherwise, we can get into unknown state
                await initializer(service).ConfigureAwait(_uiThreadRequired);
            }

            if (!continueOnCapturedContext)
            {
                // make sure that we run on BG
                await TaskScheduler.Default;
            }

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext))
            {
                _service = service;
            }

            return _service;
        }

        public void EnsureInitializationToRun(CancellationToken cancellationToken)
        {
            // ensure service to be initialized
            GetService(cancellationToken);
        }
    }
}
