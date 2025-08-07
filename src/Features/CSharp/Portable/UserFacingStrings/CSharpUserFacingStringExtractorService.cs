// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    private readonly UserFacingStringCacheService _cacheService = new();

    [ImportingConstructor]
    public CSharpUserFacingStringExtractorService()
    {
    }

    public async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> ExtractAndAnalyzeAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        // Use the cache service to handle caching and throttling
        return await _cacheService.GetOrAnalyzeAsync(document, PerformAnalysisAsync, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> PerformAnalysisAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        // Get the Copilot service
        var copilotService = document.GetLanguageService<ICopilotCodeAnalysisService>();
        if (copilotService == null || !await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;

        // Extract all string literals - NO FILTERING
        var proposal = await ExtractAllStringLiteralsAsync(document, cancellationToken).ConfigureAwait(false);
        if (proposal == null || proposal.Candidates.IsEmpty)
            return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;

        // Send everything to AI for analysis
        var result = await copilotService.GetUserFacingStringAnalysisAsync(proposal, cancellationToken).ConfigureAwait(false);

        if (result.isQuotaExceeded || result.responseDictionary == null)
            return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;

        // Combine candidates with their analysis
        var results = ArrayBuilder<(UserFacingStringCandidate, UserFacingStringAnalysis)>.GetInstance();

        foreach (var candidate in proposal.Candidates)
        {
            if (result.responseDictionary.TryGetValue(candidate.Value, out var analysis))
            {
                results.Add((candidate, analysis));
            }
        }

        return results.ToImmutableAndFree();
    }

    private async Task<UserFacingStringProposal?> ExtractAllStringLiteralsAsync(Document document, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree == null)
            return null;

        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

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
                    var context = GetBasicContext(stringLiteral);
                    var candidate = new UserFacingStringCandidate(
                        stringLiteral.Span,
                        valueText,
                        context);
                    candidates.Add(candidate);
                }
            }
        }

        var sourceCode = sourceText.ToString();
        return new UserFacingStringProposal(sourceCode, candidates.ToImmutableAndFree());
    }

    private static string GetBasicContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;

        // Provide minimal context information for the AI
        return parent switch
        {
            ArgumentSyntax arg when arg.Parent?.Parent is InvocationExpressionSyntax invocation =>
                $"Argument to method: {invocation.Expression}",
            AssignmentExpressionSyntax assignment =>
                $"Assignment to: {assignment.Left}",
            VariableDeclaratorSyntax declarator =>
                $"Variable initialization: {declarator.Identifier}",
            ReturnStatementSyntax =>
                "Return statement",
            AttributeSyntax =>
                "Attribute value",
            ThrowStatementSyntax =>
                "Exception message",
            _ => "Other context"
        };
    }

    /// <summary>
    /// Gets cached results without triggering new analysis. Fast method for retrieving existing analysis.
    /// </summary>
    public async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetCachedResultsAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        return await _cacheService.GetCachedResultsAsync(document, cancellationToken).ConfigureAwait(false);
    }
}
