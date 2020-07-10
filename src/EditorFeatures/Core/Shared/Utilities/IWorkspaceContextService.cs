// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal interface IWorkspaceContextService : IWorkspaceService
    {
        /// <summary>
        /// Used to determine if running as a client in a cloud connected environment.
        /// </summary>
        bool IsCloudEnvironmentClient();
    }

    internal sealed class DefaultWorkspaceContextService : IWorkspaceContextService
    {
        public bool IsCloudEnvironmentClient() => false;
    }

    internal sealed class CloudEnvironmentWorkspaceContextService : IWorkspaceContextService
    {
        public bool IsCloudEnvironmentClient() => true;
    }

    [ExportWorkspaceServiceFactory(typeof(IWorkspaceContextService), ServiceLayer.Editor), Shared]
    internal sealed class VisualStudioWorkspaceContextServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceContextServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace.Kind == WorkspaceKind.CloudEnvironmentClientWorkspace)
            {
                return new CloudEnvironmentWorkspaceContextService();
            }

            return new DefaultWorkspaceContextService();
        }
    }
}
