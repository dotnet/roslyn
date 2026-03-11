// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Threading;
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
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => CurrentApplicationDispatcher.BeginInvoke(action, DispatcherPriority.Background);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        protected static void InvokeOnUIThread(Action action)
        {
            if (CurrentApplicationDispatcher.CheckAccess())
            {
                // Invoke the action directly
                action();
            }
            else
            {
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                CurrentApplicationDispatcher.Invoke(action, DispatcherPriority.Background);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            }
        }

        protected static T InvokeOnUIThread<T>(Func<T> action)
        {
            if (CurrentApplicationDispatcher.CheckAccess())
            {
                // Invoke the action directly
                return action();
            }
            else
            {
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                return CurrentApplicationDispatcher.Invoke(action, DispatcherPriority.Background);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            }
        }

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
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        protected static void WaitForSystemIdle()
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        // Ensure InProcComponents live forever
        public override object? InitializeLifetimeService() => null;
    }
}
