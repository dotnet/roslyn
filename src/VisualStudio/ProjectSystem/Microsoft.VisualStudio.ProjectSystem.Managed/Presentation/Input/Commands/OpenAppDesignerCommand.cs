// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Presentation.Input.Commands
{
    // Opens the AppDesigner on double-click or ENTER on the AppDesigner folder
    [ProjectCommand(CommandGroup.UIHierarchyWindow, CommandId.UIHierarchyWindowDoubleClick, CommandId.UIHierarchyWindowEnterKey)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]  // TODO: We need an AppDesigner capability
    internal class OpenAppDesignerCommand : SingleNodeProjectCommandBase
    {
        private readonly IUnconfiguredProjectVsServices _projectServices;
        private readonly IThreadHandling _threadHandling;

        [ImportingConstructor]
        public OpenAppDesignerCommand(IUnconfiguredProjectVsServices projectServices, IThreadHandling threadHandling)
        {
            Requires.NotNull(projectServices, nameof(projectServices));
            Requires.NotNull(threadHandling, nameof(threadHandling));

            _projectServices = projectServices;
            _threadHandling = threadHandling;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (node.Capabilities.Contains(ProjectTreeCapabilities.AppDesignerFolder))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (node.Capabilities.Contains(ProjectTreeCapabilities.AppDesignerFolder))
            {
                await _threadHandling.SwitchToUIThread();

                Guid projectDesignerGuid;
                HResult hr = _projectServices.Hierarchy.GetGuidProperty(HierarchyId.Root, (int)__VSHPROPID2.VSHPROPID_ProjectDesignerEditor, out projectDesignerGuid);
                if (hr.Failed)
                    throw hr.Exception;

                IVsWindowFrame windowFrame;
                hr = _projectServices.Project.OpenItemWithSpecific(VSConstants.VSITEMID_ROOT, 0, ref projectDesignerGuid, "", VSConstants.LOGVIEWID_Primary, (IntPtr)(-1), out windowFrame);
                if (hr.Failed)
                    throw hr.Exception;

                hr = windowFrame.Show();
                if (hr.Failed)
                    throw hr.Exception;

                return true;
            }

            return false;
        }
    }
}
