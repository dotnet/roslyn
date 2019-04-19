// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class FindReferencesWindow_InProc : InProcComponent
    {
        public static FindReferencesWindow_InProc Create() => new FindReferencesWindow_InProc();

        public Reference[] GetContents(string windowCaption)
        {
            return InvokeOnUIThread(cancellationToken =>
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

                // Remove all grouping
                var columnStates = tableControl.ColumnStates;
                var newColumnsStates = new List<ColumnState2>();
                foreach (ColumnState2 state in columnStates)
                {
                    var newState = new ColumnState2(
                        state.Name,
                        state.IsVisible,
                        state.Width,
                        state.SortPriority,
                        state.DescendingSort,
                        groupingPriority: 0);
                    newColumnsStates.Add(newState);
                }
                tableControl.SetColumnStates(newColumnsStates);

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
                    handle.TryGetValue(StandardTableKeyNames.DocumentName, out string filePath);
                    handle.TryGetValue(StandardTableKeyNames.Line, out int line);
                    handle.TryGetValue(StandardTableKeyNames.Column, out int column);
                    handle.TryGetValue(StandardTableKeyNames.Text, out string code);

                    var reference = new Reference
                    {
                        FilePath = filePath,
                        Line = line,
                        Column = column,
                        Code = code
                    };

                    return reference;
                }).ToArray();
            });
        }
    }
}
