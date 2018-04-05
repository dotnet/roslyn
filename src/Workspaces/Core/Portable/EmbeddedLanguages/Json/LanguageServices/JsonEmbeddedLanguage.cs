// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonEmbeddedLanguage : IEmbeddedLanguage
    {
        private readonly JsonEmbeddedClassifier _classifier;
        private readonly int _stringLiteralKind;
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly ISemanticFactsService _semanticFacts;
        private readonly IVirtualCharService _virtualCharService;

        public JsonEmbeddedLanguage(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            _stringLiteralKind = stringLiteralKind;
            _syntaxFacts = syntaxFacts;
            _semanticFacts = semanticFacts;
            _virtualCharService = virtualCharService;

            _classifier = new JsonEmbeddedClassifier(syntaxFacts, semanticFacts, virtualCharService);
        }

        public IEmbeddedBraceMatcher BraceMatcher => JsonEmbeddedBraceMatcher.Instance;

        public IEmbeddedClassifier Classifier => _classifier;

        public IEmbeddedDiagnosticAnalyzer GetDiagnosticAnalyzer(string style)
            => new JsonDiagnosticAnalyzer(style, _stringLiteralKind, _syntaxFacts, _semanticFacts, _virtualCharService);
    }
}
