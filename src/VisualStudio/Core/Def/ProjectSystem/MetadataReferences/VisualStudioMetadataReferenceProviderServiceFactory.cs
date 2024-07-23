// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

[ExportWorkspaceServiceFactory(typeof(IMetadataService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioMetadataServiceFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new Service(workspaceServices);

    private sealed class Service(HostWorkspaceServices workspaceServices) : IMetadataService
    {
        // We will defer creation of this reference manager until we have to to avoid it being constructed too early
        // and potentially causing deadlocks. We do initialize it on the UI thread in the
        // VisualStudioWorkspaceImpl.DeferredState constructor to ensure it gets created there.
        private readonly Lazy<VisualStudioMetadataReferenceManager> _manager = new(
            () => workspaceServices.GetRequiredService<VisualStudioMetadataReferenceManager>());

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            => _manager.Value.CreateMetadataReferenceSnapshot(resolvedPath, properties);
    }
}
