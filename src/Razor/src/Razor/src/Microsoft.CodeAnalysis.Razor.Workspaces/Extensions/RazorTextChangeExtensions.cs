// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal static class RazorTextChangeExtensions
{
    public static TextChange ToTextChange(this RazorTextChange razorTextChange)
        => new TextChange(razorTextChange.Span.ToTextSpan(), razorTextChange.NewText ?? "");
}
