// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(IMetadataReferenceProviderService), WorkspaceKind.Any)]
#endif
    internal sealed class MetadataReferenceProviderServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new MetadataReferenceProviderService();
        }

        internal sealed class MetadataReferenceProviderService : IMetadataReferenceProviderService
        {
            public MetadataReferenceProvider GetProvider()
            {
                // by default we don't shadow copy, host can override this behavior
                return MetadataFileReferenceProvider.Default;
            }
        }
    }
}
