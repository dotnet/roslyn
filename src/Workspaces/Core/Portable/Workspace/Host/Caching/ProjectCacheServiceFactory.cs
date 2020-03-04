﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
