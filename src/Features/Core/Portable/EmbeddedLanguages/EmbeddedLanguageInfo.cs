// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    internal readonly struct EmbeddedLanguageInfo
    {
        public readonly ISyntaxFacts SyntaxFacts;
        public readonly ISemanticFactsService SemanticFacts;
        public readonly IVirtualCharService VirtualCharService;

        public readonly ISyntaxKinds SyntaxKinds => SyntaxFacts.SyntaxKinds;

        public EmbeddedLanguageInfo(
            ISyntaxFacts syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            SyntaxFacts = syntaxFacts;
            SemanticFacts = semanticFacts;
            VirtualCharService = virtualCharService;

            using var array = TemporaryArray<int>.Empty;
            array.Add(syntaxFacts.SyntaxKinds.StringLiteralToken);
            array.AsRef().AddIfNotNull(syntaxFacts.SyntaxKinds.SingleLineRawStringLiteralToken);
            array.AsRef().AddIfNotNull(syntaxFacts.SyntaxKinds.MultiLineRawStringLiteralToken);
            AllStringLiteralKinds = array.ToImmutableAndClear();
        }

        public readonly ImmutableArray<int> AllStringLiteralKinds { get; }

        public readonly bool IsAnyStringLiteral(int rawKind)
        {
            return rawKind == SyntaxKinds.StringLiteralToken ||
                   rawKind == SyntaxKinds.SingleLineRawStringLiteralToken ||
                   rawKind == SyntaxKinds.MultiLineRawStringLiteralToken ||
                   rawKind == SyntaxKinds.Utf8StringLiteralToken ||
                   rawKind == SyntaxKinds.Utf8SingleLineRawStringLiteralToken ||
                   rawKind == SyntaxKinds.Utf8MultiLineRawStringLiteralToken;
        }
    }
}
