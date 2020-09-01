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
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class JsonRpcConnection : RemoteServiceConnection
    {
        private readonly HostWorkspaceServices _services;
        private readonly RemoteEndPoint _serviceEndPoint;
        private readonly SolutionAssetStorage _solutionAssetStorage;

        // Non-null if the connection is pooled.
        private IPooledConnectionReclamation? _poolReclamation;

        // True if the underlying end-point has been disposed.
        private bool _disposed;

        public JsonRpcConnection(
            HostWorkspaceServices services,
            TraceSource logger,
            object? callbackTarget,
            Stream serviceStream,
            IPooledConnectionReclamation? poolReclamation)
        {
            _solutionAssetStorage = services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            _services = services;

            _serviceEndPoint = new RemoteEndPoint(serviceStream, logger, callbackTarget);
            _serviceEndPoint.UnexpectedExceptionThrown += UnexpectedExceptionThrown;
            _serviceEndPoint.StartListening();

            _poolReclamation = poolReclamation;
#if DEBUG
            _creationCallStack = Environment.StackTrace;
#endif
        }

#if DEBUG
        private readonly string _creationCallStack;

        ~JsonRpcConnection()
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

        internal void SetPoolReclamation(IPooledConnectionReclamation poolReclamation)
        {
            Contract.ThrowIfNull(poolReclamation);

            // Atomically transition from null to not-null, and verify that it was successful.
            var previousPoolReclamation = Interlocked.CompareExchange(ref _poolReclamation, poolReclamation, null);
            Contract.ThrowIfFalse(previousPoolReclamation is null);
        }

        public override void Dispose()
        {
            // If the connection was taken from a pool, return it to the pool.
            // Otherwise, dispose the underlying end-point and transition to "disposed" state.

            var poolReclamation = Interlocked.Exchange(ref _poolReclamation, null);
            if (poolReclamation != null)
            {
                poolReclamation.Return(this);
                return;
            }

            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // dispose service and snapshot channels
            _serviceEndPoint.UnexpectedExceptionThrown -= UnexpectedExceptionThrown;
            _serviceEndPoint.Dispose();

#if DEBUG
            GC.SuppressFinalize(this);
#endif
        }

        private void UnexpectedExceptionThrown(Exception exception)
            => _services.GetService<IErrorReportingService>()?.ShowRemoteHostCrashedErrorInfo(exception);

        public override async Task RunRemoteAsync(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            if (solution != null)
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<object?>.GetInstance(arguments.Count + 1, out var argumentsBuilder);

                argumentsBuilder.Add(scope.SolutionInfo);
                argumentsBuilder.AddRange(arguments);

                await _serviceEndPoint.InvokeAsync(targetName, argumentsBuilder, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _serviceEndPoint.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task<T> RunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, Func<Stream, CancellationToken, Task<T>>? dataReader, CancellationToken cancellationToken)
        {
            if (solution != null)
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<object?>.GetInstance(arguments.Count + 1, out var argumentsBuilder);

                argumentsBuilder.Add(scope.SolutionInfo);
                argumentsBuilder.AddRange(arguments);

                if (dataReader != null)
                {
                    return await _serviceEndPoint.InvokeAsync(targetName, argumentsBuilder, dataReader, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await _serviceEndPoint.InvokeAsync<T>(targetName, argumentsBuilder, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (dataReader != null)
            {
                return await _serviceEndPoint.InvokeAsync(targetName, arguments, dataReader, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await _serviceEndPoint.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
