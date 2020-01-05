// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
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
