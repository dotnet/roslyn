// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    /// <summary>
    /// Services related to a specific embedded language.
    /// </summary>
    internal interface IEmbeddedLanguageFeatures : IEmbeddedLanguage
    {
        /// <summary>
        /// A optional highlighter that can highlight spans for an embedded language string.
        /// </summary>
        IDocumentHighlightsService DocumentHighlightsService { get; }

        /// <summary>
        /// An optional completion provider that can provide completion items for this
        /// specific embedded language.
        /// 
        /// <see cref="EmbeddedLanguageCompletionProvider"/> will aggregate all these
        /// individual providers and expose them as one single completion provider to
        /// the rest of Roslyn.
        /// </summary>
        CompletionProvider CompletionProvider { get; }
    }
}
