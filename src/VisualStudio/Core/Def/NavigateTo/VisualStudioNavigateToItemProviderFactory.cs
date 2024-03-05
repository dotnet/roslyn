// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.NavigateTo;

// Used to indicate that this type should be ignored if the platform uses the new ISearchItemsSourceProvider system instead.
[OnlyNavigateToSupport]
[Export(typeof(INavigateToItemProviderFactory))]
internal sealed class VisualStudioNavigateToItemProviderFactory : INavigateToItemProviderFactory
{
    private readonly VisualStudioWorkspace _workspace;
    private readonly IThreadingContext _threadingContext;
    private readonly IUIThreadOperationExecutor _threadOperationExecutor;
    private readonly IAsynchronousOperationListener _asyncListener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioNavigateToItemProviderFactory(
        VisualStudioWorkspace workspace,
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor threadOperationExecutor,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _workspace = workspace;
        _threadingContext = threadingContext;
        _threadOperationExecutor = threadOperationExecutor;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
    }

    public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider? provider)
    {
        // Let LSP handle goto when running under the LSP editor.
        if (_workspace.Services.GetRequiredService<IWorkspaceContextService>().IsInLspEditorContext())
        {
            provider = null;
            return false;
        }

        provider = new NavigateToItemProvider(
            _workspace, _threadingContext, _threadOperationExecutor, _asyncListener);
        return true;
    }
}
