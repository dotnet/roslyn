// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
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
            private readonly IDocumentationProviderService documentationService;
            private readonly Provider provider;

            public Service(IDocumentationProviderService documentationService)
            {
                this.documentationService = documentationService;
                this.provider = new Provider(this);
            }

            public MetadataReferenceProvider GetProvider()
            {
                return provider;
            }

            public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return MetadataReference.CreateFromFile(resolvedPath, properties, this.documentationService.GetDocumentationProvider(resolvedPath));
            }
        }

        private sealed class Provider : MetadataReferenceProvider
        {
            private readonly Service service;

            internal Provider(Service service)
            {
                Debug.Assert(service != null);
                this.service = service;
            }        

            public override PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return service.GetReference(resolvedPath, properties);
            }
        }
    }
}
