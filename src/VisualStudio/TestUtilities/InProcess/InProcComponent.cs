// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Roslyn.VisualStudio.Test.Utilities.InProcess
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
        protected InProcComponent() { }

        private static Dispatcher CurrentApplicationDispatcher => Application.Current.Dispatcher;

        protected static void InvokeOnUIThread(Action action)
        {
            CurrentApplicationDispatcher.Invoke(action);
        }

        protected static T InvokeOnUIThread<T>(Func<T> action)
        {
            return CurrentApplicationDispatcher.Invoke(action);
        }

        protected static TInterface GetGlobalService<TService, TInterface>()
            where TService : class
            where TInterface : class
        {
            return InvokeOnUIThread(() =>
            {
                return (TInterface)ServiceProvider.GlobalProvider.GetService(typeof(TService));
            });
        }

        protected static TService GetComponentModelService<TService>()
            where TService : class
        {
            return InvokeOnUIThread(() =>
            {
                return GetComponentModel().GetService<TService>();
            });
        }

        protected static DTE GetDTE()
        {
            return GetGlobalService<SDTE, DTE>();
        }

        protected static IComponentModel GetComponentModel()
        {
            return GetGlobalService<SComponentModel, IComponentModel>();
        }

        protected static bool IsCommandAvailable(string commandName)
        {
            return GetDTE().Commands.Item(commandName).IsAvailable;
        }

        protected static void ExecuteCommand(string commandName, string args = "")
        {
            GetDTE().ExecuteCommand(commandName, args);
        }

        /// <summary>
        /// Waiting for the application to 'idle' means that it is done pumping messages (including WM_PAINT).
        /// </summary>
        protected static void WaitForApplicationIdle()
        {
            CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        protected static void WaitForSystemIdle()
        {
            CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);
        }
    }
}
