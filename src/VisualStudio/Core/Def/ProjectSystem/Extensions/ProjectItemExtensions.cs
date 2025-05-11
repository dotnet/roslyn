// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using EnvDTE;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

internal static class ProjectItemExtensions
{
    public static ProjectItem FindItem(this ProjectItem item, string itemName, StringComparer comparer)
        => item.ProjectItems.FindItem(itemName, comparer);

    public static bool TryGetFullPath(this ProjectItem item, [NotNullWhen(returnValue: true)] out string? fullPath)
    {
        fullPath = item.Properties.Item("FullPath").Value as string;
        return fullPath != null;
    }

    public static bool IsFolder(this ProjectItem item)
        => item != null && item.Kind == Constants.vsProjectItemKindPhysicalFolder;
}
