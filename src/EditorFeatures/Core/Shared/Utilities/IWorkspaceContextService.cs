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
        bool IsInRemoteClientContext();
    }

    internal sealed class DefaultWorkspaceContextService : IWorkspaceContextService
    {
        public bool IsInRemoteClientContext() => false;
    }

    [ExportWorkspaceServiceFactory(typeof(IWorkspaceContextService), ServiceLayer.Editor), Shared]
    internal sealed class DefaultWorkspaceContextServiceFactory : IWorkspaceServiceFactory
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultWorkspaceContextServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new DefaultWorkspaceContextService();
        }
    }
}
