using System;
using System.Diagnostics;

namespace Roslyn.VisualStudio.Test.Utilities.InProcess
{
    internal class VisualStudio_InProc : InProcComponent
    {
        private VisualStudio_InProc() { }

        public static VisualStudio_InProc Create()
        {
            return new VisualStudio_InProc();
        }

        new public void WaitForApplicationIdle()
        {
            InProcComponent.WaitForApplicationIdle();
        }

        new public void WaitForSystemIdle()
        {
            InProcComponent.WaitForSystemIdle();
        }

        new public bool IsCommandAvailable(string commandName)
        {
            return InProcComponent.IsCommandAvailable(commandName);
        }

        new public void ExecuteCommand(string commandName, string args = "")
        {
            InProcComponent.ExecuteCommand(commandName, args);
        }

        public void ActivateMainWindow()
        {
            InvokeOnUIThread(() =>
            {
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
        }

        public void Quit()
        {
            GetDTE().Quit();
        }
    }
}
