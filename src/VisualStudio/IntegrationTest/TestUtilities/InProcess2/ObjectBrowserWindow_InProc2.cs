// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class ObjectBrowserWindow_InProc2 : InProcComponent2
    {
        public ObjectBrowserWindow_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task<bool> CloseWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var uiShell = await GetGlobalServiceAsync<SVsUIShell, IVsUIShell>();
            if (ErrorHandler.Failed(uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, new Guid(ToolWindowGuids.ObjectBrowser), out var frame)))
            {
                return false;
            }

            ErrorHandler.ThrowOnFailure(frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
            return true;
        }
    }
}
