// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

#if false
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
#endif
    }
}
