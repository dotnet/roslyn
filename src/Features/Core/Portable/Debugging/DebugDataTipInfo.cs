// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Debugging;

internal enum DebugDataTipInfoKind
{
    None,
    LinqExpression,
}

internal readonly record struct DebugDataTipInfo(
    TextSpan Span, TextSpan ExpressionSpan, string? Text, DebugDataTipInfoKind Kind = DebugDataTipInfoKind.None)
{
    public DebugDataTipInfo(TextSpan span, string? text)
        : this(span, span, text)
    {
    }

    public bool IsDefault
        => Span.Length == 0 && Span.Start == 0 && Text == null;
}
