// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
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
                   RegularExpressionsOptions.ReportInvalidRegexPatterns,
                   new LocalizableResourceString(nameof(WorkspacesResources.Regex_issue_0), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                   new LocalizableResourceString(nameof(WorkspacesResources.Regex_issue_0), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)))
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

            var options = context.Options;
            var option = options.GetOption(RegularExpressionsOptions.ReportInvalidRegexPatterns, syntaxTree.Options.Language, syntaxTree, cancellationToken);
            if (!option)
            {
                return;
            }

            var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, _info);
            if (detector == null)
            {
                return;
            }

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
            SemanticModelAnalysisContext context, RegexPatternDetector detector,
            SyntaxToken token, CancellationToken cancellationToken)
        {
            if (token.RawKind == _info.StringLiteralTokenKind)
            {
                var tree = detector.TryParseRegexPattern(token, cancellationToken);
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
