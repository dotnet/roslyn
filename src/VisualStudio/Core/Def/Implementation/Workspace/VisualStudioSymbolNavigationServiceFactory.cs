// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSymbolNavigationServiceFactory(
            SVsServiceProvider serviceProvider,
            [Import] VisualStudio14StructureTaggerProvider outliningTaggerProvider)
        {
            _singleton = new VisualStudioSymbolNavigationService(serviceProvider, outliningTaggerProvider);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;
    }
}
