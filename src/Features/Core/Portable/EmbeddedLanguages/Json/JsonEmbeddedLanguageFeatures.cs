// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    internal class JsonEmbeddedLanguageFeatures : JsonEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        public ImmutableArray<AbstractCodeStyleDiagnosticAnalyzer> DiagnosticAnalyzers { get; }
        public SyntaxEditorBasedCodeFixProvider CodeFixProvider { get; }

        // No document-highlights for embedded json currently.
        public IDocumentHighlightsService Highlighter => null;

        public JsonEmbeddedLanguageFeatures(
            AbstractEmbeddedLanguageFeaturesProvider languagesProvider,
            EmbeddedLanguageInfo info) : base(info)
        {
            DiagnosticAnalyzers = ImmutableArray.Create<AbstractCodeStyleDiagnosticAnalyzer>(
                new JsonDiagnosticAnalyzer(info),
                new JsonDetectionAnalyzer(info));
            CodeFixProvider = new JsonEmbeddedCodeFixProvider(languagesProvider, info);
        }
    }
}
