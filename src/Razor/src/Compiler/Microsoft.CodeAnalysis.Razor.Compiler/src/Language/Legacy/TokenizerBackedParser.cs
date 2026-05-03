// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal abstract class TokenizerBackedParser<TTokenizer> : ParserBase, IDisposable
    where TTokenizer : Tokenizer
{
    protected delegate void SpanContextConfigAction(SpanEditHandlerBuilder? editHandlerBuilder, ref ISpanChunkGenerator? chunkGenerator);
    protected delegate void SpanContextConfigActionWithPreviousConfig(SpanEditHandlerBuilder? editHandlerBuilder, ref ISpanChunkGenerator? chunkGenerator, SpanContextConfigAction? previousConfig);

    private readonly SyntaxListPool _pool = new SyntaxListPool();
    protected readonly TokenizerView<TTokenizer> _tokenizer;
    private SyntaxListBuilder<SyntaxToken>? _tokenBuilder;

    protected SpanEditHandlerBuilder? editHandlerBuilder;
    protected ISpanChunkGenerator? chunkGenerator;

    // Following four high traffic methods cached as using method groups would cause allocation on every invocation.
    protected static readonly Func<SyntaxToken, bool> IsSpacingToken = (token) =>
    {
        return token.Kind == SyntaxKind.Whitespace;
    };

    protected static readonly Func<SyntaxToken, bool> IsSpacingTokenIncludingNewLines = (token) =>
    {
        return IsSpacingToken(token) || token.Kind == SyntaxKind.NewLine;
    };

    protected static readonly Func<SyntaxToken, bool> IsSpacingTokenIncludingComments = (token) =>
    {
        return IsSpacingToken(token) || token.Kind == SyntaxKind.CSharpComment;
    };

    protected static readonly Func<SyntaxToken, bool> IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives = (token) =>
    {
        return IsSpacingTokenIncludingNewLines(token) || token.Kind is SyntaxKind.CSharpComment or SyntaxKind.CSharpDirective or SyntaxKind.CSharpDisabledText;
    };

    protected TokenizerBackedParser(LanguageCharacteristics<TTokenizer> language, ParserContext context)
        : base(context)
    {
        Language = language;
        LanguageTokenizeString = Language.TokenizeString;

        var languageTokenizer = Language.CreateTokenizer(Context.Source);
        _tokenizer = new TokenizerView<TTokenizer>(languageTokenizer);
        editHandlerBuilder = context.Options.EnableSpanEditHandlers ? new SpanEditHandlerBuilder(LanguageTokenizeString) : null;
    }

    protected SyntaxListPool Pool => _pool;

    protected SyntaxListBuilder<SyntaxToken> TokenBuilder
    {
        get
        {
            if (_tokenBuilder == null)
            {
                var result = Pool.Allocate<SyntaxToken>();
                _tokenBuilder = result.Builder;
            }

            return _tokenBuilder.Value;
        }
    }

    protected SpanContextConfigAction? SpanContextConfig { get; set; }

    protected SyntaxToken CurrentToken
    {
        get { return _tokenizer.Current; }
    }

    protected SyntaxToken? PreviousToken { get; private set; }

    protected SourceLocation CurrentStart => _tokenizer.Tokenizer.CurrentStart;

    protected bool EndOfFile
    {
        get { return _tokenizer.EndOfFile; }
    }

    protected LanguageCharacteristics<TTokenizer> Language { get; }
    protected Func<string, IEnumerable<SyntaxToken>> LanguageTokenizeString { get; }

    protected SyntaxToken Lookahead(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        else if (count == 0)
        {
            return CurrentToken;
        }

        // We add 1 in order to store the current token.
        using var tokens = new PooledArrayBuilder<SyntaxToken>(count + 1);

        var currentToken = CurrentToken;

        tokens.Add(currentToken);

        // We need to look forward "count" many times.
        for (var i = 1; i <= count; i++)
        {
            NextToken();
            tokens.Add(CurrentToken);
        }

        // Restore Tokenizer's location to where it was pointing before the look-ahead.
        for (var i = count; i >= 0; i--)
        {
            PutBack(tokens[i]);
        }

        // The PutBacks above will set CurrentToken to null. EnsureCurrent will set our CurrentToken to the
        // next token.
        EnsureCurrent();

        return tokens[count];
    }

    internal delegate bool LookaheadUntilCondition(SyntaxToken token, ref readonly PooledArrayBuilder<SyntaxToken> previousTokens);

    /// <summary>
    /// Looks forward until the specified condition is met.
    /// </summary>
    /// <param name="condition">A predicate accepting the token being evaluated and the list of tokens which have been looped through.</param>
    /// <returns>true, if the condition was met. false - if the condition wasn't met and the last token has already been processed.</returns>
    /// <remarks>The list of previous tokens is passed in the reverse order. So the last processed element will be the first one in the list.</remarks>
    protected bool LookaheadUntil(LookaheadUntilCondition condition)
    {
        var matchFound = false;

        using var tokens = new PooledArrayBuilder<SyntaxToken>();
        tokens.Add(CurrentToken);

        while (true)
        {
            if (!NextToken())
            {
                break;
            }

            tokens.Add(CurrentToken);
            if (condition(CurrentToken, in tokens))
            {
                matchFound = true;
                break;
            }
        }

        // Restore Tokenizer's location to where it was pointing before the look-ahead.
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            PutBack(tokens[i]);
        }

        // The PutBacks above will set CurrentToken to null. EnsureCurrent will set our CurrentToken to the
        // next token.
        EnsureCurrent();

        return matchFound;
    }

    protected internal bool NextToken()
    {
        PreviousToken = CurrentToken;
        return _tokenizer.Next();
    }

    // Helpers
    [Conditional("DEBUG")]
    internal void Assert(SyntaxKind expectedType)
    {
        Debug.Assert(!EndOfFile && CurrentToken.Kind == expectedType);
    }

    protected internal void PutBack(SyntaxToken? token)
    {
        if (token != null)
        {
            _tokenizer.PutBack(token);
        }
    }

    /// <summary>
    /// Put the specified tokens back in the input stream. The provided list MUST be in the ORDER THE TOKENS WERE READ. The
    /// list WILL be reversed and the Putback(SyntaxToken) will be called on each item.
    /// </summary>
    /// <remarks>
    /// If a document contains tokens: a, b, c, d, e, f
    /// and AcceptWhile or AcceptUntil is used to collect until d
    /// the list returned by AcceptWhile/Until will contain: a, b, c IN THAT ORDER
    /// that is the correct format for providing to this method. The caller of this method would,
    /// in that case, want to put c, b and a back into the stream, so "a, b, c" is the CORRECT order
    /// </remarks>
    protected void PutBack(ref readonly PooledArrayBuilder<SyntaxToken> tokens)
    {
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            PutBack(tokens[i]);
        }
    }

    protected internal void PutCurrentBack()
    {
        if (!EndOfFile && CurrentToken != null)
        {
            PutBack(CurrentToken);
        }
    }

    protected internal bool NextIs(SyntaxKind type)
    {
        // Duplicated logic with NextIs(Func...) to prevent allocation
        var cur = CurrentToken;
        var result = false;
        if (NextToken())
        {
            result = (type == CurrentToken.Kind);
            PutCurrentBack();
        }

        PutBack(cur);
        EnsureCurrent();

        return result;
    }

    protected internal bool NextIs(params SyntaxKind[] types)
    {
        return NextIs(token => token != null && types.Any(t => t == token.Kind));
    }

    protected internal bool NextIs(Func<SyntaxToken, bool> condition)
    {
        var cur = CurrentToken;
        var result = false;
        if (NextToken())
        {
            result = condition(CurrentToken);
            PutCurrentBack();
        }

        PutBack(cur);
        EnsureCurrent();

        return result;
    }

    protected internal bool Was(SyntaxKind type)
    {
        return PreviousToken != null && PreviousToken.Kind == type;
    }

    protected internal bool At(SyntaxKind type)
    {
        return !EndOfFile && CurrentToken != null && CurrentToken.Kind == type;
    }

    protected bool TokenExistsAfterWhitespace(SyntaxKind kind, bool includeNewLines = true)
    {
        var tokenFound = false;
        using var whitespace = new PooledArrayBuilder<SyntaxToken>();
        ReadWhile(
            static (token, includeNewLines) =>
                token.Kind == SyntaxKind.Whitespace || (includeNewLines && token.Kind == SyntaxKind.NewLine),
            includeNewLines,
            ref whitespace.AsRef());
        tokenFound = At(kind);

        PutCurrentBack();
        PutBack(in whitespace);
        EnsureCurrent();

        return tokenFound;
    }

    protected bool EnsureCurrent()
    {
        if (CurrentToken == null)
        {
            return NextToken();
        }

        return true;
    }

    protected void ReadWhile<TArg>(
        Func<SyntaxToken, TArg, bool> predicate,
        TArg arg,
        ref PooledArrayBuilder<SyntaxToken> result,
        bool expectsEmptyBuilder = true)
    {
        Debug.Assert(!expectsEmptyBuilder || result.Count == 0, "Expected empty builder.");

        if (!EnsureCurrent() || !predicate(CurrentToken, arg))
        {
            return;
        }

        do
        {
            result.Add(CurrentToken);
            NextToken();
        }
        while (EnsureCurrent() && predicate(CurrentToken, arg));
    }

    protected void ReadWhile(
        Func<SyntaxToken, bool> predicate,
        ref PooledArrayBuilder<SyntaxToken> result)
    {
        Debug.Assert(result.Count == 0, "Expected empty builder.");

        if (!EnsureCurrent() || !predicate(CurrentToken))
        {
            return;
        }

        do
        {
            result.Add(CurrentToken);
            NextToken();
        }
        while (EnsureCurrent() && predicate(CurrentToken));
    }

    protected void SkipWhile(Func<SyntaxToken, bool> predicate)
    {
        if (!EnsureCurrent() || !predicate(CurrentToken))
        {
            return;
        }

        do
        {
            NextToken();
        }
        while (EnsureCurrent() && predicate(CurrentToken));
    }

    protected bool AtIdentifier(bool allowKeywords)
    {
        return CurrentToken != null &&
               (Language.IsIdentifier(CurrentToken) ||
                (allowKeywords && Language.IsKeyword(CurrentToken)));
    }

    protected RazorCommentBlockSyntax ParseRazorComment()
    {
        if (!Language.KnowsTokenType(KnownTokenType.CommentStart) ||
            !Language.KnowsTokenType(KnownTokenType.CommentStar) ||
            !Language.KnowsTokenType(KnownTokenType.CommentBody))
        {
            throw new InvalidOperationException(Resources.Language_Does_Not_Support_RazorComment);
        }

        RazorCommentBlockSyntax commentBlock;
        using (PushSpanContextConfig(CommentSpanContextConfig))
        {
            EnsureCurrent();
            var start = CurrentStart;
            Debug.Assert(At(SyntaxKind.RazorCommentTransition));
            var startTransition = EatExpectedToken(SyntaxKind.RazorCommentTransition);
            var startStar = EatExpectedToken(SyntaxKind.RazorCommentStar);
            var comment = GetOptionalToken(SyntaxKind.RazorCommentLiteral);
            if (comment == null)
            {
                comment = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentLiteral);
            }
            var endStar = GetOptionalToken(SyntaxKind.RazorCommentStar);
            if (endStar == null)
            {
                var diagnostic = RazorDiagnosticFactory.CreateParsing_RazorCommentNotTerminated(
                    new SourceSpan(start, contentLength: 2 /* @* */));
                endStar = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentStar, diagnostic);
                Context.ErrorSink.OnError(diagnostic);
            }
            var endTransition = GetOptionalToken(SyntaxKind.RazorCommentTransition);
            if (endTransition == null)
            {
                if (!endStar.IsMissing)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateParsing_RazorCommentNotTerminated(
                        new SourceSpan(start, contentLength: 2 /* @* */));
                    Context.ErrorSink.OnError(diagnostic);
                    endTransition = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentTransition, diagnostic);
                }

                endTransition = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentTransition);
            }

            commentBlock = SyntaxFactory.RazorCommentBlock(startTransition, startStar, comment, endStar, endTransition);

            // Make sure we generate a marker symbol after a comment if necessary.
            if (!comment.IsMissing || !endStar.IsMissing || !endTransition.IsMissing)
            {
                Context.MakeMarkerNode = true;
            }
        }

        InitializeContext();

        return commentBlock;
    }

    private void CommentSpanContextConfig(SpanEditHandlerBuilder? editHandler, ref ISpanChunkGenerator? generator)
    {
        generator = SpanChunkGenerator.Null;
        SetAcceptedCharacters(AcceptedCharactersInternal.Any);
        if (editHandlerBuilder == null)
        {
            return;
        }

        editHandlerBuilder.Reset();
        editHandlerBuilder.Tokenizer = LanguageTokenizeString;
    }

    protected SyntaxToken EatCurrentToken()
    {
        Debug.Assert(!EndOfFile && CurrentToken != null);
        var token = CurrentToken;
        NextToken();
        return token!;
    }

    protected SyntaxToken EatExpectedToken(SyntaxKind kind)
    {
        Debug.Assert(!EndOfFile && CurrentToken != null && kind == CurrentToken.Kind);
        var token = CurrentToken;
        NextToken();
        return token!;
    }

    protected SyntaxToken? GetOptionalToken(SyntaxKind kind)
    {
        if (At(kind))
        {
            var token = CurrentToken;
            NextToken();
            return token;
        }

        return null;
    }

    protected internal void AcceptWhile(SyntaxKind type)
    {
        AcceptWhile(static (token, type) => type == token.Kind, type);
    }

    // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
    protected internal void AcceptWhile(SyntaxKind type1, SyntaxKind type2)
    {
        AcceptWhile(static (token, arg) => arg.type1 == token.Kind || arg.type2 == token.Kind, (type1, type2));
    }

    protected internal void AcceptWhile(SyntaxKind type1, SyntaxKind type2, SyntaxKind type3)
    {
        AcceptWhile(static (token, arg) => arg.type1 == token.Kind || arg.type2 == token.Kind || arg.type3 == token.Kind, (type1, type2, type3));
    }

    protected internal void AcceptWhile(params ImmutableArray<SyntaxKind> types)
    {
        AcceptWhile(static (token, types) => types.Any(token.Kind, static (expected, kind) => expected == kind), types);
    }

    protected internal void AcceptUntil(SyntaxKind type)
    {
        AcceptWhile(static (token, type) => type != token.Kind, type);
    }

    // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
    protected internal void AcceptUntil(SyntaxKind type1, SyntaxKind type2)
    {
        AcceptWhile(static (token, arg) => arg.type1 != token.Kind && arg.type2 != token.Kind, (type1, type2));
    }

    protected internal void AcceptUntil(SyntaxKind type1, SyntaxKind type2, SyntaxKind type3)
    {
        AcceptWhile(static (token, arg) => arg.type1 != token.Kind && arg.type2 != token.Kind && arg.type3 != token.Kind, (type1, type2, type3));
    }

    protected internal void AcceptUntil(params ImmutableArray<SyntaxKind> types)
    {
        AcceptWhile(static (token, types) => types.All(token.Kind, static (expected, kind) => expected != kind), types);
    }

    protected void AcceptWhile(Func<SyntaxToken, bool> condition)
    {
        using var tokens = new PooledArrayBuilder<SyntaxToken>();
        ReadWhile(condition, ref tokens.AsRef());
        Accept(in tokens);
    }

    protected void AcceptWhile<TArg>(Func<SyntaxToken, TArg, bool> condition, TArg arg)
    {
        using var tokens = new PooledArrayBuilder<SyntaxToken>();
        ReadWhile(condition, arg, ref tokens.AsRef());
        Accept(in tokens);
    }

    protected void Accept(ref readonly PooledArrayBuilder<SyntaxToken> tokens)
    {
        foreach (var token in tokens)
        {
            Accept(token);
        }
    }

    protected internal void Accept(SyntaxToken? token)
    {
        if (token != null)
        {
            if (token.Kind == SyntaxKind.NewLine)
            {
                Context.StartOfLine = true;
            }
            else if (token.Kind != SyntaxKind.Whitespace)
            {
                Context.StartOfLine = false;
            }

            foreach (var error in token.GetDiagnostics())
            {
                Context.ErrorSink.OnError(error);
            }

            TokenBuilder.Add(token);
        }
    }

    protected internal bool AcceptAll(params SyntaxKind[] kinds)
    {
        foreach (var kind in kinds)
        {
            if (CurrentToken == null || CurrentToken.Kind != kind)
            {
                return false;
            }
            AcceptAndMoveNext();
        }
        return true;
    }

    protected internal bool AcceptAndMoveNext()
    {
        Accept(CurrentToken);
        return NextToken();
    }

    protected SyntaxList<SyntaxToken> Output()
    {
        var list = TokenBuilder.ToList();
        TokenBuilder.Clear();
        return list;
    }

    protected SyntaxToken? AcceptWhitespaceInLines()
    {
        SyntaxToken? lastWs = null;
        while (Language.IsWhitespace(CurrentToken) || Language.IsNewLine(CurrentToken))
        {
            // Capture the previous whitespace node
            if (lastWs != null)
            {
                Accept(lastWs);
            }

            if (Language.IsWhitespace(CurrentToken))
            {
                lastWs = CurrentToken;
            }
            else if (Language.IsNewLine(CurrentToken))
            {
                // Accept newline and reset last whitespace tracker
                Accept(CurrentToken);
                lastWs = null;
            }

            NextToken();
        }

        return lastWs;
    }

    protected internal bool TryAccept(SyntaxKind type)
    {
        if (At(type))
        {
            AcceptAndMoveNext();
            return true;
        }
        return false;
    }

    protected internal void AcceptMarkerTokenIfNecessary()
    {
        if (TokenBuilder.Count == 0 && Context.MakeMarkerNode)
        {
            Accept(Language.CreateMarkerToken());
        }
    }

    protected MarkupTextLiteralSyntax OutputAsMarkupLiteralRequired()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            throw new InvalidOperationException("No tokens to output.");
        }

        return SyntaxFactory.MarkupTextLiteral(tokens, chunkGenerator, GetEditHandler());
    }

    protected MarkupTextLiteralSyntax? OutputAsMarkupLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return SyntaxFactory.MarkupTextLiteral(tokens, chunkGenerator, GetEditHandler());
    }

    protected MarkupEphemeralTextLiteralSyntax? OutputAsMarkupEphemeralLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return SyntaxFactory.MarkupEphemeralTextLiteral(tokens, chunkGenerator, GetEditHandler());
    }

    protected RazorMetaCodeSyntax? OutputAsMetaCode(SyntaxList<SyntaxToken> tokens, AcceptedCharactersInternal? accepted = null)
    {
        if (tokens.Count == 0)
        {
            return null;
        }

        chunkGenerator = SpanChunkGenerator.Null;
        Context.CurrentAcceptedCharacters = accepted ?? AcceptedCharactersInternal.None;

        return SyntaxFactory.RazorMetaCode(tokens, SpanChunkGenerator.Null, GetEditHandler());
    }

    protected SpanEditHandler? GetEditHandler()
    {
        Context.MakeMarkerNode = Context.CurrentAcceptedCharacters != AcceptedCharactersInternal.Any;
        if (this.editHandlerBuilder == null)
        {
            InitializeContext();
            return null;
        }

        var editHandler = this.editHandlerBuilder.Build(Context.CurrentAcceptedCharacters);
        InitializeContext();

        return editHandler;
    }

    protected DisposableAction<(TokenizerBackedParser<TTokenizer>, SpanContextConfigAction?)> PushSpanContextConfig()
    {
        return PushSpanContextConfig(newConfig: (SpanContextConfigActionWithPreviousConfig?)null);
    }

    protected DisposableAction<(TokenizerBackedParser<TTokenizer>, SpanContextConfigAction?)> PushSpanContextConfig(SpanContextConfigAction newConfig)
    {
        return PushSpanContextConfig(newConfig == null ? null : (SpanEditHandlerBuilder? span, ref ISpanChunkGenerator? chunkGenerator, SpanContextConfigAction? _) => newConfig(span, ref chunkGenerator));
    }

    protected DisposableAction<(TokenizerBackedParser<TTokenizer>, SpanContextConfigAction?)> PushSpanContextConfig(SpanContextConfigActionWithPreviousConfig? newConfig)
    {
        var old = SpanContextConfig;
        ConfigureSpanContext(newConfig);
        return new DisposableAction<(TokenizerBackedParser<TTokenizer> Self, SpanContextConfigAction? Old)>(static arg => arg.Self.SpanContextConfig = arg.Old, arg: (this, old));
    }

    protected void ConfigureSpanContext(SpanContextConfigAction? config)
    {
        SpanContextConfig = config;
        InitializeContext();
    }

    protected void ConfigureSpanContext(SpanContextConfigActionWithPreviousConfig? config)
    {
        var prev = SpanContextConfig;
        SpanContextConfig = config == null
            ? null
            : GetNewSpanContextConfigAction(config, prev);
        InitializeContext();

        // Separated into it's own method to avoid closure allocations when not being called
        static SpanContextConfigAction GetNewSpanContextConfigAction(SpanContextConfigActionWithPreviousConfig config, SpanContextConfigAction? prev)
        {
            return (span, ref chunkGenerator) => config(span, ref chunkGenerator, prev);
        }
    }

    protected void InitializeContext()
    {
        SpanContextConfig?.Invoke(editHandlerBuilder, ref chunkGenerator);
    }

    protected void SetAcceptedCharacters(AcceptedCharactersInternal? acceptedCharacters)
    {
        Context.CurrentAcceptedCharacters = acceptedCharacters ?? AcceptedCharactersInternal.None;
    }

    internal void StartingBlock()
    {
        _tokenizer.Tokenizer.StartingBlock();
    }

    internal void EndingBlock()
    {
        _tokenizer.Tokenizer.EndingBlock();
    }

    public void Dispose()
    {
        _tokenizer.Dispose();

        if (_tokenBuilder != null)
        {
            Pool.Free(_tokenBuilder);
            _tokenBuilder = null;
        }
    }
}
