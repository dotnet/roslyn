// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.RazorExtension.NestedFiles;

/// <summary>
/// Shared helper for querying the current selection via <see cref="IVsMonitorSelection"/>.
/// Works for both Solution Explorer selection and active editor documents, because
/// <see cref="IVsMonitorSelection"/> tracks the active window frame's hierarchy item.
/// </summary>
internal static class SelectionHelper
{
    /// <summary>
    /// Returns the file path of the currently selected/active hierarchy item, or null
    /// if no single file item is selected.
    /// </summary>
    public static string? GetCurrentSelectionPath(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (serviceProvider.GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection monitorSelection)
        {
            monitorSelection.GetCurrentSelection(out var hierarchyPtr, out var itemId, out _, out var selectionContainerPtr);

            try
            {
                if (itemId is not VSConstants.VSITEMID_NIL and not VSConstants.VSITEMID_ROOT and not VSConstants.VSITEMID_SELECTION
                    && hierarchyPtr != IntPtr.Zero
                    && Marshal.GetObjectForIUnknown(hierarchyPtr) is IVsProject project
                    && project.GetMkDocument(itemId, out var filePath) == VSConstants.S_OK)
                {
                    return filePath;
                }
            }
            finally
            {
                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }

                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns true if the <see cref="RazorPackage.GuidRazorFileContext"/> UIContext is active.
    /// This context requires both the <c>DotNetCoreRazorProject</c> capability on the active
    /// project and a matching Razor/nested file selection.
    /// </summary>
    public static bool IsRazorFileUIContextActive(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var contextGuid = RazorPackage.GuidRazorFileContext;

        return serviceProvider.GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection monitorSelection
            && monitorSelection.GetCmdUIContextCookie(ref contextGuid, out var cookie) == VSConstants.S_OK
            && monitorSelection.IsCmdUIContextActive(cookie, out var isActive) == VSConstants.S_OK
            && isActive != 0;
    }
}
