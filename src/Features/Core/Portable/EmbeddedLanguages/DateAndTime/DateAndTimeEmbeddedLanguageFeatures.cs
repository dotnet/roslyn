// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.DateAndTime.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime
{
    internal class DateAndTimeEmbeddedLanguageFeatures : DateAndTimeEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        // No highlights currently for date/time literals.
        public IDocumentHighlightsService? DocumentHighlightsService { get; }
        public CompletionProvider CompletionProvider { get; }

        public DateAndTimeEmbeddedLanguageFeatures(
            EmbeddedLanguageInfo info) : base(info)
        {
            CompletionProvider = new DateAndTimeEmbeddedCompletionProvider(this);
        }
    }
}
