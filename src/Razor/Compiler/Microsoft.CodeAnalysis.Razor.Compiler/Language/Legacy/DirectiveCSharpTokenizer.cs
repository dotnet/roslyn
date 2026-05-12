// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class DirectiveCSharpTokenizer(SeekableTextReader source) : NativeCSharpTokenizer(source)
{
    private bool _visitedFirstTokenStart;
    private bool _visitedFirstTokenLineEnd;

    protected override StateResult Dispatch()
    {
        var result = base.Dispatch();
        if (result.Result != null && !_visitedFirstTokenStart && IsValidTokenType(result.Result.Kind))
        {
            _visitedFirstTokenStart = true;
        }
        else if (result.Result != null && _visitedFirstTokenStart && result.Result.Kind == SyntaxKind.NewLine)
        {
            _visitedFirstTokenLineEnd = true;
        }

        return result;
    }

    public override SyntaxToken NextToken()
    {
        // Post-Condition: Buffer should be empty at the start of Next()
        Debug.Assert(Buffer.Length == 0);
        StartToken();

        if (EndOfFile || (_visitedFirstTokenStart && _visitedFirstTokenLineEnd))
        {
            return null;
        }

        var token = Turn();

        // Post-Condition: Buffer should be empty at the end of Next()
        Debug.Assert(Buffer.Length == 0);

        return token;
    }

    private bool IsValidTokenType(SyntaxKind kind)
    {
        return kind != SyntaxKind.Whitespace &&
            kind != SyntaxKind.NewLine &&
            kind != SyntaxKind.CSharpComment &&
            kind != SyntaxKind.RazorCommentLiteral &&
            kind != SyntaxKind.RazorCommentStar &&
            kind != SyntaxKind.RazorCommentTransition &&
            kind != SyntaxKind.Transition;
    }
}
