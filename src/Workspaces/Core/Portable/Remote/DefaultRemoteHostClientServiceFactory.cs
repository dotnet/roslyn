// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Default implementation of IRemoteHostClientService
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientService)), Shared]
    internal partial class DefaultRemoteHostClientServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultRemoteHostClientServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new RemoteHostClientService(workspaceServices.Workspace);
        }
    }
}
