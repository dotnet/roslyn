// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.DateTime.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateTime
{
    internal class DateTimeEmbeddedLanguageFeatures : DateTimeEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        private readonly AbstractEmbeddedLanguageFeaturesProvider _provider;

        public IDocumentHighlightsService DocumentHighlightsService { get; }
        public CompletionProvider CompletionProvider { get; }

        public DateTimeEmbeddedLanguageFeatures(
            AbstractEmbeddedLanguageFeaturesProvider provider,
            EmbeddedLanguageInfo info) : base(info)
        {
            _provider = provider;

            CompletionProvider = new DateTimeEmbeddedCompletionProvider(this);
        }

        public string EscapeText(string text, SyntaxToken token)
            => _provider.EscapeText(text, token);
    }
}
