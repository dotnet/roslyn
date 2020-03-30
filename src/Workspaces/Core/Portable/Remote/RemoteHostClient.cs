// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This represents client in client/servier model.
    /// 
    /// user can create a connection to communicate with the server (remote host) through this client
    /// </summary>
    internal abstract partial class RemoteHostClient : IDisposable
    {
        public readonly Workspace Workspace;
        public event EventHandler<bool>? StatusChanged;

        private readonly IRemotableDataService _remoteDataService;

        protected RemoteHostClient(Workspace workspace)
        {
            Workspace = workspace;
            _remoteDataService = workspace.Services.GetRequiredService<IRemotableDataService>();
        }

        /// <summary>
        /// Return an unique string per client.
        /// 
        /// one can use this to distinguish different clients that are connected to different RemoteHosts including
        /// cases where 2 external process finding each others
        /// </summary>
        public abstract string ClientId { get; }

        /// <summary>
        /// Create <see cref="Connection"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public abstract Task<Connection?> TryCreateConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken);

        public abstract bool IsRemoteHost64Bit { get; }

        protected abstract void OnStarted();

        protected void Started()
        {
            OnStarted();

            OnStatusChanged(started: true);
        }

        public virtual void Dispose()
            => OnStatusChanged(started: false);

        private void OnStatusChanged(bool started)
            => StatusChanged?.Invoke(this, started);

        public static string CreateClientId(string prefix)
            => $"VS ({prefix}) ({Guid.NewGuid()})";

        public static Task<RemoteHostClient?> TryGetClientAsync(Project project, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return SpecializedTasks.Null<RemoteHostClient>();
            }

            return TryGetClientAsync(project.Solution.Workspace, cancellationToken);
        }

        public static Task<RemoteHostClient?> TryGetClientAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            var service = workspace.Services.GetService<IRemoteHostClientService>();
            if (service == null)
            {
                return SpecializedTasks.Null<RemoteHostClient>();
            }

            return service.TryGetRemoteHostClientAsync(cancellationToken);
        }

        /// <summary>
        /// Creates <see cref="KeepAliveSession"/> for the <paramref name="serviceName"/>, otherwise returns <see langword="null"/>.
        /// </summary>
        public async Task<KeepAliveSession?> TryCreateKeepAliveSessionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
        {
            var connection = await TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return null;
            }

            return new KeepAliveSession(this, connection, serviceName, callbackTarget);
        }

        public async Task<bool> TryRunRemoteAsync(string serviceName, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
        {
            using var connection = await TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return false;
            }

            await RunRemoteAsync(connection, _remoteDataService, targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public Task<Optional<T>> TryRunRemoteAsync<T>(string serviceName, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
            => TryRunRemoteAsync<T>(serviceName, targetName, solution, arguments, callbackTarget, dataReader: null, cancellationToken);

        public async Task<Optional<T>> TryRunRemoteAsync<T>(string serviceName, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, Func<Stream, CancellationToken, Task<T>>? dataReader, CancellationToken cancellationToken)
        {
            using var connection = await TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

            return await RunRemoteAsync<T>(connection, _remoteDataService, targetName, solution, arguments, dataReader, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task RunRemoteAsync(Connection connection, IRemotableDataService remoteDataService, string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            if (solution != null)
            {
                using var scope = await remoteDataService.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<object?>.GetInstance(arguments.Count + 1, out var argumentsBuilder);

                argumentsBuilder.Add(scope.SolutionInfo);
                argumentsBuilder.AddRange(arguments);

                await connection.InvokeAsync(targetName, argumentsBuilder, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await connection.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static async Task<T> RunRemoteAsync<T>(Connection connection, IRemotableDataService remoteDataService, string targetName, Solution? solution, IReadOnlyList<object?> arguments, Func<Stream, CancellationToken, Task<T>>? dataReader, CancellationToken cancellationToken)
        {
            if (solution != null)
            {
                using var scope = await remoteDataService.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<object?>.GetInstance(arguments.Count + 1, out var argumentsBuilder);

                argumentsBuilder.Add(scope.SolutionInfo);
                argumentsBuilder.AddRange(arguments);

                if (dataReader != null)
                {
                    return await connection.InvokeAsync(targetName, argumentsBuilder, dataReader, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await connection.InvokeAsync<T>(targetName, argumentsBuilder, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (dataReader != null)
            {
                return await connection.InvokeAsync(targetName, arguments, dataReader, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await connection.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// NoOpClient is used if a user killed our remote host process. Basically this client never
        /// create a session
        /// </summary>
        public class NoOpClient : RemoteHostClient
        {
            public NoOpClient(Workspace workspace)
                : base(workspace)
            {
            }

            public override string ClientId => nameof(NoOpClient);
            public override bool IsRemoteHost64Bit => false;

            public override Task<Connection?> TryCreateConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
                => SpecializedTasks.Null<Connection>();

            protected override void OnStarted()
            {
                // do nothing
            }
        }

        /// <summary>
        /// This is a connection between client and server. user can use this to communicate with remote host.
        /// 
        /// This doesn't know anything specific to Roslyn. this is general pure connection between client and server.
        /// </summary>
        public abstract class Connection : IDisposable
        {
            private bool _disposed;

            protected Connection()
            {
#if DEBUG
                _creationCallStack = Environment.StackTrace;
#endif
                _disposed = false;
            }

            public abstract Task InvokeAsync(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken);
            public abstract Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken);
            public abstract Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object?> arguments, Func<Stream, CancellationToken, Task<T>> dataReader, CancellationToken cancellationToken);

            protected virtual void DisposeImpl()
            {
                // do nothing
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                DisposeImpl();
                GC.SuppressFinalize(this);
            }

#if DEBUG
            private readonly string _creationCallStack;

            ~Connection()
            {
                // this can happen if someone kills OOP. 
                // when that happen, we don't want to crash VS, so this is debug only check
                if (!Environment.HasShutdownStarted)
                {
                    Debug.Assert(false,
                        $"Unless OOP process (RoslynCodeAnalysisService) is explicitly killed, this should have been disposed!\r\n {_creationCallStack}");
                }
            }
#endif
        }
    }
}
