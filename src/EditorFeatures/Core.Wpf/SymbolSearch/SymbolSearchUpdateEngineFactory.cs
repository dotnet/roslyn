// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    internal static partial class SymbolSearchUpdateEngineFactory
    {
        public static async Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace,
            ISymbolSearchLogService logService,
            ISymbolSearchProgressService progressService,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var callbackObject = new CallbackObject(logService, progressService);
                var session = await client.CreateConnectionAsync(WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine, callbackObject, cancellationToken).ConfigureAwait(false);
                return new RemoteUpdateEngine(workspace, session);
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return new SymbolSearchUpdateEngine(logService, progressService);
        }

        private sealed partial class RemoteUpdateEngine : ISymbolSearchUpdateEngine
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

        private class CallbackObject : ISymbolSearchLogService, ISymbolSearchProgressService
        {
            private readonly ISymbolSearchLogService _logService;
            private readonly ISymbolSearchProgressService _progressService;

            public CallbackObject(ISymbolSearchLogService logService, ISymbolSearchProgressService progressService)
            {
                _logService = logService;
                _progressService = progressService;
            }

            public Task LogExceptionAsync(string exception, string text)
                => _logService.LogExceptionAsync(exception, text);

            public Task LogInfoAsync(string text)
                => _logService.LogInfoAsync(text);

            public Task OnDownloadFullDatabaseStartedAsync(string title)
                => _progressService.OnDownloadFullDatabaseStartedAsync(title);

            public Task OnDownloadFullDatabaseSucceededAsync()
                => _progressService.OnDownloadFullDatabaseSucceededAsync();

            public Task OnDownloadFullDatabaseCanceledAsync()
                => _progressService.OnDownloadFullDatabaseCanceledAsync();

            public Task OnDownloadFullDatabaseFailedAsync(string message)
                => _progressService.OnDownloadFullDatabaseFailedAsync(message);
        }
    }
}
