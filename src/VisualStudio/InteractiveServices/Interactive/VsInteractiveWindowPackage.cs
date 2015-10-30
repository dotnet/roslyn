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
        protected abstract string LanguageName { get; }
        protected abstract string ProjectKind { get; }

        protected abstract void InitializeMenuCommands(OleMenuCommandService menuCommandService);
        protected abstract CommandID GetResetInteractiveFromProjectCommandID();

        protected abstract string CreateReference(string referenceName);
        protected abstract string CreateImport(string namespaceName);
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
            InitializeResetInteractiveFromProjectCommand(menuCommandService);
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

        private void InitializeResetInteractiveFromProjectCommand(OleMenuCommandService menuCommandService)
        {
            var resetInteractiveFromProjectCommand = new OleMenuCommand(
                (sender, args) =>
                {
                    var resetInteractive = new ResetInteractive(
                        (DTE)this.GetService(typeof(SDTE)),
                        _componentModel,
                        (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection)),
                        (IVsSolutionBuildManager)this.GetService(typeof(SVsSolutionBuildManager)),
                        CreateReference,
                        CreateImport);

                    var vsInteractiveWindow = _interactiveWindowProvider.Open(instanceId: 0, focus: true);

                    resetInteractive.Execute(vsInteractiveWindow, LanguageName + " Interactive");
                },
                GetResetInteractiveFromProjectCommandID());

            resetInteractiveFromProjectCommand.Supported = true;

            resetInteractiveFromProjectCommand.BeforeQueryStatus += (_, __) =>
            {
                var project = GetActiveProject();
                var available = project != null && project.Kind == ProjectKind;

                resetInteractiveFromProjectCommand.Enabled = available;
                resetInteractiveFromProjectCommand.Supported = available;
                resetInteractiveFromProjectCommand.Visible = available;
            };

            menuCommandService.AddCommand(resetInteractiveFromProjectCommand);
        }

        private EnvDTE.Project GetActiveProject()
        {
            var monitorSelection = (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection));

            IntPtr hierarchyPointer = IntPtr.Zero;
            IntPtr selectionContainerPointer = IntPtr.Zero;

            try
            {
                uint itemid;
                IVsMultiItemSelect multiItemSelect;

                Marshal.ThrowExceptionForHR(
                    monitorSelection.GetCurrentSelection(
                        out hierarchyPointer,
                        out itemid,
                        out multiItemSelect,
                        out selectionContainerPointer));

                if (itemid != (uint)VSConstants.VSITEMID.Root)
                {
                    return null;
                }

                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPointer) as IVsHierarchy;
                if (hierarchy == null)
                {
                    return null;
                }

                object extensibilityObject;
                Marshal.ThrowExceptionForHR(
                    hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out extensibilityObject));

                return extensibilityObject as EnvDTE.Project;
            }
            finally
            {
                if (hierarchyPointer != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPointer);
                }

                if (selectionContainerPointer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPointer);
                }
            }
        }
    }
}
