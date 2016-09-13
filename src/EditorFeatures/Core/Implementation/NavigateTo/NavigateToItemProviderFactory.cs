// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Extensibility.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    [Export(typeof(INavigateToItemProviderFactory)), Shared]
    internal class NavigateToItemProviderFactory : INavigateToItemProviderFactory
    {
        private readonly IGlyphService _glyphService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IEnumerable<Lazy<INavigateToOptionsService, VisualStudioVersionMetadata>> _optionsServices;

        [ImportingConstructor]
        public NavigateToItemProviderFactory(
            IGlyphService glyphService,
            [ImportMany] IEnumerable<Lazy<INavigateToOptionsService, VisualStudioVersionMetadata>> optionsServices,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            if (glyphService == null)
            {
                throw new ArgumentNullException(nameof(glyphService));
            }

            if (asyncListeners == null)
            {
                throw new ArgumentNullException(nameof(asyncListeners));
            }

            _glyphService = glyphService;
            _optionsServices = optionsServices;
            _asyncListener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.NavigateTo);
        }

        public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider provider)
        {
            var workspace = PrimaryWorkspace.Workspace;
            if (workspace == null)
            {
                // when Roslyn is not loaded, workspace is null, and so we don't want to 
                // participate in this Navigate To session. See bug 756800
                provider = null;
                return false;
            }

            provider = new NavigateToItemProvider(workspace, _glyphService,  _asyncListener, _optionsServices);
            return true;
        }
    }
}
