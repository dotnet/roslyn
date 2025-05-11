// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

internal static class VisualStudioCommandHandlerHelpers
{
    public static OleMenuCommand AddCommand(
        IMenuCommandService menuCommandService,
        int commandId,
        Guid commandGroup,
        EventHandler invokeHandler,
        EventHandler beforeQueryStatus)
    {
        var commandIdWithGroupId = new CommandID(commandGroup, commandId);
        var command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, commandIdWithGroupId);
        menuCommandService.AddCommand(command);
        return command;
    }

    public static bool TryGetSelectedProjectHierarchy(IServiceProvider? serviceProvider, [NotNullWhen(returnValue: true)] out IVsHierarchy? hierarchy)
    {
        hierarchy = null;

        // Get the DTE service and make sure there is an open solution
        if (serviceProvider?.GetService(typeof(EnvDTE.DTE)) is not EnvDTE.DTE dte ||
            dte.Solution == null)
        {
            return false;
        }

        var selectionHierarchy = IntPtr.Zero;
        var selectionContainer = IntPtr.Zero;

        // Get the current selection in the shell
        if (serviceProvider.GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection monitorSelection)
        {
            try
            {
                monitorSelection.GetCurrentSelection(out selectionHierarchy, out var itemId, out var multiSelect, out selectionContainer);
                if (selectionHierarchy != IntPtr.Zero)
                {
                    hierarchy = Marshal.GetObjectForIUnknown(selectionHierarchy) as IVsHierarchy;
                    Debug.Assert(hierarchy != null);
                    return hierarchy != null;
                }
            }
            catch (Exception)
            {
                // If anything went wrong, just ignore it
            }
            finally
            {
                // Make sure we release the COM pointers in any case
                if (selectionHierarchy != IntPtr.Zero)
                {
                    Marshal.Release(selectionHierarchy);
                }

                if (selectionContainer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainer);
                }
            }
        }

        return false;
    }

    public static bool IsBuildActive()
        => KnownUIContexts.SolutionBuildingContext.IsActive;
}
