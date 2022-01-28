// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    /// <summary>
    /// Analyzer that reports diagnostics in strings that we know are JSON text.
    /// </summary>
    internal class JsonDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string DiagnosticId = "JSON001";

        private readonly EmbeddedLanguageInfo _info;

        public JsonDiagnosticAnalyzer(EmbeddedLanguageInfo info)
            : base(DiagnosticId,
                   EnforceOnBuildValues.Json,
                   JsonFeatureOptions.ReportInvalidJsonPatterns,
                   new LocalizableResourceString(nameof(FeaturesResources.JSON_issue_0), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.JSON_issue_0), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _info = info;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Options.OptionSet options)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(Analyze);

        public void Analyze(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var option = context.GetOption(JsonFeatureOptions.ReportInvalidJsonPatterns, syntaxTree.Options.Language);
            if (!option)
                return;

            var detector = JsonPatternDetector.GetOrCreate(semanticModel, _info);

            var root = syntaxTree.GetRoot(cancellationToken);
            Analyze(context, detector, root, cancellationToken);
        }

        private void Analyze(
            SemanticModelAnalysisContext context, JsonPatternDetector detector,
            SyntaxNode node, CancellationToken cancellationToken)
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
                    if (token.RawKind == _info.StringLiteralTokenKind &&
                        detector.IsDefinitelyJson(token, cancellationToken))
                    {
                        var tree = detector.TryParseJson(token);
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
