// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceServiceFactory(typeof(IDocumentationProviderService), ServiceLayer.Default), Shared]
internal sealed class DocumentationProviderServiceFactory : IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DocumentationProviderServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DocumentationProviderService();

    internal sealed class DocumentationProviderService : IDocumentationProviderService
    {
        private readonly ConcurrentDictionary<string, DocumentationProvider> _assemblyPathToDocumentationProviderMap = new();

        public DocumentationProvider GetDocumentationProvider(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            assemblyPath = Path.ChangeExtension(assemblyPath, "xml");
            if (!_assemblyPathToDocumentationProviderMap.TryGetValue(assemblyPath, out var provider))
            {
                provider = _assemblyPathToDocumentationProviderMap.GetOrAdd(assemblyPath, XmlDocumentationProvider.CreateFromFile);
            }

            return provider;
        }
    }
}
