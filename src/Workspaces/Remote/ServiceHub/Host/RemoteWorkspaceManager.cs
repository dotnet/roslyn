// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Manages remote workspaces. Currently supports only a single, primary workspace of kind <see cref="WorkspaceKind.RemoteWorkspace"/>. 
    /// In future it should support workspaces of all kinds.
    /// </summary>
    internal class RemoteWorkspaceManager
    {
        internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
            MefHostServices.DefaultAssemblies
                .Add(typeof(ServiceBase).Assembly)
                .Add(typeof(RemoteWorkspacesResources).Assembly);

        private readonly Lazy<RemoteWorkspace> _lazyPrimaryWorkspace;

        public RemoteWorkspaceManager()
        {
            _lazyPrimaryWorkspace = new Lazy<RemoteWorkspace>(CreatePrimaryWorkspace);
        }

        private static RemoteWorkspace CreatePrimaryWorkspace()
            => new RemoteWorkspace(MefHostServices.Create(RemoteHostAssemblies), WorkspaceKind.RemoteWorkspace);

        public virtual RemoteWorkspace GetWorkspace()
            => _lazyPrimaryWorkspace.Value;
    }
}
