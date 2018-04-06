// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonEmbeddedLanguage : IEmbeddedLanguage
    {
        public JsonEmbeddedLanguage(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            Classifier = new JsonEmbeddedClassifier(stringLiteralKind, syntaxFacts, semanticFacts, virtualCharService);
            DiagnosticAnalyzer = new JsonDiagnosticAnalyzer(stringLiteralKind, syntaxFacts, semanticFacts, virtualCharService);
        }

        public IEmbeddedBraceMatcher BraceMatcher => JsonEmbeddedBraceMatcher.Instance;
        public IEmbeddedClassifier Classifier { get; }
        public IEmbeddedDiagnosticAnalyzer DiagnosticAnalyzer { get; }
    }
}
