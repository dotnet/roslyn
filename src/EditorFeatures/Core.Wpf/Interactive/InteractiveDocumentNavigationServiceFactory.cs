// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentNavigationService), WorkspaceKind.Interactive), Shared]
    internal sealed class InteractiveDocumentNavigationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDocumentNavigationService _singleton;

        [ImportingConstructor]
        public InteractiveDocumentNavigationServiceFactory()
        {
            _singleton = new InteractiveDocumentNavigationService();
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }
    }
}
