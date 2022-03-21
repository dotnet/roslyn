// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Hosting.Diagnostics.Waiters;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    /// <summary>
    /// Base class for all components that run inside of the Visual Studio process.
    /// <list type="bullet">
    /// <item>Every in-proc component should provide a public, static, parameterless "Create" method.
    /// This will be called to construct the component in the VS process.</item>
    /// <item>Public methods on in-proc components should be instance methods to ensure that they are
    /// marshalled properly and execute in the VS process. Static methods will execute in the process
    /// in which they are called.</item>
    /// </list>
    /// </summary>
    internal abstract class InProcComponent : MarshalByRefObject
    {
        private static JoinableTaskFactory? _joinableTaskFactory;

        protected InProcComponent()
        {
            // Make sure SVsExtensionManager loads before trying to execute any test commands
            JoinableTaskFactory.Run(async () =>
            {
                // Workaround for deadlock loading ExtensionManagerPackage prior to
                // https://devdiv.visualstudio.com/DevDiv/_git/VSExtensibility/pullrequest/381506
                await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsExtensionManager));
            });
        }

        private static Dispatcher CurrentApplicationDispatcher
            => Application.Current.Dispatcher;

        protected static JoinableTaskFactory JoinableTaskFactory
        {
            get
            {
                if (_joinableTaskFactory is null)
                {
#pragma warning disable RS0030 // Do not used banned APIs (this code only runs in integration tests)
                    Interlocked.CompareExchange(ref _joinableTaskFactory, ThreadHelper.JoinableTaskFactory.WithPriority(CurrentApplicationDispatcher, DispatcherPriority.Background), null);
#pragma warning restore RS0030 // Do not used banned APIs
                }

                return _joinableTaskFactory;
            }
        }

        protected static void InvokeOnUIThread(Action<CancellationToken> action)
        {
            using var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);
            var operation = JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                action(cancellationTokenSource.Token);
            });

            operation.Task.Wait(cancellationTokenSource.Token);
        }

        protected static T InvokeOnUIThread<T>(Func<CancellationToken, T> action)
        {
            using var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);
            var operation = JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                return action(cancellationTokenSource.Token);
            });

            operation.Task.Wait(cancellationTokenSource.Token);
            return operation.Task.Result;
        }

        protected static TInterface GetGlobalService<TService, TInterface>()
            where TService : class
            where TInterface : class
        => InvokeOnUIThread(cancellationToken => (TInterface)ServiceProvider.GlobalProvider.GetService(typeof(TService)));

        protected static TService GetComponentModelService<TService>()
            where TService : class
         => InvokeOnUIThread(cancellationToken => GetComponentModel().GetService<TService>());

        protected static TestWaitingService GetWaitingService()
            => new(GetComponentModel().DefaultExportProvider.GetExport<AsynchronousOperationListenerProvider>().Value);

        protected static DTE GetDTE()
            => GetGlobalService<SDTE, DTE>();

        protected static IComponentModel GetComponentModel()
            => GetGlobalService<SComponentModel, IComponentModel>();

        protected static bool IsCommandAvailable(string commandName)
            => GetDTE().Commands.Item(commandName).IsAvailable;

        protected static void ExecuteCommand(string commandName, string args = "")
        {
            var task = Task.Run(() => GetDTE().ExecuteCommand(commandName, args));
            task.Wait(Helper.HangMitigatingTimeout);
        }

        /// <summary>
        /// Waiting for the application to 'idle' means that it is done pumping messages (including WM_PAINT).
        /// </summary>
        protected static void WaitForApplicationIdle(TimeSpan timeout)
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => CurrentApplicationDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Wait(timeout);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        protected static void WaitForSystemIdle()
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        // Ensure InProcComponents live forever
        public override object? InitializeLifetimeService() => null;
    }
}
