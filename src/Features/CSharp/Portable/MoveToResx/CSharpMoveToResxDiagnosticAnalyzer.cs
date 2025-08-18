// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
/// AI-enhanced diagnostic analyzer that uses confidence-based filtering to identify user-facing strings.
/// 
/// Flow:
/// 1. Extract all string literals once
/// 2. AI analysis with confidence-based filtering:
///    - High confidence (≥75%): Create AI diagnostic directly
///    - Medium confidence (40-74%): Send to heuristic validation  
///    - Low confidence (&lt;40%): Ignore (definitely not user-facing)
/// 3. If AI unavailable: Full heuristic analysis on extracted strings
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

        // Step 1: Extract all string literals once at the beginning
        var stringLiterals = await ExtractStringLiteralsAsync(tree, cancellationToken).ConfigureAwait(false);
        if (stringLiterals.IsEmpty)
            return ImmutableArray<Diagnostic>.Empty;

        // Step 2: Choose analysis path based on AI service availability
        var extractorService = document.GetLanguageService<IUserFacingStringExtractorService>();
        
        if (extractorService != null)
        {
            // AI analysis path
            await AnalyzeWithAIAsync(document, extractorService, diagnostics, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Heuristic analysis path
            AnalyzeWithHeuristicsOnly(stringLiterals, diagnostics);
        }

        return diagnostics.ToImmutable();
    }

    /// <summary>
    /// Extract all string literals from the document once at the beginning.
    /// </summary>
    private static async Task<ImmutableArray<LiteralExpressionSyntax>> ExtractStringLiteralsAsync(
        SyntaxTree tree, CancellationToken cancellationToken)
    {
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var stringLiterals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(lit => lit.Token.IsKind(SyntaxKind.StringLiteralToken))
            .ToImmutableArray();

        return stringLiterals;
    }

    /// <summary>
    /// Analyze using document watcher cache for instant results, with fallback to direct AI analysis.
    /// </summary>
    private static async Task AnalyzeWithAIAsync(
        Document document,
        IUserFacingStringExtractorService extractorService,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        // Try to get document watcher service for cached results
        var documentWatcher = document.GetLanguageService<IUserFacingStringDocumentWatcher>();
        
        if (documentWatcher != null)
        {
            // Use cache-first approach with document watcher
            await AnalyzeWithDocumentWatcherAsync(document, documentWatcher, diagnostics, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Fallback to direct AI analysis (original implementation)
            await AnalyzeWithDirectAIAsync(document, extractorService, diagnostics, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Fast cache-based analysis using document watcher.
    /// </summary>
    private static async Task AnalyzeWithDocumentWatcherAsync(
        Document document,
        IUserFacingStringDocumentWatcher documentWatcher,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        // Ensure document is analyzed in background
        await documentWatcher.EnsureDocumentAnalyzedAsync(document, cancellationToken).ConfigureAwait(false);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var stringLiterals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(lit => lit.Token.IsKind(SyntaxKind.StringLiteralToken));

        foreach (var stringLiteral in stringLiterals)
        {
            var stringValue = stringLiteral.Token.ValueText;
            
            // Skip obviously non-user-facing strings early
            if (string.IsNullOrWhiteSpace(stringValue) || stringValue.Length < 3 ||
                IsLikelyNonUserFacingContent(stringValue) ||
                IsInNonUserFacingContext(stringLiteral) ||
                MatchesNonUserFacingPatterns(stringValue))
            {
                continue;
            }

            // Extract context for cache lookup
            var context = ExtractStringContext(stringLiteral);
            
            // Check cache first
            if (documentWatcher.TryGetCachedAnalysis(stringValue, context, out var cachedAnalysis))
            {
                // Use cached result
                if (cachedAnalysis.ConfidenceScore >= 0.4) // Only create diagnostics for reasonable confidence
                {
                    var location = stringLiteral.GetLocation();
                    diagnostics.Add(CreateAIDiagnostic(location, stringValue, cachedAnalysis));
                }
            }
            else
            {
                // No cached result - analyze with heuristics as fallback
                AnalyzeStringLiteralWithHeuristics(stringLiteral, diagnostics);
            }
        }
    }

    /// <summary>
    /// Direct AI analysis (fallback when document watcher unavailable).
    /// </summary>
    private static async Task AnalyzeWithDirectAIAsync(
        Document document,
        IUserFacingStringExtractorService extractorService,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        // Get the AI analysis for the entire document
        var aiAnalysis = await extractorService.ExtractAndAnalyzeAsync(document, cancellationToken).ConfigureAwait(false);

        // Process each AI result with confidence-based filtering
        foreach (var (candidate, analysis) in aiAnalysis)
        {
            var stringValue = candidate.Value;
            var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
            var location = Location.Create(syntaxTree, candidate.Location);

            if (analysis.ConfidenceScore >= 0.75)
            {
                // High confidence: Create AI diagnostic directly
                diagnostics.Add(CreateAIDiagnostic(location, stringValue, analysis));
            }
            else if (analysis.ConfidenceScore >= 0.4)
            {
                // Medium confidence: Send to heuristic validation
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var node = root.FindNode(candidate.Location);
                
                if (node is LiteralExpressionSyntax stringLiteral &&
                    stringLiteral.Token.IsKind(SyntaxKind.StringLiteralToken) &&
                    stringLiteral.Token.ValueText == stringValue)
                {
                    AnalyzeStringLiteralWithHeuristics(stringLiteral, diagnostics);
                }
            }
            // Low confidence (< 0.4): Ignore - definitely not user-facing
        }
    }

    /// <summary>
    /// Extract context information for a string literal.
    /// </summary>
    private static string ExtractStringContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;
        var contextParts = new List<string>();

        // Walk up the syntax tree to gather context
        while (parent != null && contextParts.Count < 3)
        {
            switch (parent)
            {
                case ArgumentSyntax arg when arg.Parent?.Parent is InvocationExpressionSyntax invocation:
                    contextParts.Add($"MethodCall:{invocation.Expression}");
                    break;
                case AssignmentExpressionSyntax assignment:
                    contextParts.Add($"Assignment:{assignment.Left}");
                    break;
                case VariableDeclaratorSyntax declarator:
                    contextParts.Add($"Variable:{declarator.Identifier}");
                    break;
                case ReturnStatementSyntax:
                    contextParts.Add("Return");
                    break;
                case ThrowStatementSyntax:
                    contextParts.Add("Exception");
                    break;
                case AttributeSyntax:
                    contextParts.Add("Attribute");
                    break;
            }

            parent = parent.Parent;
        }

        return string.Join("|", contextParts);
    }

    /// <summary>
    /// Analyze using heuristics only for the extracted string literals.
    /// </summary>
    private static void AnalyzeWithHeuristicsOnly(
        ImmutableArray<LiteralExpressionSyntax> stringLiterals,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        // Analyze each string literal using heuristics
        foreach (var stringLiteral in stringLiterals)
        {
            AnalyzeStringLiteralWithHeuristics(stringLiteral, diagnostics);
        }
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
        var diagnostic = CreateHeuristicDiagnostic(stringLiteral.GetLocation(), valueText);
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

    private static Diagnostic CreateHeuristicDiagnostic(Location location, string stringValue)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add("ConfidenceScore", "0.75"); // Heuristic confidence
        properties.Add("SuggestedResourceKey", GenerateResourceKey(stringValue));
        properties.Add("Reasoning", "Heuristic analysis suggests this may be user-facing");
        properties.Add("AnalysisMethod", "Heuristic");
        properties.Add("StringLength", stringValue.Length.ToString());
        properties.Add("StringValue", stringValue);

        return Diagnostic.Create(
            s_descriptor,
            location,
            properties.ToImmutable(),
            stringValue);
    }

    // Helper methods
    private static string GenerateResourceKey(string valueText)
    {
        // Simple resource key generation for heuristic analysis
        var key = valueText.Length <= 30 ? valueText : valueText.Substring(0, 30);
        key = System.Text.RegularExpressions.Regex.Replace(key, @"[^\w]", "");
        return string.IsNullOrEmpty(key) ? "GeneratedKey" : key;
    }

    // private static string GetContextDescription(LiteralExpressionSyntax stringLiteral)
    // {
    //     var parent = stringLiteral.Parent;
    //     return parent switch
    //     {
    //         ArgumentSyntax arg when arg.Parent?.Parent is InvocationExpressionSyntax invocation =>
    //             $"Argument to method: {invocation.Expression}",
    //         AssignmentExpressionSyntax assignment =>
    //             $"Assignment to: {assignment.Left}",
    //         VariableDeclaratorSyntax declarator =>
    //             $"Variable initialization: {declarator.Identifier}",
    //         ReturnStatementSyntax =>
    //             "Return statement",
    //         AttributeSyntax =>
    //             "Attribute value",
    //         ThrowStatementSyntax =>
    //             "Exception message",
    //         _ => "Other context"
    //     };
    // }

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
        {
            return true;
        }

        // 4. Connection strings patterns
        if (valueText.Contains("=") && valueText.Contains(";") && !valueText.Contains(" "))
            return true;

        return false;
    }
}
