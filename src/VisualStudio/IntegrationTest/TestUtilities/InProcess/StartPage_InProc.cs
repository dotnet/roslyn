// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        // These values apply to Visual Studio 2019
        private const int VS2019ShowStartWindow = 13;
        private const int VS2019ShowEmptyEnvironment = 10;

        public static StartPage_InProc Create()
            => new StartPage_InProc();

        public bool IsEnabled()
        {
            return InvokeOnUIThread(cancellationToken =>
            {
                var property = GetProperty();
                if (new Version(property.DTE.Version).Major == 16)
                {
                    return (int)property.Value == VS2019ShowStartWindow;
                }
                else
                {
                    return (int)property.Value == ShowStartPage;
                }
            });
        }

        public void SetEnabled(bool enabled)
        {
            InvokeOnUIThread(cancellationToken =>
            {
                var property = GetProperty();
                if (new Version(property.DTE.Version).Major == 16)
                {
                    property.Value = enabled ? VS2019ShowStartWindow : VS2019ShowEmptyEnvironment;
                }
                else
                {
                    property.Value = enabled ? ShowStartPage : ShowEmptyEnvironment;
                }
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

        private EnvDTE.Property GetProperty()
            => GetDTE().Properties["Environment", "Startup"].Item("OnStartUp");
    }
}
