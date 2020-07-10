// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Test.Utilities.Remote;
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
}

namespace Microsoft.CodeAnalysis.Test.Utilities.RemoteHost
{
    internal static class RemoteHostOptions
    {
        public static readonly Option2<bool> RemoteHostTest = new Option2<bool>(
            nameof(RemoteHostOptions), nameof(RemoteHostTest), defaultValue: false);
    }

    [ExportOptionProvider, Shared]
    internal sealed class RemoteHostOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteHostOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RemoteHostOptions.RemoteHostTest);
    }

    internal sealed class InProcRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider)), Shared]
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
        private readonly AsyncLazy<RemoteHostClient?> _lazyClient;

        public InProcRemoteHostClientProvider(HostWorkspaceServices services)
        {
            _services = services;

            _lazyClient = new AsyncLazy<RemoteHostClient?>(cancellationToken =>
            {
                var optionService = _services.GetRequiredService<IOptionService>();
                if (optionService.GetOption(RemoteHostOptions.RemoteHostTest))
                {
                    return InProcRemoteHostClient.CreateAsync(_services, runCacheCleanup: false).AsNullable();
                }

                return SpecializedTasks.Null<RemoteHostClient>();
            }, cacheResult: true);
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken);
    }
}
