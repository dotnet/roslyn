// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageEditorFeaturesProvider 
        : AbstractEmbeddedLanguageFeaturesProvider, IEmbeddedLanguageEditorFeaturesProvider
    {
        private readonly ImmutableArray<IEmbeddedLanguageEditorFeatures> _embeddedLanguages;
         
        protected AbstractEmbeddedLanguageEditorFeaturesProvider(
            int stringLiteralTokenKind,
            int interpolatedTextTokenKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(stringLiteralTokenKind, interpolatedTextTokenKind, syntaxFacts, semanticFacts, virtualCharService)
        {
            _embeddedLanguages = ImmutableArray.Create<IEmbeddedLanguageEditorFeatures>(
                new RegexEmbeddedLanguageEditorFeatures(stringLiteralTokenKind, syntaxFacts, semanticFacts, virtualCharService));
        }

        public new ImmutableArray<IEmbeddedLanguageEditorFeatures> GetEmbeddedLanguages()
            => _embeddedLanguages;
    }
}
