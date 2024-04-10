// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints;

internal readonly struct TypeHint(ITypeSymbol type, TextSpan span, TextChange? textChange, bool leadingSpace = false, bool trailingSpace = false)
{
    private static readonly ImmutableArray<SymbolDisplayPart> s_spaceArray = [new SymbolDisplayPart(SymbolDisplayPartKind.Space, symbol: null, " ")];

    public ITypeSymbol Type { get; } = type;
    public TextSpan Span { get; } = span;
    public TextChange? TextChange { get; } = textChange;
    public ImmutableArray<SymbolDisplayPart> Prefix { get; } = CreateSpaceSymbolPartArray(leadingSpace);
    public ImmutableArray<SymbolDisplayPart> Suffix { get; } = CreateSpaceSymbolPartArray(trailingSpace);

    private static ImmutableArray<SymbolDisplayPart> CreateSpaceSymbolPartArray(bool hasSpace)
        => hasSpace ? s_spaceArray : [];

    public void Deconstruct(out ITypeSymbol type, out TextSpan span, out TextChange? textChange, out ImmutableArray<SymbolDisplayPart> prefix, out ImmutableArray<SymbolDisplayPart> suffix)
    {
        type = Type;
        span = Span;
        textChange = TextChange;
        prefix = Prefix;
        suffix = Suffix;
    }
}
