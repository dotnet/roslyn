// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public const string DiagnosticId = "RE0001";

        private readonly RegexEmbeddedLanguage _language;
        private readonly DiagnosticDescriptor _descriptor;

        public RegexDiagnosticAnalyzer(RegexEmbeddedLanguage language)
        {
            _language = language;

            _descriptor = new DiagnosticDescriptor(DiagnosticId,
                new LocalizableResourceString(nameof(WorkspacesResources.Regex_issue_0), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                new LocalizableResourceString(nameof(WorkspacesResources.Regex_issue_0), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                "REGEX",
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

            var root = syntaxTree.GetRoot(cancellationToken);
            Analyze(context, detector, root, cancellationToken);
        }

        private void Analyze(
            SemanticModelAnalysisContext context, RegexPatternDetector detector,
            SyntaxNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    Analyze(context, detector, child.AsNode(), cancellationToken);
                }
                else
                {
                    var token = child.AsToken();
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
    }
}
