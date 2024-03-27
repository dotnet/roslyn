// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// A token parser that can be used to parse tokens continuously from a source. This parser parses continuously; every call to
/// <see cref="ParseNextToken"/> will return the next token in the source text, starting from position 0. <see cref="SkipForwardTo(int)"/>
/// can be used to skip forward in the file to a specific position, and <see cref="ResetTo(Result)"/> can be used to reset the parser
/// to a previously-lexed position.
/// </summary>
/// <remarks>
/// This type is safe to double dispose, but it is not safe to use after it has been disposed. Behavior in such scenarios
/// is undefined.
/// <para />
/// This type is not thread safe.
/// </remarks>
public sealed class SyntaxTokenParser : IDisposable
{
    private InternalSyntax.Lexer _lexer;

    internal SyntaxTokenParser(InternalSyntax.Lexer lexer)
    {
        _lexer = lexer;
    }

    public void Dispose()
    {
        var lexer = Interlocked.CompareExchange(ref _lexer!, null, _lexer);
        lexer?.Dispose();
    }

    /// <summary>
    /// Parse the next token from the input at the current position. This will advance the internal position of the token parser to the
    /// end of the returned token, including any trailing trivia.
    /// </summary>
    /// <remarks>
    /// The returned token will have a parent of <see langword="null"/>.
    /// <para />
    /// Since this API does not create a <see cref="SyntaxNode"/> that owns all produced tokens,
    /// the <see cref="SyntaxToken.GetLocation"/> API may yield surprising results for
    /// the produced tokens and its behavior is generally unspecified.
    /// </remarks>
    public Result ParseNextToken()
    {
        var startingDirectiveStack = _lexer.Directives;
        var startingPosition = _lexer.TextWindow.Position;
        var token = _lexer.Lex(InternalSyntax.LexerMode.Syntax);
        return new Result(new SyntaxToken(parent: null, token, startingPosition, index: 0), startingDirectiveStack);
    }

    /// <summary>
    /// Skip forward in the input to the specified position. Current directive state is preserved during the skip.
    /// </summary>
    /// <param name="position">The absolute location in the original text to move to.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the given position is less than the current position of the lexer.</exception>
    public void SkipForwardTo(int position)
    {
        if (position < _lexer.TextWindow.Position)
            throw new ArgumentOutOfRangeException(nameof(position));

        _lexer.TextWindow.Reset(position);
    }

    /// <summary>
    /// Resets the token parser to an earlier position in the input. The parser is reset to the start of the token that was previously
    /// parsed, before any leading trivia, with the directive state that existed at the start of the token.
    /// </summary>
    public void ResetTo(Result result)
    {
        _lexer.Reset(result.Token.Position, result.ContextStartDirectiveStack);
    }

    /// <summary>
    /// The result of a call to <see cref="ParseNextToken"/>. This is also a context object that can be used to reset the parser to
    /// before the token it represents was parsed.
    /// </summary>
    /// <remarks>
    /// This type is not default safe. Attempts to use <code>default(Result)</code> will result in undefined behavior.
    /// </remarks>
    public readonly struct Result
    {
        /// <summary>
        /// The token that was parsed.
        /// </summary>
        public readonly SyntaxToken Token { get; }

        /// <summary>
        /// If the parsed token is potentially a contextual keyword, this will return the contextual kind of the token. Otherwise, it
        /// will return <see cref="SyntaxKind.None"/>.
        /// </summary>
        public readonly SyntaxKind ContextualKind
        {
            get
            {
                var contextualKind = Token.ContextualKind();
                return contextualKind == Token.Kind() ? SyntaxKind.None : contextualKind;
            }
        }

        internal readonly InternalSyntax.DirectiveStack ContextStartDirectiveStack;

        internal Result(SyntaxToken token, InternalSyntax.DirectiveStack contextStartDirectiveStack)
        {
            Token = token;
            ContextStartDirectiveStack = contextStartDirectiveStack;
        }
    }
}
