// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using VSLangProj140;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class AnalyzersFolderItem(
    IThreadingContext threadingContext,
    Workspace workspace,
    ProjectId projectId,
    IVsHierarchyItem parentItem,
    IContextMenuController contextMenuController) : BaseItem(SolutionExplorerShim.Analyzers)
{
    public readonly IThreadingContext ThreadingContext = threadingContext;
    public Workspace Workspace { get; } = workspace;
    public ProjectId ProjectId { get; } = projectId;
    public IVsHierarchyItem ParentItem { get; } = parentItem;
    public override IContextMenuController ContextMenuController { get; } = contextMenuController;

    public override ImageMoniker IconMoniker => KnownMonikers.CodeInformation;

    public override object GetBrowseObject()
        => new BrowseObject(this);

    /// <summary>
    /// Get the DTE object for the Project.
    /// </summary>
    private VSProject3? GetVSProject()
    {
        if (Workspace is not VisualStudioWorkspace vsWorkspace)
            return null;

        var hierarchy = vsWorkspace.GetHierarchy(ProjectId);
        if (hierarchy == null)
            return null;

        if (hierarchy.TryGetProject(out var project))
        {
            var vsproject = project.Object as VSProject3;
            return vsproject;
        }

        return null;
    }

    /// <summary>
    /// Add an analyzer with the given path to this folder.
    /// </summary>
    public void AddAnalyzer(string path)
    {
        var vsproject = GetVSProject();
        if (vsproject == null)
            return;

        vsproject.AnalyzerReferences.Add(path);
    }

    /// <summary>
    /// Remove an analyzer with the given path from this folder.
    /// </summary>
    public void RemoveAnalyzer(string? path)
    {
        var vsproject = GetVSProject();
        if (vsproject == null)
            return;

        vsproject.AnalyzerReferences.Remove(path);
    }
}
