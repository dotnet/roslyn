// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

/// <summary>
/// Maps from hierarchy items to project IDs.
/// </summary>
internal interface IHierarchyItemToProjectIdMap : IWorkspaceService
{
    /// <summary>
    /// Given an <see cref="IVsHierarchyItem"/> representing a project and an optional target framework moniker,
    /// returns the <see cref="ProjectId"/> of the equivalent Roslyn <see cref="Project"/>.
    /// </summary>
    /// <param name="hierarchyItem">An <see cref="IVsHierarchyItem"/> for the project root.</param>
    /// <param name="targetFrameworkMoniker">An optional string representing a TargetFrameworkMoniker.
    /// This is only useful in multi-targeting scenarios where there may be multiple Roslyn projects 
    /// (one per target framework) for a single project on disk.</param>
    /// <param name="project">The <see cref="Project"/> of the found project, if any.</param>
    /// <returns>True if the desired project was found; false otherwise.</returns>
    /// <remarks>
    /// Can be called on any thread.
    /// </remarks>
    bool TryGetProject(IVsHierarchyItem hierarchyItem, string? targetFrameworkMoniker, [NotNullWhen(true)] out Project? project);
}

internal static class IHierarchyItemToProjectIdMapExtensions
{
    /// <inheritdoc cref="IHierarchyItemToProjectIdMap.TryGetProject"/>"/>
    public static bool TryGetProjectId(this IHierarchyItemToProjectIdMap idMap, IVsHierarchyItem hierarchyItem, string? targetFrameworkMoniker, [NotNullWhen(true)] out ProjectId? projectId)
    {
        projectId = null;
        if (!idMap.TryGetProject(hierarchyItem, targetFrameworkMoniker, out var project))
            return false;

        projectId = project.Id;
        return true;
    }
}
