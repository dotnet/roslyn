// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceServiceFactory(typeof(IMetadataService), ServiceLayer.Default), Shared]
internal sealed class MetadataServiceFactory : IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MetadataServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultMetadataService(workspaceServices.GetService<IDocumentationProviderService>());

    private sealed class DefaultMetadataService(IDocumentationProviderService documentationService) : IMetadataService
    {
        private readonly MetadataReferenceCache _metadataCache = new((path, properties) =>
                MetadataReference.CreateFromFile(path, properties, documentationService.GetDocumentationProvider(path)));

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            => (PortableExecutableReference)_metadataCache.GetReference(resolvedPath, properties);
    }
}
