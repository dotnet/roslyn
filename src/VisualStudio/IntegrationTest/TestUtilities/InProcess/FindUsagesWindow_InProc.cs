// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class FindUsagesWindow_InProc : InProcComponent
    {
        public static FindUsagesWindow_InProc Create() => new FindUsagesWindow_InProc();

        public string[] GetContents(string windowCaption)
        {
            return InvokeOnUIThread(() =>
            {
                // Find the tool window
                var toolWindow = ((DTE2)GetDTE()).ToolWindows.GetToolWindow(windowCaption);

                // Dig through to get the Find References control.
                var toolWindowType = toolWindow.GetType();
                var toolWindowControlField = toolWindowType.GetField("Control");
                var toolWindowControl = toolWindowControlField.GetValue(toolWindow);

                // Dig further to get the results table (as opposed to the toolbar).
                var tableControlAndCommandTargetType = toolWindowControl.GetType();
                var tableControlField = tableControlAndCommandTargetType.GetField("TableControl");
                var tableControl = (IWpfTableControl2)tableControlField.GetValue(toolWindowControl);

                // Force a refresh, if necessary. This doesn't re-run the Find References or
                // Find Implementations operation itself, it just forces the results to be
                // realized in the table.
                var forcedUpdateResult = ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    return await tableControl.ForceUpdateAsync();
                });

                // Extract the basic text of the results.
                return forcedUpdateResult.AllEntries.Select(handle =>
                {
                    if (handle.TryGetValue(StandardTableKeyNames.Text, out string text))
                    {
                        return text;
                    }

                    return string.Empty;
                }).ToArray();
            });
        }
    }
}
