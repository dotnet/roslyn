// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal sealed class NavigateToItemDisplayFactory : INavigateToItemDisplayFactory
    {
        private readonly IThreadingContext _threadingContext;

        public NavigateToItemDisplayFactory(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
        {
            var searchResult = (INavigateToSearchResult)item.Tag;
            return new NavigateToItemDisplay(_threadingContext, searchResult);
        }
    }
}
