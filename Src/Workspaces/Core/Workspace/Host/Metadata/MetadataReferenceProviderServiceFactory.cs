// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IMetadataReferenceProviderService), ServiceLayer.Default)]
    internal sealed class MetadataReferenceProviderServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new MetadataReferenceProviderService();
        }

        internal sealed class MetadataReferenceProviderService : IMetadataReferenceProviderService
        {
            public MetadataReferenceProvider GetProvider()
            {
                return MetadataFileReferenceProvider.Default;
            }
        }
    }
}
