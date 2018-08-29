// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    /// <summary>
    /// Analyzer that reports diagnostics in strings that we know are regex text.
    /// </summary>
    internal sealed class RegexDiagnosticAnalyzer : IEmbeddedDiagnosticAnalyzer
    {
        public const string DiagnosticId = "RE001";

        private readonly RegexEmbeddedLanguage _language;
        private readonly DiagnosticDescriptor _descriptor;

        public RegexDiagnosticAnalyzer(RegexEmbeddedLanguage language)
        {
            _language = language;

            _descriptor = new DiagnosticDescriptor(DiagnosticId,
                new LocalizableResourceString(nameof(WorkspacesResources.Regex_issue_0), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                new LocalizableResourceString(nameof(WorkspacesResources.Regex_issue_0), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                category: "REGEX",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            SupportedDiagnostics = ImmutableArray.Create(_descriptor);
        }

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public void Analyze(SemanticModelAnalysisContext context, OptionSet optionSet)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var option = optionSet.GetOption(RegularExpressionsOptions.ReportInvalidRegexPatterns, syntaxTree.Options.Language);
            if (!option)
            {
                return;
            }

            var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, _language);
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
            if (token.RawKind == _language.StringLiteralKind)
            {
                var tree = detector.TryParseRegexPattern(token, cancellationToken);
                if (tree != null)
                {
                    foreach (var diag in tree.Diagnostics)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            _descriptor,
                            Location.Create(context.SemanticModel.SyntaxTree, diag.Span),
                            diag.Message));
                    }
                }
            }
        }
    }
}
