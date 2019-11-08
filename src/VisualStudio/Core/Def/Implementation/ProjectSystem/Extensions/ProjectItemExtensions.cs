// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions
{
    internal static class ProjectItemExtensions
    {
        public static ProjectItem FindItem(this ProjectItem item, string itemName, StringComparer comparer)
        {
            return item.ProjectItems.FindItem(itemName, comparer);
        }

        public static bool TryGetFullPath(this ProjectItem item, out string fullPath)
        {
            fullPath = item.Properties.Item("FullPath").Value as string;
            return fullPath != null;
        }

        public static bool IsFolder(this ProjectItem item)
        {
            return item is { Kind: Constants.vsProjectItemKindPhysicalFolder };
        }
    }
}
