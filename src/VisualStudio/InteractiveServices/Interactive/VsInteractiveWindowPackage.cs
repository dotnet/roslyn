// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias core;

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    internal abstract partial class VsInteractiveWindowPackage<TVsInteractiveWindowProvider> : Package, IVsToolWindowFactory
        where TVsInteractiveWindowProvider : VsInteractiveWindowProvider
    {
        protected abstract void InitializeMenuCommands(OleMenuCommandService menuCommandService);

        protected abstract Guid LanguageServiceGuid { get; }
        protected abstract Guid ToolWindowId { get; }

        private IComponentModel _componentModel;
        private TVsInteractiveWindowProvider _interactiveWindowProvider;

        protected override void Initialize()
        {
            base.Initialize();

            // Load the Roslyn package so that its FatalError handlers are hooked up.
            IVsPackage roslynPackage;
            var shell = (IVsShell)this.GetService(typeof(SVsShell));
            shell.LoadPackage(Guids.RoslynPackageId, out roslynPackage);
            Debug.Assert(core::Microsoft.CodeAnalysis.ErrorReporting.FatalError.Handler != null);
            Debug.Assert(core::Microsoft.CodeAnalysis.ErrorReporting.FatalError.NonFatalHandler != null);
            Debug.Assert(core::Microsoft.CodeAnalysis.Internal.Log.Logger.GetLogger() != null);

            // Explicitly set up FatalError handlers for the InteractiveWindowPackage.
            // NB: Microsoft.CodeAnalysis.ErrorReporting.FatalError (InteractiveWindow), not 
            // Microsoft.CodeAnalysis.FatalError (compiler) or core::Microsoft.CodeAnalysis.ErrorReporting.FatalError (workspaces).
            Microsoft.CodeAnalysis.ErrorReporting.FatalError.Handler = core::Microsoft.CodeAnalysis.FailFast.OnFatalException;
            Microsoft.CodeAnalysis.ErrorReporting.FatalError.NonFatalHandler = WatsonReporter.Report;

            _componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _interactiveWindowProvider = _componentModel.DefaultExportProvider.GetExportedValue<TVsInteractiveWindowProvider>();
            KnownUIContexts.ShellInitializedContext.WhenActivated(() =>
                _componentModel.GetService<HACK_ThemeColorFixer>());

            var menuCommandService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            InitializeMenuCommands(menuCommandService);
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
