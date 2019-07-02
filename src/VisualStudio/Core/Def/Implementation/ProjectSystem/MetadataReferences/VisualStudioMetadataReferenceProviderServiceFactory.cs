// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceServiceFactory(typeof(IMetadataService), ServiceLayer.Host), Shared]
    internal sealed class VsMetadataServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public VsMetadataServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices);
        }

        private sealed class Service : IMetadataService
        {
            private readonly Lazy<VisualStudioMetadataReferenceManager> _manager;

            public Service(HostWorkspaceServices workspaceServices)
            {
                // We will defer creation of this reference manager until we have to to avoid it being constructed too
                // early and potentially causing deadlocks. We do initialize it on the UI thread in the
                // VisualStudioWorkspaceImpl.DeferredState constructor to ensure it gets created there.
                _manager = new Lazy<VisualStudioMetadataReferenceManager>(
                    () => workspaceServices.GetRequiredService<VisualStudioMetadataReferenceManager>());
            }

            public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return _manager.Value.CreateMetadataReferenceSnapshot(resolvedPath, properties);
            }
        }
    }
}
