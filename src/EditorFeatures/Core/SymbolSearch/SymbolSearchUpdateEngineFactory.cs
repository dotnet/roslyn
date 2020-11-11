// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Factory that will produce the <see cref="ISymbolSearchUpdateEngine"/>.  The default
    /// implementation produces an engine that will run in-process.  Implementations at
    /// other layers can behave differently (for example, running the engine out-of-process).
    /// </summary>
    /// <remarks>
    /// This returns an No-op engine on non-Windows OS, because the backing storage depends on Windows APIs.
    /// </remarks>
    internal static partial class SymbolSearchUpdateEngineFactory
    {
        public static async Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace,
            ISymbolSearchLogService logService,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                return new RemoteUpdateEngine(client, logService);
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return CreateEngineInProcess();
        }

        /// <summary>
        /// This returns a No-op engine if called on non-Windows OS, because the backing storage depends on Windows APIs.
        /// </summary>
        public static ISymbolSearchUpdateEngine CreateEngineInProcess()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new SymbolSearchUpdateEngine()
                : (ISymbolSearchUpdateEngine)new NoOpUpdateEngine();
        }

        private sealed class NoOpUpdateEngine : ISymbolSearchUpdateEngine
        {
            public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(ImmutableArray<PackageWithAssemblyResult>.Empty);

            public ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(ImmutableArray<PackageWithTypeResult>.Empty);

            public ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty);

            public ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, ISymbolSearchLogService logService, CancellationToken cancellationToken)
                => default;
        }

        private sealed class RemoteUpdateEngine : ISymbolSearchUpdateEngine
        {
            private readonly RemoteServiceConnection<IRemoteSymbolSearchUpdateService> _connection;

            public RemoteUpdateEngine(RemoteHostClient client, ISymbolSearchLogService logService)
                => _connection = client.CreateConnection<IRemoteSymbolSearchUpdateService>(logService);

            public void Dispose()
                => _connection.Dispose();

            public async ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
            {
                var result = await _connection.TryInvokeAsync<ImmutableArray<PackageWithTypeResult>>(
                    (service, cancellationToken) => service.FindPackagesWithTypeAsync(source, name, arity, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : ImmutableArray<PackageWithTypeResult>.Empty;
            }

            public async ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string assemblyName, CancellationToken cancellationToken)
            {
                var result = await _connection.TryInvokeAsync<ImmutableArray<PackageWithAssemblyResult>>(
                    (service, cancellationToken) => service.FindPackagesWithAssemblyAsync(source, assemblyName, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : ImmutableArray<PackageWithAssemblyResult>.Empty;
            }

            public async ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity, CancellationToken cancellationToken)
            {
                var result = await _connection.TryInvokeAsync<ImmutableArray<ReferenceAssemblyWithTypeResult>>(
                    (service, cancellationToken) => service.FindReferenceAssembliesWithTypeAsync(name, arity, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty;
            }

            public async ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, ISymbolSearchLogService logService, CancellationToken cancellationToken)
            {
                // logService parameter is ignored since it's already set on the connection as a callback
                _ = logService;

                _ = await _connection.TryInvokeAsync(
                    (service, callbackId, cancellationToken) => service.UpdateContinuouslyAsync(callbackId, sourceName, localSettingsDirectory, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
