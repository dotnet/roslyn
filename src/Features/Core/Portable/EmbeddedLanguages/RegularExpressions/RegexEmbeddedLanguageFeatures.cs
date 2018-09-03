// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguageFeatures : RegexEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        public IDocumentHighlightsService Highlighter { get; }
        public ImmutableArray<AbstractCodeStyleDiagnosticAnalyzer> DiagnosticAnalyzers { get; }
        public SyntaxEditorBasedCodeFixProvider CodeFixProvider { get; }

        public RegexEmbeddedLanguageFeatures(EmbeddedLanguageInfo info) : base(info)
        {
            Highlighter = new RegexEmbeddedHighlighter(info);
            DiagnosticAnalyzers = ImmutableArray.Create<AbstractCodeStyleDiagnosticAnalyzer>(
                new RegexDiagnosticAnalyzer(info));
        }
    }
}
