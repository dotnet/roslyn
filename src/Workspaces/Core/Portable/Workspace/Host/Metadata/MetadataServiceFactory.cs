// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceServiceFactory(typeof(IMetadataService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class MetadataServiceFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new MetadataService(
            workspaceServices.GetRequiredService<IDocumentationProviderService>(),
            workspaceServices.GetRequiredService<IMetadataReferenceCacheService>());

    private sealed class MetadataService : IMetadataService
    {
        private readonly IDocumentationProviderService _documentationProviderService;
        private readonly IMetadataReferenceCacheService _metadataReferenceCacheService;
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _createReference;

        public MetadataService(
            IDocumentationProviderService documentationProviderService,
            IMetadataReferenceCacheService metadataReferenceCacheService)
        {
            _documentationProviderService = documentationProviderService;
            _metadataReferenceCacheService = metadataReferenceCacheService;
            _createReference = CreateReference;
        }

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            => _metadataReferenceCacheService.GetReference(
                resolvedPath, properties, _createReference);

        private PortableExecutableReference CreateReference(
            string path, MetadataReferenceProperties properties)
        {
            var documentationProvider = _documentationProviderService.GetDocumentationProvider(path);

            try
            {
                return MetadataReference.CreateFromFile(path, properties, documentationProvider);
            }
            catch (IOException e)
            {
                // Store failed references in the cache so that the behavior stays consistent once we observe the failure.
                return new ThrowingExecutableReference(path, properties, documentationProvider, e);
            }
        }

        private sealed class ThrowingExecutableReference(string resolvedPath, MetadataReferenceProperties properties, DocumentationProvider documentationProvider, IOException exception)
            : PortableExecutableReference(properties, resolvedPath)
        {
            protected override DocumentationProvider CreateDocumentationProvider()
                => documentationProvider;

            protected override Metadata GetMetadataImpl()
                => throw exception;

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
                => new ThrowingExecutableReference(FilePath!, properties, documentationProvider, exception);
        }
    }
}
