// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguageFeatures : RegexEmbeddedLanguage, IEmbeddedLanguageFeatures
    {
        public IDocumentHighlightsService Highlighter { get; }
        public DiagnosticAnalyzer DiagnosticAnalyzer { get; }
        public SyntaxEditorBasedCodeFixProvider CodeFixProvider { get; }

        public RegexEmbeddedLanguageFeatures(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(stringLiteralKind, syntaxFacts, semanticFacts, virtualCharService)
        {
            Highlighter = new RegexEmbeddedHighlighter(this);
            DiagnosticAnalyzer = new RegexDiagnosticAnalyzer(this);
        }
    }
}
