// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions
{
    internal static class ProjectItemsExtensions
    {
        public static ProjectItem FindItem(this ProjectItems items, string itemName, StringComparer comparer)
        {
            return items.OfType<ProjectItem>().FirstOrDefault(p => comparer.Compare(p.Name, itemName) == 0);
        }

        public static ProjectItem FindFolder(this ProjectItems items, string folderName)
        {
            var item = items.FindItem(folderName, StringComparer.OrdinalIgnoreCase);
            return item.IsFolder() ? item : null;
        }

        public static string GetUniqueName(this ProjectItems items, string itemName, string extension)
        {
            return NameGenerator.GenerateUniqueName(itemName, extension, n => items.FindItem(n, StringComparer.OrdinalIgnoreCase) == null);
        }
    }
}
