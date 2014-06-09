// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IMetadataReferenceProviderService), ServiceLayer.Default)]
    internal sealed class MetadataReferenceProviderServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new MetadataReferenceProviderService(workspaceServices);
        }

        private sealed class MetadataReferenceProviderService : IMetadataReferenceProviderService
        {
            private readonly HostWorkspaceServices workspaceServices;

            public MetadataReferenceProviderService(HostWorkspaceServices workspaceServices)
            {
                this.workspaceServices = workspaceServices;
            }

            public MetadataReferenceProvider GetProvider()
            {
                return new Provider(this.workspaceServices.GetService<IDocumentationProviderService>());
            }
        }

        private sealed class Provider : MetadataReferenceProvider
        {
            private readonly IDocumentationProviderService documentationService;

            public Provider(IDocumentationProviderService documentationService)
            {
                this.documentationService = documentationService;
            }        

            public override PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return new MetadataFileReference(resolvedPath, properties, this.documentationService.GetDocumentationProvider(resolvedPath));
            }
        }
    }
}
