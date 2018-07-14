// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class ServiceInitializer<TInterface, TService> where TInterface : class
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly Func<TInterface, Task> _initializer;
        private readonly bool _uiThreadRequired;

        private TInterface _service;

        public ServiceInitializer(IAsyncServiceProvider serviceProvider, Func<TInterface, Task> initializer, bool uiThreadRequired)
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

        public Task<TInterface> GetServiceAsync(CancellationToken cancellationToken)
        {
            if (_uiThreadRequired)
            {
                return GetServiceFromForegroundThreadAsync(cancellationToken);
            }

            return GetServiceFromBackgroundThreadAsync(cancellationToken);
        }

        private async Task<TInterface> GetServiceFromForegroundThreadAsync(CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(true))
            {
                if (_service == null)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    await SetAndInitializeAsync(continueOnCapturedContext: true, cancellationToken).ConfigureAwait(true);
                }

                return _service;
            }
        }

        private async Task<TInterface> GetServiceFromBackgroundThreadAsync(CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_service == null)
                {
                    await SetAndInitializeAsync(continueOnCapturedContext: false, cancellationToken).ConfigureAwait(false);
                }

                return _service;
            }
        }

        private async Task SetAndInitializeAsync(bool continueOnCapturedContext, CancellationToken cancellationToken)
        {
            var service = (TInterface)await _serviceProvider.GetServiceAsync(typeof(TService)).ConfigureAwait(continueOnCapturedContext);

            cancellationToken.ThrowIfCancellationRequested();

            // initialization can't be cancelled, otherwise, we can get into unknown state
            await _initializer(service).ConfigureAwait(continueOnCapturedContext);

            _service = service;
        }

        public void Ensure(CancellationToken cancellationToken)
        {
            // ensure service to be initialized
            GetService(cancellationToken);
        }
    }
}
