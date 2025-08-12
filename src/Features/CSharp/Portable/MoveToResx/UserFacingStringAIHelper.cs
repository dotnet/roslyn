// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx;

/// <summary>
/// Helper class showing how to integrate your AI-powered user-facing string analysis
/// with code fix providers and other services that have access to Document.
/// </summary>
internal static class UserFacingStringAIHelper
{
    /// <summary>
    /// Gets AI analysis results for all strings in a document.
    /// This can be called from code fix providers or other services that have access to Document.
    /// </summary>
    public static async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetAIAnalysisAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var extractorService = document.GetLanguageService<IUserFacingStringExtractorService>();
        if (extractorService == null)
            return ImmutableArray<(UserFacingStringCandidate, UserFacingStringAnalysis)>.Empty;

        return await extractorService.ExtractAndAnalyzeAsync(document, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets only high-confidence user-facing strings from AI analysis.
    /// </summary>
    public static async Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetHighConfidenceUserFacingStringsAsync(
        Document document,
        double minimumConfidence = 0.7,
        CancellationToken cancellationToken = default)
    {
        var results = await GetAIAnalysisAsync(document, cancellationToken).ConfigureAwait(false);
        
        return results
            .Where(r => r.analysis.ConfidenceScore >= minimumConfidence)
            .ToImmutableArray();
    }

    /// <summary>
    /// Checks if a specific string at a specific location is considered user-facing by AI.
    /// Useful for diagnostic analyzers that process individual strings.
    /// </summary>
    public static async Task<(bool isUserFacing, UserFacingStringAnalysis? analysis)> IsStringUserFacingAsync(
        Document document,
        string stringValue,
        CancellationToken cancellationToken)
    {
        var results = await GetAIAnalysisAsync(document, cancellationToken).ConfigureAwait(false);
        
        var match = results.FirstOrDefault(r => r.candidate.Value == stringValue);
        if (match.analysis != null && match.analysis.ConfidenceScore >= 0.7)
        {
            return (true, match.analysis);
        }

        return (false, null);
    }

    /// <summary>
    /// Example of how to use AI analysis in a code fix provider.
    /// Call this from your CSharpMoveToResxCodeFixProvider.
    /// </summary>
    public static async Task<ImmutableArray<string>> GetUserFacingStringsForCodeFixAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var highConfidenceStrings = await GetHighConfidenceUserFacingStringsAsync(
            document, 
            minimumConfidence: 0.7, 
            cancellationToken).ConfigureAwait(false);

        return highConfidenceStrings
            .Select(r => r.candidate.Value)
            .ToImmutableArray();
    }
}

/// <summary>
/// Example enhanced diagnostic descriptor that includes AI metadata.
/// You can use this approach to create diagnostics with AI confidence scores.
/// </summary>
internal static class EnhancedMoveToResxDiagnostics
{
    public static Diagnostic CreateAIEnhancedDiagnostic(
        DiagnosticDescriptor descriptor,
        Location location,
        string stringValue,
        UserFacingStringAnalysis? aiAnalysis = null)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        
        if (aiAnalysis != null)
        {
            properties.Add("ConfidenceScore", aiAnalysis.ConfidenceScore.ToString("F2"));
            properties.Add("SuggestedResourceKey", aiAnalysis.SuggestedResourceKey);
            properties.Add("Reasoning", aiAnalysis.Reasoning);
            properties.Add("AnalysisMethod", "AI");
        }
        else
        {
            properties.Add("ConfidenceScore", "0.5");
            properties.Add("SuggestedResourceKey", GenerateHeuristicResourceKey(stringValue));
            properties.Add("Reasoning", "Heuristic analysis suggests this may be user-facing");
            properties.Add("AnalysisMethod", "Heuristic");
        }

        return Diagnostic.Create(
            descriptor,
            location,
            properties.ToImmutable(),
            stringValue);
    }

    private static string GenerateHeuristicResourceKey(string valueText)
    {
        var key = valueText.Length <= 30 ? valueText : valueText.Substring(0, 30);
        key = System.Text.RegularExpressions.Regex.Replace(key, @"[^\w]", "");
        return string.IsNullOrEmpty(key) ? "GeneratedKey" : key;
    }
}
