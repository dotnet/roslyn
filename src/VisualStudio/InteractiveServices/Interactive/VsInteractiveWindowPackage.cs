// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias core;

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    internal abstract partial class VsInteractiveWindowPackage<TVsInteractiveWindowProvider> : AsyncPackage, IVsToolWindowFactory
        where TVsInteractiveWindowProvider : VsInteractiveWindowProvider
    {
        protected abstract void InitializeMenuCommands(OleMenuCommandService menuCommandService);

        protected abstract Guid LanguageServiceGuid { get; }
        protected abstract Guid ToolWindowId { get; }

        private IComponentModel _componentModel;
        private TVsInteractiveWindowProvider _interactiveWindowProvider;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // Load the Roslyn package so that its FatalError handlers are hooked up.
            var shell = (IVsShell)await GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
            shell.LoadPackage(Guids.RoslynPackageId, out var roslynPackage);
            
            // Explicitly set up FatalError handlers for the InteractiveWindowPackage.
            SetErrorHandlers(typeof(IInteractiveWindow).Assembly);
            SetErrorHandlers(typeof(IVsInteractiveWindow).Assembly);

            _componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
            _interactiveWindowProvider = _componentModel.DefaultExportProvider.GetExportedValue<TVsInteractiveWindowProvider>();

            var menuCommandService = (OleMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);
            InitializeMenuCommands(menuCommandService);
        }

        private static void SetErrorHandlers(Assembly assembly)
        {
            Debug.Assert(core::Microsoft.CodeAnalysis.ErrorReporting.FatalError.Handler != null);
            Debug.Assert(core::Microsoft.CodeAnalysis.ErrorReporting.FatalError.NonFatalHandler != null);
            Debug.Assert(core::Microsoft.CodeAnalysis.Internal.Log.Logger.GetLogger() != null);

            var type = assembly.GetType("Microsoft.VisualStudio.InteractiveWindow.FatalError", throwOnError: true).GetTypeInfo();

            var handlerSetter = type.GetDeclaredMethod("set_Handler");
            var nonFatalHandlerSetter = type.GetDeclaredMethod("set_NonFatalHandler");

            handlerSetter.Invoke(null, new object[] { new Action<Exception>(core::Microsoft.CodeAnalysis.FailFast.OnFatalException) });
            nonFatalHandlerSetter.Invoke(null, new object[] { new Action<Exception>(WatsonReporter.Report) });
        }

        protected TVsInteractiveWindowProvider InteractiveWindowProvider
        {
            get { return _interactiveWindowProvider; }
        }

        /// <summary>
        /// When a VSPackage supports multi-instance tool windows, each window uses the same rguidPersistenceSlot.
        /// The dwToolWindowId parameter is used to differentiate between the various instances of the tool window.
        /// </summary>
        int IVsToolWindowFactory.CreateToolWindow(ref Guid rguidPersistenceSlot, uint id)
        {
            if (rguidPersistenceSlot == ToolWindowId)
            {
                _interactiveWindowProvider.Create((int)id);
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }
    }
}
