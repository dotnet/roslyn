// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // TODO: Remove this type. This factory is needed just to instantiate a singleton of VisualStudioMetadataReferenceProvider.
    // We should be able to MEF-instantiate a singleton of VisualStudioMetadataReferenceProvider without creating this factory.
    [ExportWorkspaceServiceFactory(typeof(VisualStudioMetadataReferenceManager), ServiceLayer.Host), Shared]
    internal class VisualStudioMetadataReferenceManagerFactory : IWorkspaceServiceFactory
    {
        private VisualStudioMetadataReferenceManager _singleton;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VisualStudioMetadataReferenceManagerFactory(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_singleton == null)
            {
                var temporaryStorage = workspaceServices.GetService<ITemporaryStorageService>();
                Interlocked.CompareExchange(ref _singleton, new VisualStudioMetadataReferenceManager(_serviceProvider, temporaryStorage), null);
            }

            return _singleton;
        }
    }
}
