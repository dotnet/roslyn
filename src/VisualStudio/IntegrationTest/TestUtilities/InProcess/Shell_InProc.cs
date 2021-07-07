﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Shell_InProc : InProcComponent
    {
        public static Shell_InProc Create() => new Shell_InProc();

        public string GetVersion()
        {
            return InvokeOnUIThread(cancellationToken =>
            {
                var shell = GetGlobalService<SVsShell, IVsShell>();
                ErrorHandler.ThrowOnFailure(shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var version));
                return (string)version;
            });
        }

        public string GetActiveWindowCaption()
            => InvokeOnUIThread(cancellationToken => GetDTE().ActiveWindow.Caption);

        public IntPtr GetHWnd()
            => (IntPtr)GetDTE().MainWindow.HWnd;

        public bool IsActiveTabProvisional()
            => InvokeOnUIThread(cancellationToken =>
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

        public bool IsUIContextActive(Guid context)
        {
            return UIContext.FromUIContextGuid(context).IsActive;
        }
    }
}
