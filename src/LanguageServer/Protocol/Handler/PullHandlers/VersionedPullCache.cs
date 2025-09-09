// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Specialized cache used by the 'pull' LSP handlers.  Supports storing data to know when to tell a client
/// that existing results can be reused, or if new results need to be computed.  Multiple keys can be used,
/// with different computation costs to determine if the previous cached data is still valid.
/// </summary>
internal abstract partial class VersionedPullCache<TVersion, TState, TComputedData>(string uniqueKey)
{
    /// <summary>
    /// Map of workspace and diagnostic source to the data used to make the last pull report.
    /// This is a concurrent dictionary as parallel access is allowed for different workspace+project/doc combinations.
    /// 
    /// The <see cref="CacheItem"/> itself however will internally guarantee that the state for a specific workspace+project/doc will only
    /// be updated sequentially.
    /// </summary>
    private readonly ConcurrentDictionary<(Workspace workspace, ProjectOrDocumentId id), CacheItem> _idToLastReportedResult = [];

    /// <summary>
    /// The next available id to label results with.  Note that results are tagged on a per-document bases.  That
    /// way we can update results with the client with per-doc granularity.
    /// 
    /// Called by <see cref="CacheItem"/> with Interlocked access to ensure that all cache items generate unique resultIds.
    /// </summary>
    private long _nextDocumentResultId;

    /// <summary>
    /// Computes the version of the current state.  We compare the version of the current state against the
    /// version we have cached for the client's previous resultId.
    /// 
    /// Note - this will run under the semaphore in <see cref="CacheItem._gate"/>.
    /// </summary>
    public abstract Task<TVersion> ComputeVersionAsync(TState state, CancellationToken cancellationToken);

    /// <summary>
    /// Computes new data for this request.  This data must be hashable as it we store the hash with the requestId to determine if
    /// the data has changed between requests.
    /// 
    /// Note - this will run under the semaphore in <see cref="CacheItem._gate"/>.
    /// </summary>
    public abstract Task<TComputedData> ComputeDataAsync(TState state, CancellationToken cancellationToken);

    public abstract Checksum ComputeChecksum(TComputedData data, string language);

    /// <summary>
    /// If results have changed since the last request this calculates and returns a new
    /// non-null resultId to use for subsequent computation and caches it.
    /// </summary>
    /// <param name="idToClientLastResult">a map of roslyn document or project id to the previous result the client sent us for that doc.</param>
    /// <param name="projectOrDocumentId">the id of the project or document that we are checking to see if it has changed.</param>
    /// <returns>Null when results are unchanged, otherwise returns a non-null new resultId.</returns>
    public async Task<(string ResultId, TComputedData Data)?> GetOrComputeNewDataAsync(
        Dictionary<ProjectOrDocumentId, PreviousPullResult> idToClientLastResult,
        ProjectOrDocumentId projectOrDocumentId,
        Project project,
        TState state,
        CancellationToken cancellationToken)
    {
        // We have to make sure we've been fully loaded before using cached results as the previous results may not be complete.
        var isFullyLoaded = await IsFullyLoadedAsync(project.Solution, cancellationToken).ConfigureAwait(false);
        var previousResult = IDictionaryExtensions.GetValueOrDefault(idToClientLastResult, projectOrDocumentId);

        var cacheEntry = _idToLastReportedResult.GetOrAdd(
            (project.Solution.Workspace, projectOrDocumentId),
            static (_, uniqueKey) => new CacheItem(uniqueKey),
            uniqueKey);
        return await cacheEntry.UpdateCacheItemAsync(
            this, previousResult, isFullyLoaded, state, project.Language, cancellationToken).ConfigureAwait(false);
    }

    private long GetNextResultId()
    {
        return Interlocked.Increment(ref _nextDocumentResultId);
    }

    private static async Task<bool> IsFullyLoadedAsync(Solution solution, CancellationToken cancellationToken)
    {
        var workspaceStatusService = solution.Services.GetRequiredService<IWorkspaceStatusService>();
        var isFullyLoaded = await workspaceStatusService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
        return isFullyLoaded;
    }
}
