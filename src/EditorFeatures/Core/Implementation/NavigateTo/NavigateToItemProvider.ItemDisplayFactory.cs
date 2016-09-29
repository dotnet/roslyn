// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private class ItemDisplayFactory : INavigateToItemDisplayFactory, IDisposable
        {
            private readonly NavigateToIconFactory _iconFactory;

            public ItemDisplayFactory(NavigateToIconFactory iconFactory)
            {
                Contract.ThrowIfNull(iconFactory);

                _iconFactory = iconFactory;
            }

            public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
            {
                var searchResult = (INavigateToSearchResult)item.Tag;
                return new NavigateToItemDisplay(searchResult, _iconFactory);
            }

            public void Dispose()
            {
                _iconFactory.Dispose();
            }
        }
    }
}