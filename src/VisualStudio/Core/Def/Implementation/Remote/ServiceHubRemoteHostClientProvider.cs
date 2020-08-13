﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed class ServiceHubRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider), WorkspaceKind.Host), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            {
                if (!RemoteHostOptions.IsUsingServiceHubOutOfProcess(workspaceServices)
                    || workspaceServices.Workspace is not VisualStudioWorkspace)
                {
                    // Run code in the current process
                    return new DefaultRemoteHostClientProvider();
                }

                return new ServiceHubRemoteHostClientProvider(workspaceServices);
            }
        }

        private readonly HostWorkspaceServices _services;
        private readonly AsyncLazy<RemoteHostClient> _lazyClient;

        private ServiceHubRemoteHostClientProvider(HostWorkspaceServices services)
        {
            _services = services;
            _lazyClient = new AsyncLazy<RemoteHostClient>(cancellationToken => ServiceHubRemoteHostClient.CreateAsync(_services, cancellationToken), cacheResult: true);
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken).AsNullable();
    }
}
