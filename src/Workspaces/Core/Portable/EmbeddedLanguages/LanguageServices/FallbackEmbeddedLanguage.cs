// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// A 'fallback' embedded language that can classify normal escape sequences in 
    /// C# or VB strings if no other embedded languages produce results.
    /// </summary>
    internal class FallbackEmbeddedLanguage : IEmbeddedLanguage
    {
        public int StringLiteralTokenKind { get; }
        public int InterpolatedTextTokenKind { get; }
        public ISyntaxFactsService SyntaxFacts { get; }
        public ISemanticFactsService SemanticFacts { get; }
        public IVirtualCharService VirtualCharService { get; }

        public FallbackEmbeddedLanguage(
            AbstractEmbeddedLanguageProvider languageProvider,
            int stringLiteralTokenKind,
            int interpolatedTextTokenKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            StringLiteralTokenKind = stringLiteralTokenKind;
            InterpolatedTextTokenKind = interpolatedTextTokenKind;
            SyntaxFacts = syntaxFacts;
            SemanticFacts = semanticFacts;
            VirtualCharService = virtualCharService;
            Classifier = new FallbackEmbeddedClassifier(languageProvider, this);
        }

        public IEmbeddedClassifier Classifier { get; }
    }
}
