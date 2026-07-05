// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class HtmlIntermediateToken : IntermediateToken
{
    public HtmlIntermediateToken(string content, SourceSpan? source)
        : base(content, source)
    {
    }

    internal HtmlIntermediateToken(LazyContent content, SourceSpan? source)
        : base(content, source)
    {
    }
}
