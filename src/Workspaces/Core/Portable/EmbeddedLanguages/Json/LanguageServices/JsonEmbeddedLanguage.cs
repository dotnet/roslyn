// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonEmbeddedLanguage : IEmbeddedLanguage
    {
        private readonly JsonEmbeddedClassifier _classifier;

        public JsonEmbeddedLanguage(
            ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, IVirtualCharService virtualCharService)
        {
            _classifier = new JsonEmbeddedClassifier(syntaxFacts, semanticFacts, virtualCharService);
        }

        public IEmbeddedBraceMatcher BraceMatcher => JsonEmbeddedBraceMatcher.Instance;

        public IEmbeddedClassifier Classifier => _classifier;
    }
}
