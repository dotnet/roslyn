// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal static class IntermediateNodeFactory
{
    public static CSharpIntermediateToken CSharpToken(string content, SourceSpan? source = null)
        => new(content, source);

    public static CSharpIntermediateToken CSharpToken<T>(T arg, Func<T, string> contentFactory, SourceSpan? source = null)
        => new(LazyContent.Create(arg, contentFactory), source);

    public static HtmlIntermediateToken HtmlToken(string content, SourceSpan? source = null)
        => new(content, source);

    public static HtmlIntermediateToken HtmlToken<T>(T arg, Func<T, string> contentFactory, SourceSpan? source = null)
        => new(LazyContent.Create(arg, contentFactory), source);
}
