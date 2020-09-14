// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Manages remote workspaces. Currently supports only a single, primary workspace of kind <see cref="WorkspaceKind.RemoteWorkspace"/>. 
    /// In future it should support workspaces of all kinds.
    /// </summary>
    internal class RemoteWorkspaceManager
    {
        /// <summary>
        /// Default workspace manager used by the product. Tests may specify a custom <see cref="RemoteWorkspaceManager"/>
        /// in order to override workspace services.
        /// </summary>
        internal static readonly RemoteWorkspaceManager Default = new RemoteWorkspaceManager(
            new SolutionAssetCache(cleanupInterval: TimeSpan.FromMinutes(1), purgeAfter: TimeSpan.FromMinutes(3), gcAfter: TimeSpan.FromMinutes(5)));

        internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
            MefHostServices.DefaultAssemblies
                .Add(typeof(ServiceBase).Assembly)
                .Add(typeof(RemoteWorkspacesResources).Assembly);

        private readonly Lazy<RemoteWorkspace> _lazyPrimaryWorkspace;
        internal readonly SolutionAssetCache SolutionAssetCache;

        // TODO: remove
        private IAssetSource? _solutionAssetSource;

        public RemoteWorkspaceManager(SolutionAssetCache assetCache)
        {
            _lazyPrimaryWorkspace = new Lazy<RemoteWorkspace>(CreatePrimaryWorkspace);
            SolutionAssetCache = assetCache;
        }

        // TODO: remove
        internal IAssetSource GetAssetSource()
        {
            Contract.ThrowIfNull(_solutionAssetSource, "Storage not initialized");
            return _solutionAssetSource;
        }

        // TODO: remove
        internal void InitializeAssetSource(IAssetSource assetSource)
        {
            Contract.ThrowIfFalse(_solutionAssetSource == null);
            _solutionAssetSource = assetSource;
        }

        [Obsolete("To be removed: https://github.com/dotnet/roslyn/issues/43477")]
        public IAssetSource? TryGetAssetSource()
            => _solutionAssetSource;

        private static RemoteWorkspace CreatePrimaryWorkspace()
            => new RemoteWorkspace(MefHostServices.Create(RemoteHostAssemblies), WorkspaceKind.RemoteWorkspace);

        public virtual RemoteWorkspace GetWorkspace()
            => _lazyPrimaryWorkspace.Value;
    }
}
