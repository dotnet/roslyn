// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public abstract class InProcComponent2
    {
        protected InProcComponent2(JoinableTaskFactory joinableTaskFactory)
        {
            JoinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
        }

        protected JoinableTaskFactory JoinableTaskFactory
        {
            get;
        }

        protected void BeginInvokeOnUIThread(Func<Task> action)
        {
            JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                await action();
            });
        }

        protected async Task InvokeOnUIThreadAsync(Func<Task> action)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await action();
        }

        protected async Task<T> InvokeOnUIThreadAsync<T>(Func<Task<T>> action)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return await action();
        }

        protected async Task<TInterface> GetGlobalServiceAsync<TService, TInterface>()
            where TService : class
            where TInterface : class
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return (TInterface)ServiceProvider.GlobalProvider.GetService(typeof(TService));
        }

        protected async Task<TService> GetComponentModelServiceAsync<TService>()
            where TService : class
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return (await GetComponentModelAsync()).GetService<TService>();
        }

        protected async Task<DTE> GetDTEAsync()
        {
            return await GetGlobalServiceAsync<SDTE, DTE>();
        }

        protected async Task<IComponentModel> GetComponentModelAsync()
        {
            return await GetGlobalServiceAsync<SComponentModel, IComponentModel>();
        }

        protected async Task<bool> IsCommandAvailableAsync(string commandName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return (await GetDTEAsync()).Commands.Item(commandName).IsAvailable;
        }

        protected async Task ExecuteCommandAsync(string commandName, string args = "")
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            (await GetDTEAsync()).ExecuteCommand(commandName, args);
        }

        /// <summary>
        /// Waiting for the application to 'idle' means that it is done pumping messages (including WM_PAINT).
        /// </summary>
        protected static async Task WaitForApplicationIdleAsync()
        {
            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.ApplicationIdle);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            await Task.Factory.StartNew(
                () => { },
                CancellationToken.None,
                TaskCreationOptions.None,
                taskScheduler);
        }

        protected static async Task WaitForSystemIdleAsync()
        {
            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.ApplicationIdle);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            await Task.Factory.StartNew(
                () => { },
                CancellationToken.None,
                TaskCreationOptions.None,
                taskScheduler);
        }
    }
}
