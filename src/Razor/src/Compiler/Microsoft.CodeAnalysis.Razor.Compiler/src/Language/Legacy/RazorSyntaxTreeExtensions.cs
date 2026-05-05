// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal static class RazorSyntaxTreeExtensions
{
    public static ImmutableArray<ClassifiedSpanInternal> GetClassifiedSpans(this RazorSyntaxTree syntaxTree)
    {
        ArgHelper.ThrowIfNull(syntaxTree);

        return ClassifiedSpanVisitor.VisitRoot(syntaxTree);
    }

    public static ImmutableArray<TagHelperSpanInternal> GetTagHelperSpans(this RazorSyntaxTree syntaxTree)
    {
        ArgHelper.ThrowIfNull(syntaxTree);

        return TagHelperSpanVisitor.VisitRoot(syntaxTree);
    }
}
