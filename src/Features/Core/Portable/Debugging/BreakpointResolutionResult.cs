// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Debugging;

internal sealed class BreakpointResolutionResult
{
    public Document Document { get; }
    public TextSpan TextSpan { get; }
    public string? LocationNameOpt { get; }
    public bool IsLineBreakpoint { get; }

    private BreakpointResolutionResult(Document document, TextSpan textSpan, string? locationNameOpt, bool isLineBreakpoint)
    {
        Document = document;
        TextSpan = textSpan;
        LocationNameOpt = locationNameOpt;
        IsLineBreakpoint = isLineBreakpoint;
    }

    internal static BreakpointResolutionResult CreateSpanResult(Document document, TextSpan textSpan, string? locationNameOpt = null)
        => new(document, textSpan, locationNameOpt, isLineBreakpoint: false);

    internal static BreakpointResolutionResult CreateLineResult(Document document, string? locationNameOpt = null)
        => new(document, new TextSpan(), locationNameOpt, isLineBreakpoint: true);
}
