// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceServiceFactory(typeof(IMetadataService), ServiceLayer.Host), Shared]
internal sealed class MetadataServiceFactory : IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MetadataServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return new MetadataService(workspaceServices.GetRequiredService<IDocumentationProviderService>());
    }

    internal sealed class MetadataService : IMetadataService
    {
        private readonly IDocumentationProviderService _documentationProviderService;

        public MetadataService(IDocumentationProviderService documentationProviderService)
        {
            _documentationProviderService = documentationProviderService;
        }

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
        {
            // HACK: right now the FileWatchedPortableExecutableReferenceFactory in Roslyn presumes that each time it calls IMetadataService
            // it gets a unique instance back; the default MetadataService at the workspace layer has a cache to encourage sharing, but that
            // breaks the assumption.
            try
            {
                return MetadataReference.CreateFromFile(resolvedPath, properties, _documentationProviderService.GetDocumentationProvider(resolvedPath));
            }
            catch (IOException ex)
            {
                return new ThrowingExecutableReference(resolvedPath, properties, ex);
            }
        }

        private class ThrowingExecutableReference : PortableExecutableReference
        {
            private readonly IOException _ex;

            public ThrowingExecutableReference(string resolvedPath, MetadataReferenceProperties properties, IOException ex) : base(properties, resolvedPath)
            {
                _ex = ex;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                throw new NotImplementedException();
            }

            protected override Metadata GetMetadataImpl()
            {
                throw _ex;
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                return new ThrowingExecutableReference(FilePath!, properties, _ex);
            }
        }
    }
}
