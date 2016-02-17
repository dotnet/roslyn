// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.VisualStudio.Services.Interactive;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interactive
{
    internal abstract class AbstractResetInteractiveMenuCommand
    {
        protected abstract string ProjectKind { get; }

        protected abstract CommandID GetResetInteractiveFromProjectCommandID();

        private readonly OleMenuCommandService _menuCommandService;
        private readonly IVsMonitorSelection _monitorSelection;
        private readonly IComponentModel _componentModel;
        private readonly string _contentType;

        private AbstractResetInteractiveCommand ResetInteractiveService
        {
            get
            {
                return _componentModel.DefaultExportProvider
                    .GetExports<AbstractResetInteractiveCommand, ContentTypeMetadata>()
                    .Where(resetInteractiveService => resetInteractiveService.Metadata.ContentTypes.Contains(_contentType))
                    .Single().Value;
            }
        }

        public AbstractResetInteractiveMenuCommand(
            string contentType,
            OleMenuCommandService menuCommandService,
            IVsMonitorSelection monitorSelection,
            IComponentModel componentModel)
        {
            _contentType = contentType;
            _menuCommandService = menuCommandService;
            _monitorSelection = monitorSelection;
            _componentModel = componentModel;
        }

        internal void InitializeResetInteractiveFromProjectCommand()
        {
            var resetInteractiveFromProjectCommand = new OleMenuCommand(
                (sender, args) =>
                {
                    ResetInteractiveService.ExecuteResetInteractive();
                },
                GetResetInteractiveFromProjectCommandID());

            resetInteractiveFromProjectCommand.Supported = true;

            resetInteractiveFromProjectCommand.BeforeQueryStatus += (_, __) =>
            {
                EnvDTE.Project project;
                FrameworkName frameworkName;
                GetActiveProject(out project, out frameworkName);
                var targetFramework = project.Properties.Item("TargetFramework");
                var available = project != null && project.Kind == ProjectKind
                    && frameworkName != null && frameworkName.Identifier == ".NETFramework";

                resetInteractiveFromProjectCommand.Enabled = available;
                resetInteractiveFromProjectCommand.Supported = available;
                resetInteractiveFromProjectCommand.Visible = available;
            };

            _menuCommandService.AddCommand(resetInteractiveFromProjectCommand);
        }

        private bool GetActiveProject(out EnvDTE.Project project, out FrameworkName frameworkName)
        {
            project = null;
            frameworkName = null;

            IntPtr hierarchyPointer = IntPtr.Zero;
            IntPtr selectionContainerPointer = IntPtr.Zero;

            try
            {
                uint itemid;
                IVsMultiItemSelect multiItemSelect;

                Marshal.ThrowExceptionForHR(
                    _monitorSelection.GetCurrentSelection(
                        out hierarchyPointer,
                        out itemid,
                        out multiItemSelect,
                        out selectionContainerPointer));

                if (itemid != (uint)VSConstants.VSITEMID.Root)
                {
                    return false;
                }

                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPointer) as IVsHierarchy;
                if (hierarchy == null)
                {
                    return false;
                }

                object extensibilityObject;
                object targetFrameworkVersion;
                object targetFrameworkMonikerObject;
                Marshal.ThrowExceptionForHR(
                    hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out extensibilityObject));
                Marshal.ThrowExceptionForHR(
                    hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID3.VSHPROPID_TargetFrameworkVersion, out targetFrameworkVersion));
                Marshal.ThrowExceptionForHR(
                    hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker, out targetFrameworkMonikerObject));

                string targetFrameworkMoniker = targetFrameworkMonikerObject as string;
                frameworkName = new System.Runtime.Versioning.FrameworkName(targetFrameworkMoniker);

                project = extensibilityObject as EnvDTE.Project;
                return true;
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
