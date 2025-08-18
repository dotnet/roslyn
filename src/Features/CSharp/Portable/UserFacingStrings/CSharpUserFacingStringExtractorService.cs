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
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

[ExportLanguageService(typeof(IUserFacingStringExtractorService), LanguageNames.CSharp), Shared]
internal sealed class CSharpUserFacingStringExtractorService : IUserFacingStringExtractorService
{
    private readonly UserFacingStringGlobalCache _globalCache = new();
    
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUserFacingStringExtractorService()
    {
    }

    public async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> ExtractAndAnalyzeAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        // Use the new document-centric cache system for better performance
        return await ExtractAndAnalyzeWithDocumentCacheAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> ExtractAndAnalyzeWithDocumentCacheAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        // Get the Copilot service
        var copilotService = document.GetLanguageService<ICopilotCodeAnalysisService>();
        if (copilotService == null || !await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;

        // STEP 1: Extract all string literals (with basic context for stable caching)
        var allStringsWithBasicContext = await ExtractStringLiteralsWithBasicContextAsync(document, cancellationToken).ConfigureAwait(false);
        if (allStringsWithBasicContext.IsEmpty)
            return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;

        // STEP 2: Check cache and separate cached vs uncached strings
        var (cachedResults, uncachedStrings) = SeparateCachedFromUncached(document.Id, allStringsWithBasicContext);

        // STEP 3: Send uncached strings to AI (with enhanced context for better analysis)
        if (uncachedStrings.Count > 0)
        {
            var newAnalyses = await AnalyzeUncachedStringsWithAIAsync(document, uncachedStrings, cancellationToken).ConfigureAwait(false);
            cachedResults.AddRange(newAnalyses);
        }

        return cachedResults.ToImmutableAndFree();
    }

