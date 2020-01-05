// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
