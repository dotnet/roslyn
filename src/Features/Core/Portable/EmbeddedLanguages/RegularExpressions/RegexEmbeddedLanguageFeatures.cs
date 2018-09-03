// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguageFeatures : RegexEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        public IDocumentHighlightsService DocumentHighlightsService { get; }
        public ImmutableArray<AbstractCodeStyleDiagnosticAnalyzer> DiagnosticAnalyzers { get; }

        // No CodeFixProvider for any regex diagnostics.
        public SyntaxEditorBasedCodeFixProvider CodeFixProvider => null;

        public RegexEmbeddedLanguageFeatures(EmbeddedLanguageInfo info) : base(info)
        {
            DocumentHighlightsService = new RegexDocumentHighlightsService(this);
            DiagnosticAnalyzers = ImmutableArray.Create<AbstractCodeStyleDiagnosticAnalyzer>(
                new RegexDiagnosticAnalyzer(info));
        }
    }
}
