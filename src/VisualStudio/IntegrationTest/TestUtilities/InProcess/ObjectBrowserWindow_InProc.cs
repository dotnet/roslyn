// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ObjectBrowserWindow_InProc : InProcComponent
    {
        public static ObjectBrowserWindow_InProc Create() => new ObjectBrowserWindow_InProc();

        public bool CloseWindow()
        {
            return InvokeOnUIThread(cancellationToken =>
            {
                var uiShell = GetGlobalService<SVsUIShell, IVsUIShell>();
                if (ErrorHandler.Failed(uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, new Guid(ToolWindowGuids.ObjectBrowser), out var frame)))
                {
                    return false;
                }

                ErrorHandler.ThrowOnFailure(frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
                return true;
            });
        }
    }
}
