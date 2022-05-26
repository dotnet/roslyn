// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime
{
    internal class DateAndTimeEmbeddedLanguageFeatures : DateAndTimeEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        // No highlights currently for date/time literals.
        public IDocumentHighlightsService? DocumentHighlightsService { get; }
        public EmbeddedLanguageCompletionProvider CompletionProvider { get; }

        public DateAndTimeEmbeddedLanguageFeatures(
            EmbeddedLanguageInfo info) : base(info)
        {
            CompletionProvider = new DateAndTimeEmbeddedCompletionProvider(this);
        }
    }
}
