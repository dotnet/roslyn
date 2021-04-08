// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.NavigateTo
{
    [Export(typeof(INavigateToItemProviderFactory)), Shared]
    internal sealed class VisualStudioNavigateToItemProviderFactory : INavigateToItemProviderFactory
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly VisualStudioWorkspace _workspace;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioNavigateToItemProviderFactory(
            VisualStudioWorkspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
        {
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
            _workspace = workspace;
            _threadingContext = threadingContext;
        }

        public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider? provider)
        {
            // Let LSP handle goto when running under the LSP editor.
            if (_workspace.Services.GetRequiredService<IWorkspaceContextService>().IsInLspEditorContext())
            {
                provider = null;
                return false;
            }

            provider = new NavigateToItemProvider(_workspace, _asyncListener, _threadingContext);
            return true;
        }
    }
}
