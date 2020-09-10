// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioNavigateToItemProviderFactory(VisualStudioWorkspace workspace, IAsynchronousOperationListenerProvider listenerProvider)
        {
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
            _workspace = workspace;
        }

        public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider provider)
        {
            provider = new NavigateToItemProvider(_workspace, _asyncListener);
            return true;
        }
    }
}
