// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

internal abstract partial class AbstractOleCommandTarget
{
    public virtual int Exec(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut)
    {
        try
        {
            this.CurrentlyExecutingCommand = commandId;

            // If we don't have a subject buffer, then that means we're outside our code and we should ignore it
            // Also, ignore the command if executeInformation indicates isn't meant to be executed. From env\msenv\core\cmdwin.cpp:
            //      To query the parameter type list of a command, we call Exec with 
            //      the LOWORD of nCmdexecopt set to OLECMDEXECOPT_SHOWHELP (instead of
            //      the more usual OLECMDEXECOPT_DODEFAULT), the HIWORD of nCmdexecopt
            //      set to VSCmdOptQueryParameterList, pvaIn set to NULL, and pvaOut 
            //      pointing to a VARIANT ready to receive the result BSTR.
            var shouldSkipCommand = executeInformation == (((uint)VsMenus.VSCmdOptQueryParameterList << 16) | (uint)OLECMDEXECOPT.OLECMDEXECOPT_SHOWHELP);
            if (shouldSkipCommand || GetSubjectBufferContainingCaret() == null)
            {
                return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                return ExecuteVisualStudio2000(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }
            else
            {
                return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }
        }
        finally
        {
            this.CurrentlyExecutingCommand = default;
        }
    }

    protected virtual int ExecuteVisualStudio2000(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut)
    {
        return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
    }
}
