﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    internal sealed class RegexEmbeddedLanguage : IEmbeddedLanguage
    {
        public int StringLiteralKind { get; }
        public ISyntaxFactsService SyntaxFacts { get; }
        public ISemanticFactsService SemanticFacts { get; }
        public IVirtualCharService VirtualCharService { get; }

        public IEmbeddedBraceMatcher BraceMatcher { get; }
        public IEmbeddedClassifier Classifier { get; }
        public IEmbeddedHighlighter Highlighter { get; }
        public IEmbeddedDiagnosticAnalyzer DiagnosticAnalyzer { get; }
        public IEmbeddedCodeFixProvider CodeFixProvider { get; }

        public RegexEmbeddedLanguage(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            StringLiteralKind = stringLiteralKind;
            SyntaxFacts = syntaxFacts;
            SemanticFacts = semanticFacts;
            VirtualCharService = virtualCharService;

            BraceMatcher = new RegexEmbeddedBraceMatcher(this);
            Classifier = new RegexEmbeddedClassifier(this);
            Highlighter = new RegexEmbeddedHighlighter(this);
            DiagnosticAnalyzer = new RegexDiagnosticAnalyzer(this);
        }
    }
}
