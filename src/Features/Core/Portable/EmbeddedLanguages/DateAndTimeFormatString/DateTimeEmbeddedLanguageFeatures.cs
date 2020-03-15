// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.DateAndTimeFormatString.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTimeFormatString
{
    internal class DateAndTimeFormatStringEmbeddedLanguageFeatures : DateAndTimeFormatStringEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        public IDocumentHighlightsService DocumentHighlightsService { get; }
        public CompletionProvider CompletionProvider { get; }

        public DateAndTimeFormatStringEmbeddedLanguageFeatures(
            EmbeddedLanguageInfo info) : base(info)
        {
            CompletionProvider = new DateAndTimeFormatStringEmbeddedCompletionProvider(this);
        }
    }
}
