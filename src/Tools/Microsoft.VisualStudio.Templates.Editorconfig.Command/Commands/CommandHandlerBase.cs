// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

internal abstract class CommandHandlerBase : IWorkspaceCommandHandler
{
    public virtual int Priority => 100;
    public virtual bool IgnoreOnMultiselect => true;

    protected abstract CommandID Id { get; }

    protected abstract bool QueryStatus(List<WorkspaceVisualNodeBase> selection);
    protected abstract bool Execute(List<WorkspaceVisualNodeBase> selection);

    public int Exec(List<WorkspaceVisualNodeBase> selection, Guid cmdGroup, uint cmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (IsSupportedCommand(cmdGroup, cmdId) && Execute(selection))
        {
            return VSConstants.S_OK;
        }

        return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
    }

    public bool QueryStatus(List<WorkspaceVisualNodeBase> selection, Guid cmdGroup, uint cmdId, ref uint cmdf, ref string customTitle)
    {
        if (IsSupportedCommand(cmdGroup, cmdId))
        {
            if (QueryStatus(selection))
            {
                cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
            }
            else
            {
                cmdf = (uint)OLECMDF.OLECMDF_INVISIBLE;
            }

            return true;
        }

        return false;
    }

    private bool IsSupportedCommand(Guid cmdGroup, uint cmdId)
        => cmdGroup == Id.Guid && cmdId == Id.ID;
}

