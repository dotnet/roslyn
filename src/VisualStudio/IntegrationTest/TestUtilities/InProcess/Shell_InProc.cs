// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Shell_InProc : InProcComponent
    {
        public static Shell_InProc Create() => new Shell_InProc();

        public string GetActiveWindowCaption()
            => InvokeOnUIThread(() => GetDTE().ActiveWindow.Caption);

        public IntPtr GetHWnd()
            => (IntPtr)GetDTE().MainWindow.HWnd;

        public bool IsActiveTabProvisional()
            => InvokeOnUIThread(() =>
            {
                var shellMonitorSelection = GetGlobalService<SVsShellMonitorSelection, IVsMonitorSelection>();
                if (!ErrorHandler.Succeeded(shellMonitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var windowFrameObject)))
                {
                    throw new InvalidOperationException("Tried to get the active document frame but no documents were open.");
                }

                var windowFrame = (IVsWindowFrame)windowFrameObject;
                if (!ErrorHandler.Succeeded(windowFrame.GetProperty((int)VsFramePropID.IsProvisional, out var isProvisionalObject)))
                {
                    throw new InvalidOperationException("The active window frame did not have an 'IsProvisional' property.");
                }

                return (bool)isProvisionalObject;
            });
    }
}
