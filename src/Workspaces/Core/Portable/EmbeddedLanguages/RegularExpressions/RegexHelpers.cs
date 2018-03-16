// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    internal static class RegexHelpers
    {
        public static bool HasOption(RegexOptions options, RegexOptions val)
            => (options & val) != 0;

        public static RegexToken CreateToken(RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars)
            => CreateToken(kind, leadingTrivia, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static RegexToken CreateToken(
            RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => CreateToken(kind, leadingTrivia, virtualChars, diagnostics, value: null);

        public static RegexToken CreateToken(
            RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<EmbeddedDiagnostic> diagnostics, object value)
            => new RegexToken(kind, leadingTrivia, virtualChars, ImmutableArray<RegexTrivia>.Empty, diagnostics, value);

        public static RegexToken CreateMissingToken(RegexKind kind)
            => CreateToken(kind, ImmutableArray<RegexTrivia>.Empty, ImmutableArray<VirtualChar>.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new RegexTrivia(kind, virtualChars, diagnostics);
    }
}
