// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    /// <summary>
    /// Analyzer that reports diagnostics in strings that we know are JSON text.
    /// </summary>
    internal abstract class AbstractJsonDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string DiagnosticId = "JSON001";

        private readonly EmbeddedLanguageInfo _info;

        protected AbstractJsonDiagnosticAnalyzer(EmbeddedLanguageInfo info)
            : base(DiagnosticId,
                   EnforceOnBuildValues.Json,
                   option: null,
                   new LocalizableResourceString(nameof(FeaturesResources.Invalid_JSON_pattern), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.JSON_issue_0), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _info = info;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(SimplifierOptions? options)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(Analyze);

        public void Analyze(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var option = context.Options.GetIdeOptions().ReportInvalidJsonPatterns;
            if (!option)
                return;

            var detector = JsonLanguageDetector.GetOrCreate(semanticModel.Compilation, _info);
            var root = syntaxTree.GetRoot(cancellationToken);
            Analyze(context, detector, root, cancellationToken);
        }

        private void Analyze(
            SemanticModelAnalysisContext context,
            JsonLanguageDetector detector,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    Analyze(context, detector, child.AsNode()!, cancellationToken);
                }
                else
                {
                    var token = child.AsToken();
                    if (_info.IsAnyStringLiteral(token.RawKind))
                    {
                        var tree = detector.TryParseString(token, context.SemanticModel, includeProbableStrings: false, cancellationToken);
                        if (tree != null)
                        {
                            foreach (var diag in tree.Diagnostics)
                            {
                                context.ReportDiagnostic(DiagnosticHelper.Create(
                                    this.Descriptor,
                                    Location.Create(context.SemanticModel.SyntaxTree, diag.Span),
                                    ReportDiagnostic.Warn,
                                    additionalLocations: null,
                                    properties: null,
                                    diag.Message));
                            }
                        }
                    }
                }
            }
        }
    }
}
