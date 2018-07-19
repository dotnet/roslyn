// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.Shell.Interop;
    using Xunit.Harness;
    using DTE = EnvDTE.DTE;

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
        protected InProcComponent()
        {
        }

        private static Dispatcher CurrentApplicationDispatcher
            => Application.Current.Dispatcher;

        protected static void BeginInvokeOnUIThread(Action action)
            => CurrentApplicationDispatcher.BeginInvoke(action, DispatcherPriority.Background);

        protected static void InvokeOnUIThread(Action action)
            => CurrentApplicationDispatcher.Invoke(action, DispatcherPriority.Background);

        protected static T InvokeOnUIThread<T>(Func<T> action)
            => CurrentApplicationDispatcher.Invoke(action, DispatcherPriority.Background);

        protected static TInterface GetGlobalService<TService, TInterface>()
            where TService : class
            where TInterface : class
            => InvokeOnUIThread(() => (TInterface)new OleServiceProvider(GetDTE()).GetService(typeof(TService)));

        protected static DTE GetDTE()
            => InvokeOnUIThread(() => (DTE)GlobalServiceProvider.ServiceProvider.GetService(typeof(SDTE)));

        protected static bool IsCommandAvailable(string commandName)
            => GetDTE().Commands.Item(commandName).IsAvailable;

        protected static void ExecuteCommand(string commandName, string args = "")
            => GetDTE().ExecuteCommand(commandName, args);

        /// <summary>
        /// Waiting for the application to 'idle' means that it is done pumping messages (including WM_PAINT).
        /// </summary>
        protected static void WaitForApplicationIdle()
            => CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

        protected static void WaitForSystemIdle()
            => CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);

        // Ensure InProcComponents live forever
        public override object InitializeLifetimeService() => null;
    }
}
