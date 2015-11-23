// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input.Commands
{
    internal abstract class OpenProjectDesignerCommandBase : SingleNodeProjectCommandBase
    {
        private readonly IProjectDesignerService _designerService;

        protected OpenProjectDesignerCommandBase(IProjectDesignerService designerService)
        {
            Requires.NotNull(designerService, nameof(designerService));

            _designerService = designerService;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            // We assume that if the AppDesignerTreeModifier marked an AppDesignerFolder, that we must support the Project Designer
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
                await _designerService.ShowProjectDesignerAsync()
                                      .ConfigureAwait(false);
                return true;
            }

            return false;
        }
    }
}
