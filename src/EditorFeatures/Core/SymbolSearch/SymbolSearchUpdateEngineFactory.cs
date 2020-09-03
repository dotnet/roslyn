// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
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
                var callbackObject = new CallbackObject(logService);
                var session = await client.CreateConnectionAsync(WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine, callbackObject, cancellationToken).ConfigureAwait(false);
                return new RemoteUpdateEngine(workspace, session);
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return CreateEngineInProcess(logService);
        }

        /// <summary>
        /// This returns a No-op engine if called on non-Windows OS, because the backing storage depends on Windows APIs.
        /// </summary>
        public static ISymbolSearchUpdateEngine CreateEngineInProcess(ISymbolSearchLogService logService)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new SymbolSearchUpdateEngine(logService)
                : (ISymbolSearchUpdateEngine)new NoOpUpdateEngine();
        }

        private sealed class NoOpUpdateEngine : ISymbolSearchUpdateEngine
        {
            public Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
                => SpecializedTasks.EmptyImmutableArray<PackageWithAssemblyResult>();

            public Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
                => SpecializedTasks.EmptyImmutableArray<PackageWithTypeResult>();

            public Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
                => SpecializedTasks.EmptyImmutableArray<ReferenceAssemblyWithTypeResult>();

            public Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory)
                => Task.CompletedTask;
        }

        private sealed class RemoteUpdateEngine : ISymbolSearchUpdateEngine
        {
            private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

            private readonly Workspace _workspace;
            private readonly RemoteServiceConnection _session;

            public RemoteUpdateEngine(
                Workspace workspace,
                RemoteServiceConnection session)
            {
                _workspace = workspace;
                _session = session;
            }

            public void Dispose()
            {
                _session.Dispose();
            }

            public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity, CancellationToken cancellationToken)
            {
                var results = await _session.RunRemoteAsync<IList<PackageWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithTypeAsync),
                    solution: null,
                    new object[] { source, name, arity },
                    cancellationToken).ConfigureAwait(false);

                return results.ToImmutableArray();
            }

            public async Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string assemblyName, CancellationToken cancellationToken)
            {
                var results = await _session.RunRemoteAsync<IList<PackageWithAssemblyResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithAssemblyAsync),
                    solution: null,
                    new object[] { source, assemblyName },
                    cancellationToken).ConfigureAwait(false);

                return results.ToImmutableArray();
            }

            public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity, CancellationToken cancellationToken)
            {
                var results = await _session.RunRemoteAsync<IList<ReferenceAssemblyWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindReferenceAssembliesWithTypeAsync),
                    solution: null,
                    new object[] { name, arity },
                    cancellationToken).ConfigureAwait(false);

                return results.ToImmutableArray();
            }

            public Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory)
                => _session.RunRemoteAsync(
                    nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                    solution: null,
                    new object[] { sourceName, localSettingsDirectory },
                    CancellationToken.None);
        }

        private class CallbackObject : ISymbolSearchLogService
        {
            private readonly ISymbolSearchLogService _logService;

            public CallbackObject(ISymbolSearchLogService logService)
            {
                _logService = logService;
            }

            public Task LogExceptionAsync(string exception, string text)
                => _logService.LogExceptionAsync(exception, text);

            public Task LogInfoAsync(string text)
                => _logService.LogInfoAsync(text);
        }
    }
}
