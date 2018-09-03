// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguageFeatures : RegexEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        public IDocumentHighlightsService DocumentHighlightsService { get; }
        public DiagnosticAnalyzer DiagnosticAnalyzer { get; }
        public SyntaxEditorBasedCodeFixProvider CodeFixProvider { get; }

        public RegexEmbeddedLanguageFeatures(EmbeddedLanguageInfo info) : base(info)
        {
            DocumentHighlightsService = new RegexDocumentHighlightsService(info);
            DiagnosticAnalyzer = new RegexDiagnosticAnalyzer(info);
        }
    }
}
