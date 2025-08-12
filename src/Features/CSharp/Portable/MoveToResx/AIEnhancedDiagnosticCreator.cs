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
/// Enhanced diagnostic creator that includes AI metadata when available.
/// This shows how to create diagnostics with AI-ready properties.
/// </summary>
internal static class AIEnhancedDiagnosticCreator
{
    /// <summary>
    /// Creates a diagnostic with enhanced properties for AI analysis.
    /// This is the pattern to use in your diagnostic analyzer.
    /// </summary>
    public static Diagnostic CreateEnhancedDiagnostic(
        DiagnosticDescriptor descriptor,
        Location location,
        string stringValue,
        UserFacingStringAnalysis? aiAnalysis = null)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        
        if (aiAnalysis != null)
        {
            // AI analysis completed - use AI results
            properties.Add("ConfidenceScore", aiAnalysis.ConfidenceScore.ToString("F2"));
            properties.Add("SuggestedResourceKey", aiAnalysis.SuggestedResourceKey);
            properties.Add("Reasoning", aiAnalysis.Reasoning);
            properties.Add("AnalysisMethod", "AI");
            properties.Add("AIAnalysisCompleted", "true");
        }
        else
        {
            // Heuristic analysis - mark for AI enhancement later
            properties.Add("ConfidenceScore", "0.75");
            properties.Add("SuggestedResourceKey", GenerateHeuristicResourceKey(stringValue));
            properties.Add("Reasoning", "Heuristic analysis suggests this may be user-facing");
            properties.Add("AnalysisMethod", "Heuristic");
            properties.Add("RequiresAIAnalysis", "true"); // Flag for later AI processing
        }

        // Always include the string value for AI processing
        properties.Add("StringValue", stringValue);
        properties.Add("StringLength", stringValue.Length.ToString());

        return Diagnostic.Create(
            descriptor,
            location,
            properties.ToImmutable(),
            stringValue);
    }

    /// <summary>
    /// Gets AI analysis for a string if available, otherwise returns null.
    /// This can be called from code fix providers or other services.
    /// </summary>
    public static async Task<UserFacingStringAnalysis?> GetAIAnalysisForStringAsync(
        Document document,
        string stringValue,
        CancellationToken cancellationToken)
    {
        var extractorService = document.GetLanguageService<IUserFacingStringExtractorService>();
        if (extractorService == null)
            return null;

        try
        {
            var results = await extractorService.ExtractAndAnalyzeAsync(document, cancellationToken).ConfigureAwait(false);
            
            foreach (var result in results)
            {
                if (result.candidate.Value == stringValue)
                {
                    return result.analysis;
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Enhanced the diagnostic with AI analysis if available.
    /// This can be called to upgrade a heuristic diagnostic to an AI-enhanced one.
    /// </summary>
    public static async Task<Diagnostic> EnhanceDiagnosticWithAIAsync(
        Document document,
        Diagnostic originalDiagnostic,
        CancellationToken cancellationToken)
    {
        // Only enhance if it's marked for AI analysis
        if (!originalDiagnostic.Properties.TryGetValue("RequiresAIAnalysis", out var needsAI) || needsAI != "true")
            return originalDiagnostic;

        if (!originalDiagnostic.Properties.TryGetValue("StringValue", out var stringValue) || string.IsNullOrEmpty(stringValue))
            return originalDiagnostic;

        var aiAnalysis = await GetAIAnalysisForStringAsync(document, stringValue, cancellationToken).ConfigureAwait(false);
        if (aiAnalysis == null)
            return originalDiagnostic;

        // Create new enhanced diagnostic
        return CreateEnhancedDiagnostic(
            originalDiagnostic.Descriptor,
            originalDiagnostic.Location,
            stringValue,
            aiAnalysis);
    }

    /// <summary>
    /// Checks if AI analysis is available for the document.
    /// </summary>
    public static bool IsAIAnalysisAvailable(Document document)
    {
        var extractorService = document.GetLanguageService<IUserFacingStringExtractorService>();
        return extractorService != null;
    }

    private static string GenerateHeuristicResourceKey(string valueText)
    {
        var key = valueText.Length <= 30 ? valueText : valueText.Substring(0, 30);
        key = System.Text.RegularExpressions.Regex.Replace(key, @"[^\w]", "");
        return string.IsNullOrEmpty(key) ? "GeneratedKey" : key;
    }
}
