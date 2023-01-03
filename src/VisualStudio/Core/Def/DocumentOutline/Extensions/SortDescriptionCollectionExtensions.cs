// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal static class SortDescriptionCollectionExtensions
    {
        private static ImmutableArray<SortDescription> NameSortDescriptions { get; } = ImmutableArray.Create(new SortDescription(nameof(DocumentSymbolDataViewModel.Name), ListSortDirection.Ascending));
        private static ImmutableArray<SortDescription> LocationSortDescriptions { get; } = ImmutableArray.Create(new SortDescription(nameof(DocumentSymbolDataViewModel.StartPosition), ListSortDirection.Ascending));
        private static ImmutableArray<SortDescription> TypeSortDescriptions { get; } = ImmutableArray.Create(new SortDescription(nameof(DocumentSymbolDataViewModel.SymbolKind), ListSortDirection.Ascending), new SortDescription(nameof(DocumentSymbolDataViewModel.Name), ListSortDirection.Ascending));

        public static void UpdateSortDescription(this SortDescriptionCollection sortDescriptions, SortOption sortOption)
        {
            sortDescriptions.Clear();
            var newSortDescriptions = sortOption switch
            {
                SortOption.Name => NameSortDescriptions,
                SortOption.Location => LocationSortDescriptions,
                SortOption.Type => TypeSortDescriptions,
                _ => throw new InvalidOperationException(),
            };

            foreach (var newSortDescription in newSortDescriptions)
            {
                sortDescriptions.Add(newSortDescription);
            }
        }
    }
}
