// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal class RegexEmbeddedLanguageEditorFeatures : RegexEmbeddedLanguageFeatures, IEmbeddedLanguageEditorFeatures
    {
        public IBraceMatcher BraceMatcher { get; }

        public RegexEmbeddedLanguageEditorFeatures(
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
