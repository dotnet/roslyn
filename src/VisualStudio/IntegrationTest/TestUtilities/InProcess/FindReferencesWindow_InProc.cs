// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return InvokeOnUIThread<Reference[]>(cancellationToken =>
            {
                // Find the tool window
                var tableControl = GetFindReferencesWindow(windowCaption);

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
                return forcedUpdateResult.AllEntries.Select(CreateReference).ToArray();
            });
        }

        public void NavigateTo(string windowCaption, Reference reference, bool isPreview, bool shouldActivate)
        {
            InvokeOnUIThread(cancellationToken =>
            {
                var findReferencesWindow = GetFindReferencesWindow(windowCaption);

                foreach (var item in findReferencesWindow.Entries)
                {
                    if (reference.Equals(CreateReference(item)))
                    {
                        item.NavigateTo(isPreview, shouldActivate);
                    }
                }
            });
        }

        private static IWpfTableControl2 GetFindReferencesWindow(string windowCaption)
        {
            var toolWindow = ((DTE2)GetDTE()).ToolWindows.GetToolWindow(windowCaption);

            // Dig through to get the Find References control.
            var toolWindowType = toolWindow.GetType();
            var toolWindowControlField = toolWindowType.GetField("Control");
            var toolWindowControl = toolWindowControlField.GetValue(toolWindow);

            // Dig further to get the results table (as opposed to the toolbar).
            var tableControlAndCommandTargetType = toolWindowControl.GetType();
            var tableControlField = tableControlAndCommandTargetType.GetField("TableControl");
            var tableControl = (IWpfTableControl2)tableControlField.GetValue(toolWindowControl);
            return tableControl;
        }

        private static Reference CreateReference(ITableEntryHandle tableEntryHandle)
        {
            tableEntryHandle.TryGetValue(StandardTableKeyNames.DocumentName, out string filePath);
            tableEntryHandle.TryGetValue(StandardTableKeyNames.Line, out int line);
            tableEntryHandle.TryGetValue(StandardTableKeyNames.Column, out int column);
            tableEntryHandle.TryGetValue(StandardTableKeyNames.Text, out string code);

            var reference = new Reference
            {
                FilePath = filePath,
                Line = line,
                Column = column,
                Code = code
            };

            return reference;
        }
    }
}
