// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions
{
    internal static class ProjectExtensions
    {
        public static ProjectItem FindOrCreateFolder(this EnvDTE.Project project, IEnumerable<string> containers)
        {
            Debug.Assert(containers.Any());

            var currentItems = project.ProjectItems;
            foreach (var container in containers)
            {
                var folderItem = currentItems.FindFolder(container);
                if (folderItem == null)
                {
                    folderItem = CreateFolder(currentItems, container);
                }

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

        public static ProjectItem FindItem(this EnvDTE.Project project, string itemName, StringComparer comparer)
        {
            return project.ProjectItems.FindItem(itemName, comparer);
        }

        public static ProjectItem FindItemByPath(this EnvDTE.Project project, string itemFilePath, StringComparer comparer)
        {
            var stack = new Stack<ProjectItems>();
            stack.Push(project.ProjectItems);

            while (stack.Count > 0)
            {
                var currentItems = stack.Pop();

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

        public static bool TryGetFullPath(this EnvDTE.Project project, out string fullPath)
        {
            fullPath = project.Properties.Item("FullPath").Value as string;
            return fullPath != null;
        }
    }
}
