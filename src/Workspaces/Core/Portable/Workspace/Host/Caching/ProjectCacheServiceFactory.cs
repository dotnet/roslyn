// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IProjectCacheService), ServiceLayer.Default)]
    [Shared]
    internal class ProjectCacheServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public ProjectCacheServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var hostService = workspaceServices.GetService<IProjectCacheHostService>();
            return new Service(hostService);
        }

        private class Service : IProjectCacheService
        {
            private readonly IProjectCacheHostService _hostService;

            public Service(IProjectCacheHostService hostService)
            {
                _hostService = hostService;
            }

            public IDisposable EnableCaching(ProjectId key)
            {
                return _hostService != null
                    ? _hostService.EnableCaching(key)
                    : null;
            }
        }
    }
}
