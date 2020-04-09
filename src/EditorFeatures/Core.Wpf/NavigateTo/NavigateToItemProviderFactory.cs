// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    [Export(typeof(INavigateToItemProviderFactory)), Shared]
    internal class NavigateToItemProviderFactory : INavigateToItemProviderFactory
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly PrimaryWorkspace _primaryWorkspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigateToItemProviderFactory(IAsynchronousOperationListenerProvider listenerProvider, PrimaryWorkspace primaryWorkspace)
        {
            if (listenerProvider == null)
            {
                throw new ArgumentNullException(nameof(listenerProvider));
            }

            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
            _primaryWorkspace = primaryWorkspace;
        }

        public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider provider)
        {
            var workspace = _primaryWorkspace.Workspace;
            if (workspace == null)
            {
                // when Roslyn is not loaded, workspace is null, and so we don't want to 
                // participate in this Navigate To session. See bug 756800
                provider = null;
                return false;
            }

            provider = new NavigateToItemProvider(workspace, _asyncListener);
            return true;
        }
    }
}
