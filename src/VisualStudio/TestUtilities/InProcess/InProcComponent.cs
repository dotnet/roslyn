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
    /// Base class for all components that run inside of the Visual Studio process. Every in-proc component
    /// must provide a public, static, parameterless "Create" method.
    /// </summary>
    public abstract class InProcComponent : MarshalByRefObject
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
