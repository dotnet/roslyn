// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// this is desktop implementation of IReferenceSerializationService
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IReferenceSerializationService), layer: ServiceLayer.Desktop), Shared]
    internal class DesktopReferenceSerializationServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesktopReferenceSerializationServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(
                workspaceServices.GetRequiredService<ITemporaryStorageService>(),
                workspaceServices.GetRequiredService<IDocumentationProviderService>(),
                workspaceServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>());
        }

        private sealed class Service : AbstractReferenceSerializationService
        {
            public Service(ITemporaryStorageService service, IDocumentationProviderService documentationService, IAnalyzerAssemblyLoaderProvider analyzerLoaderProvider)
                : base(service, documentationService, analyzerLoaderProvider)
            {
            }
        }
    }
}
