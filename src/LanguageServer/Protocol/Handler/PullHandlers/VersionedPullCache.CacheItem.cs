// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal abstract partial class VersionedPullCache<TVersion, TState, TComputedData>
{
    /// <summary>
    /// Internal cache item that updates state for a particular <see cref="Workspace"/> and <see cref="ProjectOrDocumentId"/> in <see cref="VersionedPullCache{TVersion, TState, TComputedData}"/>
    /// This type ensures that the state for a particular key is never updated concurrently for the same key (but different key states can be concurrent).
    /// </summary>
    private sealed class CacheItem(string uniqueKey)
    {
        /// <summary>
        /// Guards access to <see cref="_lastResult"/>.
        /// This ensures that a cache entry is fully updated in a single transaction.
        /// </summary>
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// Stores the current state associated with this cache item.
        /// Guarded by <see cref="_gate"/>
        /// 
        /// <list type="bullet">
        ///   <item>The resultId reported to the client.</item>
        ///   <item>The TCheapVersion of the data that was used to calculate results.
        ///       <para>
        ///       Note that this version can change even when nothing has actually changed (for example, forking the 
        ///       LSP text, reloading the same project). So we additionally store:</para></item>
        ///   <item>A TExpensiveVersion (normally a checksum) checksum that will still allow us to reuse data even when
        ///   unimportant changes happen that trigger the cheap version change detection.</item>
        ///   <item>The checksum of the data that was computed when the resultId was generated.
        ///       <para>
        ///       When the versions above change, we must recalculate the data.  However sometimes that data ends up being exactly the same as the prior request.
        ///       When that happens, this allows us to send back an unchanged result instead of reserializing data the client already has.
        ///       </para>
        ///   </item>
        /// </list>
        /// 
        /// </summary>
        private (string resultId, TVersion version, Checksum dataChecksum)? _lastResult;

        /// <summary>
        /// Updates the values for this cache entry.  Guarded by <see cref="_gate"/>
        /// 
        /// Returns <see langword="null"/> if the previousPullResult can be re-used, otherwise returns a new resultId and the new data associated with it.
        /// </summary>
        public async Task<(string, TComputedData)?> UpdateCacheItemAsync(
            VersionedPullCache<TVersion, TState, TComputedData> cache,
            PreviousPullResult? previousPullResult,
            bool isFullyLoaded,
            TState state,
            string language,
            CancellationToken cancellationToken)
        {
            // Ensure that we only update the cache item one at a time.
            // This means that the computation of new data for this item only occurs sequentially.
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                TVersion version;

                // Check if the version we have in the cache matches the request version.  If so we can re-use the resultId.
                if (isFullyLoaded &&
                    _lastResult is not null &&
                    _lastResult.Value.resultId == previousPullResult?.PreviousResultId)
                {
                    // The current cheap version does not match the last reported.  This may be because we've forked
                    // or reloaded a project, so fall back to calculating the full expensive version to determine if
                    // anything is actually changed.
                    version = await cache.ComputeVersionAsync(state, cancellationToken).ConfigureAwait(false);
                    if (version != null && version.Equals(_lastResult.Value.version))
                    {
                        return null;
                    }
                }
                else
                {
                    // The version we have in our cache does not match the one provided by the client (if any).
                    // We need to calculate new results.
                    version = await cache.ComputeVersionAsync(state, cancellationToken).ConfigureAwait(false);
                }

                // Compute the new result for the request.
                var data = await cache.ComputeDataAsync(state, cancellationToken).ConfigureAwait(false);
                var dataChecksum = cache.ComputeChecksum(data, language);

                string newResultId;
                if (_lastResult is not null && _lastResult?.resultId == previousPullResult?.PreviousResultId && _lastResult?.dataChecksum == dataChecksum)
                {
                    // The new data we've computed is exactly the same as the data we computed last time even though the versions have changed.
                    // Instead of reserializing everything, we can return the same result id back to the client.

                    // Ensure we store the updated versions we calculated against old resultId.  If we do not do this,
                    // subsequent requests will always fail the version comparison check (the resultId is still associated with the older version even
                    // though we reused it here for a newer version) and will trigger re-computation.
                    // By storing the updated version with the resultId we can short circuit earlier in the version checks.
                    _lastResult = (_lastResult.Value.resultId, version, dataChecksum);
                    return null;
                }
                else
                {
                    // Keep track of the results we reported here so that we can short-circuit producing results for
                    // the same state of the world in the future.  Use a custom result-id per type (doc requests or workspace
                    // requests) so that clients of one don't errantly call into the other.
                    //
                    // For example, a client getting document diagnostics should not ask for workspace diagnostics with the result-ids it got for
                    // doc-diagnostics.  The two systems are different and cannot share results, or do things like report
                    // what changed between each other.
                    //
                    // Note that we can safely update the map before computation as any cancellation or exception
                    // during computation means that the client will never recieve this resultId and so cannot ask us for it.
                    newResultId = $"{uniqueKey}:{cache.GetNextResultId()}";
                    _lastResult = (newResultId, version, dataChecksum);
                    return (newResultId, data);
                }
            }
        }
    }
}

