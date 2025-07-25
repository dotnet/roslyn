// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using EnvDTE;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

internal static class ProjectItemExtensions
{
    extension(ProjectItem item)
    {
        public ProjectItem FindItem(string itemName, StringComparer comparer)
        => item.ProjectItems.FindItem(itemName, comparer);

        public bool TryGetFullPath([NotNullWhen(returnValue: true)] out string? fullPath)
        {
            fullPath = item.Properties.Item("FullPath").Value as string;
            return fullPath != null;
        }

        public bool IsFolder()
            => item != null && item.Kind == Constants.vsProjectItemKindPhysicalFolder;
    }
}
