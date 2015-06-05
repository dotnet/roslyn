// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

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
            private readonly Provider _provider;
            private readonly MetadataReferenceCache _metadataCache;

            public Service(IDocumentationProviderService documentationService)
            {
                _provider = new Provider(this);
                _metadataCache = new MetadataReferenceCache((path, properties) => 
                    MetadataReference.CreateFromFile(path, properties, documentationService.GetDocumentationProvider(path)));
            }

            public MetadataFileReferenceProvider GetProvider()
            {
                return _provider;
            }

            public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return (PortableExecutableReference)_metadataCache.GetReference(resolvedPath, properties);
            }
        }

        private sealed class Provider : MetadataFileReferenceProvider
        {
            private readonly Service _service;

            internal Provider(Service service)
            {
                Debug.Assert(service != null);
                _service = service;
            }

            public override PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return _service.GetReference(resolvedPath, properties);
            }
        }
    }
}
