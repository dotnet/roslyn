// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentNavigationService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioDocumentNavigationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDocumentNavigationService _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDocumentNavigationServiceFactory(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
        {
            _singleton = new VisualStudioDocumentNavigationService(threadingContext, serviceProvider, editorAdaptersFactoryService);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;
    }
}
