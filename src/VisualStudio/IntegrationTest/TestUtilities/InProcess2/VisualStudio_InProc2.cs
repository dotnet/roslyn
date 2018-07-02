// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class VisualStudio_InProc2 : InProcComponent2
    {
        public VisualStudio_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

#if false
        public string[] GetAvailableCommands()
        {
            List<string> result = new List<string>();
            var commands = GetDTE().Commands;
            foreach (Command command in commands)
            {
                try
                {
                    string commandName = command.Name;
                    if (command.IsAvailable)
                    {
                        result.Add(commandName);
                    }
                }
                finally { }
            }

            return result.ToArray();
        }
#endif

        public async Task ActivateMainWindowAsync(bool skipAttachingThreads = false)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDTEAsync();

            var activeVisualStudioWindow = (IntPtr)dte.ActiveWindow.HWnd;
            Debug.WriteLine($"DTE.ActiveWindow.HWnd = {activeVisualStudioWindow}");

            if (activeVisualStudioWindow == IntPtr.Zero)
            {
                activeVisualStudioWindow = (IntPtr)dte.MainWindow.HWnd;
                Debug.WriteLine($"DTE.MainWindow.HWnd = {activeVisualStudioWindow}");
            }

            IntegrationHelper.SetForegroundWindow(activeVisualStudioWindow, skipAttachingThreads);
        }

#if false
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
#endif

        public new async Task<bool> IsCommandAvailableAsync(string commandName)
        {
            return await base.IsCommandAvailableAsync(commandName);
        }

        public new async Task ExecuteCommandAsync(string commandName, string args = "")
        {
            await base.ExecuteCommandAsync(commandName, args);
        }
    }
}
