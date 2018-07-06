// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class Shell_InProc2 : InProcComponent2
    {
        public Shell_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task<string> GetActiveWindowCaptionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            return (await GetDTEAsync()).ActiveWindow.Caption;
        }

        public async Task<bool> IsActiveTabProvisionalAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var shellMonitorSelection = await GetGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>();
            if (!ErrorHandler.Succeeded(shellMonitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var windowFrameObject)))
            {
                throw new InvalidOperationException("Tried to get the active document frame but no documents were open.");
            }

            var windowFrame = (IVsWindowFrame)windowFrameObject;
            if (!ErrorHandler.Succeeded(windowFrame.GetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, out var isProvisionalObject)))
            {
                throw new InvalidOperationException("The active window frame did not have an 'IsProvisional' property.");
            }

            return (bool)isProvisionalObject;
        }
    }
}
