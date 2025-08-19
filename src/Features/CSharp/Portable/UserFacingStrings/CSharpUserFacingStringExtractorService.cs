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
    private readonly UserFacingStringCacheService _legacyCacheService = new(); // For backward compatibility
    
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

        // Extract all string literals with enhanced context
        var candidates = await ExtractStringLiteralsWithContextAsync(document, cancellationToken).ConfigureAwait(false);
        if (candidates.IsEmpty)
            return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;

        var results = ArrayBuilder<(UserFacingStringCandidate, UserFacingStringAnalysis)>.GetInstance();
        var newCandidates = ArrayBuilder<UserFacingStringCandidate>.GetInstance();

        foreach (var candidate in candidates)
        {
            var basicContext = EnhancedContextExtractor.ExtractBasicContext(default); // Extract from syntax
            var cacheKey = new StringCacheKey(candidate.Value, basicContext);
            
            if (_globalCache.TryGetCachedAnalysis(document.Id, cacheKey, out var cacheEntry))
            {
                // CACHE HIT: Reuse existing analysis - SAVES AI CALL!
                results.Add((candidate, cacheEntry.Analysis));
            }
            else
            {
                // CACHE MISS: Need to analyze this string
                newCandidates.Add(candidate);
            }
        }

        // Only send NEW/CHANGED strings to AI
        if (newCandidates.Count > 0)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newProposal = new UserFacingStringProposal(sourceText.ToString(), newCandidates.ToImmutable());
            
            var result = await copilotService.GetUserFacingStringAnalysisAsync(newProposal, cancellationToken).ConfigureAwait(false);

            if (!result.isQuotaExceeded && result.responseDictionary != null)
            {
                // Cache the new results in the document-centric cache
                foreach (var candidate in newCandidates.ToImmutable())
                {
                    if (result.responseDictionary.TryGetValue(candidate.Value, out var analysis))
                    {
                        var basicContext = EnhancedContextExtractor.ExtractBasicContext(default);
                        var cacheEntry = new StringCacheEntry(candidate.Value, basicContext, analysis);
                        _globalCache.AddOrUpdateEntry(document.Id, cacheEntry);
                        
                        results.Add((candidate, analysis));
                    }
                }
            }
        }

        newCandidates.Free();
        return results.ToImmutableAndFree();
    }

    private static async Task<ImmutableArray<UserFacingStringCandidate>> ExtractStringLiteralsWithContextAsync(
        Document document, 
        CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree == null)
            return ImmutableArray<UserFacingStringCandidate>.Empty;

        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var candidates = ArrayBuilder<UserFacingStringCandidate>.GetInstance();

        // Extract ALL string literals - no filtering whatsoever
        foreach (var stringLiteral in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (stringLiteral.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                var valueText = stringLiteral.Token.ValueText;

                // Only skip completely empty strings
                if (!string.IsNullOrEmpty(valueText))
                {
                    // Get enhanced context for AI analysis
                    var enhancedContext = await EnhancedContextExtractor.ExtractEnhancedContextAsync(
                        stringLiteral.Token, document, cancellationToken).ConfigureAwait(false);
                    
                    var candidate = new UserFacingStringCandidate(
                        stringLiteral.Span,
                        valueText,
                        enhancedContext); // Use enhanced context for AI
                    candidates.Add(candidate);
                }
            }
        }

        return candidates.ToImmutableAndFree();
    }

    /// <summary>
    /// Gets cached results using the new document-centric cache.
    /// Falls back to legacy cache for backward compatibility.
    /// </summary>
    public async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetCachedResultsAsync(
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

        // If no results from new cache, fall back to legacy cache
        if (results.Count == 0)
        {
            var legacyResults = await _legacyCacheService.GetCachedResultsAsync(document, cancellationToken).ConfigureAwait(false);
            results.AddRange(legacyResults);
        }

        return results.ToImmutableAndFree();
    }
}
