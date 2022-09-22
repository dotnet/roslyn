// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interactive
{
    internal abstract class AbstractResetInteractiveMenuCommand
    {
        protected abstract string ProjectKind { get; }

        protected abstract CommandID GetResetInteractiveFromProjectCommandID();

        private readonly OleMenuCommandService _menuCommandService;
        private readonly IVsMonitorSelection _monitorSelection;
        private readonly IComponentModel _componentModel;
        private readonly IThreadingContext _threadingContext;
        private readonly string _contentType;

        private readonly Lazy<IResetInteractiveCommand> _resetInteractiveCommand;

        private Lazy<IResetInteractiveCommand> ResetInteractiveCommand => _resetInteractiveCommand;

        public AbstractResetInteractiveMenuCommand(
            string contentType,
            OleMenuCommandService menuCommandService,
            IVsMonitorSelection monitorSelection,
            IComponentModel componentModel,
            IThreadingContext threadingContext)
        {
            _contentType = contentType;
            _menuCommandService = menuCommandService;
            _monitorSelection = monitorSelection;
            _componentModel = componentModel;
            _threadingContext = threadingContext;
            _resetInteractiveCommand = _componentModel.DefaultExportProvider
                .GetExports<IResetInteractiveCommand, ContentTypeMetadata>()
                .Where(resetInteractiveService => resetInteractiveService.Metadata.ContentTypes.Contains(_contentType))
                .SingleOrDefault();
        }

        internal async Task InitializeResetInteractiveFromProjectCommandAsync()
        {
            var resetInteractiveFromProjectCommand = new OleMenuCommand(
                (sender, args) =>
                {
                    ResetInteractiveCommand.Value.ExecuteResetInteractive();
                },
                GetResetInteractiveFromProjectCommandID());

            resetInteractiveFromProjectCommand.Supported = true;

            resetInteractiveFromProjectCommand.BeforeQueryStatus += (_, __) =>
            {
                GetActiveProject(out var project, out var frameworkName);
                var available = ResetInteractiveCommand != null
                    && project != null && project.Kind == ProjectKind
                    && frameworkName != null && frameworkName.Identifier == ".NETFramework";

                resetInteractiveFromProjectCommand.Enabled = available;
                resetInteractiveFromProjectCommand.Supported = available;
                resetInteractiveFromProjectCommand.Visible = available;
            };

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            _menuCommandService.AddCommand(resetInteractiveFromProjectCommand);
        }

        private bool GetActiveProject(out EnvDTE.Project project, out FrameworkName frameworkName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            project = null;
            frameworkName = null;

            var hierarchyPointer = IntPtr.Zero;
            var selectionContainerPointer = IntPtr.Zero;

            try
            {
                Marshal.ThrowExceptionForHR(
                    _monitorSelection.GetCurrentSelection(
                        out hierarchyPointer,
                        out var itemid,
                        out var multiItemSelect,
                        out selectionContainerPointer));

                if (itemid != (uint)VSConstants.VSITEMID.Root)
                {
                    return false;
                }

                if (Marshal.GetObjectForIUnknown(hierarchyPointer) is not IVsHierarchy hierarchy)
                {
                    return false;
                }

                Marshal.ThrowExceptionForHR(
                    hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out var extensibilityObject));
                Marshal.ThrowExceptionForHR(
                    hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID3.VSHPROPID_TargetFrameworkVersion, out var targetFrameworkVersion));
                Marshal.ThrowExceptionForHR(
                    hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker, out var targetFrameworkMonikerObject));

                var targetFrameworkMoniker = targetFrameworkMonikerObject as string;
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
