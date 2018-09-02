// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    internal class RegexEditorFeaturesEmbeddedLanguage : RegexFeaturesEmbeddedLanguage, IEditorFeaturesEmbeddedLanguage
    {
        public IBraceMatcher BraceMatcher { get; }

        public RegexEditorFeaturesEmbeddedLanguage(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(stringLiteralKind, syntaxFacts, semanticFacts, virtualCharService)
        {
            BraceMatcher = new RegexEmbeddedBraceMatcher(this);
        }
    }
}
