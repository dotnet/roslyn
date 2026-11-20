// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal static class TextChangeExtensions
{
    public static RazorTextChange ToRazorTextChange(this TextChange textChange)
    {
        return new RazorTextChange()
        {
            Span = new RazorTextSpan()
            {
                Start = textChange.Span.Start,
                Length = textChange.Span.Length,
            },
            NewText = textChange.NewText
        };
    }
}
