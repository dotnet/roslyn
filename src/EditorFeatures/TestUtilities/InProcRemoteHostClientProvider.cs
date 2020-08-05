// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Testing
{
    public enum TestHost
    {
        /// <summary>
        /// Features that optionally dispatch to a remote implementation service will
        /// not do so and instead directly call their local implementation.
        /// </summary>
        InProcess,

        /// <summary>
        /// Features that optionally dispatch to a remote implementation service will do so.
        /// This remote implementation will execute in the same process to simplify debugging
        /// and avoid cost of process management.
        /// </summary>
        OutOfProcess,
    }

    internal sealed class InProcRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider), ServiceLayer.Test), Shared, PartNotDiscoverable]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new InProcRemoteHostClientProvider(workspaceServices);
        }

        private readonly HostWorkspaceServices _services;
        private readonly AsyncLazy<RemoteHostClient> _lazyClient;

        public InProcRemoteHostClientProvider(HostWorkspaceServices services)
        {
            _services = services;

            _lazyClient = new AsyncLazy<RemoteHostClient>(
                cancellationToken => InProcRemoteHostClient.CreateAsync(_services, runCacheCleanup: false),
                cacheResult: true);
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken).AsNullable();
    }
}
