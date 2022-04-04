// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.DocumentHighlighting;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonEmbeddedLanguage : IEmbeddedLanguageFeatures
    {
        // No document-highlights for embedded json currently.
        public IDocumentHighlightsService? DocumentHighlightsService => null;

        // No completion for embedded json currently.
        public EmbeddedLanguageCompletionProvider? CompletionProvider => null;
    }
}
