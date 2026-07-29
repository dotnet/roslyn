// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceServiceFactory(typeof(IMetadataService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerMetadataServiceFactory(ServerConfiguration serverConfiguration) : IWorkspaceServiceFactory
{
    private readonly SharedMetadataCache? _sharedMetadataCache = serverConfiguration.UseSharedMetadataCache
        ? new SharedMetadataCache(collectStatistics: serverConfiguration.CollectSharedMetadataCacheStatistics)
        : null;

    internal SharedMetadataCache.Statistics? GetSharedMetadataCacheStatistics()
        => serverConfiguration.CollectSharedMetadataCacheStatistics
            ? _sharedMetadataCache?.GetStatistics()
            : null;

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new MetadataService(
            workspaceServices.GetRequiredService<IDocumentationProviderService>(),
            _sharedMetadataCache);

    private sealed class MetadataService(
        IDocumentationProviderService documentationProviderService,
        SharedMetadataCache? metadataCache) : IMetadataService
    {
        private readonly MetadataReferenceCache _metadataCache = new((path, properties) =>
        {
            var documentationProvider = documentationProviderService.GetDocumentationProvider(path);

            try
            {
                if (metadataCache is null)
                    return MetadataReference.CreateFromFile(path, properties, documentationProvider);

                var metadata = metadataCache.GetMetadata(path, properties.Kind);
                return metadata switch
                {
                    AssemblyMetadata assembly => assembly.GetReference(
                        documentationProvider,
                        properties.Aliases,
                        properties.EmbedInteropTypes,
                        path),
                    ModuleMetadata module => module.GetReference(documentationProvider, path),
                    _ => throw ExceptionUtilities.UnexpectedValue(metadata.Kind),
                };
            }
            catch (IOException e)
            {
                return new ThrowingExecutableReference(path, properties, documentationProvider, e);
            }
        });

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            => (PortableExecutableReference)_metadataCache.GetReference(resolvedPath, properties);

        private sealed class ThrowingExecutableReference(
            string resolvedPath,
            MetadataReferenceProperties properties,
            DocumentationProvider documentationProvider,
            IOException exception) : PortableExecutableReference(properties, resolvedPath)
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
