// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IMetadataService), ServiceLayer.Default), Shared]
    internal sealed class MetadataServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices.GetService<IDocumentationProviderService>());
        }

        private sealed class Service : IMetadataService
        {
            private readonly MetadataReferenceCache _metadataCache;

            public Service(IDocumentationProviderService documentationService)
            {
                _metadataCache = new MetadataReferenceCache((path, properties) =>
                    MetadataReference.CreateFromFile(path, properties, documentationService.GetDocumentationProvider(path)));
            }

            public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return (PortableExecutableReference)_metadataCache.GetReference(resolvedPath, properties);
            }
        }
    }
}
