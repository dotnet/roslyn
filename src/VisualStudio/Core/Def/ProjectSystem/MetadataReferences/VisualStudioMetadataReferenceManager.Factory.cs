// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
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
        private VisualStudioMetadataReferenceManager? _singleton;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMetadataReferenceManagerFactory(SVsServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_singleton == null)
            {
                // If we're in VS we know we must be able to get a TemporaryStorageService
                var temporaryStorage = (TemporaryStorageService)workspaceServices.GetRequiredService<ITemporaryStorageServiceInternal>();
                Interlocked.CompareExchange(ref _singleton, new VisualStudioMetadataReferenceManager(_serviceProvider, temporaryStorage), null);
            }

            return _singleton;
        }
    }
}
