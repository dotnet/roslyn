// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Testing.InProcess
{
    public abstract class InProcComponent
    {
        protected InProcComponent(TestServices testServices)
        {
            TestServices = testServices ?? throw new ArgumentNullException(nameof(testServices));
        }

        public TestServices TestServices { get; }

        protected JoinableTaskFactory JoinableTaskFactory => TestServices.JoinableTaskFactory;

        protected async Task ExecuteCommandAsync(string commandName, string args = "")
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetGlobalServiceAsync<SDTE, EnvDTE.DTE>();
            dte.ExecuteCommand(commandName, args);
        }

        protected async Task<TInterface> GetGlobalServiceAsync<TService, TInterface>()
            where TService : class
            where TInterface : class
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var serviceProvider = (IAsyncServiceProvider2)ServiceProvider.GlobalProvider.GetService(typeof(SAsyncServiceProvider));
            Assumes.Present(serviceProvider);
            return (TInterface)await serviceProvider.GetServiceAsync(typeof(TService));
        }

        protected async Task<TService> GetComponentModelServiceAsync<TService>()
            where TService : class
        {
            var componentModel = await GetGlobalServiceAsync<SComponentModel, IComponentModel>();
            return componentModel.GetService<TService>();
        }

        /// <summary>
        /// Waiting for the application to 'idle' means that it is done pumping messages (including WM_PAINT).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected static async Task WaitForApplicationIdleAsync(CancellationToken cancellationToken)
        {
            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.ApplicationIdle);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            await Task.Factory.StartNew(
                () => { },
                cancellationToken,
                TaskCreationOptions.None,
                taskScheduler);
        }
    }
}
