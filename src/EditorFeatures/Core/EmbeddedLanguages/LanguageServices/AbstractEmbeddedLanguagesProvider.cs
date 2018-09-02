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
    internal abstract class AbstractEditorFeaturesEmbeddedLanguagesProvider : AbstractFeaturesEmbeddedLanguagesProvider, IEditorFeaturesEmbeddedLanguagesProvider
    {
        private readonly ImmutableArray<IEditorFeaturesEmbeddedLanguage> _embeddedLanguages;
         
        protected AbstractEditorFeaturesEmbeddedLanguagesProvider(
            int stringLiteralTokenKind,
            int interpolatedTextTokenKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(stringLiteralTokenKind, interpolatedTextTokenKind, syntaxFacts, semanticFacts, virtualCharService)
        {
            _embeddedLanguages = ImmutableArray.Create<IEditorFeaturesEmbeddedLanguage>(
                new RegexEditorFeaturesEmbeddedLanguage(stringLiteralTokenKind, syntaxFacts, semanticFacts, virtualCharService));
        }

        public new ImmutableArray<IEditorFeaturesEmbeddedLanguage> GetEmbeddedLanguages()
            => _embeddedLanguages;
    }
}
