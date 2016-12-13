// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class VisualStudio_InProc : InProcComponent
    {
        private VisualStudio_InProc() { }

        public static VisualStudio_InProc Create()
            => new VisualStudio_InProc();

        new public void WaitForApplicationIdle()
            => InProcComponent.WaitForApplicationIdle();

        new public void WaitForSystemIdle()
            => InProcComponent.WaitForSystemIdle();

        new public bool IsCommandAvailable(string commandName)
            => InProcComponent.IsCommandAvailable(commandName);

        new public void ExecuteCommand(string commandName, string args = "")
            => InProcComponent.ExecuteCommand(commandName, args);

        public void ActivateMainWindow()
            => InvokeOnUIThread(() => {
                var dte = GetDTE();

                var activeVisualStudioWindow = (IntPtr)dte.ActiveWindow.HWnd;
                Debug.WriteLine($"DTE.ActiveWindow.HWnd = {activeVisualStudioWindow}");

                if (activeVisualStudioWindow == IntPtr.Zero)
                {
                    activeVisualStudioWindow = (IntPtr)dte.MainWindow.HWnd;
                    Debug.WriteLine($"DTE.MainWindow.HWnd = {activeVisualStudioWindow}");
                }

                IntegrationHelper.SetForegroundWindow(activeVisualStudioWindow);
            });

        public void Quit()
            => GetDTE().Quit();
    }
}
