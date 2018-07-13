// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class StartPage_InProc2 : InProcComponent2
    {
        public StartPage_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task<bool> IsEnabledAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var property = await GetPropertyAsync();
            return (int)property.Value == 4;
        }

        public async Task SetEnabledAsync(bool enabled)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var property = await GetPropertyAsync();
            property.Value = enabled ? 5 : 4;
        }

        public async Task<bool> CloseWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var uiShell = await GetGlobalServiceAsync<SVsUIShell, IVsUIShell>();
            if (ErrorHandler.Failed(uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, new Guid(ToolWindowGuids80.StartPage), out var frame)))
            {
                return false;
            }

            ErrorHandler.ThrowOnFailure(frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
            return true;
        }

        private async Task<EnvDTE.Property> GetPropertyAsync()
        {
            return (await GetDTEAsync()).Properties["Environment", "Startup"].Item("OnStartUp");
        }
    }
}
