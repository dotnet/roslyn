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
            workspaceServices.GetRequiredService<IMetadataProviderService>());

    private sealed class MetadataService(
        IDocumentationProviderService documentationProviderService,
        IMetadataProviderService metadataProviderService) : IMetadataService
    {
        private readonly MetadataReferenceCache _metadataCache = new((path, properties) =>
        {
            var documentationProvider = documentationProviderService.GetDocumentationProvider(path);

            try
            {
                var metadata = metadataProviderService.GetMetadata(path, properties.Kind).Metadata;
                return metadata switch
                {
                    AssemblyMetadata assembly => assembly.GetReference(documentationProvider, filePath: path).WithProperties(properties),
                    ModuleMetadata module => module.GetReference(documentationProvider, path).WithProperties(properties),
                    _ => throw ExceptionUtilities.UnexpectedValue(metadata.Kind),
                };
            }
            catch (IOException e)
            {
                // Store failed references in the cache so that the behavior stays consistent once we observe the failure.
                return new ThrowingExecutableReference(path, properties, documentationProvider, e);
            }
        });

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            => (PortableExecutableReference)_metadataCache.GetReference(resolvedPath, properties);

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
