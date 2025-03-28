// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;

internal sealed class NavigateToItemDisplayFactory : INavigateToItemDisplayFactory
{
    private readonly IThreadingContext _threadingContext;
    private readonly IUIThreadOperationExecutor _threadOperationExecutor;
    private readonly IAsynchronousOperationListener _asyncListener;

    public NavigateToItemDisplayFactory(
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor threadOperationExecutor,
        IAsynchronousOperationListener asyncListener)
    {
        _threadingContext = threadingContext;
        _threadOperationExecutor = threadOperationExecutor;
        _asyncListener = asyncListener;
    }

    public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
        => new NavigateToItemDisplay(
            _threadingContext, _threadOperationExecutor, _asyncListener, (INavigateToSearchResult)item.Tag);
}
