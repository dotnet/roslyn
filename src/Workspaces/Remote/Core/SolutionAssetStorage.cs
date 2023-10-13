﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Stores solution snapshots available to remote services.
/// </summary>
internal partial class SolutionAssetStorage
{
    /// <summary>
    /// Lock over <see cref="_checksumToScope"/>.  Note: We could consider making this a SemaphoreSlim if
    /// the locking proves to be a problem. However, it would greatly complicate the implementation and consumption
    /// side due to the pattern around <c>await using</c>.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Mapping from operation checksum to the scope for the syncing operation that we've created for it.
    /// Ref-counted so that if we have many concurrent calls going out from the host to the OOP side that we share
    /// the same storage here so that all OOP calls can safely call back into us and get the assets they need, even
    /// if individual calls get canceled.
    /// </summary>
    private readonly Dictionary<Checksum, Scope> _checksumToScope = new();

    public Scope GetScope(Checksum solutionChecksum)
    {
        lock (_gate)
        {
            if (!_checksumToScope.TryGetValue(solutionChecksum, out var scope))
            {
                Debug.Fail($"Request for solution-checksum '{solutionChecksum}' that was not pinned on the host side.");
                throw new InvalidOperationException($"Request for solution-checksum '{solutionChecksum}' that was not pinned on the host side.");
            }

            return scope;
        }
    }

    /// <summary>
    /// Adds given snapshot into the storage. This snapshot will be available within the returned <see cref="Scope"/>.
    /// </summary>
    public ValueTask<Scope> StoreAssetsAsync(Solution solution, CancellationToken cancellationToken)
        => StoreAssetsAsync(solution.State, cancellationToken);

    /// <inheritdoc cref="StoreAssetsAsync(Solution, CancellationToken)"/>
    public ValueTask<Scope> StoreAssetsAsync(Project project, CancellationToken cancellationToken)
        => StoreAssetsAsync(project.Solution.State, project.Id, cancellationToken);

    /// <inheritdoc cref="StoreAssetsAsync(Solution, CancellationToken)"/>
    public ValueTask<Scope> StoreAssetsAsync(SolutionState solution, CancellationToken cancellationToken)
        => StoreAssetsAsync(solution, projectId: null, cancellationToken);

    /// <inheritdoc cref="StoreAssetsAsync(Solution, CancellationToken)"/>
    public async ValueTask<Scope> StoreAssetsAsync(SolutionState solutionState, ProjectId? projectId, CancellationToken cancellationToken)
    {
        var checksum = projectId == null
            ? await solutionState.GetChecksumAsync(cancellationToken).ConfigureAwait(false)
            : await solutionState.GetChecksumAsync(projectId, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            if (_checksumToScope.TryGetValue(checksum, out var scope))
            {
                Contract.ThrowIfTrue(scope.RefCount <= 0);
                scope.RefCount++;
                return scope;
            }

            scope = new Scope(this, checksum, solutionState);
            _checksumToScope[checksum] = scope;
            return scope;
        }
    }

    private void DecreaseScopeRefCount(Scope scope)
    {
        lock (_gate)
        {
            var solutionChecksum = scope.SolutionChecksum;
            var existingScope = _checksumToScope[solutionChecksum];
            Contract.ThrowIfTrue(existingScope != scope);

            Contract.ThrowIfTrue(scope.RefCount <= 0);
            scope.RefCount--;

            // If our refcount is still above 0, then nothing else to do at this point.
            if (scope.RefCount > 0)
                return;

            // Last ref went away, update our maps while under the lock, then cleanup its context data outside of the lock.
            _checksumToScope.Remove(solutionChecksum);
        }

        scope.ReplicationContext.Dispose();
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly SolutionAssetStorage _solutionAssetStorage;

        internal TestAccessor(SolutionAssetStorage solutionAssetStorage)
        {
            _solutionAssetStorage = solutionAssetStorage;
        }

        public async ValueTask<object> GetRequiredAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            return await _solutionAssetStorage._checksumToScope.Single().Value.GetTestAccessor().GetAssetAsync(checksum, cancellationToken).ConfigureAwait(false);
        }

        public bool IsPinned(Checksum checksum)
        {
            lock (_solutionAssetStorage._gate)
            {
                return _solutionAssetStorage._checksumToScope.TryGetValue(checksum, out var scope) &&
                    scope.RefCount >= 1;
            }
        }

        public int PinnedScopesCount
        {
            get
            {
                lock (_solutionAssetStorage._gate)
                {
                    return _solutionAssetStorage._checksumToScope.Count;
                }
            }
        }
    }
}
