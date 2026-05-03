// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal interface ILegacySyntax
{
    ISpanChunkGenerator? ChunkGenerator { get; }
    SpanEditHandler? EditHandler { get; }

    SyntaxNode Update(ISpanChunkGenerator? chunkGenerator, SpanEditHandler? editHandler);
    SyntaxNode WithEditHandler(SpanEditHandler? editHandler);
}
