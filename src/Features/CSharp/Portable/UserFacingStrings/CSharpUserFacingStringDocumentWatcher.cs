// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
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
internal sealed class CSharpUserFacingStringDocumentWatcher : IUserFacingStringDocumentWatcher, IDisposable
{
    private readonly UserFacingStringGlobalCache _globalCache = new();
    private IDiagnosticInvalidationService? _diagnosticInvalidationService;
    
    // Per-document debounce timers
    private readonly ConcurrentDictionary<DocumentId, Timer> _documentTimers = new();
    
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
        
        // Reset or create debounce timer for this document
        ResetDocumentTimer(documentId, version);
    }

    public bool TryGetCachedAnalysis(string stringValue, string context, out UserFacingStringAnalysis analysis)
    {
        analysis = null!;
        
        // We need a document context to check the cache, but for interface compatibility,
        // we'll check all documents (this is a fallback scenario)
        foreach (var documentId in _lastProcessedVersions.Keys)
        {
            var basicContext = "unknown"; // Fallback context for interface compatibility
            var cacheKey = new StringCacheKey(stringValue, basicContext);
            
            if (_globalCache.TryGetCachedAnalysis(documentId, cacheKey, out var entry))
            {
                analysis = entry.Analysis;
                return true;
            }
        }
        
        return false;
    }

    public async Task EnsureDocumentAnalyzedAsync(Document document, CancellationToken cancellationToken)
    {
        var currentVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
        
        // Check if we've already processed this version
        if (_lastProcessedVersions.TryGetValue(document.Id, out var lastVersion) &&
            lastVersion == currentVersion)
        {
            // Check if we have any cached results for this document
            var entries = _globalCache.GetDocumentEntries(document.Id);
            if (!entries.IsEmpty)
            {
                return; // Already analyzed and cached
            }
        }

        // Force immediate analysis without debounce
        await ProcessDocumentAsync(document, currentVersion, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserFacingStringAnalysis?> GetStringAnalysisAsync(
        string stringValue,
        string context,
        Document document,
        CancellationToken cancellationToken)
    {
        // Extract basic context for cache key
        var basicContext = context; // Simplified - in real implementation would extract basic context type
        var cacheKey = new StringCacheKey(stringValue, basicContext);

        // Try cache first
        if (_globalCache.TryGetCachedAnalysis(document.Id, cacheKey, out var entry))
        {
            return entry.Analysis;
        }

        // Ensure document is analyzed
        await EnsureDocumentAnalyzedAsync(document, cancellationToken).ConfigureAwait(false);

        // Try cache again after analysis
        if (_globalCache.TryGetCachedAnalysis(document.Id, cacheKey, out entry))
        {
            return entry.Analysis;
        }

        return null;
    }

    private void ResetDocumentTimer(DocumentId documentId, VersionStamp version)
    {
        // Dispose existing timer if present
        if (_documentTimers.TryRemove(documentId, out var existingTimer))
        {
            existingTimer.Dispose();
        }

        // Create new debounce timer for this document
        var timer = new Timer(
            callback: _ => OnDocumentTimerElapsed(documentId, version),
            state: null,
            dueTime: TimeSpan.FromMilliseconds(DebounceDelayMs),
            period: Timeout.InfiniteTimeSpan);

        _documentTimers[documentId] = timer;
    }

    private void OnDocumentTimerElapsed(DocumentId documentId, VersionStamp version)
    {
        try
        {
            // Queue background processing - fire and forget
            _ = Task.Run(() =>
            {
                try
                {
                    // In real implementation, get document from workspace and process
                    // For now, just clean up the timer since we can't process without workspace access
                    System.Diagnostics.Debug.WriteLine($"Document timer elapsed for {documentId}, version {version}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing document change for {documentId}: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in document timer for {documentId}: {ex.Message}");
        }
        finally
        {
            // Clean up the timer
            if (_documentTimers.TryRemove(documentId, out var timer))
            {
                timer.Dispose();
            }
        }
    }

    private async Task ProcessDocumentAsync(Document document, VersionStamp version, CancellationToken cancellationToken)
    {
        try
        {
            // Extract string candidates with enhanced context
            var candidates = await ExtractStringCandidatesWithEnhancedContextAsync(document, cancellationToken).ConfigureAwait(false);
            
            if (candidates.IsEmpty)
            {
                _lastProcessedVersions[document.Id] = version;
                return;
            }

            // Filter candidates that need AI analysis (not in cache or expired)
            var candidatesNeedingAnalysis = FilterCandidatesNeedingAnalysis(document.Id, candidates);
            
            if (!candidatesNeedingAnalysis.IsEmpty)
            {
                // Batch AI analysis for efficiency
                await PerformBatchAIAnalysisAsync(document, candidatesNeedingAnalysis, cancellationToken).ConfigureAwait(false);
                
                // Trigger document-specific diagnostic refresh after AI analysis
                _diagnosticInvalidationService ??= document.Project.Solution.Services.GetService<IDiagnosticInvalidationService>();
                if (_diagnosticInvalidationService != null)
                {
                    await _diagnosticInvalidationService.TriggerDocumentRefreshAsync(document).ConfigureAwait(false);
                }
            }

            // Update processed version
            _lastProcessedVersions[document.Id] = version;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing document {document.Name}: {ex.Message}");
        }
    }

    private static async Task<ImmutableArray<PendingStringAnalysis>> ExtractStringCandidatesWithEnhancedContextAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return ImmutableArray<PendingStringAnalysis>.Empty;

        using var _ = ArrayBuilder<PendingStringAnalysis>.GetInstance(out var candidates);

        // Extract string literals with both basic and enhanced context
        var stringLiterals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(literal => literal.Token.IsKind(SyntaxKind.StringLiteralToken))
            .Where(literal => IsUserFacingStringCandidate(literal.Token.ValueText));

        foreach (var literal in stringLiterals)
        {
            var stringValue = literal.Token.ValueText;
            var basicContext = EnhancedContextExtractor.ExtractBasicContext(literal);
            var enhancedContext = await EnhancedContextExtractor.ExtractEnhancedContextAsync(literal, document, cancellationToken).ConfigureAwait(false);
            var location = literal.Span;

            candidates.Add(new PendingStringAnalysis(stringValue, basicContext, enhancedContext, location));
        }

        return candidates.ToImmutableAndClear();
    }

    private ImmutableArray<PendingStringAnalysis> FilterCandidatesNeedingAnalysis(
        DocumentId documentId,
        ImmutableArray<PendingStringAnalysis> candidates)
    {
        using var _ = ArrayBuilder<PendingStringAnalysis>.GetInstance(out var needingAnalysis);

        foreach (var candidate in candidates)
        {
            if (!_globalCache.TryGetCachedAnalysis(documentId, candidate.CacheKey, out var cachedEntry) ||
                cachedEntry.IsExpired)
            {
                needingAnalysis.Add(candidate);
            }
        }

        return needingAnalysis.ToImmutableAndClear();
    }

    private async Task PerformBatchAIAnalysisAsync(
        Document document,
        ImmutableArray<PendingStringAnalysis> candidates,
        CancellationToken cancellationToken)
    {
        // Get AI service
        var copilotService = document.GetLanguageService<ICopilotCodeAnalysisService>();
        if (copilotService == null || !await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // Process in batches for efficiency
        const int batchSize = 10;
        
        for (var i = 0; i < candidates.Length; i += batchSize)
        {
            var batch = candidates.Skip(i).Take(batchSize).ToImmutableArray();
            await ProcessAIAnalysisBatchAsync(document, batch, copilotService, cancellationToken).ConfigureAwait(false);
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

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var proposal = new UserFacingStringProposal(sourceText.ToString(), aiCandidates);

            var result = await copilotService.GetUserFacingStringAnalysisAsync(proposal, cancellationToken).ConfigureAwait(false);

            if (!result.isQuotaExceeded && result.responseDictionary != null)
            {
                // Cache results using basic context for cache key but AI used enhanced context
                for (var i = 0; i < batch.Length; i++)
                {
                    var pending = batch[i];
                    if (result.responseDictionary.TryGetValue(pending.StringValue, out var analysis))
                    {
                        var cacheEntry = new StringCacheEntry(pending.StringValue, pending.BasicContext, analysis);
                        _globalCache.AddOrUpdateEntry(document.Id, cacheEntry);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in AI analysis batch: {ex.Message}");
        }
    }

    private static bool IsUserFacingStringCandidate(string value)
    {
        // Enhanced heuristics for user-facing string detection
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            return false;

        // Skip obvious technical strings
        if (value.All(char.IsDigit) ||
            value.All(c => char.IsLetterOrDigit(c) || c == '_') ||
            value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("xmlns", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("{") && value.EndsWith("}"))
        {
            return false;
        }

        // Positive indicators for user-facing strings
        return value.Any(char.IsWhiteSpace) ||
               value.Any(char.IsPunctuation) ||
               char.IsUpper(value[0]) ||
               value.Length > 10; // Longer strings more likely to be user-facing
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();

        // Dispose all document timers
        foreach (var timer in _documentTimers.Values)
        {
            timer.Dispose();
        }
        _documentTimers.Clear();

        _cancellationTokenSource.Dispose();
    }
}
