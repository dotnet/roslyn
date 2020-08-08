// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguage : IEmbeddedLanguageFeatures
    {
        public readonly EmbeddedLanguageInfo Info;

        private readonly AbstractEmbeddedLanguageFeaturesProvider _provider;

        public ISyntaxClassifier Classifier { get; }
        public IDocumentHighlightsService DocumentHighlightsService { get; }
        public CompletionProvider CompletionProvider { get; }

        public RegexEmbeddedLanguage(
            AbstractEmbeddedLanguageFeaturesProvider provider,
            EmbeddedLanguageInfo info)
        {
            Info = info;
            Classifier = new RegexSyntaxClassifier(info);

            _provider = provider;

            DocumentHighlightsService = new RegexDocumentHighlightsService(this);
            CompletionProvider = new RegexEmbeddedCompletionProvider(this);
        }

        internal async Task<(RegexTree tree, SyntaxToken token)> TryGetTreeAndTokenAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!RegexPatternDetector.IsPossiblyPatternToken(token, syntaxFacts))
                return default;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, this.Info);
            var tree = detector?.TryParseRegexPattern(token, cancellationToken);
            return tree == null ? default : (tree, token);
        }

        internal async Task<RegexTree> TryGetTreeAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var (tree, _) = await TryGetTreeAndTokenAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            return tree;
        }

        public string EscapeText(string text, SyntaxToken token)
            => _provider.EscapeText(text, token);
    }
}
