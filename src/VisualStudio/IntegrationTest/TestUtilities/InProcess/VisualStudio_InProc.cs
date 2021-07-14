// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using EnvDTE;
using EnvDTE80;

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

        public string[] GetAvailableCommands()
        {
            var result = new List<string>();
            var commands = GetDTE().Commands;
            foreach (Command command in commands)
            {
                try
                {
                    var commandName = command.Name;
                    if (command.IsAvailable)
                    {
                        result.Add(commandName);
                    }
                }
                finally { }
            }

            return result.ToArray();
        }

        public void ActivateMainWindow()
            => InvokeOnUIThread(cancellationToken =>
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

        public int GetErrorListErrorCount()
        {
            var dte = (DTE2)GetDTE();
            var errorList = dte.ToolWindows.ErrorList;

            var errorItems = errorList.ErrorItems;
            var errorItemsCount = errorItems.Count;

            var errorCount = 0;

            try
            {
                for (var index = 1; index <= errorItemsCount; index++)
                {
                    var errorItem = errorItems.Item(index);

                    if (errorItem.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                    {
                        errorCount += 1;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return GetErrorListErrorCount();
            }

            return errorCount;
        }

        public void WaitForNoErrorsInErrorList()
        {
            while (GetErrorListErrorCount() != 0)
            {
                System.Threading.Thread.Yield();
            }
        }

        public void Quit()
            => GetDTE().Quit();
    }
}
