// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal sealed class NavigateToItemDisplayFactory : INavigateToItemDisplayFactory
    {
        public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
        {
            var searchResult = (INavigateToSearchResult)item.Tag;
            return new NavigateToItemDisplay(searchResult);
        }
    }
}
