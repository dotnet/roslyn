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

        private TInterface _service;

        public ServiceInitializer(IAsyncServiceProvider serviceProvider, Func<TInterface, Task> initializer)
        {
            _serviceProvider = serviceProvider;
            _initializer = initializer;
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
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(true))
            {
                if (_service == null)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    var service = (TInterface)await _serviceProvider.GetServiceAsync(typeof(TService)).ConfigureAwait(true);

                    cancellationToken.ThrowIfCancellationRequested();

                    // initialization can't be cancelled, otherwise, we can get into unknown state
                    await _initializer(service).ConfigureAwait(true);

                    _service = service;
                }

                return _service;
            }
        }

        public void Ensure(CancellationToken cancellationToken)
        {
            // ensure service to be initialized
            GetService(cancellationToken);
        }
    }
}
