// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal struct EmbeddedLanguageInfo
    {
        public readonly int StringLiteralTokenKind;
        public readonly int InterpolatedTextTokenKind;
        public readonly ISyntaxFactsService SyntaxFacts;
        public readonly ISemanticFactsService SemanticFacts;
        public readonly IVirtualCharService VirtualCharService;

        public EmbeddedLanguageInfo(int stringLiteralTokenKind, int interpolatedTextTokenKind, ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, IVirtualCharService virtualCharService)
        {
            StringLiteralTokenKind = stringLiteralTokenKind;
            InterpolatedTextTokenKind = interpolatedTextTokenKind;
            SyntaxFacts = syntaxFacts;
            SemanticFacts = semanticFacts;
            VirtualCharService = virtualCharService;
        }
    }
}
