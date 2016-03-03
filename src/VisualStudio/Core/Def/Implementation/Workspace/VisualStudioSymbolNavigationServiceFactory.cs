// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(ISymbolNavigationService), ServiceLayer.Host), Shared]
    internal class VisualStudioSymbolNavigationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly ISymbolNavigationService _singleton;

        [ImportingConstructor]
        private VisualStudioSymbolNavigationServiceFactory(
            SVsServiceProvider serviceProvider,
            [Import] OutliningTaggerProvider outliningTaggerProvider)
        {
            _singleton = new VisualStudioSymbolNavigationService(serviceProvider, outliningTaggerProvider);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }
    }
}
