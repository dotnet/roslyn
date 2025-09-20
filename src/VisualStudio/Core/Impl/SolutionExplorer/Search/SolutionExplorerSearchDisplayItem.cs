// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class SolutionExplorerSearchDisplayItem(
    RoslynSolutionExplorerSearchProvider provider,
    INavigateToSearchResult result,
    string name,
    ImageMoniker imageMoniker)
    : BaseItem(name, canPreview: true),
    IInvocationController
{
    public readonly INavigateToSearchResult Result = result;

    public override ImageMoniker IconMoniker { get; } = imageMoniker;

    public override IInvocationController? InvocationController => this;

    public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
    {
        if (items.FirstOrDefault() is not SolutionExplorerSearchDisplayItem displayItem)
            return false;

        provider.NavigationSupport.NavigateTo(
            displayItem.Result.NavigableItem.Document.Id,
            displayItem.Result.NavigableItem.SourceSpan.Start,
            preview: true);
        return true;
    }
}
