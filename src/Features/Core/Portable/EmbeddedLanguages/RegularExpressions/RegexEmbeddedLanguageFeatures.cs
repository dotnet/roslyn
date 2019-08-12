// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguageFeatures : RegexEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        private readonly AbstractEmbeddedLanguageFeaturesProvider _provider;

        public IDocumentHighlightsService DocumentHighlightsService { get; }
        public CompletionProvider CompletionProvider { get; }

        public RegexEmbeddedLanguageFeatures(
            AbstractEmbeddedLanguageFeaturesProvider provider,
            EmbeddedLanguageInfo info) : base(info)
        {
            _provider = provider;

            DocumentHighlightsService = new RegexDocumentHighlightsService(this);
            CompletionProvider = new RegexEmbeddedCompletionProvider(this);
        }

        public string EscapeText(string text, SyntaxToken token)
            => _provider.EscapeText(text, token);
    }
}