    /// <summary>
    /// STEP 1: Extract all string literals with basic context for stable cache keys.
    /// </summary>
    private static async Task<ImmutableArray<UserFacingStringCandidate>> ExtractStringLiteralsWithBasicContextAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree == null)
            return ImmutableArray<UserFacingStringCandidate>.Empty;

        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var stringsWithBasicContext = ArrayBuilder<UserFacingStringCandidate>.GetInstance();

        // Extract ALL string literals - no filtering whatsoever
        foreach (var stringLiteral in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (stringLiteral.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                var valueText = stringLiteral.Token.ValueText;

                // Only skip completely empty strings
                if (!string.IsNullOrEmpty(valueText))
                {
                    // Store basic context for stable cache keys
                    var basicContext = EnhancedContextExtractor.ExtractBasicContext(stringLiteral);
                    
                    var stringWithBasicContext = new UserFacingStringCandidate(
                        stringLiteral.Span,
                        valueText,
                        basicContext); // Basic context for cache consistency
                    stringsWithBasicContext.Add(stringWithBasicContext);
                }
            }
        }

        return stringsWithBasicContext.ToImmutableAndFree();
    }

    /// <summary>
    /// STEP 2: Separate cached strings (cache hits) from uncached strings (need AI analysis).
    /// </summary>
    private (ArrayBuilder<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)> cachedResults, 
             ArrayBuilder<UserFacingStringCandidate> uncachedStrings) SeparateCachedFromUncached(
        DocumentId documentId,
        ImmutableArray<UserFacingStringCandidate> allStrings)
    {
        var cachedResults = ArrayBuilder<(UserFacingStringCandidate, UserFacingStringAnalysis)>.GetInstance();
        var uncachedStrings = ArrayBuilder<UserFacingStringCandidate>.GetInstance();

        foreach (var stringCandidate in allStrings)
        {
            // Use basic context for stable cache lookup
            var cacheKey = new StringCacheKey(stringCandidate.Value, stringCandidate.Context);
            
            if (_globalCache.TryGetCachedAnalysis(documentId, cacheKey, out var cacheEntry))
            {
                // CACHE HIT: Reuse existing analysis - SAVES AI CALL!
                cachedResults.Add((stringCandidate, cacheEntry.Analysis));
            }
            else
            {
                // CACHE MISS: Need AI analysis
                uncachedStrings.Add(stringCandidate);
            }
        }

        return (cachedResults, uncachedStrings);
    }

    /// <summary>
    /// STEP 3: Send uncached strings to AI with enhanced context for better analysis.
    /// </summary>
    private async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> AnalyzeUncachedStringsWithAIAsync(
        Document document,
        ArrayBuilder<UserFacingStringCandidate> uncachedStrings,
        CancellationToken cancellationToken)
    {
        var results = ArrayBuilder<(UserFacingStringCandidate, UserFacingStringAnalysis)>.GetInstance();
        
        // Create enhanced context versions for AI (AI gets rich context)
        var stringsWithEnhancedContext = await CreateStringsWithEnhancedContextForAIAsync(document, uncachedStrings.ToImmutable(), cancellationToken).ConfigureAwait(false);
        
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var proposalForAI = new UserFacingStringProposal(sourceText.ToString(), stringsWithEnhancedContext);
        
        var copilotService = document.GetLanguageService<ICopilotCodeAnalysisService>();
        var aiResult = await copilotService!.GetUserFacingStringAnalysisAsync(proposalForAI, cancellationToken).ConfigureAwait(false);

        if (!aiResult.isQuotaExceeded && aiResult.responseDictionary != null)
        {
            // Cache and return the new AI analyses
            foreach (var uncachedString in uncachedStrings)
            {
                if (aiResult.responseDictionary.TryGetValue(uncachedString.Value, out var analysis))
                {
                    // Cache using basic context for stable keys
                    var cacheEntry = new StringCacheEntry(uncachedString.Value, uncachedString.Context, analysis);
                    _globalCache.AddOrUpdateEntry(document.Id, cacheEntry);
                    
                    results.Add((uncachedString, analysis));
                }
            }
        }

        return results.ToImmutableAndFree();
    }

    /// <summary>
    /// Convert strings with basic context to strings with enhanced context for AI analysis.
    /// </summary>
    private static async Task<ImmutableArray<UserFacingStringCandidate>> CreateStringsWithEnhancedContextForAIAsync(
        Document document,
        ImmutableArray<UserFacingStringCandidate> stringsWithBasicContext,
        CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree == null)
            return stringsWithBasicContext; // Fallback to basic context

        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var stringsWithEnhancedContext = ArrayBuilder<UserFacingStringCandidate>.GetInstance();

        // Find syntax nodes and extract enhanced context for each string
        foreach (var stringWithBasicContext in stringsWithBasicContext)
        {
            // Find the string literal at this location
            var node = root.FindNode(stringWithBasicContext.Location);
            if (node is LiteralExpressionSyntax stringLiteral && 
                stringLiteral.Token.IsKind(SyntaxKind.StringLiteralToken) &&
                stringLiteral.Token.ValueText == stringWithBasicContext.Value)
            {
                // Extract enhanced context for AI
                var enhancedContext = await EnhancedContextExtractor.ExtractEnhancedContextAsync(
                    stringLiteral, document, cancellationToken).ConfigureAwait(false);
                
                var stringWithEnhancedContext = new UserFacingStringCandidate(
                    stringWithBasicContext.Location,
                    stringWithBasicContext.Value,
                    enhancedContext); // Enhanced context for AI
                
                stringsWithEnhancedContext.Add(stringWithEnhancedContext);
            }
            else
            {
                // Fallback: use basic context if we can't find the syntax node
                stringsWithEnhancedContext.Add(stringWithBasicContext);
            }
        }

        return stringsWithEnhancedContext.ToImmutableAndFree();
    }

    /// <summary>
    /// Gets cached results using the document-centric cache.
    /// </summary>
    public Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetCachedResultsAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var results = ArrayBuilder<(UserFacingStringCandidate, UserFacingStringAnalysis)>.GetInstance();
        
        // Get cached entries from the document-centric cache
        var cachedEntries = _globalCache.GetDocumentEntries(document.Id);
        
        foreach (var entry in cachedEntries)
        {
            if (!entry.IsExpired)
            {
                // Create a candidate from the cached entry
                var candidate = new UserFacingStringCandidate(
                    default, // TextSpan not stored in cache
                    entry.StringValue,
                    entry.BasicContext);
                
                results.Add((candidate, entry.Analysis));
            }
        }

        return Task.FromResult(results.ToImmutableAndFree());
    }
}
