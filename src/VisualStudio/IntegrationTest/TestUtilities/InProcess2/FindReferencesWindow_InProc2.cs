// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using DTE2 = EnvDTE80.DTE2;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class FindReferencesWindow_InProc2 : InProcComponent2
    {
        public FindReferencesWindow_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        /// <summary>
        /// Waits for any in-progress Find Reference operations to complete and returns the set of displayed results.
        /// </summary>
        /// <param name="windowCaption">The name of the window. Generally this will be something like
        /// "'Alpha' references" or "'Beta' implementations".</param>
        /// <returns>An array of <see cref="Reference"/> items capturing the current contents of the 
        /// Find References window.</returns>
        public async Task<Reference[]> GetContentsAsync(string windowCaption)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // Wait for any pending FindReferences operation to complete.
            // Go to Definition/Go to Implementation are synchronous so we don't need to wait for them
            // (and currently can't, anyway); if they are made asynchronous we will need to wait for
            // them here as well.
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.FindReferences);

            // Find the tool window
            var toolWindow = ((DTE2)await GetDTEAsync()).ToolWindows.GetToolWindow(windowCaption);

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
            var forcedUpdateResult = await tableControl.ForceUpdateAsync();

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
        }
    }
}
