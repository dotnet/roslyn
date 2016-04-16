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
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var manager = workspaceServices.GetService<VisualStudioMetadataReferenceManager>();
            Debug.Assert(manager != null);

            return new Service(manager);
        }

        private sealed class Service : IMetadataService
        {
            private readonly VisualStudioMetadataReferenceManager _manager;

            public Service(VisualStudioMetadataReferenceManager manager)
            {
                _manager = manager;
            }

            public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return _manager.CreateMetadataReferenceSnapshot(resolvedPath, properties);
            }
        }
    }
}
