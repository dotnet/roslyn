// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;

using JsonToken = EmbeddedSyntaxToken<JsonKind>;
using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

internal static class JsonHelpers
{
    public static JsonToken CreateToken(
        JsonKind kind, ImmutableArray<JsonTrivia> leadingTrivia,
        VirtualCharSequence virtualChars, ImmutableArray<JsonTrivia> trailingTrivia)
        => CreateToken(kind, leadingTrivia, virtualChars, trailingTrivia, []);

    public static JsonToken CreateToken(JsonKind kind,
        ImmutableArray<JsonTrivia> leadingTrivia, VirtualCharSequence virtualChars,
        ImmutableArray<JsonTrivia> trailingTrivia, ImmutableArray<EmbeddedDiagnostic> diagnostics)
        => new(kind, leadingTrivia, virtualChars, trailingTrivia, diagnostics, value: null!);

    public static JsonToken CreateMissingToken(JsonKind kind)
        => CreateToken(kind, [], VirtualCharSequence.Empty, []);

    public static JsonTrivia CreateTrivia(JsonKind kind, VirtualCharSequence virtualChars)
        => CreateTrivia(kind, virtualChars, []);

    public static JsonTrivia CreateTrivia(JsonKind kind, VirtualCharSequence virtualChars, EmbeddedDiagnostic diagnostic)
        => CreateTrivia(kind, virtualChars, [diagnostic]);

    public static JsonTrivia CreateTrivia(JsonKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
        => new(kind, virtualChars, diagnostics);
}
