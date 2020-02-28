// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
