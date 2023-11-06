// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal partial class VisualStudio_InProc : InProcComponent
    {
        private VisualStudio_InProc() { }

        public static VisualStudio_InProc Create()
            => new VisualStudio_InProc();

        public new void WaitForApplicationIdle(TimeSpan timeout)
            => InProcComponent.WaitForApplicationIdle(timeout);

        public new void WaitForSystemIdle()
            => InProcComponent.WaitForSystemIdle();

        public new bool IsCommandAvailable(string commandName)
            => InProcComponent.IsCommandAvailable(commandName);

        public new void ExecuteCommand(string commandName, string args = "")
            => InProcComponent.ExecuteCommand(commandName, args);

        public void ActivateMainWindow()
            => InvokeOnUIThread(cancellationToken =>
            {
                var dte = GetDTE();

                var activeVisualStudioWindow = dte.ActiveWindow.HWnd;
                Debug.WriteLine($"DTE.ActiveWindow.HWnd = {activeVisualStudioWindow}");
                if (activeVisualStudioWindow != IntPtr.Zero)
                {
                    if (IntegrationHelper.TrySetForegroundWindow(activeVisualStudioWindow))
                        return;
                }

                activeVisualStudioWindow = dte.MainWindow.HWnd;
                Debug.WriteLine($"DTE.MainWindow.HWnd = {activeVisualStudioWindow}");
                if (!IntegrationHelper.TrySetForegroundWindow(activeVisualStudioWindow))
                    throw new InvalidOperationException("Failed to set the foreground window.");
            });

        public void Quit()
            => GetDTE().Quit();
    }
}
