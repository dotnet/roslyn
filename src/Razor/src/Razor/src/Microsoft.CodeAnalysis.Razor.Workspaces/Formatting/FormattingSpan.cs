// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed record class FormattingSpan(
    TextSpan Span,
    FormattingSpanKind Kind,
    int RazorIndentationLevel,
    int HtmlIndentationLevel,
    bool IsInGlobalNamespace,
    bool IsInClassBody = false,
    int ComponentLambdaNestingLevel = 0)
{
    public int IndentationLevel => RazorIndentationLevel + HtmlIndentationLevel;

    public int MinCSharpIndentLevel
    {
        get
        {
            var baseIndent = 1;

            if (!IsInGlobalNamespace)
            {
                baseIndent++;
            }

            if (!IsInClassBody)
            {
                baseIndent++;
            }

            return baseIndent + ComponentLambdaNestingLevel;
        }
    }
}
