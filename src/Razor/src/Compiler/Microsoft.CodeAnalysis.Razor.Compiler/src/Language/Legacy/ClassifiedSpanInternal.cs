// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal struct ClassifiedSpanInternal
{
    public ClassifiedSpanInternal(SourceSpan span, SourceSpan blockSpan, SpanKindInternal spanKind, BlockKindInternal blockKind, AcceptedCharactersInternal acceptedCharacters)
    {
        Span = span;
        BlockSpan = blockSpan;
        SpanKind = spanKind;
        BlockKind = blockKind;
#pragma warning disable CS0618 // Type or member is obsolete
        AcceptedCharacters = acceptedCharacters;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Obsolete("This property is deprecated. Do not use it.", error: false)]
    public AcceptedCharactersInternal AcceptedCharacters { get; }

    public BlockKindInternal BlockKind { get; }

    public SourceSpan BlockSpan { get; }

    public SourceSpan Span { get; }

    public SpanKindInternal SpanKind { get; }
}
