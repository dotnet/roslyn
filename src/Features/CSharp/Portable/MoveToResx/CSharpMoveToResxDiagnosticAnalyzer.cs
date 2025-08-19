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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx;

/// <summary>
/// Analyzer integrates with a background document watcher + cache to classify user-facing strings.
/// Cache is keyed by (string literal + BASIC context). For cache hits with confidence >= 0.5 we create diagnostics.
/// Cache misses are queued to the watcher for AI analysis. The analyzer never awaits watcher work.
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

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var documentWatcher = document.GetLanguageService<IUserFacingStringDocumentWatcher>();

        // If no document watcher available, skip cache-based analysis
        if (documentWatcher == null)
            return diagnostics.ToImmutable();

        // Ensure the document is tracked in the watcher
        var version = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
        documentWatcher.OnDocumentChanged(document.Id, version);

        var missedStrings = new List<(string stringValue, string basicContext, TextSpan location)>();
        
        foreach (var stringLiteral in root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                     .Where(lit => lit.Token.IsKind(SyntaxKind.StringLiteralToken)))
        {
            var value = stringLiteral.Token.ValueText;
            var basicContext = CSharp.UserFacingStrings.BasicContextExtractor.GetBasicContext(stringLiteral);

            if (documentWatcher.TryGetCachedAnalysis(value, basicContext, out var analysis))
            {
                // Cache hit: create diagnostic if confidence >= 0.5
                if (analysis.ConfidenceScore >= 0.5)
                {
                    diagnostics.Add(CreateAIDiagnostic(stringLiteral.GetLocation(), value, analysis));
                }
            }
            else
            {
                // Cache miss: collect for batch analysis
                missedStrings.Add((value, basicContext, stringLiteral.Span));
            }
        }

        if (missedStrings.Count > 0)
        {
            // Fire-and-forget analysis of only the missed strings
            _ = documentWatcher.AnalyzeSpecificStringsAsync(document, missedStrings, cancellationToken);
        }

        return diagnostics.ToImmutable();
    }


    /// <summary>
    /// Creates a diagnostic from AI analysis results.
    /// </summary>
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
}