// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioDocumentTrackingServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private IDocumentTrackingService _singleton;

        [ImportingConstructor]
        public VisualStudioDocumentTrackingServiceFactory(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton ?? (_singleton = new VisualStudioDocumentTrackingService(_serviceProvider));
        }
    }
}
