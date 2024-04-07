// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

internal static class ProjectItemsExtensions
{
    public static ProjectItem FindItem(this ProjectItems items, string itemName, StringComparer comparer)
        => items.OfType<ProjectItem>().FirstOrDefault(p => comparer.Compare(p.Name, itemName) == 0);

    public static ProjectItem FindFolder(this ProjectItems items, string folderName)
    {
        var item = items.FindItem(folderName, StringComparer.OrdinalIgnoreCase);
        return item.IsFolder() ? item : null;
    }

    public static string GetUniqueName(this ProjectItems items, string itemName, string extension)
        => NameGenerator.GenerateUniqueName(itemName, extension, n => items.FindItem(n, StringComparer.OrdinalIgnoreCase) == null);

    public static string GetUniqueNameIgnoringProjectItem(this ProjectItems items, ProjectItem itemToIgnore, string itemName, string extension)
    {
        return NameGenerator.GenerateUniqueName(itemName, extension, n =>
        {
            var foundItem = items.FindItem(n, StringComparer.OrdinalIgnoreCase);
            return foundItem == null ||
                foundItem == itemToIgnore;
        });
    }
}
