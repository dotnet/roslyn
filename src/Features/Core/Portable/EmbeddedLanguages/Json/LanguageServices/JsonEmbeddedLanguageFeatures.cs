// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonEmbeddedLanguageFeatures : JsonEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        public ImmutableArray<AbstractBuiltInCodeStyleDiagnosticAnalyzer> DiagnosticAnalyzers { get; }
        public SyntaxEditorBasedCodeFixProvider CodeFixProvider { get; }

        // No document-highlights for embedded json currently.
        public IDocumentHighlightsService? DocumentHighlightsService => null;

        // No completion for embedded json currently.
        public EmbeddedLanguageCompletionProvider? CompletionProvider => null;

        public JsonEmbeddedLanguageFeatures(
            AbstractEmbeddedLanguageFeaturesProvider languagesProvider,
            EmbeddedLanguageInfo info) : base(info)
        {
            DiagnosticAnalyzers = ImmutableArray.Create<AbstractBuiltInCodeStyleDiagnosticAnalyzer>(
                new JsonDiagnosticAnalyzer(info),
                new JsonDetectionAnalyzer(info));
            CodeFixProvider = new JsonEmbeddedCodeFixProvider(languagesProvider, info);
        }
    }
}
