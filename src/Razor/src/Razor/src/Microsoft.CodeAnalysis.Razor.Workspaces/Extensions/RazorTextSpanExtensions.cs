// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal static class RazorTextSpanExtensions
{
    public static TextSpan ToTextSpan(this RazorTextSpan razorTextSpan)
        => new TextSpan(razorTextSpan.Start, razorTextSpan.Length);
}
