// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal static class EmbeddedSyntaxHelpers
    {
        public static TextSpan GetSpan<TSyntaxKind>(EmbeddedSyntaxToken<TSyntaxKind> token1, EmbeddedSyntaxToken<TSyntaxKind> token2) where TSyntaxKind : struct
            => GetSpan(token1.VirtualChars[0], token2.VirtualChars.Last());

        public static TextSpan GetSpan(VirtualCharSequence virtualChars)
            => GetSpan(virtualChars[0], virtualChars.Last());

        public static TextSpan GetSpan(VirtualChar firstChar, VirtualChar lastChar)
            => TextSpan.FromBounds(firstChar.Span.Start, lastChar.Span.End);
    }
}
