// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal partial class AbstractLibraryManager : IOleCommandTarget
{
    protected virtual bool TryQueryStatus(Guid commandGroup, uint commandId, ref OLECMDF commandFlags)
        => false;

    protected virtual bool TryExec(Guid commandGroup, uint commandId)
        => false;

    int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        if (TryExec(pguidCmdGroup, nCmdID))
        {
            return VSConstants.S_OK;
        }

        return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
    }

    int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
        if (cCmds != 1)
        {
            return VSConstants.E_UNEXPECTED;
        }

        var flags = (OLECMDF)prgCmds[0].cmdf;
        if (TryQueryStatus(pguidCmdGroup, prgCmds[0].cmdID, ref flags))
        {
            prgCmds[0].cmdf = (uint)flags;
            return VSConstants.S_OK;
        }

        return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
    }
}
