// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class RazorSyntaxTokenExtensions
{
    public static bool IsValid(this SyntaxToken token)
        => token.Kind != SyntaxKind.None && !token.IsMissing;

    public static bool IsValid(this SyntaxToken token, out SyntaxToken validToken)
    {
        if (token.IsValid())
        {
            validToken = token;
            return true;
        }

        validToken = default;
        return false;
    }

    public static bool IsWhitespace(this SyntaxToken token)
        => token.Kind is SyntaxKind.Whitespace or SyntaxKind.NewLine;

    public static bool IsSpace(this SyntaxToken token)
        => token.Kind == SyntaxKind.Whitespace && token.Content == " ";

    public static bool IsTab(this SyntaxToken token)
        => token.Kind == SyntaxKind.Whitespace && token.Content == "\t";

    public static bool TryGetPreviousToken(this SyntaxToken token, out SyntaxToken result)
        => token.TryGetPreviousToken(includeZeroWidth: false, out result);

    public static bool TryGetPreviousToken(this SyntaxToken token, bool includeZeroWidth, out SyntaxToken result)
    {
        result = token.GetPreviousToken(includeZeroWidth);
        return result != default;
    }

    public static bool TryGetNextToken(this SyntaxToken token, out SyntaxToken result)
        => token.TryGetNextToken(includeZeroWidth: false, out result);

    public static bool TryGetNextToken(this SyntaxToken token, bool includeZeroWidth, out SyntaxToken result)
    {
        result = token.GetNextToken(includeZeroWidth);
        return result != default;
    }

    public static bool ContainsOnlyWhitespace(this SyntaxToken token, bool includingNewLines = true)
        => token.Kind == SyntaxKind.Whitespace || (includingNewLines && token.Kind == SyntaxKind.NewLine);

    public static LinePositionSpan GetLinePositionSpan(this SyntaxToken token, RazorSourceDocument source)
    {
        var start = token.Position;
        var end = token.EndPosition;
        var sourceText = source.Text;

        Debug.Assert(start <= sourceText.Length && end <= sourceText.Length, "Node position exceeds source length.");

        if (start == sourceText.Length && token.Width == 0)
        {
            // Marker symbol at the end of the document.
            var location = token.GetSourceLocation(source);

            return location.ToLinePosition().ToZeroWidthSpan();
        }

        return sourceText.GetLinePositionSpan(start, end);
    }
}
