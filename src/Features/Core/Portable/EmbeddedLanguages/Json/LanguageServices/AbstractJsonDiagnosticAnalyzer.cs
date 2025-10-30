// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

/// <summary>
/// Analyzer that reports diagnostics in strings that we know are JSON text.
/// </summary>
internal abstract class AbstractJsonDiagnosticAnalyzer(EmbeddedLanguageInfo info)
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        DiagnosticId,
        EnforceOnBuildValues.Json,
        option: null,
        new LocalizableResourceString(nameof(FeaturesResources.Invalid_JSON_pattern), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
        new LocalizableResourceString(nameof(FeaturesResources.JSON_issue_0), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
{
    public const string DiagnosticId = "JSON001";

    private readonly EmbeddedLanguageInfo _info = info;

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSemanticModelAction(Analyze);

    public void Analyze(SemanticModelAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;

        if (!context.GetAnalyzerOptions().GetOption(JsonDetectionOptionsStorage.ReportInvalidJsonPatterns) ||
            ShouldSkipAnalysis(context, notification: null))
        {
            return;
        }

        var detector = JsonLanguageDetector.GetOrCreate(context.SemanticModel.Compilation, _info);

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
        stack.Push(context.GetAnalysisRoot(findInTrivia: true));

        while (stack.TryPop(out var currentNode))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in currentNode.ChildNodesAndTokens().Reverse())
            {
                if (!context.ShouldAnalyzeSpan(child.FullSpan))
                    continue;

                if (child.AsNode(out var childNode))
                {
                    stack.Push(childNode);
                }
                else
                {
                    AnalyzeToken(child.AsToken());
                }
            }
        }

        void AnalyzeToken(SyntaxToken token)
        {
            if (!_info.IsAnyStringLiteral(token.RawKind))
                return;

            var tree = detector.TryParseString(token, context.SemanticModel, includeProbableStrings: false, cancellationToken);
            if (tree is null)
                return;

            foreach (var diag in tree.Diagnostics)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    this.Descriptor,
                    Location.Create(context.SemanticModel.SyntaxTree, diag.Span),
                    NotificationOption2.Warning,
                    context.Options,
                    additionalLocations: null,
                    properties: null,
                    diag.Message));
            }
        }
    }
}
