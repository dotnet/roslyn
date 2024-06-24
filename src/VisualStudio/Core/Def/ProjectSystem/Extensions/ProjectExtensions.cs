// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

internal static class ProjectExtensions
{
    public static ProjectItem FindOrCreateFolder(this EnvDTE.Project project, IEnumerable<string> containers)
    {
        Debug.Assert(containers.Any());

        var currentItems = project.ProjectItems;
        foreach (var container in containers)
        {
            var folderItem = currentItems.FindFolder(container);
            folderItem ??= CreateFolder(currentItems, container);

            currentItems = folderItem.ProjectItems;
        }

        return (ProjectItem)currentItems.Parent;
    }

    private static ProjectItem CreateFolder(ProjectItems currentItems, string container)
    {
        var folderName = container;
        var index = 1;

        // Keep looking for a unique name as long as we collide with some item.

        // NOTE(cyrusn): There shouldn't be a race condition here.  While it looks like a rogue
        // component could stomp on the name we've found once we've decided on it, they really
        // can't since we're running on the main thread.  And, if someone does stomp on us
        // somehow, then we really should just throw in that case.
        while (currentItems.FindItem(folderName, StringComparer.OrdinalIgnoreCase) != null)
        {
            folderName = container + index;
            index++;
        }

        return currentItems.AddFolder(folderName);
    }

    public static ProjectItem? FindItemByPath(this EnvDTE.Project project, string itemFilePath, StringComparer comparer)
    {
        using var _ = ArrayBuilder<ProjectItems>.GetInstance(out var stack);
        stack.Push(project.ProjectItems);

        while (stack.TryPop(out var currentItems))
        {
            foreach (var projectItem in currentItems.OfType<ProjectItem>())
            {
                if (projectItem.TryGetFullPath(out var filePath) && comparer.Equals(filePath, itemFilePath))
                {
                    return projectItem;
                }

                if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
                {
                    stack.Push(projectItem.ProjectItems);
                }
            }
        }

        return null;
    }

    public static bool TryGetFullPath(this EnvDTE.Project project, [NotNullWhen(returnValue: true)] out string? fullPath)
    {
        fullPath = project.Properties.Item("FullPath").Value as string;
        return fullPath != null;
    }
}
