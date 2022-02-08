// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class FindReferencesWindow_InProc : InProcComponent
    {
        public static FindReferencesWindow_InProc Create() => new FindReferencesWindow_InProc();

        public Reference[] GetContents()
        {
            return InvokeOnUIThread<Reference[]>(cancellationToken =>
            {
                // Find the tool window
                var tableControl = GetFindReferencesWindow();

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

        public void NavigateTo(Reference reference, bool isPreview, bool shouldActivate)
        {
            InvokeOnUIThread(cancellationToken =>
            {
                var findReferencesWindow = GetFindReferencesWindow();

                foreach (var item in findReferencesWindow.Entries)
                {
                    if (reference.Equals(CreateReference(item)))
                    {
                        item.NavigateTo(isPreview, shouldActivate);
                    }
                }
            });
        }

        private static IWpfTableControl2 GetFindReferencesWindow()
        {
            // Guid of the FindRefs window.  Defined here:
            // https://devdiv.visualstudio.com/DevDiv/_git/VS?path=/src/env/ErrorList/Pkg/Guids.cs&version=GBmain&line=24
            var findReferencesWindowGuid = new Guid("a80febb4-e7e0-4147-b476-21aaf2453969");

            var uiShell = GetGlobalService<SVsUIShell, IVsUIShell>();
            ErrorHandler.ThrowOnFailure(uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref findReferencesWindowGuid, out var windowFrame));
            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var toolWindow));

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
