// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageFeaturesProvider : AbstractEmbeddedLanguagesProvider, IEmbeddedLanguageFeaturesProvider
    {
        private readonly ImmutableArray<IEmbeddedLanguageFeatures> _embeddedLanguages;
         
        protected AbstractEmbeddedLanguageFeaturesProvider(
            int stringLiteralTokenKind,
            int interpolatedTextTokenKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(stringLiteralTokenKind, interpolatedTextTokenKind, syntaxFacts, semanticFacts, virtualCharService)
        {
            _embeddedLanguages = ImmutableArray.Create<IEmbeddedLanguageFeatures>(
                new RegexEmbeddedLanguageFeatures(stringLiteralTokenKind, syntaxFacts, semanticFacts, virtualCharService));
        }

        public new ImmutableArray<IEmbeddedLanguageFeatures> GetEmbeddedLanguages()
            => _embeddedLanguages;
    }
}
