// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices
{
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
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var option = context.Options.GetIdeOptions().ReportInvalidRegexPatterns;
            if (!option)
                return;

            var detector = RegexLanguageDetector.GetOrCreate(semanticModel.Compilation, _info);

            // Use an actual stack object so that we don't blow the actual stack through recursion.
            var root = syntaxTree.GetRoot(cancellationToken);
            var stack = new Stack<SyntaxNode>();
            stack.Push(root);

            while (stack.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stack.Pop();

                foreach (var child in current.ChildNodesAndTokens())
                {
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
