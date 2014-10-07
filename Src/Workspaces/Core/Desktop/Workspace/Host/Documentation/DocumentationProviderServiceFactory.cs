using System;
using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentationProviderService), ServiceLayer.Default), Shared]
    internal sealed class DocumentationProviderServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new DocumentationProviderService();
        }

        internal sealed class DocumentationProviderService : IDocumentationProviderService
        {
            private readonly ConcurrentDictionary<string, DocumentationProvider> assemblyPathToDocumentationProviderMap =
                new ConcurrentDictionary<string, DocumentationProvider>();

            public DocumentationProvider GetDocumentationProvider(string assemblyPath)
            {
                if (assemblyPath == null)
                {
                    throw new ArgumentNullException("assemblyPath");
                }

                assemblyPath = Path.ChangeExtension(assemblyPath, "xml");

                DocumentationProvider provider;
                if (!this.assemblyPathToDocumentationProviderMap.TryGetValue(assemblyPath, out provider))
                {
                    provider = this.assemblyPathToDocumentationProviderMap.GetOrAdd(assemblyPath, _path => new FileBasedXmlDocumentationProvider(_path));
                }

                return provider;
            }
        }
    }
}
