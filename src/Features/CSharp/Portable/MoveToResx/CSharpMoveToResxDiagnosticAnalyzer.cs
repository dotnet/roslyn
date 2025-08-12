// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx;

/// <summary>
/// AI-enhanced diagnostic analyzer that uses AI as the first resort to identify user-facing strings,
/// falling back to heuristics for low-confidence strings or when AI is unavailable.
/// 
/// Flow:
/// 1. Try AI analysis first
/// 2. For AI confidence >= 75%: Report directly as user-facing
/// 3. For AI confidence < 75%: Run through heuristic filters
/// 4. If AI fails: Full heuristic analysis
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMoveToResxDiagnosticAnalyzer : DocumentDiagnosticAnalyzer, IBuiltInAnalyzer
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        IDEDiagnosticIds.MoveToResxDiagnosticId,
        new LocalizableResourceString(nameof(CSharpFeaturesResources.Move_to_Resx), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
        new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_Resx_for_user_facing_strings), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
        DiagnosticCategory.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_descriptor];

    public bool IsHighPriority => false;

    public override int Priority => 50; // Default priority

    // Use SemanticDocumentAnalysis to have access to Document for AI analysis
    public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(
        TextDocument textDocument, SyntaxTree? tree, CancellationToken cancellationToken)
    {
        if (textDocument is not Document document || tree == null)
            return ImmutableArray<Diagnostic>.Empty;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        // ✅ Now we have direct access to Document!
        var extractorService = document.GetLanguageService<IUserFacingStringExtractorService>();
        
        if (extractorService != null)
        {
            // AI analysis path - run AI first
            await AnalyzeWithAIAsync(document, extractorService, diagnostics, cancellationToken);
        }
        else
        {
            // No AI service available - fall back to heuristic analysis only
            await AnalyzeWithHeuristicsOnlyAsync(document, tree, diagnostics, cancellationToken);
        }

        return diagnostics.ToImmutable();
    }

    /// <summary>
    /// Updates the diagnostic analyzer to actually use AI analysis first as described in the comments.
    /// </summary>
    private static async Task AnalyzeWithAIAsync(
        Document document, 
        IUserFacingStringExtractorService extractorService, 
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            // First check if we have cached results to avoid triggering new analysis
            var cachedResults = await extractorService.GetCachedResultsAsync(document, cancellationToken).ConfigureAwait(false);
            
            ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)> aiResults;
            
            if (!cachedResults.IsEmpty)
            {
                // Use cached results if available
                aiResults = cachedResults;
            }
            else
            {
                // Trigger fresh AI analysis
                aiResults = await extractorService.ExtractAndAnalyzeAsync(document, cancellationToken).ConfigureAwait(false);
            }

            if (aiResults.IsEmpty)
            {
                // No AI results available, fall back to heuristics
                var tree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
                await AnalyzeWithHeuristicsOnlyAsync(document, tree, diagnostics, cancellationToken);
                return;
            }

            // Process each result individually with its specific location
            var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
            
            foreach (var (candidate, analysis) in aiResults)
            {
                var stringValue = candidate.Value;
                // Use the exact TextSpan location from the candidate to create a precise Location
                var location = Location.Create(syntaxTree, candidate.Location);

                if (analysis.ConfidenceScore >= 0.75)
                {
                    // High confidence AI result - report directly
                    var diagnostic = CreateAIDiagnostic(location, stringValue, analysis);
                    diagnostics.Add(diagnostic);
                }
                else if (analysis.ConfidenceScore >= 0.4)
                {
                    // Medium confidence AI result - apply additional heuristic validation
                    await AnalyzeSpecificLocationWithHeuristicsAsync(document, candidate.Location, stringValue, analysis, diagnostics, cancellationToken);
                }
                // Low confidence strings (< 0.4) are ignored - AI determined they're likely internal
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // AI analysis failed completely, fall back to heuristics
            var tree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
            await AnalyzeWithHeuristicsOnlyAsync(document, tree, diagnostics, cancellationToken);
        }
    }

    private static async Task AnalyzeWithHeuristicsOnlyAsync(
        Document document, 
        SyntaxTree tree, 
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        // Full heuristic analysis for all strings when AI is unavailable
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var stringLiterals = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                                  .Where(lit => lit.Token.IsKind(SyntaxKind.StringLiteralToken));

        foreach (var stringLiteral in stringLiterals)
        {
            AnalyzeStringLiteralWithHeuristics(stringLiteral, diagnostics);
        }
    }

    private static async Task AnalyzeSpecificLocationWithHeuristicsAsync(
        Document document,
        Microsoft.CodeAnalysis.Text.TextSpan textSpan, 
        string stringValue,
        UserFacingStringAnalysis? aiAnalysis,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        // Enhanced heuristic analysis for a specific string at a specific location 
        // that AI marked as medium confidence - use AI insights combined with heuristics
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var node = root.FindNode(textSpan);
        
        if (node is LiteralExpressionSyntax stringLiteral && 
            stringLiteral.Token.IsKind(SyntaxKind.StringLiteralToken) &&
            stringLiteral.Token.ValueText == stringValue)
        {
            // Apply enhanced analysis combining AI insights with heuristics
            if (ShouldReportBasedOnHeuristics(stringLiteral, aiAnalysis))
            {
                var location = Location.Create(document.GetRequiredSyntaxTreeSynchronously(cancellationToken), textSpan);
                var diagnostic = CreateHybridDiagnostic(location, stringValue, stringLiteral, aiAnalysis);
                diagnostics.Add(diagnostic);
            }
        }
    }

    // Overload for legacy calls without AI analysis
    private static async Task AnalyzeSpecificLocationWithHeuristicsAsync(
        Document document,
        Microsoft.CodeAnalysis.Text.TextSpan textSpan, 
        string stringValue,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        await AnalyzeSpecificLocationWithHeuristicsAsync(document, textSpan, stringValue, null, diagnostics, cancellationToken);
    }

    private static void AnalyzeStringLiteralWithHeuristics(
        LiteralExpressionSyntax stringLiteral, 
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var valueText = stringLiteral.Token.ValueText;

        // Quick early filters for obviously non-user-facing strings
        if (string.IsNullOrWhiteSpace(valueText) || valueText.Length < 3)
            return;

        // Apply existing heuristic filters
        if (IsLikelyNonUserFacingContent(valueText) ||
            IsInNonUserFacingContext(stringLiteral) ||
            MatchesNonUserFacingPatterns(valueText))
        {
            return;
        }

        // Create heuristic-based diagnostic
        var diagnostic = CreateHeuristicDiagnostic(stringLiteral.GetLocation(), valueText, stringLiteral);
        diagnostics.Add(diagnostic);
    }

    private static Diagnostic CreateAIDiagnostic(Location location, string stringValue, UserFacingStringAnalysis analysis)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add("ConfidenceScore", analysis.ConfidenceScore.ToString("F2"));
        properties.Add("SuggestedResourceKey", analysis.SuggestedResourceKey);
        properties.Add("Reasoning", analysis.Reasoning);
        properties.Add("AnalysisMethod", "AI");
        properties.Add("StringValue", stringValue);
        properties.Add("StringLength", stringValue.Length.ToString());
        properties.Add("AIAnalysisCompleted", "true");

        return Diagnostic.Create(
            s_descriptor,
            location,
            properties.ToImmutable(),
            stringValue);
    }

    private static Diagnostic CreateHeuristicDiagnostic(Location location, string stringValue, LiteralExpressionSyntax stringLiteral)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add("ConfidenceScore", "0.75"); // Heuristic confidence
        properties.Add("SuggestedResourceKey", GenerateResourceKey(stringValue));
        properties.Add("Reasoning", "Heuristic analysis suggests this may be user-facing");
        properties.Add("AnalysisMethod", "Heuristic");
        properties.Add("StringLength", stringValue.Length.ToString());
        properties.Add("Context", GetContextDescription(stringLiteral));
        properties.Add("StringValue", stringValue);

        return Diagnostic.Create(
            s_descriptor,
            location,
            properties.ToImmutable(),
            stringValue);
    }

    private static Diagnostic CreateHybridDiagnostic(Location location, string stringValue, LiteralExpressionSyntax stringLiteral, UserFacingStringAnalysis? aiAnalysis)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        
        if (aiAnalysis != null)
        {
            // Combine AI insights with heuristic analysis
            var combinedConfidence = Math.Max(0.7, aiAnalysis.ConfidenceScore); // Boost medium confidence to reportable level
            properties.Add("ConfidenceScore", combinedConfidence.ToString("F2"));
            properties.Add("SuggestedResourceKey", aiAnalysis.SuggestedResourceKey);
            properties.Add("Reasoning", $"AI + Heuristic: {aiAnalysis.Reasoning} | Heuristic validation passed");
            properties.Add("AnalysisMethod", "AI+Heuristic");
            properties.Add("AIConfidence", aiAnalysis.ConfidenceScore.ToString("F2"));
        }
        else
        {
            // Pure heuristic analysis
            properties.Add("ConfidenceScore", "0.75");
            properties.Add("SuggestedResourceKey", GenerateResourceKey(stringValue));
            properties.Add("Reasoning", "Heuristic analysis suggests this may be user-facing");
            properties.Add("AnalysisMethod", "Heuristic");
        }
        
        properties.Add("StringLength", stringValue.Length.ToString());
        properties.Add("Context", GetContextDescription(stringLiteral));
        properties.Add("StringValue", stringValue);

        return Diagnostic.Create(
            s_descriptor,
            location,
            properties.ToImmutable(),
            stringValue);
    }

    private static bool ShouldReportBasedOnHeuristics(LiteralExpressionSyntax stringLiteral, UserFacingStringAnalysis? aiAnalysis)
    {
        var valueText = stringLiteral.Token.ValueText;

        // Quick early filters for obviously non-user-facing strings
        if (string.IsNullOrWhiteSpace(valueText) || valueText.Length < 3)
            return false;

        // If AI provided reasoning that indicates internal use, respect that even for medium confidence
        if (aiAnalysis?.Reasoning?.ToLowerInvariant().Contains("internal") == true ||
            aiAnalysis?.Reasoning?.ToLowerInvariant().Contains("debug") == true ||
            aiAnalysis?.Reasoning?.ToLowerInvariant().Contains("log") == true)
        {
            return false;
        }

        // Apply existing heuristic filters, but be more lenient since AI gave medium confidence
        if (IsLikelyNonUserFacingContent(valueText) ||
            IsInNonUserFacingContext(stringLiteral) ||
            MatchesNonUserFacingPatterns(valueText))
        {
            return false;
        }

        // If we get here and AI gave medium confidence, report it
        return true;
    }

    // Helper methods
    private static string GenerateResourceKey(string valueText)
    {
        // Simple resource key generation for heuristic analysis
        var key = valueText.Length <= 30 ? valueText : valueText.Substring(0, 30);
        key = System.Text.RegularExpressions.Regex.Replace(key, @"[^\w]", "");
        return string.IsNullOrEmpty(key) ? "GeneratedKey" : key;
    }

    private static string GetContextDescription(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;
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

    // Existing heuristic filter methods
    private static bool IsLikelyNonUserFacingContent(string valueText)
    {
        // 1. Ignore strings that look like identifiers or code (but allow single common words)
        if (valueText.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            // Allow common user-facing single words that might be identifiers
            var commonUserWords = new[] { "Ready", "Done", "OK", "Yes", "No", "Save", "Load", "Open", "Close", "New", "Edit", "Delete", "Cancel", "Submit", "Start", "Stop", "Pause", "Play", "Next", "Previous", "Home", "Back", "Forward", "Up", "Down", "Left", "Right", "Help", "About", "Settings", "Options", "Preferences", "File", "View", "Tools", "Window", "Search", "Find", "Replace", "Print", "Copy", "Cut", "Paste", "Undo", "Redo" };
            if (!commonUserWords.Contains(valueText, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        // 2. Ignore file paths, URLs, and system identifiers
        if (valueText.Contains("\\") || valueText.Contains("/") ||
            valueText.Contains(".cs") || valueText.Contains(".dll") || valueText.Contains(".exe") ||
            valueText.Contains(".json") || valueText.Contains(".xml") || valueText.Contains(".config") ||
            valueText.Contains(".txt") || valueText.Contains(".log") || valueText.Contains(".tmp"))
        {
            return true;
        }

        // Check for URLs and paths more specifically
        if (valueText.Contains("://") || valueText.Contains("www.") ||
            (valueText.Contains(":") && valueText.Length > 2 && char.IsLetter(valueText[0]) && valueText[1] == ':'))
        {
            return true;
        }

        // 3. Ignore namespace-like strings
        if (valueText.StartsWith("System.") || valueText.StartsWith("Microsoft.") ||
            valueText.StartsWith("System:") || valueText.StartsWith("Microsoft:") ||
            valueText.Contains("::"))
        {
            return true;
        }

        // 4. Ignore numbers, GUIDs, and technical identifiers
        if (Guid.TryParse(valueText, out _) || double.TryParse(valueText, out _) ||
            long.TryParse(valueText, out _) || DateTime.TryParse(valueText, out _))
        {
            return true;
        }

        // 5. Ignore resource keys and constants
        if (valueText.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)) && valueText.Length > 3)
        {
            return true;
        }

        return false;
    }

    private static bool IsInNonUserFacingContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;

        // Check if string is used as an argument to a method call
        if (parent is ArgumentSyntax arg)
        {
            var invocation = arg.Parent?.Parent as InvocationExpressionSyntax;
            if (invocation != null)
            {
                var invoked = invocation.Expression.ToString();

                // Check for common non-user-facing method patterns
                if (invoked.Contains("Log") || invoked.Contains("Debug") || invoked.Contains("Trace") ||
                    invoked.Contains("Assert") || invoked.Contains("WriteLine") || invoked.Contains("Parse") ||
                    invoked.Contains("typeof") || invoked.Contains("nameof") || invoked.Contains("Exception"))
                {
                    return true;
                }
            }
        }

        // Check if string is used in attribute
        var current = stringLiteral.Parent;
        while (current != null)
        {
            if (current is AttributeSyntax)
                return true;
            current = current.Parent;
        }

        return false;
    }

    private static bool MatchesNonUserFacingPatterns(string valueText)
    {
        // 1. Version numbers
        if (System.Text.RegularExpressions.Regex.IsMatch(valueText, @"^v?\d+\.\d+(\.\d+)*$"))
            return true;

        // 2. MIME types
        if (valueText.Contains("/") && valueText.Split('/').Length == 2 && !valueText.Contains(" "))
            return true;

        // 3. Color codes and hex values
        if (valueText.StartsWith("#") && valueText.Length > 1 &&
            valueText.Skip(1).All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            return true;

        // 4. Connection strings patterns
        if (valueText.Contains("=") && valueText.Contains(";") && !valueText.Contains(" "))
            return true;

        return false;
    }
}
