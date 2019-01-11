// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class StartPage_InProc : InProcComponent
    {
        // Values come from Tools -> Options -> Environment -> Startup -> At startup
        private const int ShowEmptyEnvironment = 4;
        private const int ShowStartPage = 5;

        public static StartPage_InProc Create()
            => new StartPage_InProc();

        public bool IsEnabled()
        {
            return InvokeOnUIThread(() =>
            {
                var property = GetProperty();
                return (int)property.Value == ShowStartPage;
            });
        }

        public void SetEnabled(bool enabled)
        {
            InvokeOnUIThread(() =>
            {
                var property = GetProperty();
                property.Value = enabled ? ShowStartPage : ShowEmptyEnvironment;
            });
        }

        public bool CloseWindow()
        {
            return InvokeOnUIThread(() =>
            {
                var uiShell = GetGlobalService<SVsUIShell, IVsUIShell>();
                if (ErrorHandler.Failed(uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, new Guid(ToolWindowGuids80.StartPage), out var frame)))
                {
                    return false;
                }

                ErrorHandler.ThrowOnFailure(frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
                return true;
            });
        }

        private EnvDTE.Property GetProperty()
            => GetDTE().Properties["Environment", "Startup"].Item("OnStartUp");
    }
}
