// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp;

public readonly struct SourceTextLexer : IDisposable
{
    private readonly InternalSyntax.Lexer _lexer;

    internal SourceTextLexer(InternalSyntax.Lexer lexer)
    {
        _lexer = lexer;
    }

    public void Dispose()
    {
        _lexer.Dispose();
    }

    public SyntaxToken LexSyntax(int position)
    {
        _lexer.Reset(position, default(InternalSyntax.DirectiveStack));
        var token = _lexer.Lex(InternalSyntax.LexerMode.Syntax);
        return new SyntaxToken(parent: null, token, position, index: 0);
    }
}
