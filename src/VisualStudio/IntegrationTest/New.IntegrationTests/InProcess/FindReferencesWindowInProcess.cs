// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class FindReferencesWindowInProcess
    {
        // Guid of the FindRefs window.  Defined here:
        // https://devdiv.visualstudio.com/DevDiv/_git/VS?path=/src/env/ErrorList/Pkg/Guids.cs&version=GBmain&line=24
        internal static readonly Guid FindReferencesWindowGuid = new("{a80febb4-e7e0-4147-b476-21aaf2453969}");

        public async Task<ImmutableArray<ITableEntryHandle2>> GetContentsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.FindReferences, cancellationToken);

            // Find the tool window
            var tableControl = await GetFindReferencesWindowAsync(cancellationToken);

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

        public async Task NavigateToAsync(ITableEntryHandle2 referenceInGeneratedFile, bool isPreview, bool shouldActivate, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            referenceInGeneratedFile.NavigateTo(isPreview, shouldActivate);

            // Navigation operations handled by Roslyn are tracked by FeatureAttribute.FindReferences
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.FindReferences], cancellationToken);

            // Navigation operations handled by the editor are tracked within its own JoinableTaskFactory instance
            await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
        }

        private async Task<IWpfTableControl2> GetFindReferencesWindowAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);
            ErrorHandler.ThrowOnFailure(shell.FindToolWindowEx((uint)__VSFINDTOOLWIN.FTW_fFindFirst, FindReferencesWindowGuid, dwToolWinId: 0, out var windowFrame));
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
    }
}
