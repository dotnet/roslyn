// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class ServiceInitializer<TInterface, TService> where TInterface : class
    {
        private readonly AsyncLazy<TInterface> _asyncLazy;

        public ServiceInitializer(Shell.IAsyncServiceProvider serviceProvider, Func<TInterface, Task> initializer, bool uiThreadRequired, JoinableTaskFactory joinableTaskFactory)
        {
            _asyncLazy = new Threading.AsyncLazy<TInterface>(async () =>
            {
                // why async lazy doesn't give in cancellation token?
                if (uiThreadRequired)
                {
                    await joinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                }

#pragma warning disable CA2007 // under JTF, don't use explict ConfigureAwait
                var service = (TInterface)await serviceProvider.GetServiceAsync(typeof(TService));
                await initializer(service);
#pragma warning restore CA2007 

                return service;
            }, joinableTaskFactory);
        }

        public TInterface GetService(CancellationToken cancellationToken)
        {
            return ThreadHelper.JoinableTaskFactory.Run(() => _asyncLazy.GetValueAsync(cancellationToken));
        }

        public Task<TInterface> GetServiceAsync(CancellationToken cancellationToken)
        {
            return _asyncLazy.GetValueAsync(cancellationToken);
        }

        public void EnsureInitializationToRun(CancellationToken cancellationToken)
        {
            // ensure service to be initialized
            GetService(cancellationToken);
        }
    }
}
