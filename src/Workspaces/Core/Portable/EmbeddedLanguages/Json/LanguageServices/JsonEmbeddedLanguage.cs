// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonEmbeddedLanguage : IEmbeddedLanguage
    {
        public int StringLiteralKind { get; }
        public ISyntaxFactsService SyntaxFacts { get; }
        public ISemanticFactsService SemanticFacts { get; }
        public IVirtualCharService VirtualCharService { get; }

        public JsonEmbeddedLanguage(
            AbstractEmbeddedLanguagesProvider languagesProvider,
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            StringLiteralKind = stringLiteralKind;
            SyntaxFacts = syntaxFacts;
            SemanticFacts = semanticFacts;
            VirtualCharService = virtualCharService;

            BraceMatcher = new JsonEmbeddedBraceMatcher(this);
            Classifier = new JsonEmbeddedClassifier(this);
            DiagnosticAnalyzer = new AggregateEmbeddedDiagnosticAnalyzer(
                new JsonDiagnosticAnalyzer(this),
                new JsonDetectionAnalyzer(this));
            CodeFixProvider = new JsonEmbeddedCodeFixProvider(languagesProvider, this);
        }

        public IEmbeddedBraceMatcher BraceMatcher { get; }
        public IEmbeddedClassifier Classifier { get; }
        public IEmbeddedDiagnosticAnalyzer DiagnosticAnalyzer { get; }
        public IEmbeddedCodeFixProvider CodeFixProvider { get; }

        // No document-highlights for embedded json currently.
        public IEmbeddedHighlighter Highlighter => null;
    }
}
