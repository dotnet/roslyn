// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class FindReferencesWindowInProcess
    {
        public async Task<ImmutableArray<ITableEntryHandle2>> GetContentsAsync(string windowCaption, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.FindReferences, cancellationToken);

            // Find the tool window
            var tableControl = await GetFindReferencesWindowAsync(windowCaption, cancellationToken);

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
            var forcedUpdateResult = await tableControl.ForceUpdateAsync().WithCancellation(cancellationToken);

            // Extract the basic text of the results.
            return forcedUpdateResult.AllEntries.Cast<ITableEntryHandle2>().ToImmutableArray();
        }

        private async Task<IWpfTableControl2> GetFindReferencesWindowAsync(string windowCaption, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE80.DTE2>(cancellationToken);

            var toolWindow = dte.ToolWindows.GetToolWindow(windowCaption);

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
    }
}
