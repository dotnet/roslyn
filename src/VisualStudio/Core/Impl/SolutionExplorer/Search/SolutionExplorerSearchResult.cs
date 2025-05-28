// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class SolutionExplorerSearchResult(
    RoslynSolutionExplorerSearchProvider provider,
    INavigateToSearchResult result,
    string name,
    ImageMoniker imageMoniker) : ISearchResult
{
    public object GetDisplayItem()
        => new SolutionExplorerSearchDisplayItem(provider, result, name, imageMoniker);
}
