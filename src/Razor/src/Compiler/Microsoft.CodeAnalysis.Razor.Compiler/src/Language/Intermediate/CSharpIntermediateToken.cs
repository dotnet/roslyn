// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class CSharpIntermediateToken : IntermediateToken
{
    public CSharpIntermediateToken(string content, SourceSpan? source)
        : base(content, source)
    {
    }

    internal CSharpIntermediateToken(LazyContent content, SourceSpan? source)
        : base(content, source)
    {
    }
}
