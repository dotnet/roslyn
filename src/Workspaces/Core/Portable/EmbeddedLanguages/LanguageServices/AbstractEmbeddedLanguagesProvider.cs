// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
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
                new RegexEmbeddedLanguage(stringLiteralTokenKind, syntaxFacts, semanticFacts, virtualCharService),
                new FallbackEmbeddedLanguage(stringLiteralTokenKind, interpolatedTextTokenKind, syntaxFacts, semanticFacts, virtualCharService));
        }

        public ImmutableArray<IEmbeddedLanguage> GetEmbeddedLanguages()
            => _embeddedLanguages;

        /// <summary>
        /// Helper method used by the VB and C# <see cref="IEmbeddedCodeFixProvider"/>s so they can
        /// add special comments to string literals to convey that language services should light up
        /// for them.
        /// </summary>
        internal abstract void AddComment(
            SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents);
    }
}
