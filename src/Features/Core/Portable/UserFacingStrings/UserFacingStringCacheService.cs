// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Service that provides caching and throttling for user-facing string analysis to prevent
/// excessive AI calls when documents change frequently.
/// </summary>
internal sealed class UserFacingStringCacheService
{
    private readonly ConditionalWeakTable<Document, CachedAnalysisResult> _documentCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastAnalysisTime = new();
    
    // Throttle: Only allow one analysis per document per 30 seconds
    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(30);

    private sealed class CachedAnalysisResult
    {
        public ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)> Results { get; }
        public VersionStamp DocumentVersion { get; }
        public DateTime AnalysisTime { get; }

        public CachedAnalysisResult(
            ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)> results,
            VersionStamp documentVersion)
        {
            Results = results;
            DocumentVersion = documentVersion;
            AnalysisTime = DateTime.UtcNow;
        }
    }

    public async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetOrAnalyzeAsync(
        Document document,
        Func<Document, CancellationToken, Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>>> analyzer,
        CancellationToken cancellationToken)
    {
        var currentVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
        var documentPath = document.FilePath ?? document.Id.ToString();

        // Check if we have a cached result that's still valid
        if (_documentCache.TryGetValue(document, out var cachedResult))
        {
            // Return cached result if the document hasn't changed
            if (cachedResult.DocumentVersion == currentVersion)
            {
                return cachedResult.Results;
            }
        }

        // Check throttling - only allow analysis if enough time has passed
        var now = DateTime.UtcNow;
        if (_lastAnalysisTime.TryGetValue(documentPath, out var lastTime))
        {
            var timeSinceLastAnalysis = now - lastTime;
            if (timeSinceLastAnalysis < ThrottleInterval)
            {
                // Return cached result if available, otherwise empty
                return cachedResult?.Results ?? ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;
            }
        }

        // Update the last analysis time before starting analysis to prevent concurrent calls
        _lastAnalysisTime[documentPath] = now;

        try
        {
            // Perform the analysis
            var results = await analyzer(document, cancellationToken).ConfigureAwait(false);
            
            // Cache the results
            var newCachedResult = new CachedAnalysisResult(results, currentVersion);
            _documentCache.Remove(document);
            _documentCache.Add(document, newCachedResult);

            return results;
        }
        catch
        {
            // If analysis fails, restore the previous timestamp to allow retry sooner
            if (cachedResult != null)
            {
                _lastAnalysisTime[documentPath] = lastTime;
            }
            else
            {
                _lastAnalysisTime.TryRemove(documentPath, out _);
            }
            throw;
        }
    }

    /// <summary>
    /// Gets cached results without triggering new analysis. Used for fast retrieval.
    /// </summary>
    public async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetCachedResultsAsync(
        Document document, 
        CancellationToken cancellationToken)
    {
        if (_documentCache.TryGetValue(document, out var cachedResult))
        {
            var currentVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
            if (cachedResult.DocumentVersion == currentVersion)
            {
                return cachedResult.Results;
            }
        }

        return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;
    }

    /// <summary>
    /// Clears the cache for a specific document. Used when we want to force re-analysis.
    /// </summary>
    public void ClearDocumentCache(Document document)
    {
        _documentCache.Remove(document);
        var documentPath = document.FilePath ?? document.Id.ToString();
        _lastAnalysisTime.TryRemove(documentPath, out _);
    }
}
