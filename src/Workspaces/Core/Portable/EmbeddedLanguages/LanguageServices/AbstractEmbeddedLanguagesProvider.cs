// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguagesProvider : IEmbeddedLanguagesProvider
    {
        private readonly ImmutableArray<IEmbeddedLanguage> _embeddedLanguages;
         
        protected AbstractEmbeddedLanguagesProvider(
            int stringLiteralTokenKind,
            int interpolatedTextTokenKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            _embeddedLanguages = ImmutableArray.Create<IEmbeddedLanguage>(
                new RegexEmbeddedLanguage(this, stringLiteralTokenKind, syntaxFacts, semanticFacts, virtualCharService),
                new FallbackEmbeddedLanguage(stringLiteralTokenKind, interpolatedTextTokenKind, syntaxFacts, semanticFacts, virtualCharService));
        }

        public ImmutableArray<IEmbeddedLanguage> GetEmbeddedLanguages()
            => _embeddedLanguages;

        /// <summary>
        /// Escapes the provided text given the rules of the language for this specific token.
        /// For example, in a normal c# string literal (```""```), this will escape backslashes.
        /// However, in a verbatim string literal (```@""```) it will not.
        /// </summary>
        internal abstract string EscapeText(string text, SyntaxToken token);
    }
}
