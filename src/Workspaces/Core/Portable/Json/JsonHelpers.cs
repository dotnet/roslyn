// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.Json
{
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

    internal static class JsonHelpers
    {
        public static JsonToken CreateToken(JsonKind kind, ImmutableArray<JsonTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<JsonTrivia> trailingTrivia)
            => CreateToken(kind, leadingTrivia, virtualChars, trailingTrivia, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static JsonToken CreateToken(JsonKind kind, 
            ImmutableArray<JsonTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars,
             ImmutableArray<JsonTrivia> trailingTrivia, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => CreateToken(kind, leadingTrivia, virtualChars, trailingTrivia, diagnostics, value: null);

        public static JsonToken CreateToken(JsonKind kind,
            ImmutableArray<JsonTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<JsonTrivia> trailingTrivia, ImmutableArray<EmbeddedDiagnostic> diagnostics, object value)
            => new JsonToken(kind, leadingTrivia, virtualChars, trailingTrivia, diagnostics, value);

        public static JsonToken CreateMissingToken(JsonKind kind)
            => CreateToken(kind, ImmutableArray<JsonTrivia>.Empty, ImmutableArray<VirtualChar>.Empty, ImmutableArray<JsonTrivia>.Empty);

        public static JsonTrivia CreateTrivia(JsonKind kind, ImmutableArray<VirtualChar> virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static JsonTrivia CreateTrivia(JsonKind kind, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new JsonTrivia(kind, virtualChars, diagnostics);
    }
}
