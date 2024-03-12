// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp;

public readonly struct SourceTextTokenParser : IDisposable
{
    private readonly InternalSyntax.Lexer _lexer;

    internal SourceTextTokenParser(InternalSyntax.Lexer lexer)
    {
        _lexer = lexer;
    }

    public void Dispose()
    {
        _lexer.Dispose();
    }

    public Result LexSyntax()
    {
        var startingDirectiveStack = _lexer.Directives;
        var startingPosition = _lexer.TextWindow.Position;
        var token = _lexer.Lex(InternalSyntax.LexerMode.Syntax);
        return new Result(new SyntaxToken(parent: null, token, startingPosition, index: 0), startingDirectiveStack);
    }

    public void SkipForwardTo(int position)
    {
        _lexer.TextWindow.Reset(position);
    }

    public void ResetTo(Result context)
    {
        _lexer.Reset(context.Token.Position, context.ContextStartDirectiveStack);
    }

    public readonly struct Result
    {
        public readonly SyntaxToken Token;
        public readonly SyntaxKind ContextualKind => Token.ContextualKind();
        internal readonly InternalSyntax.DirectiveStack ContextStartDirectiveStack;

        internal Result(SyntaxToken token, InternalSyntax.DirectiveStack contextStartDirectiveStack)
        {
            Token = token;
            ContextStartDirectiveStack = contextStartDirectiveStack;
        }
    }
}
