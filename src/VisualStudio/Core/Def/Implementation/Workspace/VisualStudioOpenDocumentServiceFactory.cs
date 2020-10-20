// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OpenDocument;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IOpenDocumentService), ServiceLayer.Host), Shared]
    internal class VisualStudioOpenDocumentServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IOpenDocumentService _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioOpenDocumentServiceFactory(
            SVsServiceProvider serviceProvider)
        {
            _singleton = new VisualStudioOpenDocumentService(serviceProvider);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;
    }
}
