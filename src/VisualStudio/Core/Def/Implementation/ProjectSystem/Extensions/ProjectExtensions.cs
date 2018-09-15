// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions
{
    internal static class ProjectExtensions
    {      
        public static ProjectItem FindOrCreateFolder(this EnvDTE.Project project, IEnumerable<string> containers, out ImmutableArray<string> createdContainer)
        {
            return FindOrCreateFolder(project, containers, createIfNotFound:true, out createdContainer);
        }

        public static ProjectItem FindFolder(this EnvDTE.Project project, IEnumerable<string> containers)
        {         
            return FindOrCreateFolder(project, containers, createIfNotFound: false, out _);
        }

        /// <remarks>
        /// If any folder is created, <paramref name="createdContainer"/> contains from first entry in <paramref name="containers"/> 
        /// up to the first new folder created. Otherwise, <paramref name="createdContainer"/> is empty.
        /// </remarks> 
        private static ProjectItem FindOrCreateFolder(this EnvDTE.Project project, IEnumerable<string> containers, bool createIfNotFound, out ImmutableArray<string> createdContainer)
        {
            Debug.Assert(containers.Any());

            int firstCreationIndex = -1;
            int index = 0;
            var currentItems = project.ProjectItems;
            foreach (var container in containers)
            {
                var folderItem = currentItems.FindFolder(container);
                if (folderItem == null)
                {
                    if (createIfNotFound)
                    {
                        folderItem = CreateFolder(currentItems, container);
                        if (firstCreationIndex < 0)
                        {
                            firstCreationIndex = index;
                        }
                    }
                    else
                    {
                        createdContainer = ImmutableArray<string>.Empty;
                        return null;
                    }
                }

                currentItems = folderItem.ProjectItems;
                ++index;
            }

            createdContainer = containers.Take(firstCreationIndex + 1).ToImmutableArrayOrEmpty();
            return (ProjectItem)currentItems.Parent;
        } 


        private static ProjectItem CreateFolder(ProjectItems currentItems, string container)
        {
            var folderName = container;
            int index = 1;

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
            return project.ProjectItems.FindItemByPath(itemFilePath, comparer);
        }

        public static bool TryGetFullPath(this EnvDTE.Project project, out string fullPath)
        {
            fullPath = project.Properties.Item("FullPath").Value as string;
            return fullPath != null;
        }
    }
}
