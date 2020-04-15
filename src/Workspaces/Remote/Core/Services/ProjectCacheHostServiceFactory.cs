// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    [ExportWorkspaceServiceFactory(typeof(IProjectCacheHostService), ServiceLayer.Host)]
    [Shared]
    internal partial class ProjectCacheHostServiceFactory : IWorkspaceServiceFactory
    {
        private const int ImplicitCacheTimeoutInMS = 10000;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectCacheHostServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new ProjectCacheService(workspaceServices.Workspace, ImplicitCacheTimeoutInMS);
    }
}
