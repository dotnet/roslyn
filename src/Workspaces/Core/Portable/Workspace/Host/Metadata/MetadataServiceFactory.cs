// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

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

        private static ModuleMetadata CreateModuleMetadata(string path, bool prefetchEntireImage)
        {
            var fileStream = FileUtilities.OpenRead(path);

            var options = PEStreamOptions.PrefetchMetadata;
            if (prefetchEntireImage)
            {
                options |= PEStreamOptions.PrefetchEntireImage;
            }

            return ModuleMetadata.CreateFromStream(fileStream, options);
        }

        private static ImmutableArray<ModuleMetadata> GetAllModules(ModuleMetadata manifestModule, string assemblyDir)
        {
            var moduleNames = manifestModule.GetModuleNames();
            if (moduleNames is [])
            {
                return [manifestModule];
            }

            var moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance(moduleNames.Length + 1);
            moduleBuilder.Add(manifestModule);

            foreach (var moduleName in moduleNames)
            {
                var module = CreateModuleMetadata(PathUtilities.CombineAbsoluteAndRelativePaths(assemblyDir, moduleName)!, prefetchEntireImage: false);
                moduleBuilder.Add(module);
            }

            return moduleBuilder.ToImmutableAndFree();
        }

        private PortableExecutableReference CreateReference(
            string path, MetadataReferenceProperties properties)
        {
            var documentationProvider = _documentationProviderService.GetDocumentationProvider(path);

            try
            {
                if (properties.Kind == MetadataImageKind.Module)
                {
                    var module = CreateModuleMetadata(path, prefetchEntireImage: true);
                    return module.GetReference(documentationProvider, filePath: path, display: null).WithProperties(properties);
                }

                var primaryModule = CreateModuleMetadata(path, prefetchEntireImage: false);

                // Get all the modules, and load them. Create an assembly metadata.
                var allModules = GetAllModules(primaryModule, PathUtilities.GetDirectoryName(path));

                var assembly = AssemblyMetadata.Create(allModules);
                return assembly.GetReference(documentationProvider, filePath: path, display: null).WithProperties(properties);
            }
            catch (Exception e) when (e is IOException or BadImageFormatException)
            {
                // Store failed references in the cache so that the behavior stays consistent once we observe the failure.
                return new ThrowingExecutableReference(path, properties, documentationProvider, e);
            }
        }

        private sealed class ThrowingExecutableReference(string resolvedPath, MetadataReferenceProperties properties, DocumentationProvider documentationProvider, Exception exception)
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
