// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class SolutionExplorerSearchResult(
    RoslynSolutionExplorerSearchProvider provider,
    INavigateToSearchResult result) : ISearchResult
{
    public object GetDisplayItem()
    {
        var name = result.NavigableItem.DisplayTaggedParts.JoinText();
        return new SolutionExplorerSearchDisplayItem(
            provider, name, result);
    }
}
