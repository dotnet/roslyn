// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Helper that can be used to efficiently build up a <see cref="SourceSpan"/> or
///  <see cref="CodeAnalysis.Text.TextSpan"/> from a set of syntax tokens.
/// </summary>
internal ref struct SpanComputer()
{
    private SyntaxToken _firstToken;
    private SyntaxToken _lastToken;

    public void Add(SyntaxToken token)
    {
        if (token.Kind == SyntaxKind.None)
        {
            return;
        }

        if (_firstToken.Kind == SyntaxKind.None)
        {
            _firstToken = token;
        }

        _lastToken = token;
    }

    public void Add(SyntaxTokenList tokenList)
    {
        if (tokenList.Count == 0)
        {
            return;
        }

        if (_firstToken.Kind == SyntaxKind.None)
        {
            _firstToken = tokenList[0];
        }

        _lastToken = tokenList[^1];
    }

    public void Add(SyntaxTokenList? tokenList)
    {
        if (tokenList is not [_, ..] tokens)
        {
            return;
        }

        if (_firstToken.Kind == SyntaxKind.None)
        {
            _firstToken = tokens[0];
        }

        _lastToken = tokens[^1];
    }

    public void Add(CSharpEphemeralTextLiteralSyntax? literal)
    {
        Add(literal?.LiteralTokens);
    }

    public void Add(CSharpExpressionLiteralSyntax? literal)
    {
        Add(literal?.LiteralTokens);
    }

    public void Add(CSharpStatementLiteralSyntax? literal)
    {
        Add(literal?.LiteralTokens);
    }

    public void Add(MarkupEphemeralTextLiteralSyntax? literal)
    {
        Add(literal?.LiteralTokens);
    }

    public void Add(MarkupTextLiteralSyntax? literal)
    {
        Add(literal?.LiteralTokens);
    }

    public void Add(UnclassifiedTextLiteralSyntax? literal)
    {
        Add(literal?.LiteralTokens);
    }

    public readonly SourceSpan ToSourceSpan(RazorSourceDocument source)
    {
        if (_firstToken.Kind == SyntaxKind.None)
        {
            return default;
        }

        Debug.Assert(_lastToken.Kind != SyntaxKind.None, "Last token should not be None when first token is set.");

        var start = _firstToken.Span.Start;
        var end = _lastToken.Span.End;

        Debug.Assert(start <= end, "Start position should not be greater than end position.");

        var length = end - start;

        var text = source.Text;
        var startLinePosition = text.Lines.GetLinePosition(start);
        var endLinePosition = text.Lines.GetLinePosition(end);
        var lineCount = endLinePosition.Line - startLinePosition.Line;

        return new SourceSpan(source.FilePath, absoluteIndex: start, startLinePosition.Line, startLinePosition.Character, length, lineCount, endLinePosition.Character);
    }

    public readonly TextSpan ToTextSpan()
    {
        if (_firstToken.Kind == SyntaxKind.None)
        {
            return default;
        }

        Debug.Assert(_lastToken.Kind != SyntaxKind.None, "Last token should not be None when first token is set.");

        var start = _firstToken.Span.Start;
        var end = _lastToken.Span.End;

        Debug.Assert(start <= end, "Start position should not be greater than end position.");

        return TextSpan.FromBounds(start, end);
    }
}
