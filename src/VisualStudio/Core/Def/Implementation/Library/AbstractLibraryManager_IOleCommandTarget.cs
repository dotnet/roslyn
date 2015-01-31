// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library
{
    internal partial class AbstractLibraryManager : IOleCommandTarget
    {
        protected virtual bool TryQueryStatus(Guid commandGroup, uint commandId, ref OLECMDF commandFlags)
        {
            return false;
        }

        protected virtual bool TryExec(Guid commandGroup, uint commandId)
        {
            return false;
        }

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
}
