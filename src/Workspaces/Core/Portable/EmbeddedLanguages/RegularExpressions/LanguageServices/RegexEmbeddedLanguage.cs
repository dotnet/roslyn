// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    internal class RegexEmbeddedLanguage : IEmbeddedLanguage
    {
        public int StringLiteralKind { get; }
        public ISyntaxFactsService SyntaxFacts { get; }
        public ISemanticFactsService SemanticFacts { get; }
        public IVirtualCharService VirtualCharService { get; }

        public RegexEmbeddedLanguage(
            AbstractEmbeddedLanguageProvider languageProvider,
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            StringLiteralKind = stringLiteralKind;
            SyntaxFacts = syntaxFacts;
            SemanticFacts = semanticFacts;
            VirtualCharService = virtualCharService;

            BraceMatcher = new RegexEmbeddedBraceMatcher(this);
            Classifier = new RegexEmbeddedClassifier(this);
            Highlighter = new RegexEmbeddedHighlighter(this);
            DiagnosticAnalyzer = new RegexDiagnosticAnalyzer(this);
            CompletionProvider = new RegexEmbeddedCompletionProvider(this);
        }

        public IEmbeddedBraceMatcher BraceMatcher { get; }
        public IEmbeddedClassifier Classifier { get; }
        public IEmbeddedHighlighter Highlighter { get; }
        public IEmbeddedDiagnosticAnalyzer DiagnosticAnalyzer { get; }
        public IEmbeddedCodeFixProvider CodeFixProvider { get; }
        public IEmbeddedCompletionProvider CompletionProvider { get; }

        internal async Task<RegexTree> TryGetTreeAtPositionAsync(
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
            var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, this);
            var tree = detector?.TryParseRegexPattern(token, cancellationToken);

            return tree;
        }
    }
}
