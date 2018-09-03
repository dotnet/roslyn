// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguageFeatures : RegexEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        private readonly AbstractEmbeddedLanguageFeaturesProvider _provider;

        public IDocumentHighlightsService DocumentHighlightsService { get; }
        public DiagnosticAnalyzer DiagnosticAnalyzer { get; }
        public CompletionProvider CompletionProvider { get; }

        public RegexEmbeddedLanguageFeatures(
            AbstractEmbeddedLanguageFeaturesProvider provider,
            EmbeddedLanguageInfo info) : base(info)
        {
            _provider = provider;

            DocumentHighlightsService = new RegexDocumentHighlightsService(this);
            DiagnosticAnalyzer = new RegexDiagnosticAnalyzer(info);
            CompletionProvider = new RegexEmbeddedCompletionProvider(this);
        }

        public string EscapeText(string text, SyntaxToken token)
            => _provider.EscapeText(text, token);

        internal async Task<(RegexTree tree, SyntaxToken token)?> TryGetTreeAndTokenAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (RegexPatternDetector.IsDefinitelyNotPattern(token, syntaxFacts))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, this.Info);
            var tree = detector?.TryParseRegexPattern(token, cancellationToken);
            if (tree == null)
            {
                return null;
            }

            return (tree, token);
        }

        internal async Task<RegexTree> TryGetTreeAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var treeAndToken = await TryGetTreeAndTokenAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            return treeAndToken?.tree;
        }
    }
}
