// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Search.Data;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

/// <summary>
/// Roslyn implementation of the <see cref="ISearchItemsSourceProvider"/>.  This is the entry-point from VS to
/// support the 'all in one search provider' UI (which supercedes the previous 'go to' UI).
/// </summary>
[Export(typeof(ISearchItemsSourceProvider))]
[Name(nameof(RoslynSearchItemsSourceProvider))]
[ProducesResultType(CodeSearchResultType.Class)]
[ProducesResultType(CodeSearchResultType.Constant)]
[ProducesResultType(CodeSearchResultType.Delegate)]
[ProducesResultType(CodeSearchResultType.Enum)]
[ProducesResultType(CodeSearchResultType.EnumItem)]
[ProducesResultType(CodeSearchResultType.Event)]
[ProducesResultType(CodeSearchResultType.Field)]
[ProducesResultType(CodeSearchResultType.Interface)]
[ProducesResultType(CodeSearchResultType.Method)]
[ProducesResultType(CodeSearchResultType.Module)]
[ProducesResultType(CodeSearchResultType.OtherSymbol)]
[ProducesResultType(CodeSearchResultType.Property)]
[ProducesResultType(CodeSearchResultType.Structure)]
internal sealed partial class RoslynSearchItemsSourceProvider : ISearchItemsSourceProvider
{
    private readonly VisualStudioWorkspace _workspace;
    private readonly IThreadingContext _threadingContext;
    private readonly IUIThreadOperationExecutor _threadOperationExecutor;
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly RoslynSearchResultViewFactory _viewFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynSearchItemsSourceProvider(
        VisualStudioWorkspace workspace,
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor threadOperationExecutor,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _workspace = workspace;
        _threadingContext = threadingContext;
        _threadOperationExecutor = threadOperationExecutor;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);

        _viewFactory = new RoslynSearchResultViewFactory(this);
    }

    public ISearchItemsSource CreateItemsSource()
        => new RoslynSearchItemsSource(this);
}
