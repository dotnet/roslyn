// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

internal static class VisualStudioWorkspaceUtilities
{
    public static bool TryGetVsHierarchyAndItemId(
        [NotNullWhen(true)] TextDocument? document,
        [NotNullWhen(true)] out IVsHierarchy? hierarchy,
        out uint itemID)
    {
        return TryGetVsHierarchyAndItemId(document?.Project.Solution.Workspace, document?.State, out hierarchy, out itemID);
    }

    public static bool TryGetVsHierarchyAndItemId(
        [NotNullWhen(true)] Workspace? workspace,
        [NotNullWhen(true)] TextDocumentState? document,
        [NotNullWhen(true)] out IVsHierarchy? hierarchy,
        out uint itemID)
    {
        if (workspace is VisualStudioWorkspace visualStudioWorkspace &&
            document?.FilePath != null)
        {
            hierarchy = visualStudioWorkspace.GetHierarchy(document.Id.ProjectId);
            if (hierarchy is not null)
            {
                itemID = hierarchy.TryGetItemId(document.FilePath);
                if (itemID != VSConstants.VSITEMID_NIL)
                    return true;
            }
        }

        hierarchy = null;
        itemID = (uint)VSConstants.VSITEMID.Nil;
        return false;
    }

    public static bool TryGetVsHierarchyItem(
        IVsHierarchyItemManager hierarchyItemManager,
        [NotNullWhen(true)] TextDocument? document,
        [NotNullWhen(true)] out IVsHierarchyItem? hierarchyItem)
    {
        hierarchyItem = null;
        return
            TryGetVsHierarchyAndItemId(document, out var hierarchy, out var itemID) &&
            hierarchyItemManager.TryGetHierarchyItem(hierarchy, itemID, out hierarchyItem);
    }
}
