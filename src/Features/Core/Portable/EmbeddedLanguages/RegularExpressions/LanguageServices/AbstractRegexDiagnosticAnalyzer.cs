// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;

/// <summary>
/// Analyzer that reports diagnostics in strings that we know are regex text.
/// </summary>
internal abstract class AbstractRegexDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public const string DiagnosticId = "RE0001";

    private readonly EmbeddedLanguageInfo _info;

    protected AbstractRegexDiagnosticAnalyzer(EmbeddedLanguageInfo info)
        : base(DiagnosticId,
               EnforceOnBuildValues.Regex,
               option: null,
               new LocalizableResourceString(nameof(FeaturesResources.Invalid_regex_pattern), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
               new LocalizableResourceString(nameof(FeaturesResources.Regex_issue_0), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
    {
        _info = info;
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSemanticModelAction(Analyze);

    public void Analyze(SemanticModelAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        var option = context.GetAnalyzerOptions().GetOption(RegexOptionsStorage.ReportInvalidRegexPatterns);
        if (!option || ShouldSkipAnalysis(context, notification: null))
            return;

        var detector = RegexLanguageDetector.GetOrCreate(semanticModel.Compilation, _info);

        // Use an actual stack object so that we don't blow the actual stack through recursion.
        var root = context.GetAnalysisRoot(findInTrivia: true);
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
        stack.Push(root);

        while (stack.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in current.ChildNodesAndTokens())
            {
                if (!context.ShouldAnalyzeSpan(child.FullSpan))
                    continue;

                if (child.IsNode)
                {
                    stack.Push(child.AsNode());
                }
                else
                {
                    AnalyzeToken(context, detector, child.AsToken(), cancellationToken);
                }
            }
        }
    }

    private void AnalyzeToken(
        SemanticModelAnalysisContext context,
        RegexLanguageDetector detector,
        SyntaxToken token,
        CancellationToken cancellationToken)
    {
        if (_info.IsAnyStringLiteral(token.RawKind))
        {
            var tree = detector.TryParseString(token, context.SemanticModel, cancellationToken);
            if (tree != null)
            {
                foreach (var diag in tree.Diagnostics)
                {
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        Descriptor,
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
}
