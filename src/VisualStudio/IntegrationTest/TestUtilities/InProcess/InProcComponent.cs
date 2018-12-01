// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
        protected VisualStudioHost _visualStudioHost;

        protected InProcComponent(VisualStudioHost visualStudioHost)
        {
            _visualStudioHost = visualStudioHost;
        }

        private static Dispatcher CurrentApplicationDispatcher
            => Application.Current.Dispatcher;

        protected static void BeginInvokeOnUIThread(Action action)
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => CurrentApplicationDispatcher.BeginInvoke(action, DispatcherPriority.Background);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        protected static void InvokeOnUIThread(Action action)
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => CurrentApplicationDispatcher.Invoke(action, DispatcherPriority.Background);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        protected T InvokeOnUIThread<T>(Func<T> action)
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            => _visualStudioHost.InvokeOnUIThread<T>(action);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

        protected TInterface GetGlobalService<TService, TInterface>()
            where TService : class
            where TInterface : class
            => (TInterface) _visualStudioHost.ServiceProvider.GetService(typeof(TService));
        //=> InvokeOnUIThread(() => (TInterface)_visualStudioHost.ServiceProvider.GetService(typeof(TService)));

        protected TService GetComponentModelService<TService>()
            where TService : class
            => GetComponentModel().GetService<TService>();
         //=> InvokeOnUIThread(() => GetComponentModel().GetService<TService>());

        protected DTE GetDTE()
            => _visualStudioHost.Dte;

        protected IComponentModel GetComponentModel()
            => GetGlobalService<SComponentModel, IComponentModel>();

        protected bool IsCommandAvailable(string commandName)
            => GetDTE().Commands.Item(commandName).IsAvailable;

        protected void ExecuteCommand(string commandName, string args = "")
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
        public override object InitializeLifetimeService() => null;
    }
}
