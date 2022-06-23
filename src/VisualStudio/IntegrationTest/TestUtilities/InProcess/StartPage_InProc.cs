// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using vsStartUp = EnvDTE.vsStartUp;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class StartPage_InProc : InProcComponent
    {
        // Values come from Tools -> Options -> Environment -> Startup -> At startup
        private const int ShowEmptyEnvironment = (int)vsStartUp.vsStartUpEmptyEnvironment;
        private const int ShowStartPage = 5;

        public static StartPage_InProc Create()
            => new StartPage_InProc();

        public void SetEnabled(bool enabled)
        {
            InvokeOnUIThread(cancellationToken =>
            {
                var property = GetDTE().get_Properties("Environment", "Startup").Item("OnStartUp");
                property.Value = enabled ? ShowStartPage : ShowEmptyEnvironment;
            });
        }

        public bool CloseWindow()
        {
            return InvokeOnUIThread(cancellationToken =>
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
    }
}
