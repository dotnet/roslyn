// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Document-centric watcher with per-document debounce timers and batch AI processing.
/// Uses ConditionalWeakTable for automatic memory management and enhanced context for AI analysis.
/// Implements multi-tier diagnostic invalidation for optimal performance.
/// </summary>
[ExportLanguageService(typeof(IUserFacingStringDocumentWatcher), LanguageNames.CSharp), Shared]
internal sealed class CSharpUserFacingStringDocumentWatcher : IUserFacingStringDocumentWatcher
{
    private readonly UserFacingStringGlobalCache _globalCache = new();
    
    // Document version tracking for change detection
    private readonly ConcurrentDictionary<DocumentId, VersionStamp> _lastProcessedVersions = new();
    
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private const int DebounceDelayMs = 500; // 500ms debounce per document

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUserFacingStringDocumentWatcher()
    {
        // Diagnostic invalidation service will be obtained when needed from the workspace
    }

    public void OnDocumentChanged(DocumentId documentId, VersionStamp version)
    {
        // Update version tracking
        _lastProcessedVersions[documentId] = version;
    }

    public bool TryGetCachedAnalysis(string stringValue, string context, out UserFacingStringAnalysis analysis)
    {
        analysis = null!;
        
        // Use the provided basic context directly for the cache key
        var cacheKey = new StringCacheKey(stringValue, context);

        // Try all known documents; if any has the entry, return it
        foreach (var documentId in _lastProcessedVersions.Keys)
        {
            if (_globalCache.TryGetCachedAnalysis(documentId, cacheKey, out var entry))
            {
                analysis = entry.Analysis;
                return true;
            }
        }

        return false;
    }


    public async Task AnalyzeSpecificStringsAsync(
        Document document,
        IReadOnlyList<(string stringValue, string basicContext, TextSpan location)> stringsToAnalyze,
        CancellationToken cancellationToken)
    {
        if (stringsToAnalyze.Count == 0)
            return;

        try
        {
            // Get AI service
            var copilotService = document.GetLanguageService<ICopilotCodeAnalysisService>();
            if (copilotService == null || !await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                // AI not available, fallback to heuristic analysis
                await AnalyzeWithHeuristicAsync(document, stringsToAnalyze, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Convert specific strings to pending analysis format
            using var _ = ArrayBuilder<PendingStringAnalysis>.GetInstance(out var pendingAnalyses);

            foreach (var (stringValue, basicContext, location) in stringsToAnalyze)
            {
                // Extract enhanced context for AI
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var foundNode = root?.FindNode(location, getInnermostNodeForTie: true);
                
                System.Diagnostics.Debug.WriteLine("String: " + stringValue + ", Location: " + location + ", Found node type: " + (foundNode?.GetType().Name ?? "null"));
                
                if (foundNode is LiteralExpressionSyntax literal)
                {
                    var token = literal.Token;
                    var enhancedContext = await EnhancedContextExtractor.ExtractEnhancedContextAsync(token, document, cancellationToken).ConfigureAwait(false);
                    pendingAnalyses.Add(new PendingStringAnalysis(stringValue, basicContext, enhancedContext, location));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to find LiteralExpressionSyntax for: " + stringValue);
                }
            }

            if (!pendingAnalyses.IsEmpty)
            {
                // Process with AI
                await ProcessAIAnalysisBatchAsync(document, pendingAnalyses.ToImmutableAndClear(), copilotService, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error analyzing specific strings in document {document.Name}: {ex.Message}");
            
            // AI analysis failed, fallback to heuristic analysis
            try
            {
                await AnalyzeWithHeuristicAsync(document, stringsToAnalyze, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception heuristicEx)
            {
                System.Diagnostics.Debug.WriteLine($"Heuristic fallback also failed: {heuristicEx.Message}");
            }
        }
    }


    private async Task ProcessAIAnalysisBatchAsync(
        Document document,
        ImmutableArray<PendingStringAnalysis> batch,
        ICopilotCodeAnalysisService copilotService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Convert to candidates for AI service
            var aiCandidates = batch.Select(p => new UserFacingStringCandidate(
                p.Location,
                p.StringValue,
                p.EnhancedContext)) // Use enhanced context for AI
                .ToImmutableArray();

            var proposal = new UserFacingStringProposal(aiCandidates);

            var result = await copilotService.GetUserFacingStringAnalysisAsync(proposal, cancellationToken).ConfigureAwait(false);

            if (!result.isQuotaExceeded && result.responseDictionary != null)
            {
                var cacheEntriesAdded = 0;
                
                // Cache results using basic context for cache key but AI used enhanced context
                for (var i = 0; i < batch.Length; i++)
                {
                    var pending = batch[i];
                    if (result.responseDictionary.TryGetValue(pending.StringValue, out var analysis))
                    {
                        var cacheEntry = new StringCacheEntry(pending.StringValue, pending.BasicContext, analysis);
                        _globalCache.AddOrUpdateEntry(document.Id, cacheEntry);
                        cacheEntriesAdded++;
                    }
                }

                // 🎯 NEW: Trigger document refresh if we cached any results
                if (cacheEntriesAdded > 0)
                {
                    await TriggerDocumentRefreshAsync(document, cacheEntriesAdded, "AI").ConfigureAwait(false);
                }
            }
            else if (result.isQuotaExceeded)
            {
                // Quota exceeded: fallback to heuristic analysis for the same strings
                System.Diagnostics.Debug.WriteLine("AI quota exceeded for document " + document.Name + ", falling back to heuristic analysis for " + batch.Length + " strings");
                
                var stringsToAnalyze = batch.Select(p => (p.StringValue, p.BasicContext, p.Location)).ToList();
                await AnalyzeWithHeuristicAsync(document, stringsToAnalyze, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error in AI analysis batch: " + ex.Message);
        }
    }

    /// <summary>
    /// Triggers a document refresh after caching analysis results.
    /// </summary>
    private Task TriggerDocumentRefreshAsync(Document document, int cacheEntriesAdded, string analysisMethod)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Small delay to ensure cache is fully updated
                await Task.Delay(100).ConfigureAwait(false);
                
                var invalidationService = document.Project.Solution.Services.GetService<IDiagnosticInvalidationService>();
                if (invalidationService != null)
                {
                    await invalidationService.TriggerDocumentRefreshAsync(document).ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"✅ Triggered refresh for document {document.Name} after caching {cacheEntriesAdded} {analysisMethod} results");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to trigger document refresh: {ex.Message}");
            }
        });
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Analyzes a specific list of strings using heuristic analysis and updates the cache.
    /// </summary>
    private async Task AnalyzeWithHeuristicAsync(
        Document document,
        IReadOnlyList<(string stringValue, string basicContext, TextSpan location)> stringsToAnalyze,
        CancellationToken cancellationToken)
    {
        try
        {
            await CSharp.MoveToResx.CSharpMoveToResxHeuristicHelper.AnalyzeBatchAsync(
                document, stringsToAnalyze, _globalCache, cancellationToken).ConfigureAwait(false);
            
            // Trigger refresh after heuristic analysis
            await TriggerDocumentRefreshAsync(document, stringsToAnalyze.Count, "Heuristic").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in heuristic analysis: {ex.Message}");
        }
    }
}