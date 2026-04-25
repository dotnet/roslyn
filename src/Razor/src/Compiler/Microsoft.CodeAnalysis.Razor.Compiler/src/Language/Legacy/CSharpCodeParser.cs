// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using static Microsoft.AspNetCore.Razor.Language.Syntax.GreenNodeExtensions;

using CSharpSyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class CSharpCodeParser : TokenizerBackedParser<CSharpTokenizer>
{
    private static readonly FrozenSet<char> InvalidNonWhitespaceNameCharacters = FrozenSet.Create(
        '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*');

    private static readonly Func<SyntaxToken, bool> IsValidStatementSpacingToken =
        IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives;

    internal static readonly DirectiveDescriptor AddTagHelperDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
        SyntaxConstants.CSharp.AddTagHelperKeyword,
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddStringToken(Resources.AddTagHelperDirective_StringToken_Name, Resources.AddTagHelperDirective_StringToken_Description);
            builder.Description = Resources.AddTagHelperDirective_Description;
        });

    internal static readonly DirectiveDescriptor UsingDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
        SyntaxConstants.CSharp.UsingKeyword,
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.Description = Resources.UsingDirective_Description;
        });

    internal static readonly DirectiveDescriptor RemoveTagHelperDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
        SyntaxConstants.CSharp.RemoveTagHelperKeyword,
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddStringToken(Resources.RemoveTagHelperDirective_StringToken_Name, Resources.RemoveTagHelperDirective_StringToken_Description);
            builder.Description = Resources.RemoveTagHelperDirective_Description;
        });

    internal static readonly DirectiveDescriptor TagHelperPrefixDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
        SyntaxConstants.CSharp.TagHelperPrefixKeyword,
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddStringToken(Resources.TagHelperPrefixDirective_PrefixToken_Name, Resources.TagHelperPrefixDirective_PrefixToken_Description);
            builder.Description = Resources.TagHelperPrefixDirective_Description;
        });

    private static readonly string[] s_defaultKeywords = [
        SyntaxConstants.CSharp.TagHelperPrefixKeyword,
        SyntaxConstants.CSharp.AddTagHelperKeyword,
        SyntaxConstants.CSharp.RemoveTagHelperKeyword,
        "if",
        "do",
        "try",
        "for",
        "foreach",
        "while",
        "switch",
        "lock",
        "using",
        "namespace",
        "class",
        "where"];

    private static readonly CSharpSyntaxKind[] s_conditionalBlockKeywordKinds = [
        CSharpSyntaxKind.ForKeyword,
        CSharpSyntaxKind.ForEachKeyword,
        CSharpSyntaxKind.WhileKeyword,
        CSharpSyntaxKind.SwitchKeyword,
        CSharpSyntaxKind.LockKeyword];

    private static readonly CSharpSyntaxKind[] s_caseStatementKeywordKinds = [
        CSharpSyntaxKind.CaseKeyword,
        CSharpSyntaxKind.DefaultKeyword];

    private static readonly CSharpSyntaxKind[] s_ifStatementKeywordKinds = [
        CSharpSyntaxKind.IfKeyword];

    private static readonly CSharpSyntaxKind[] s_tryStatementKeywordKinds = [
        CSharpSyntaxKind.TryKeyword];

    private static readonly CSharpSyntaxKind[] s_doStatementKeywordKinds = [
        CSharpSyntaxKind.DoKeyword];

    private static readonly CSharpSyntaxKind[] s_usingKeywordKinds = [
        CSharpSyntaxKind.UsingKeyword];

    private static readonly int s_initialKeywordCount =
        s_conditionalBlockKeywordKinds.Length +
        s_caseStatementKeywordKinds.Length +
        s_ifStatementKeywordKinds.Length +
        s_tryStatementKeywordKinds.Length +
        s_doStatementKeywordKinds.Length +
        s_usingKeywordKinds.Length;

    internal static KeywordSet DefaultKeywords { get; } = new(
        FrozenSet.Create(StringComparer.Ordinal, s_defaultKeywords));

    private readonly KeywordSet _currentKeywords;

    private readonly Dictionary<CSharpSyntaxKind, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax?>> _keywordParserMap;
    private readonly Dictionary<string, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax>> _directiveParserMap;

    public CSharpCodeParser(ParserContext context)
        : this(directives: [], context)
    {
    }

    public CSharpCodeParser(ImmutableArray<DirectiveDescriptor> directives, ParserContext context)
        : base(context.Options.ParseLeadingDirectives
            ? FirstDirectiveCSharpLanguageCharacteristics.Instance
            : context.Options.UseRoslynTokenizer
                ? new RoslynCSharpLanguageCharacteristics(context.Options.CSharpParseOptions)
                : NativeCSharpLanguageCharacteristics.Instance, context)
    {
        ArgHelper.ThrowIfNull(context);

        directives = directives.NullToEmpty();

#if NET
        // We know that we're going to add the keywords specified in SetupKeywordParsers()
        // along with each directive keyword and a handful more SetupDirectiveParsers().
        var keywordsSet = new HashSet<string>(capacity: s_initialKeywordCount + directives.Length + 5, StringComparer.Ordinal);

        // We'll be adding the default keywords and the directive keywords.
        // So, set the capacity accordingly and add the default keywords.
        var currentKeywordsSet = new HashSet<string>(capacity: s_defaultKeywords.Length + directives.Length, StringComparer.Ordinal);
        currentKeywordsSet.UnionWith(s_defaultKeywords);
#else
        // Unfortunately, HashSet doesn't have a constructor that takes capacity in netstandard2.0.
        var keywordsSet = new HashSet<string>(StringComparer.Ordinal);

        // Adding the default keywords in the constructor initializes the HashSet
        // with a capacity based on the length s_defaultKeywords.
        var currentKeywordsSet = new HashSet<string>(s_defaultKeywords, StringComparer.Ordinal);
#endif

        // This dictionary should have a capacity based on the keywords added in SetupKeywordParsers()
        // plus one more for SetupExpressionParsers().
        var keywordParserMap = new Dictionary<CSharpSyntaxKind, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax?>>(capacity: s_initialKeywordCount + 1);

        // This dictionary should have a capacity based on the directives potentially
        // added in SetupDirectiveParsers().
        var directiveParserMap = new Dictionary<string, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax>>(capacity: directives.Length + 5, StringComparer.Ordinal);

        SetupKeywordParsers();
        SetupExpressionParsers();
        SetupDirectiveParsers(directives);

        Keywords = new(keywordsSet);
        _currentKeywords = new(currentKeywordsSet);
        _keywordParserMap = keywordParserMap;
        _directiveParserMap = directiveParserMap;

        void SetupKeywordParsers()
        {
            MapKeywords(ParseConditionalBlock, topLevel: true, s_conditionalBlockKeywordKinds);
            MapKeywords(ParseCaseStatement, topLevel: false, s_caseStatementKeywordKinds);
            MapKeywords(ParseIfStatement, topLevel: true, s_ifStatementKeywordKinds);
            MapKeywords(ParseTryStatement, topLevel: true, s_tryStatementKeywordKinds);
            MapKeywords(ParseDoStatement, topLevel: true, s_doStatementKeywordKinds);
            MapKeywords(ParseUsingKeyword, topLevel: true, s_usingKeywordKinds);
        }

        void MapKeywords(
            Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax?> handler,
            bool topLevel,
            CSharpSyntaxKind[] keywords)
        {
            foreach (var keyword in keywords)
            {
                keywordParserMap.Add(keyword, handler);

                if (topLevel)
                {
                    keywordsSet.Add(CSharpSyntaxFacts.GetText(keyword));
                }
            }
        }

        void SetupExpressionParsers()
        {
            keywordParserMap.Add(CSharpSyntaxKind.AwaitKeyword, ParseAwaitExpression);
        }

        void SetupDirectiveParsers(ImmutableArray<DirectiveDescriptor> directiveDescriptors)
        {
            foreach (var directiveDescriptor in directiveDescriptors)
            {
                currentKeywordsSet.Add(directiveDescriptor.Directive);
                MapDirective((builder, transition) => ParseExtensibleDirective(builder, transition, directiveDescriptor), directiveParserMap, keywordsSet, context, directiveDescriptor.Directive);
            }

            MapDirective(ParseTagHelperPrefixDirective, directiveParserMap, keywordsSet, context, SyntaxConstants.CSharp.TagHelperPrefixKeyword);
            MapDirective(ParseAddTagHelperDirective, directiveParserMap, keywordsSet, context, SyntaxConstants.CSharp.AddTagHelperKeyword);
            MapDirective(ParseRemoveTagHelperDirective, directiveParserMap, keywordsSet, context, SyntaxConstants.CSharp.RemoveTagHelperKeyword);

            // If there wasn't any extensible directives relating to the reserved directives then map them.
            if (!directiveParserMap.ContainsKey("class"))
            {
                MapDirective(ParseReservedDirective, directiveParserMap, keywordsSet, context, "class");
            }

            if (!directiveParserMap.ContainsKey("namespace"))
            {
                MapDirective(ParseReservedDirective, directiveParserMap, keywordsSet, context, "namespace");
            }
        }

        static void MapDirective(
            Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax> handler,
            Dictionary<string, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax>> directiveParserMap,
            HashSet<string> keywords,
            ParserContext context,
            string directive)
        {
            if (directiveParserMap.ContainsKey(directive))
            {
                // It is possible for the list to contain duplicates in cases when the project is misconfigured.
                // In those cases, we shouldn't register multiple handlers per keyword.
                return;
            }

            directiveParserMap.Add(directive, (builder, transition) =>
            {
                handler(builder, transition);
                context.SeenDirectives.Add(directive);
            });

            keywords.Add(directive);
        }
    }

    private HtmlMarkupParser? _htmlParser;
    public HtmlMarkupParser HtmlParser
    {
        get
        {
            // Note: Circular reference with CSharpCodeParser means we can't set this in the constructor
            Debug.Assert(_htmlParser != null, "HtmlParser should have been set during initialization");
            return _htmlParser!;
        }
        set => _htmlParser = value;
    }

    protected internal KeywordSet Keywords { get; private set; }

    public bool IsNested { get; set; }

    public CSharpCodeBlockSyntax? ParseBlock()
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (Context == null)
        {
            throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
        }

        if (EndOfFile)
        {
            // Nothing to parse.
            return null;
        }

        StartingBlock();

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        using (PushSpanContextConfig(DefaultSpanContextConfig))
        {
            var builder = pooledResult.Builder;
            try
            {
                NextToken();

                using var precedingWhitespace = new PooledArrayBuilder<SyntaxToken>();
                ReadWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives, ref precedingWhitespace.AsRef());

                // We are usually called when the other parser sees a transition '@'. Look for it.
                SyntaxToken? transitionToken = null;
                if (At(SyntaxKind.StringLiteral) &&
                    CurrentToken.Content.Length > 0 &&
                    CurrentToken.Content[0] == SyntaxConstants.TransitionCharacter)
                {
                    var split = Language.SplitToken(CurrentToken, 1, SyntaxKind.Transition);
                    transitionToken = split.left;

                    // Back up to the end of the transition
                    _tokenizer.Reset(Context.Source.Position - split.right.Content.Length);
                    NextToken();
                }
                else if (At(SyntaxKind.Transition))
                {
                    transitionToken = EatCurrentToken();
                }

                if (transitionToken == null)
                {
                    transitionToken = SyntaxFactory.MissingToken(SyntaxKind.Transition);
                }

                chunkGenerator = SpanChunkGenerator.Null;
                SetAcceptedCharacters(AcceptedCharactersInternal.None);
                var transition = SyntaxFactory.CSharpTransition(transitionToken, chunkGenerator, GetEditHandler());

                if (At(SyntaxKind.LeftBrace))
                {
                    // This is a statement. We want to preserve preceding whitespace in the output.
                    Accept(in precedingWhitespace);
                    builder.Add(OutputTokensAsStatementLiteral());

                    var statementBody = ParseStatementBody();
                    var statement = SyntaxFactory.CSharpStatement(transition, statementBody);
                    builder.Add(statement);
                }
                else if (At(SyntaxKind.LeftParenthesis))
                {
                    // This is an explicit expression. We want to preserve preceding whitespace in the output.
                    Accept(in precedingWhitespace);
                    builder.Add(OutputTokensAsStatementLiteral());

                    var expressionBody = ParseExplicitExpressionBody();
                    var expression = SyntaxFactory.CSharpExplicitExpression(transition, expressionBody);
                    builder.Add(expression);
                }
                else if (At(SyntaxKind.Identifier))
                {
                    if (!TryParseDirective(builder, in precedingWhitespace, transition, CurrentToken.Content))
                    {
                        // Not a directive.
                        // This is an implicit expression. We want to preserve preceding whitespace in the output.
                        Accept(in precedingWhitespace);
                        builder.Add(OutputTokensAsStatementLiteral());

                        if (string.Equals(
                            CurrentToken.Content,
                            SyntaxConstants.CSharp.HelperKeyword,
                            StringComparison.Ordinal))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateParsing_HelperDirectiveNotAvailable(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length));
                            CurrentToken.SetDiagnostics([diagnostic]);
                            Context.ErrorSink.OnError(diagnostic);
                        }

                        var implicitExpressionBody = ParseImplicitExpressionBody();
                        var implicitExpression = SyntaxFactory.CSharpImplicitExpression(transition, implicitExpressionBody);
                        builder.Add(implicitExpression);
                    }
                }
                else if (At(SyntaxKind.Keyword))
                {
                    if (!TryParseDirective(builder, in precedingWhitespace, transition, CurrentToken.Content) &&
                        !TryParseKeyword(builder, in precedingWhitespace, transition))
                    {
                        // Not a directive or keyword.
                        // This is an implicit expression. We want to preserve preceding whitespace in the output.
                        Accept(in precedingWhitespace);
                        builder.Add(OutputTokensAsStatementLiteral());

                        // Not a directive or a special keyword. Just parse as an implicit expression.
                        var implicitExpressionBody = ParseImplicitExpressionBody();
                        var implicitExpression = SyntaxFactory.CSharpImplicitExpression(transition, implicitExpressionBody);
                        builder.Add(implicitExpression);
                    }

                    builder.Add(OutputTokensAsStatementLiteral());
                }
                else
                {
                    // Invalid character after transition.
                    // Preserve the preceding whitespace in the output
                    Accept(in precedingWhitespace);
                    builder.Add(OutputTokensAsStatementLiteral());

                    chunkGenerator = new ExpressionChunkGenerator();
                    SetAcceptedCharacters(AcceptedCharactersInternal.NonWhitespace);
                    if (editHandlerBuilder != null)
                    {
                        ImplicitExpressionEditHandler.SetupBuilder(editHandlerBuilder,
                            tokenizer: LanguageTokenizeString,
                            acceptTrailingDot: IsNested,
                            keywords: _currentKeywords);
                    }

                    // In this error case, we always want to accept a marker token. This allows intellisense to know
                    // that we're still in a CSharp context and offer the correct set of completions to the user.
                    Accept(Language.CreateMarkerToken());

                    var expressionLiteral = SyntaxFactory.CSharpCodeBlock(OutputTokensAsExpressionLiteral());
                    var expressionBody = SyntaxFactory.CSharpImplicitExpressionBody(expressionLiteral);
                    var expressionBlock = SyntaxFactory.CSharpImplicitExpression(transition, expressionBody);
                    builder.Add(expressionBlock);

                    if (At(SyntaxKind.Whitespace) || At(SyntaxKind.NewLine))
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length)));
                    }
                    else if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndOfFileAtStartOfCodeBlock(
                                new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));
                    }
                    else
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length),
                                CurrentToken.Content));
                    }
                }

                Debug.Assert(TokenBuilder.Count == 0, "We should not have any tokens left.");

                var codeBlock = SyntaxFactory.CSharpCodeBlock(builder.ToList());
                return codeBlock;
            }
            finally
            {
                // Always put current character back in the buffer for the next parser.
                PutCurrentBack();
            }
        }
    }

    private CSharpExplicitExpressionBodySyntax ParseExplicitExpressionBody()
    {
        var block = new Block(Resources.BlockName_ExplicitExpression, CurrentStart);
        Assert(SyntaxKind.LeftParenthesis);
        var leftParenToken = EatCurrentToken();
        var leftParen = OutputAsMetaCode(leftParenToken);

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var expressionBuilder = pooledResult.Builder;
            using (PushSpanContextConfig(ExplicitExpressionSpanContextConfig))
            {
                var success = Balance(
                    expressionBuilder,
                    BalancingModes.BacktrackOnFailure |
                        BalancingModes.NoErrorOnFailure |
                        BalancingModes.AllowCommentsAndTemplates,
                    SyntaxKind.LeftParenthesis,
                    SyntaxKind.RightParenthesis,
                    block.Start);

                if (!success)
                {
                    AcceptUntil(SyntaxKind.LessThan);
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                            new SourceSpan(block.Start, contentLength: 1 /* ( */), block.Name, ")", "("));
                }

                // If necessary, put an empty-content marker token here
                AcceptMarkerTokenIfNecessary();
                expressionBuilder.Add(OutputTokensAsExpressionLiteral());
            }

            var expressionBlock = SyntaxFactory.CSharpCodeBlock(expressionBuilder.ToList());

            RazorMetaCodeSyntax? rightParen = null;
            if (At(SyntaxKind.RightParenthesis))
            {
                rightParen = OutputAsMetaCode(EatCurrentToken());
            }
            else
            {
                var missingToken = SyntaxFactory.MissingToken(SyntaxKind.RightParenthesis);
                rightParen = OutputAsMetaCode(missingToken, Context.CurrentAcceptedCharacters);
            }
            if (!EndOfFile)
            {
                PutCurrentBack();
            }

            return SyntaxFactory.CSharpExplicitExpressionBody(leftParen, expressionBlock, rightParen);
        }
    }

    private CSharpImplicitExpressionBodySyntax ParseImplicitExpressionBody(bool async = false)
    {
        var accepted = AcceptedCharactersInternal.NonWhitespace;
        if (async)
        {
            // Async implicit expressions include the "await" keyword and therefore need to allow spaces to
            // separate the "await" and the following code.
            accepted = AcceptedCharactersInternal.AnyExceptNewline;
        }

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var expressionBuilder = pooledResult.Builder;
            ParseImplicitExpression(expressionBuilder, accepted);
            var codeBlock = SyntaxFactory.CSharpCodeBlock(expressionBuilder.ToList());
            return SyntaxFactory.CSharpImplicitExpressionBody(codeBlock);
        }
    }

    private void ParseImplicitExpression(in SyntaxListBuilder<RazorSyntaxNode> builder, AcceptedCharactersInternal acceptedCharacters)
    {
        using (PushSpanContextConfig((SpanEditHandlerBuilder? editHandlerBuilder, ref ISpanChunkGenerator? generator) =>
        {
            generator = new ExpressionChunkGenerator();
            SetAcceptedCharacters(acceptedCharacters);
            if (editHandlerBuilder == null)
            {
                return;
            }

            ImplicitExpressionEditHandler.SetupBuilder(editHandlerBuilder,
                tokenizer: LanguageTokenizeString,
                acceptTrailingDot: IsNested,
                keywords: Keywords);
        }))
        {
            do
            {
                if (AtIdentifier(allowKeywords: true))
                {
                    AcceptAndMoveNext();
                }
            }
            while (ParseMethodCallOrArrayIndex(builder, acceptedCharacters));

            PutCurrentBack();
            builder.Add(OutputTokensAsExpressionLiteral());
        }
    }

    private bool ParseMethodCallOrArrayIndex(in SyntaxListBuilder<RazorSyntaxNode> builder, AcceptedCharactersInternal acceptedCharacters)
    {
        if (!EndOfFile)
        {
            if (CurrentToken.Kind == SyntaxKind.LeftParenthesis ||
                CurrentToken.Kind == SyntaxKind.LeftBracket)
            {
                // If we end within "(", whitespace is fine
                SetAcceptedCharacters(AcceptedCharactersInternal.Any);

                SyntaxKind right;
                bool success;

                using (PushSpanContextConfig((SpanEditHandlerBuilder? editHandlerBuilder, ref ISpanChunkGenerator? generator, SpanContextConfigAction? prev) =>
                {
                    prev?.Invoke(editHandlerBuilder, ref generator);
                    SetAcceptedCharacters(AcceptedCharactersInternal.Any);
                }))
                {
                    right = Language.FlipBracket(CurrentToken.Kind);
                    success = Balance(builder, BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
                }

                if (!success)
                {
                    AcceptUntil(SyntaxKind.LessThan);
                }
                if (At(right))
                {
                    AcceptAndMoveNext();

                    // At the ending brace, restore the initial accepted characters.
                    SetAcceptedCharacters(acceptedCharacters);
                }
                return ParseMethodCallOrArrayIndex(builder, acceptedCharacters);
            }
            if (At(SyntaxKind.QuestionMark))
            {
                var next = Lookahead(count: 1);

                if (next != null)
                {
                    if (next.Kind == SyntaxKind.Dot)
                    {
                        // Accept null conditional dot operator (?.).
                        AcceptAndMoveNext();
                        AcceptAndMoveNext();

                        // If the next piece after the ?. is a keyword or identifier then we want to continue.
                        return At(SyntaxKind.Identifier) || At(SyntaxKind.Keyword);
                    }
                    else if (next.Kind == SyntaxKind.LeftBracket)
                    {
                        // We're at the ? for a null conditional bracket operator (?[).
                        AcceptAndMoveNext();

                        // Accept the [ and any content inside (it will attempt to balance).
                        return ParseMethodCallOrArrayIndex(builder, acceptedCharacters);
                    }
                }
            }
            else if (At(SyntaxKind.Not) && Context.Options.AllowNullableForgivenessOperator)
            {
                // C# 8.0 Null forgiveness Operator

                var next = Lookahead(count: 1);
                if (next == null)
                {
                    // Null forgiveness operator at the end of the file, don't include it in the expression.
                    // We don't allow trailing null forgiveness operators to avoid breaking scenarios such as:
                    //
                    // <p>Hello @Person! Good day!</p>
                    return false;
                }

                if (next.Kind == SyntaxKind.Dot)
                {
                    var nextNext = Lookahead(count: 2);
                    if (nextNext == null)
                    {
                        // End of file after the dot (!.EOF)
                        return false;
                    }

                    if (nextNext.Kind == SyntaxKind.Identifier || nextNext.Kind == SyntaxKind.Keyword)
                    {
                        // Accept null forgiveness operator followed by a dot (!.)
                        AcceptAndMoveNext();

                        // Accept the dot
                        AcceptAndMoveNext();
                        return true;
                    }

                    // We're in an odd situation where the user is attempting to use a null-forgiven implicit expression at the
                    // end of a sentence, i.e.
                    //
                    // <p>@Person!.</p>
                    //
                    // We don't allow trailing null forgiveness operators so don't include it in the implicit expression.
                    return false;
                }
                else if (next.Kind == SyntaxKind.QuestionMark)
                {
                    // We're at the ! for a null forgiveness + null conditional operator (!?).
                    AcceptAndMoveNext();

                    return true;
                }
                else if (next.Kind == SyntaxKind.LeftBracket || next.Kind == SyntaxKind.LeftParenthesis)
                {
                    // We're at the ! for a null forgiveness bracket or parenthesis operator (![).
                    AcceptAndMoveNext();

                    // Accept the [ or ( and any content inside (it will attempt to balance).
                    return ParseMethodCallOrArrayIndex(builder, acceptedCharacters);
                }

                return false;
            }
            else if (At(SyntaxKind.Dot))
            {
                var dot = CurrentToken;
                if (NextToken())
                {
                    if (At(SyntaxKind.Identifier) || At(SyntaxKind.Keyword))
                    {
                        // Accept the dot and return to the start
                        Accept(dot);
                        return true; // continue
                    }
                    else
                    {
                        // Put the token back
                        PutCurrentBack();
                    }
                }
                if (!IsNested)
                {
                    // Put the "." back
                    PutBack(dot);
                }
                else
                {
                    Accept(dot);
                }
            }
            else if (!At(SyntaxKind.Whitespace) && !At(SyntaxKind.NewLine))
            {
                PutCurrentBack();
            }
        }

        // Implicit Expression is complete
        return false;
    }

    private CSharpStatementBodySyntax ParseStatementBody(Block? block = null)
    {
        Assert(SyntaxKind.LeftBrace);
        block = block ?? new Block(Resources.BlockName_Code, CurrentStart);
        var leftBrace = OutputAsMetaCode(EatExpectedToken(SyntaxKind.LeftBrace));
        CSharpCodeBlockSyntax? codeBlock = null;
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var builder = pooledResult.Builder;
            // Set up auto-complete and parse the code block
            AutoCompleteEditHandler.AutoCompleteStringAccessor? acceptCloseBraceAccessor = null;
            if (editHandlerBuilder != null)
            {
                AutoCompleteEditHandler.SetupBuilder(editHandlerBuilder, LanguageTokenizeString, autoCompleteAtEndOfSpan: false, out acceptCloseBraceAccessor);
            }
            ParseCodeBlock(builder, block);

            if (EndOfFile)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                        new SourceSpan(block.Start, contentLength: 1 /* { OR } */), block.Name, "}", "{"));
            }

            EnsureCurrent();
            chunkGenerator = StatementChunkGenerator.Instance;
            AcceptMarkerTokenIfNecessary();
            if (acceptCloseBraceAccessor != null)
            {
                acceptCloseBraceAccessor.CanAcceptCloseBrace = !At(SyntaxKind.RightBrace);
            }
            builder.Add(OutputTokensAsStatementLiteral());

            codeBlock = SyntaxFactory.CSharpCodeBlock(builder.ToList());
        }

        RazorMetaCodeSyntax? rightBrace;
        if (At(SyntaxKind.RightBrace))
        {
            rightBrace = OutputAsMetaCode(EatCurrentToken());
        }
        else
        {
            rightBrace = OutputAsMetaCode(
                SyntaxFactory.MissingToken(SyntaxKind.RightBrace),
                Context.CurrentAcceptedCharacters);
        }

        if (!IsNested)
        {
            EnsureCurrent();
            if (At(SyntaxKind.NewLine) ||
                (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.NewLine)))
            {
                Context.NullGenerateWhitespaceAndNewLine = true;
            }
        }

        return SyntaxFactory.CSharpStatementBody(leftBrace, codeBlock, rightBrace);
    }

    private void ParseCodeBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block)
    {
        EnsureCurrent();
        while (!EndOfFile && !At(SyntaxKind.RightBrace))
        {
            CancellationToken.ThrowIfCancellationRequested();

            // Parse a statement, then return here
            ParseStatement(builder, block: block, encounteredUnexpectedMarkupTransition: false);
            EnsureCurrent();
        }
    }

    private void ParseStatement(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block, bool encounteredUnexpectedMarkupTransition)
    {
        SetAcceptedCharacters(AcceptedCharactersInternal.Any);
        // Accept whitespace but always keep the last whitespace node so we can put it back if necessary
        using var tokens = new PooledArrayBuilder<SyntaxToken>();
        ReadWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives, ref tokens.AsRef());

#pragma warning disable RS0042 // Do not copy value https://github.com/dotnet/roslyn-analyzers/issues/7389
        var lastWhitespace = tokens is [.., { Kind: SyntaxKind.Whitespace } whitespace] ? whitespace : null;
#pragma warning restore RS0042 // Do not copy value

        if (lastWhitespace != null)
        {
            tokens.RemoveAt(^1);
        }

        Accept(in tokens);

        if (EndOfFile)
        {
            if (lastWhitespace != null)
            {
                Accept(lastWhitespace);
            }

            builder.Add(OutputTokensAsStatementLiteral());
            return;
        }

        var kind = CurrentToken.Kind;
        var location = CurrentStart;

        // Both cases @: and @:: are triggered as markup, second colon in second case will be triggered as a plain text
        var isSingleLineMarkup = kind == SyntaxKind.Transition &&
            (NextIs(SyntaxKind.Colon, SyntaxKind.DoubleColon));

        var isMarkup = isSingleLineMarkup ||
            kind == SyntaxKind.LessThan ||
            (kind == SyntaxKind.Transition && NextIs(SyntaxKind.LessThan));

        if (Context.DesignTimeMode || !isMarkup)
        {
            // CODE owns whitespace, MARKUP owns it ONLY in DesignTimeMode.
            if (lastWhitespace != null)
            {
                Accept(lastWhitespace);
            }
        }
        else
        {
            var nextToken = Lookahead(1);

            // MARKUP owns whitespace EXCEPT in DesignTimeMode.
            PutCurrentBack();

            // Put back the whitespace unless it precedes a '<text>' tag.
            if (nextToken != null &&
                !string.Equals(nextToken.Content, SyntaxConstants.TextTagName, StringComparison.Ordinal))
            {
                PutBack(lastWhitespace);
            }
            else
            {
                // If it precedes a '<text>' tag, it should be accepted as code.
                Accept(lastWhitespace);
            }
        }

        if (isMarkup)
        {
            if (kind == SyntaxKind.Transition && !isSingleLineMarkup)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart(
                        new SourceSpan(location, contentLength: 1 /* @ */)));
            }

            // Markup block
            builder.Add(OutputTokensAsStatementLiteral());
            if (Context.DesignTimeMode && CurrentToken != null &&
                (CurrentToken.Kind == SyntaxKind.LessThan || CurrentToken.Kind == SyntaxKind.Transition))
            {
                PutCurrentBack();
            }
            OtherParserBlock(builder);
        }
        else
        {
            // What kind of statement is this?
            switch (kind)
            {
                case SyntaxKind.RazorCommentTransition:
                    AcceptMarkerTokenIfNecessary();
                    builder.Add(OutputTokensAsStatementLiteral());
                    var comment = ParseRazorComment();
                    builder.Add(comment);
                    ParseStatement(builder, block, encounteredUnexpectedMarkupTransition);
                    break;
                case SyntaxKind.LeftBrace:
                    // Verbatim Block
                    AcceptAndMoveNext();
                    ParseCodeBlock(builder, block);

                    // ParseCodeBlock is responsible for parsing the insides of a code block (non-inclusive of braces).
                    // Therefore, there's one of two cases after parsing:
                    //  1. We've hit the End of File (incomplete parse block).
                    //  2. It's a complete parse block and we're at a right brace.

                    if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(block.Start, contentLength: 1 /* { OR } */), block.Name, "}", "{"));
                    }
                    else
                    {
                        Assert(SyntaxKind.RightBrace);
                        SetAcceptedCharacters(AcceptedCharactersInternal.None);
                        AcceptAndMoveNext();
                    }
                    break;
                case SyntaxKind.Keyword:
                    if (!TryParseKeyword(builder))
                    {
                        ParseStandardStatement(builder, encounteredUnexpectedMarkupTransition);
                    }
                    break;
                case SyntaxKind.Transition:
                    // Embedded Expression block
                    ParseEmbeddedExpression(builder, encounteredUnexpectedMarkupTransition);
                    break;
                case SyntaxKind.RightBrace:
                    // Possible end of Code Block, just run the continuation
                    break;
                case SyntaxKind.CSharpComment:
                    Accept(CurrentToken);
                    NextToken();
                    break;
                default:
                    // Other statement
                    ParseStandardStatement(builder, encounteredUnexpectedMarkupTransition);
                    break;
            }
        }
    }

    private void ParseEmbeddedExpression(in SyntaxListBuilder<RazorSyntaxNode> builder, bool encounteredUnexpectedMarkupTransition)
    {
        // First, verify the type of the block
        Assert(SyntaxKind.Transition);
        var transition = CurrentToken;
        NextToken();

        if (At(SyntaxKind.Transition))
        {
            // Escaped "@"
            builder.Add(OutputTokensAsStatementLiteral());

            // Output "@" as hidden span
            Accept(transition);
            chunkGenerator = SpanChunkGenerator.Null;
            builder.Add(OutputTokensAsEphemeralLiteral());

            Assert(SyntaxKind.Transition);
            AcceptAndMoveNext();
            ParseStandardStatement(builder, encounteredUnexpectedMarkupTransition);
        }
        else
        {
            // Throw errors as necessary, but continue parsing
            if (At(SyntaxKind.LeftBrace))
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_UnexpectedNestedCodeBlock(
                        new SourceSpan(CurrentStart, contentLength: 1 /* { */)));
            }

            // @( or @foo - Nested expression, parse a child block
            PutCurrentBack();
            PutBack(transition);

            // Before exiting, add a marker span if necessary
            AcceptMarkerTokenIfNecessary();
            builder.Add(OutputTokensAsStatementLiteral());

            var nestedBlock = ParseNestedBlock();
            builder.Add(nestedBlock);
        }
    }

    private RazorSyntaxNode? ParseNestedBlock()
    {
        var wasNested = IsNested;
        IsNested = true;

        RazorSyntaxNode? nestedBlock;
        using (PushSpanContextConfig())
        {
            nestedBlock = ParseBlock();
        }

        InitializeContext();
        IsNested = wasNested;
        NextToken();

        return nestedBlock;
    }

    private void ParseStandardStatement(in SyntaxListBuilder<RazorSyntaxNode> builder, bool encounteredUnexpectedMarkupTransition)
    {
        while (!EndOfFile)
        {
            var bookmark = CurrentStart.AbsoluteIndex;
            using var read = new PooledArrayBuilder<SyntaxToken>();
            ReadWhile(
                static token =>
                    token.Kind is not SyntaxKind.Semicolon and
                                  not SyntaxKind.RazorCommentTransition and
                                  not SyntaxKind.Transition and
                                  not SyntaxKind.LeftBrace and
                                  not SyntaxKind.LeftParenthesis and
                                  not SyntaxKind.LeftBracket and
                                  not SyntaxKind.RightBrace and
                                  not SyntaxKind.Keyword,
                ref read.AsRef());

            if ((!Context.Options.AllowRazorInAllCodeBlocks && At(SyntaxKind.LeftBrace)) ||
                At(SyntaxKind.LeftParenthesis) ||
                At(SyntaxKind.LeftBracket))
            {
                Accept(in read);
                if (!TryBalanceBlock(builder))
                {
                    return;
                }
            }
            else if (Context.Options.AllowRazorInAllCodeBlocks && At(SyntaxKind.LeftBrace))
            {
                Accept(in read);
                return;
            }
            else if (At(SyntaxKind.Transition))
            {
                // We're not at the start of a statement, as that would have been handled by ParseStatement proper.
                // So a transition can be one of two things:
                // 1. A transition to a template, indicated by either @< or @:
                // 2. A C# identifier.
                var nextToken = Lookahead(1);
                switch (nextToken.Kind)
                {
                    case SyntaxKind.LessThan:
                    case SyntaxKind.Colon:
                        Accept(in read);
                        builder.Add(OutputTokensAsStatementLiteral());
                        ParseTemplate(builder);
                        continue;

                    case SyntaxKind.Keyword when encounteredUnexpectedMarkupTransition:
                        // In this case, we were in an unexpected markup transition, such as:
                        //
                        // @if (condition) @<p>Markup</p>
                        // @if
                        //
                        // In such a case, the likelihood is that the user actually wants this to be interpreted as a new statement,
                        // not as an identifier. So we simply accept what we have and return to continue to main parsing loop.
                        Accept(in read);
                        return;

                    case SyntaxKind.Identifier:
                    case SyntaxKind.Keyword:
                        // We want to stitch together `@text`.
                        Accept(in read);
                        Accept(NextAsEscapedIdentifier());
                        continue;

                    // We special case @@identifier because the old compiler behavior was to simply accept it and treat it as if it was @identifier. While
                    // this isn't legal, the runtime compiler doesn't handle @identifier correctly. We'll continue to accept this for now, but will potentially
                    // break it in the future when we move to the roslyn lexer and the runtime/compiletime split is much greater.
                    case SyntaxKind.Transition:
                        if (Lookahead(2) is not { Kind: SyntaxKind.Identifier or SyntaxKind.Keyword })
                        {
                            goto default;
                        }

                        Accept(in read);
                        AcceptAndMoveNext();
                        Accept(NextAsEscapedIdentifier());
                        continue;

                    default:
                        // Accept a broken identifier `@` and mark an error
                        Accept(in read);

                        var transition = CurrentToken;

                        Debug.Assert(transition.Kind == SyntaxKind.Transition);

                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart(
                                new SourceSpan(CurrentStart, contentLength: 1 /* @ */)));

                        NextToken();
                        var finalIdentifier = SyntaxFactory.Token(SyntaxKind.Identifier, transition.Content);
                        Accept(finalIdentifier);
                        continue;
                }
            }
            else if (At(SyntaxKind.RazorCommentTransition))
            {
                Accept(in read);
                AcceptMarkerTokenIfNecessary();
                builder.Add(OutputTokensAsStatementLiteral());
                builder.Add(ParseRazorComment());
                continue;
            }
            else if (At(SyntaxKind.Semicolon))
            {
                Accept(in read);
                AcceptAndMoveNext();
                return;
            }
            else if (At(SyntaxKind.RightBrace))
            {
                Accept(in read);
                return;
            }
            else if (At(SyntaxKind.Keyword))
            {
                Accept(in read);
                if (CurrentToken.Content == "switch")
                {
                    AcceptUntil(SyntaxKind.LeftBrace); // TODO: how do we do error recovery at this point?
                    if (!TryBalanceBlock(builder))
                    {
                        return;
                    }
                }
                else
                {
                    // unknown keyword, continue parsing
                    AcceptAndMoveNext();
                }
            }
            else
            {
                _tokenizer.Reset(bookmark);
                NextToken();
                AcceptUntil(SyntaxKind.LessThan, SyntaxKind.LeftBrace, SyntaxKind.RightBrace);
                return;
            }
        }

        bool TryBalanceBlock(SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            if (Balance(builder, BalancingModes.AllowCommentsAndTemplates | BalancingModes.BacktrackOnFailure))
            {
                TryAccept(SyntaxKind.RightBrace);
            }
            else
            {
                // Recovery
                AcceptUntil(SyntaxKind.LessThan, SyntaxKind.RightBrace);
                return false;
            }

            return true;
        }
    }

    private void ParseTemplate(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (Context.InTemplateContext)
        {
            Context.ErrorSink.OnError(
                RazorDiagnosticFactory.CreateParsing_InlineMarkupBlocksCannotBeNested(
                    new SourceSpan(CurrentStart, contentLength: 1 /* @ */)));
        }
        if (chunkGenerator is ExpressionChunkGenerator)
        {
            builder.Add(OutputTokensAsExpressionLiteral());
        }
        else
        {
            builder.Add(OutputTokensAsStatementLiteral());
        }

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var templateBuilder = pooledResult.Builder;
            Context.InTemplateContext = true;
            PutCurrentBack();
            OtherParserBlock(templateBuilder);

            var template = SyntaxFactory.CSharpTemplateBlock(templateBuilder.ToList());
            builder.Add(template);

            Context.InTemplateContext = false;
        }
    }

    private bool TryParseDirective(
        in SyntaxListBuilder<RazorSyntaxNode> builder,
        ref readonly PooledArrayBuilder<SyntaxToken> whitespace,
        CSharpTransitionSyntax transition,
        string directive)
    {
        if (_directiveParserMap.TryGetValue(directive, out var handler))
        {
            // This is a directive. We don't want to generate the preceding whitespace in the output.
            Accept(in whitespace);
            builder.Add(OutputTokensAsEphemeralLiteral());

            chunkGenerator = SpanChunkGenerator.Null;
            handler(builder, transition);
            return true;
        }

        return false;
    }

    private void EnsureDirectiveIsAtStartOfLine()
    {
        // 1 is the offset of the @ transition for the directive.
        if (CurrentStart.CharacterIndex > 1)
        {
            var index = CurrentStart.AbsoluteIndex - 1;
            var lineStart = CurrentStart.AbsoluteIndex - CurrentStart.CharacterIndex;
            while (--index >= lineStart)
            {
                var @char = Context.SourceDocument.Text[index];

                if (!char.IsWhiteSpace(@char))
                {
                    var currentDirective = CurrentToken.Content;
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_DirectiveMustAppearAtStartOfLine(
                            new SourceSpan(CurrentStart, currentDirective.Length), currentDirective));
                    break;
                }
            }
        }
    }

    private void ParseTagHelperPrefixDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        RazorDiagnostic? duplicateDiagnostic = null;
        if (Context.SeenDirectives.Contains(SyntaxConstants.CSharp.TagHelperPrefixKeyword))
        {
            var directiveStart = CurrentStart;
            if (transition != null)
            {
                // Start the error from the Transition '@'.
                directiveStart = new SourceLocation(
                    directiveStart.FilePath,
                    directiveStart.AbsoluteIndex - 1,
                    directiveStart.LineIndex,
                    directiveStart.CharacterIndex - 1);
            }
            var errorLength = /* @ */ 1 + SyntaxConstants.CSharp.TagHelperPrefixKeyword.Length;
            duplicateDiagnostic = RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
                new SourceSpan(directiveStart, errorLength),
                SyntaxConstants.CSharp.TagHelperPrefixKeyword);
        }

        var directiveBody = ParseTagHelperDirective(
            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
            (prefix, errors, startLocation) =>
            {
                if (duplicateDiagnostic != null)
                {
                    errors.Add(duplicateDiagnostic);
                }

                var parsedDirective = ParseDirective(prefix, startLocation, TagHelperDirectiveType.TagHelperPrefix, errors);

                return new TagHelperPrefixDirectiveChunkGenerator(
                    prefix,
                    parsedDirective.DirectiveText,
                    errors);
            });

        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
    }

    private void ParseAddTagHelperDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        var directiveBody = ParseTagHelperDirective(
            SyntaxConstants.CSharp.AddTagHelperKeyword,
            (lookupText, errors, startLocation) =>
            {
                var parsedDirective = ParseDirective(lookupText, startLocation, TagHelperDirectiveType.AddTagHelper, errors);

                return new AddTagHelperChunkGenerator(
                    lookupText,
                    parsedDirective.DirectiveText,
                    parsedDirective.TypePattern,
                    parsedDirective.AssemblyName,
                    errors);
            });

        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
    }

    private void ParseRemoveTagHelperDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        var directiveBody = ParseTagHelperDirective(
            SyntaxConstants.CSharp.RemoveTagHelperKeyword,
            (lookupText, errors, startLocation) =>
            {
                var parsedDirective = ParseDirective(lookupText, startLocation, TagHelperDirectiveType.RemoveTagHelper, errors);

                return new RemoveTagHelperChunkGenerator(
                    lookupText,
                    parsedDirective.DirectiveText,
                    parsedDirective.TypePattern,
                    parsedDirective.AssemblyName,
                    errors);
            });

        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
    }

    [Conditional("DEBUG")]
    protected void AssertDirective(string directive)
    {
        Debug.Assert(CurrentToken.Kind == SyntaxKind.Identifier || CurrentToken.Kind == SyntaxKind.Keyword);
        Debug.Assert(string.Equals(CurrentToken.Content, directive, StringComparison.Ordinal));
    }

    private RazorDirectiveBodySyntax ParseTagHelperDirective(
        string keyword,
        Func<string, List<RazorDiagnostic>, SourceLocation, ISpanChunkGenerator> chunkGeneratorFactory)
    {
        AssertDirective(keyword);

        RazorMetaCodeSyntax? keywordBlock = null;
        using var pooledResult = Pool.Allocate<RazorSyntaxNode>();
        var directiveBuilder = pooledResult.Builder;

        using var directiveErrorSink = new ErrorSink();
        using (Context.PushNewErrorScope(directiveErrorSink))
        {
            string? directiveValue = null;
            SourceLocation? valueStartLocation = null;
            EnsureDirectiveIsAtStartOfLine();

            var keywordStartLocation = CurrentStart;

            // Accept the directive name
            var keywordToken = EatCurrentToken();
            var keywordLength = keywordToken.Width + 1 /* @ */;

            var foundWhitespace = At(SyntaxKind.Whitespace);

            // If we found whitespace then any content placed within the whitespace MAY cause a destructive change
            // to the document.  We can't accept it.
            var acceptedCharacters = foundWhitespace ? AcceptedCharactersInternal.None : AcceptedCharactersInternal.AnyExceptNewline;
            Accept(keywordToken);
            keywordBlock = OutputAsMetaCode(Output(), acceptedCharacters);

            AcceptWhile(SyntaxKind.Whitespace);
            chunkGenerator = SpanChunkGenerator.Null;
            SetAcceptedCharacters(acceptedCharacters);
            directiveBuilder.Add(OutputAsMarkupLiteral());

            if (EndOfFile || At(SyntaxKind.NewLine))
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_DirectiveMustHaveValue(
                        new SourceSpan(keywordStartLocation, keywordLength), keyword));

                directiveValue = string.Empty;
            }
            else
            {
                // Need to grab the current location before we accept until the end of the line.
                valueStartLocation = CurrentStart;

                // Parse to the end of the line. Essentially accepts anything until end of line, comments, invalid code
                // etc.
                AcceptUntil(SyntaxKind.NewLine);

                // Pull out the value and remove whitespaces and optional quotes
                var rawValue = string.Concat(TokenBuilder.ToList().Nodes.Select(s => s.Content)).Trim();

                var startsWithQuote = rawValue.StartsWith("\"", StringComparison.Ordinal);
                var endsWithQuote = rawValue.EndsWith("\"", StringComparison.Ordinal);
                if (startsWithQuote != endsWithQuote)
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_IncompleteQuotesAroundDirective(
                            new SourceSpan(valueStartLocation.Value, rawValue.Length), keyword));
                }

                directiveValue = rawValue;
            }

            chunkGenerator = chunkGeneratorFactory(
                directiveValue,
                [.. directiveErrorSink.GetErrorsAndClear()],
                valueStartLocation ?? CurrentStart);
        }

        // Finish the block and output the tokens
        CompleteBlock();
        SetAcceptedCharacters(AcceptedCharactersInternal.AnyExceptNewline);

        directiveBuilder.Add(OutputTokensAsStatementLiteral());
        var directiveCodeBlock = SyntaxFactory.CSharpCodeBlock(directiveBuilder.ToList());

        return SyntaxFactory.RazorDirectiveBody(keywordBlock, directiveCodeBlock);
    }

    private ParsedDirective ParseDirective(
        string directiveText,
        SourceLocation directiveLocation,
        TagHelperDirectiveType directiveType,
        List<RazorDiagnostic> errors)
    {
        var offset = 0;
        var directiveTextSpan = directiveText.AsSpanOrDefault();

        directiveTextSpan = directiveTextSpan.Trim();

        if (directiveTextSpan is ['"', .. var innerTextSpan, '"'])
        {
            directiveTextSpan = innerTextSpan;

            if (directiveTextSpan.IsEmpty)
            {
                offset = 1;
            }
        }

        // If this is the "string literal" form of a directive, we'll need to postprocess the location
        // and content.
        //
        // Ex: @addTagHelper "*, Microsoft.AspNetCore.CoolLibrary"
        //                    ^                                 ^
        //                  Start                              End
        if (TokenBuilder.Count == 1 &&
            TokenBuilder[0] is SyntaxToken { Kind: SyntaxKind.StringLiteral } token)
        {
            var contentSpan = token.Content.AsSpan();
            offset += contentSpan.IndexOf(directiveTextSpan, StringComparison.Ordinal);

            // This is safe because inside one of these directives all of the text needs to be on the
            // same line.
            var original = directiveLocation;
            directiveLocation = new SourceLocation(
                original.FilePath,
                original.AbsoluteIndex + offset,
                original.LineIndex,
                original.CharacterIndex + offset);
        }

        var parsedDirective = new ParsedDirective()
        {
            DirectiveText = directiveTextSpan.ToString()
        };

        if (directiveType == TagHelperDirectiveType.TagHelperPrefix)
        {
            ValidateTagHelperPrefix(parsedDirective.DirectiveText, directiveLocation, errors);

            return parsedDirective;
        }

        return ParseAddOrRemoveDirective(parsedDirective, directiveLocation, errors);
    }

    // Internal for testing.
    internal static ParsedDirective ParseAddOrRemoveDirective(ParsedDirective directive, SourceLocation directiveLocation, List<RazorDiagnostic> errors)
    {
        // Ensure that we have valid lookupStrings to work with. The valid format is "typeName, assemblyName"
        var text = directive.DirectiveText;
        if (!TrySplitDirectiveText(text.AsSpanOrDefault(), out var typeName, out var assemblyName))
        {
            errors.Add(
                RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
                    new SourceSpan(directiveLocation, Math.Max(text?.Length ?? 0, 1)), text ?? string.Empty));

            return directive;
        }

        directive.TypePattern = typeName.ToString();
        directive.AssemblyName = assemblyName.ToString();

        return directive;

        static bool TrySplitDirectiveText(
            ReadOnlySpan<char> directiveText,
            out ReadOnlySpan<char> typeName,
            out ReadOnlySpan<char> assemblyName)
        {
            // We expect the form "typeName, assemblyName".

            typeName = default;
            assemblyName = default;

            if (directiveText.IsEmpty || directiveText[0] == '\'' || directiveText[^1] == '\'')
            {
                return false;
            }

            var commaIndex = directiveText.IndexOf(',');
            if (commaIndex < 0)
            {
                return false;
            }

            typeName = directiveText[..commaIndex].Trim();
            assemblyName = directiveText[(commaIndex + 1)..].Trim();

            if (typeName.IsEmpty || assemblyName.IsEmpty || assemblyName.IndexOf(',') >= 0)
            {
                return false;
            }

            return true;
        }
    }

    // Internal for testing.
    internal static void ValidateTagHelperPrefix(
        string prefix,
        SourceLocation directiveLocation,
        List<RazorDiagnostic> diagnostics)
    {
        foreach (var character in prefix)
        {
            // Prefixes are correlated with tag names, tag names cannot have whitespace.
            if (char.IsWhiteSpace(character) || InvalidNonWhitespaceNameCharacters.Contains(character))
            {
                diagnostics.Add(
                    RazorDiagnosticFactory.CreateParsing_InvalidTagHelperPrefixValue(
                        new SourceSpan(directiveLocation, prefix.Length),
                        SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                        character,
                        prefix));

                return;
            }
        }
    }

    private void ParseExtensibleDirective(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition, DirectiveDescriptor descriptor)
    {
        AssertDirective(descriptor.Directive);

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var directiveBuilder = pooledResult.Builder;
            RazorMetaCodeSyntax? keywordBlock = null;
            bool shouldCaptureWhitespaceToEndOfLine = false;

            using var directiveErrorSink = new ErrorSink();
            using (Context.PushNewErrorScope(directiveErrorSink))
            {
                EnsureDirectiveIsAtStartOfLine();
                var directiveStart = CurrentStart;
                if (transition != null)
                {
                    // Start the error from the Transition '@'.
                    directiveStart = new SourceLocation(
                        directiveStart.FilePath,
                        directiveStart.AbsoluteIndex - 1,
                        directiveStart.LineIndex,
                        directiveStart.CharacterIndex - 1);
                }

                AcceptAndMoveNext();
                keywordBlock = OutputAsMetaCode(Output());

                // Even if an error was logged do not bail out early. If a directive was used incorrectly it doesn't mean it can't be parsed.
                ValidateDirectiveUsage(descriptor, directiveStart);

                // Capture the last member for validating generic type constraints.
                // Generic type parameters are described by a member token followed by a generic constraint token.
                // The generic constraint token includes the 'where' keyword, the identifier it applies and the constraint list and is represented as a token list.
                // For the directive to be valid we need to check that the identifier for the member token matches the identifier in the generic constraint token.
                // Once we are parsing the constraint token we have lost "easy" access to the identifier for the member. To avoid having complex logic in the generic
                // constraint token parsing code, we instead keep track of the last identifier we've seen on a member token and use that information to check the
                // identifier for the constraint an emit a diagnostic in case they are not the same.
                string? lastSeenMemberIdentifier = null;

                for (var i = 0; i < descriptor.Tokens.Count; i++)
                {
                    if (!At(SyntaxKind.Whitespace) &&
                        !At(SyntaxKind.NewLine) &&
                        !At(SyntaxKind.Semicolon) &&
                        !EndOfFile)
                    {
                        // This case should never happen in a real scenario. We're just being defensive.
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_DirectiveTokensMustBeSeparatedByWhitespace(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));

                        builder.Add(BuildDirective(SyntaxKind.Whitespace));
                        return;
                    }

                    var tokenDescriptor = descriptor.Tokens[i];

                    if (At(SyntaxKind.Whitespace))
                    {
                        AcceptWhile(IsSpacingTokenIncludingComments);

                        chunkGenerator = SpanChunkGenerator.Null;
                        SetAcceptedCharacters(AcceptedCharactersInternal.Whitespace);

                        if (tokenDescriptor.Kind == DirectiveTokenKind.Member ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Namespace ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Type ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Attribute ||
                            tokenDescriptor.Kind == DirectiveTokenKind.GenericTypeConstraint ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Boolean ||
                            tokenDescriptor.Kind == DirectiveTokenKind.IdentifierOrExpression)
                        {
                            directiveBuilder.Add(OutputTokensAsStatementLiteral());

                            if (EndOfFile || At(SyntaxKind.NewLine))
                            {
                                // Add a marker token to provide CSharp intellisense when we start typing the directive token.
                                // We want CSharp intellisense only if there is whitespace after the directive keyword.
                                AcceptMarkerTokenIfNecessary();
                                chunkGenerator = new DirectiveTokenChunkGenerator(tokenDescriptor);
                                SetAcceptedCharacters(AcceptedCharactersInternal.NonWhitespace);
                                if (editHandlerBuilder != null)
                                {
                                    DirectiveTokenEditHandler.SetupBuilder(editHandlerBuilder, LanguageTokenizeString);
                                }
                                directiveBuilder.Add(OutputTokensAsStatementLiteral());
                            }
                        }
                        else
                        {
                            directiveBuilder.Add(OutputAsMarkupEphemeralLiteral());
                        }
                    }

                    if (tokenDescriptor.Optional && (EndOfFile || At(SyntaxKind.NewLine)))
                    {
                        break;
                    }
                    else if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEOFAfterDirective(
                                new SourceSpan(CurrentStart, contentLength: 1),
                                descriptor.Directive,
                                tokenDescriptor.Kind.ToString().ToLowerInvariant()));
                        builder.Add(BuildDirective(SyntaxKind.Identifier));
                        return;
                    }

                    switch (tokenDescriptor.Kind)
                    {
                        case DirectiveTokenKind.Type:
                            if (!TryParseNamespaceOrTypeName(directiveBuilder))
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsTypeName(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));

                                builder.Add(BuildDirective(SyntaxKind.Identifier));
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Namespace:
                            if (!TryParseQualifiedIdentifier(out var identifierLength))
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsNamespace(
                                        new SourceSpan(CurrentStart, identifierLength), descriptor.Directive));

                                builder.Add(BuildDirective(SyntaxKind.Identifier));
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Member:
                            if (At(SyntaxKind.Identifier))
                            {
                                lastSeenMemberIdentifier = CurrentToken.Content;
                                AcceptAndMoveNext();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsIdentifier(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                builder.Add(BuildDirective(SyntaxKind.Identifier));
                                return;
                            }
                            break;

                        case DirectiveTokenKind.String:
                            if (At(SyntaxKind.StringLiteral) && !CurrentToken.ContainsDiagnostics)
                            {
                                AcceptAndMoveNext();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsQuotedStringLiteral(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                builder.Add(BuildDirective(SyntaxKind.StringLiteral));
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Boolean:
                            if (AtBooleanLiteral() && !CurrentToken.ContainsDiagnostics)
                            {
                                AcceptAndMoveNext();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsBooleanLiteral(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                builder.Add(BuildDirective(SyntaxKind.CSharpExpressionLiteral));
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Attribute:
                            if (At(SyntaxKind.LeftBracket))
                            {
                                if (Balance(directiveBuilder, BalancingModes.NoErrorOnFailure))
                                {
                                    TryAccept(SyntaxKind.RightBracket);
                                }
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsCSharpAttribute(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                builder.Add(BuildDirective(SyntaxKind.LeftBracket));
                                return;
                            }

                            break;
                        case DirectiveTokenKind.GenericTypeConstraint:
                            if (At(SyntaxKind.Keyword) &&
                                string.Equals(CurrentToken.Content, CSharpSyntaxFacts.GetText(CSharpSyntaxKind.WhereKeyword), StringComparison.Ordinal))
                            {
                                // Consume the 'where' keyword plus any aditional whitespace
                                AcceptAndMoveNext();
                                AcceptWhile(SyntaxKind.Whitespace);
                                // Check that the type name matches the type name before the where clause.
                                // Find a better way to do this
                                if (!string.Equals(CurrentToken.Content, lastSeenMemberIdentifier, StringComparison.Ordinal))
                                {
                                    // @typeparam TKey where TValue : ...
                                    // The type parameter in the generic type constraint 'TValue' does not match the type parameter 'TKey' defined in the directive '@typeparam'.
                                    Context.ErrorSink.OnError(
                                        RazorDiagnosticFactory.CreateParsing_GenericTypeParameterIdentifierMismatch(
                                            new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive, CurrentToken.Content, lastSeenMemberIdentifier ?? string.Empty));
                                    builder.Add(BuildDirective(SyntaxKind.Identifier));
                                    return;
                                }
                                else
                                {
                                    while (!At(SyntaxKind.NewLine))
                                    {
                                        if (At(SyntaxKind.Semicolon))
                                        {
                                            break;
                                        }

                                        AcceptAndMoveNext();
                                        if (EndOfFile)
                                        {
                                            // We've reached the end of the file, which is unusual but can happen, for example if we start typing in a new file.
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (At(SyntaxKind.Semicolon))
                            {
                                break;
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_UnexpectedIdentifier(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length),
                                        CurrentToken.Content,
                                        CSharpSyntaxFacts.GetText(CSharpSyntaxKind.WhereKeyword)));

                                builder.Add(BuildDirective(SyntaxKind.Keyword));
                                return;
                            }

                            break;

                        case DirectiveTokenKind.IdentifierOrExpression:
                            if (At(SyntaxKind.Transition) && NextIs(SyntaxKind.LeftParenthesis))
                            {
                                AcceptAndMoveNext();
                                directiveBuilder.Add(OutputAsMetaCode(Output()));

                                var expression = ParseExplicitExpressionBody();
                                directiveBuilder.Add(expression);
                            }
                            else if (!TryParseQualifiedIdentifier(out identifierLength))
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsIdentifierOrExpression(
                                        new SourceSpan(CurrentStart, identifierLength), descriptor.Directive));

                                builder.Add(BuildDirective(SyntaxKind.Identifier));
                                return;
                            }

                            break;
                    }

                    chunkGenerator = new DirectiveTokenChunkGenerator(tokenDescriptor);
                    SetAcceptedCharacters(AcceptedCharactersInternal.NonWhitespace);
                    if (editHandlerBuilder != null)
                    {
                        DirectiveTokenEditHandler.SetupBuilder(editHandlerBuilder, LanguageTokenizeString);
                    }
                    directiveBuilder.Add(OutputTokensAsStatementLiteral());
                }

                AcceptWhile(IsSpacingTokenIncludingComments);
                chunkGenerator = SpanChunkGenerator.Null;

                switch (descriptor.Kind)
                {
                    case DirectiveKind.SingleLine:
                        SetAcceptedCharacters(AcceptedCharactersInternal.Whitespace);
                        directiveBuilder.Add(OutputTokensAsUnclassifiedLiteral());

                        TryAccept(SyntaxKind.Semicolon);
                        directiveBuilder.Add(OutputAsMetaCode(Output(), AcceptedCharactersInternal.Whitespace));

                        AcceptWhile(IsSpacingTokenIncludingComments);

                        if (At(SyntaxKind.NewLine))
                        {
                            AcceptAndMoveNext();
                        }
                        else if (!EndOfFile)
                        {
                            Context.ErrorSink.OnError(
                                RazorDiagnosticFactory.CreateParsing_UnexpectedDirectiveLiteral(
                                    new SourceSpan(CurrentStart, CurrentToken.Content.Length),
                                    descriptor.Directive,
                                    Resources.ErrorComponent_Newline));
                        }

                        // This should contain the optional whitespace after the optional semicolon and the new line.
                        // Output as Markup as we want intellisense here.
                        chunkGenerator = SpanChunkGenerator.Null;
                        SetAcceptedCharacters(AcceptedCharactersInternal.Whitespace);
                        directiveBuilder.Add(OutputAsMarkupEphemeralLiteral());
                        break;
                    case DirectiveKind.RazorBlock:
                        shouldCaptureWhitespaceToEndOfLine = true;
                        AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);
                        SetAcceptedCharacters(AcceptedCharactersInternal.AllWhitespace);
                        directiveBuilder.Add(OutputTokensAsUnclassifiedLiteral());

                        ParseDirectiveBlock(directiveBuilder, descriptor, parseChildren: (childBuilder, startingBraceLocation) =>
                        {
                            // When transitioning to the HTML parser we no longer want to act as if we're in a nested C# state.
                            // For instance, if <div>@hello.</div> is in a nested C# block we don't want the trailing '.' to be handled
                            // as C#; it should be handled as a period because it's wrapped in markup.
                            var wasNested = IsNested;
                            IsNested = false;

                            using (PushSpanContextConfig())
                            {
                                EndingBlock();
                                var razorBlock = HtmlParser.ParseRazorBlock(Tuple.Create("{", "}"), caseSensitive: true);
                                directiveBuilder.Add(razorBlock);
                                StartingBlock();
                            }

                            InitializeContext();
                            IsNested = wasNested;
                            NextToken();
                        });
                        break;
                    case DirectiveKind.CodeBlock:
                        shouldCaptureWhitespaceToEndOfLine = true;
                        AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);
                        SetAcceptedCharacters(AcceptedCharactersInternal.AllWhitespace);
                        directiveBuilder.Add(OutputTokensAsUnclassifiedLiteral());

                        ParseDirectiveBlock(directiveBuilder, descriptor, parseChildren: (childBuilder, startingBraceLocation) =>
                        {
                            NextToken();

                            var existingEditHandler = editHandlerBuilder;
                            if (editHandlerBuilder != null)
                            {
                                CodeBlockEditHandler.SetupBuilder(editHandlerBuilder, LanguageTokenizeString);
                            }

                            if (Context.Options.AllowRazorInAllCodeBlocks)
                            {
                                var block = new Block(descriptor.Directive, directiveStart);
                                ParseCodeBlock(childBuilder, block);
                            }
                            else
                            {
                                Balance(childBuilder, BalancingModes.NoErrorOnFailure, SyntaxKind.LeftBrace, SyntaxKind.RightBrace, startingBraceLocation);
                            }

                            chunkGenerator = StatementChunkGenerator.Instance;

                            AcceptMarkerTokenIfNecessary();

                            childBuilder.Add(OutputTokensAsStatementLiteral());

                            editHandlerBuilder = existingEditHandler;
                        });
                        break;
                }
            }

            builder.Add(BuildDirective(SyntaxKind.Identifier));

            if (shouldCaptureWhitespaceToEndOfLine)
            {
                CaptureWhitespaceToEndOfLine();
                builder.Add(OutputAsMetaCode(Output(), Context.CurrentAcceptedCharacters));
            }

            RazorDirectiveSyntax BuildDirective(SyntaxKind expectedTokenKindIfMissing)
            {
                var node = OutputTokensAsStatementLiteral();
                if (node == null && directiveBuilder.Count == 0)
                {
                    node = SyntaxFactory.CSharpStatementLiteral(SyntaxFactory.MissingToken(expectedTokenKindIfMissing), chunkGenerator, editHandler: null);
                }

                directiveBuilder.Add(node);
                var directiveCodeBlock = SyntaxFactory.CSharpCodeBlock(directiveBuilder.ToList());

                var directiveBody = SyntaxFactory.RazorDirectiveBody(keywordBlock, directiveCodeBlock);
                var directive = SyntaxFactory.RazorDirective(transition, directiveBody, descriptor);

                var diagnostics = directiveErrorSink.GetErrorsAndClear();
                directive = directive.WithDiagnosticsGreen(diagnostics);
                return directive;
            }
        }
    }

    private void ValidateDirectiveUsage(DirectiveDescriptor descriptor, SourceLocation directiveStart)
    {
        if (descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
        {
            if (Context.SeenDirectives.Contains(descriptor.Directive))
            {
                // There will always be at least 1 child because of the `@` transition.
                var errorLength = /* @ */ 1 + descriptor.Directive.Length;
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
                        new SourceSpan(directiveStart, errorLength), descriptor.Directive));

                return;
            }
        }
    }

    // Used for parsing a qualified name like that which follows the `namespace` keyword.
    //
    // qualified-identifier:
    //      identifier
    //      qualified-identifier . identifier
    protected bool TryParseQualifiedIdentifier(out int identifierLength)
    {
        using var tokens = new PooledArrayBuilder<SyntaxToken>();

        var currentIdentifierLength = 0;
        var expectingDot = false;
        ReadWhile(
            token =>
            {
                var type = token.Kind;
                if ((expectingDot && type == SyntaxKind.Dot) ||
                    (!expectingDot && type == SyntaxKind.Identifier))
                {
                    expectingDot = !expectingDot;
                    return true;
                }

                if (type != SyntaxKind.Whitespace &&
                    type != SyntaxKind.NewLine)
                {
                    expectingDot = false;
                    currentIdentifierLength += token.Content.Length;
                }

                return false;
            },
            ref tokens.AsRef());

        identifierLength = currentIdentifierLength;
        var validQualifiedIdentifier = expectingDot;
        if (validQualifiedIdentifier)
        {
            foreach (var token in tokens)
            {
                identifierLength += token.Content.Length;
                Accept(token);
            }

            return true;
        }
        else
        {
            PutCurrentBack();

            foreach (var token in tokens)
            {
                identifierLength += token.Content.Length;
                PutBack(token);
            }

            EnsureCurrent();
            return false;
        }
    }

    private void ParseDirectiveBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, DirectiveDescriptor descriptor, Action<SyntaxListBuilder<RazorSyntaxNode>, SourceLocation> parseChildren)
    {
        if (EndOfFile)
        {
            Context.ErrorSink.OnError(
                RazorDiagnosticFactory.CreateParsing_UnexpectedEOFAfterDirective(
                    new SourceSpan(CurrentStart, contentLength: 1 /* { */), descriptor.Directive, "{"));
        }
        else if (!At(SyntaxKind.LeftBrace))
        {
            Context.ErrorSink.OnError(
                RazorDiagnosticFactory.CreateParsing_UnexpectedDirectiveLiteral(
                    new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive, "{"));
        }
        else
        {
            AutoCompleteEditHandler.AutoCompleteStringAccessor? autoCompleteStringAccessor = null;
            if (editHandlerBuilder != null)
            {
                AutoCompleteEditHandler.SetupBuilder(editHandlerBuilder, LanguageTokenizeString, autoCompleteAtEndOfSpan: true, out autoCompleteStringAccessor);
            }
            var startingBraceLocation = CurrentStart;
            Accept(CurrentToken);
            builder.Add(OutputAsMetaCode(Output()));

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            {
                var childBuilder = pooledResult.Builder;
                parseChildren(childBuilder, startingBraceLocation);
                if (childBuilder.Count > 0)
                {
                    builder.Add(SyntaxFactory.CSharpCodeBlock(childBuilder.ToList()));
                }
            }

            chunkGenerator = SpanChunkGenerator.Null;
            bool canAcceptCloseBrace;
            if (!TryAccept(SyntaxKind.RightBrace))
            {
                canAcceptCloseBrace = true;
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                        new SourceSpan(startingBraceLocation, contentLength: 1 /* } */), descriptor.Directive, "}", "{"));

                Accept(SyntaxFactory.MissingToken(SyntaxKind.RightBrace));
            }
            else
            {
                canAcceptCloseBrace = false;
                SetAcceptedCharacters(AcceptedCharactersInternal.None);
            }

            if (autoCompleteStringAccessor != null)
            {
                autoCompleteStringAccessor.CanAcceptCloseBrace = canAcceptCloseBrace;
            }

            builder.Add(OutputAsMetaCode(Output(), Context.CurrentAcceptedCharacters));
        }
    }

    private bool TryParseKeyword(
        in SyntaxListBuilder<RazorSyntaxNode> builder,
        ref readonly PooledArrayBuilder<SyntaxToken> whitespace,
        CSharpTransitionSyntax? transition)
    {
        var result = _tokenizer.Tokenizer.GetTokenKeyword(CurrentToken);
        Debug.Assert(CurrentToken.Kind == SyntaxKind.Keyword && result.HasValue);
        if (_keywordParserMap.TryGetValue(result!.Value, out var handler))
        {
            // This is a keyword. We want to preserve preceding whitespace in the output.
            Accept(in whitespace);
            builder.Add(OutputTokensAsStatementLiteral());

            handler(builder, transition);
            return true;
        }

        return false;
    }

    private bool TryParseKeyword(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        var result = _tokenizer.Tokenizer.GetTokenKeyword(CurrentToken);
        Debug.Assert(CurrentToken.Kind == SyntaxKind.Keyword && result.HasValue);
        if (_keywordParserMap.TryGetValue(result!.Value, out var handler))
        {
            handler(builder, null);
            return true;
        }

        return false;
    }

    private bool AtBooleanLiteral()
    {
        return _tokenizer.Tokenizer.GetTokenKeyword(CurrentToken) is CSharpSyntaxKind.TrueKeyword or CSharpSyntaxKind.FalseKeyword;
    }

    private void ParseAwaitExpression(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        // Ensure that we're on the await statement (only runs in debug)
        Assert(CSharpSyntaxKind.AwaitKeyword);

        // Accept the "await" and move on
        AcceptAndMoveNext();

        // Accept 1 or more spaces between the await and the following code.
        AcceptWhile(IsSpacingTokenIncludingComments);

        // Top level basically indicates if we're within an expression or statement.
        // Ex: topLevel true = @await Foo()  |  topLevel false = @{ await Foo(); }
        // Note that in this case @{ <b>@await Foo()</b> } top level is true for await.
        // Therefore, if we're top level then we want to act like an implicit expression,
        // otherwise just act as whatever we're contained in.
        var topLevel = transition != null;
        if (!topLevel)
        {
            return;
        }

        if (At(CSharpSyntaxKind.ForEachKeyword))
        {
            // C# 8 async streams. @await foreach (var value in asyncEnumerable) { .... }

            ParseConditionalBlock(builder, transition);
        }
        else
        {
            // Setup the Span to be an async implicit expression (an implicit expression that allows spaces).
            // Spaces are allowed because of "@await Foo()".
            var implicitExpressionBody = ParseImplicitExpressionBody(async: true);
            builder.Add(SyntaxFactory.CSharpImplicitExpression(transition, implicitExpressionBody));
        }
    }

    private void ParseConditionalBlock(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        var topLevel = transition != null;
        ParseConditionalBlock(builder, transition, topLevel);
    }

    private void ParseConditionalBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition, bool topLevel)
    {
        Assert(SyntaxKind.Keyword);
        if (transition != null)
        {
            builder.Add(transition);
        }

        var block = new Block(GetBlockName(CurrentToken), CurrentStart);
        ParseConditionalBlock(builder, block);
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseConditionalBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block)
    {
        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);

        // Parse the condition, if present (if not present, we'll let the C# compiler complain)
        if (TryParseCondition(builder))
        {
            AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);

            ParseExpectedCodeBlock(builder, block);
        }
    }

    private bool TryParseCondition(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (At(SyntaxKind.LeftParenthesis))
        {
            var complete = Balance(builder, BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
            if (!complete)
            {
                AcceptUntil(SyntaxKind.NewLine);
            }
            else
            {
                TryAccept(SyntaxKind.RightParenthesis);
            }
            return complete;
        }
        return true;
    }

    private void ParseExpectedCodeBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block)
    {
        if (!EndOfFile)
        {
            // If it's a block control flow statement the current syntax token will be a LeftBrace {,
            // otherwise we're acting on a single line control flow statement which cannot allow markup.

            var encounteredUnexpectedMarkupTransition = false;

            if (At(SyntaxKind.LessThan))
            {
                // if (...) <p>Hello World</p>
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(
                        new SourceSpan(CurrentStart, CurrentToken.Content.Length)));
                encounteredUnexpectedMarkupTransition = true;
            }
            else if (At(SyntaxKind.Transition) && NextIs(SyntaxKind.Colon))
            {
                // if (...) @: <p>The time is @DateTime.Now</p>
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(
                        new SourceSpan(CurrentStart, contentLength: 2 /* @: */)));
                encounteredUnexpectedMarkupTransition = true;
            }
            else if (At(SyntaxKind.Transition) && NextIs(SyntaxKind.Transition))
            {
                // if (...) @@JohnDoe <strong>Hi!</strong>
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(
                        new SourceSpan(CurrentStart, contentLength: 2 /* @@ */)));
                encounteredUnexpectedMarkupTransition = true;
            }

            // Parse the statement and then we're done
            ParseStatement(builder, block, encounteredUnexpectedMarkupTransition);
        }
    }

    private void ParseUnconditionalBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        Assert(SyntaxKind.Keyword);
        var block = new Block(GetBlockName(CurrentToken), CurrentStart);
        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);
        ParseExpectedCodeBlock(builder, block);
    }

    private void ParseCaseStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        Assert(SyntaxKind.Keyword);
        if (transition != null)
        {
            // Normally, case statement won't start with a transition in a valid scenario.
            // If it does, just accept it and let the compiler complain.
            builder.Add(transition);
        }
        var result = _tokenizer.Tokenizer.GetTokenKeyword(CurrentToken);
        Debug.Assert(result is CSharpSyntaxKind.CaseKeyword or CSharpSyntaxKind.DefaultKeyword);
        AcceptAndMoveNext();
        while (EnsureCurrent() && CurrentToken.Kind != SyntaxKind.Colon)
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxKind.LeftBrace:
                case SyntaxKind.LeftParenthesis:
                case SyntaxKind.LeftBracket:
                    Balance(builder, BalancingModes.None);
                    break;

                default:
                    AcceptAndMoveNext();
                    break;
            }
        }
        TryAccept(SyntaxKind.Colon);
    }

    private void ParseIfStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        Assert(CSharpSyntaxKind.IfKeyword);
        ParseConditionalBlock(builder, transition, topLevel: false);
        ParseAfterIfClause(builder);
        var topLevel = transition != null;
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseAfterIfClause(SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // Grab whitespace and razor comments
        using var whitespace = new PooledArrayBuilder<SyntaxToken>();
        SkipToNextImportantToken(builder, ref whitespace.AsRef());

        // Check for an else part
        if (At(CSharpSyntaxKind.ElseKeyword))
        {
            Accept(in whitespace);
            Assert(CSharpSyntaxKind.ElseKeyword);
            ParseElseClause(builder);
        }
        else
        {
            // No else, return whitespace
            PutCurrentBack();
            PutBack(in whitespace);
            SetAcceptedCharacters(AcceptedCharactersInternal.Any);
        }
    }

    private void ParseElseClause(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (!At(CSharpSyntaxKind.ElseKeyword))
        {
            return;
        }
        var block = new Block(GetBlockName(CurrentToken), CurrentStart);

        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);
        if (At(CSharpSyntaxKind.IfKeyword))
        {
            // ElseIf
            block.Name = SyntaxConstants.CSharp.ElseIfKeyword;
            ParseConditionalBlock(builder, block);
            ParseAfterIfClause(builder);
        }
        else if (!EndOfFile)
        {
            // Else
            ParseExpectedCodeBlock(builder, block);
        }
    }

    private void ParseTryStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        Assert(CSharpSyntaxKind.TryKeyword);
        var topLevel = transition != null;
        if (topLevel)
        {
            builder.Add(transition);
        }

        ParseUnconditionalBlock(builder);
        ParseAfterTryClause(builder);
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseAfterTryClause(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // Grab whitespace
        using var whitespace = new PooledArrayBuilder<SyntaxToken>();
        SkipToNextImportantToken(builder, ref whitespace.AsRef());

        // Check for a catch or finally part
        if (At(CSharpSyntaxKind.CatchKeyword))
        {
            Accept(in whitespace);
            Assert(CSharpSyntaxKind.CatchKeyword);
            ParseFilterableCatchBlock(builder);
            ParseAfterTryClause(builder);
        }
        else if (At(CSharpSyntaxKind.FinallyKeyword))
        {
            Accept(in whitespace);
            Assert(CSharpSyntaxKind.FinallyKeyword);
            ParseUnconditionalBlock(builder);
        }
        else
        {
            // Return whitespace and end the block
            PutCurrentBack();
            PutBack(in whitespace);
            SetAcceptedCharacters(AcceptedCharactersInternal.Any);
        }
    }

    private void ParseFilterableCatchBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        Assert(CSharpSyntaxKind.CatchKeyword);

        var block = new Block(GetBlockName(CurrentToken), CurrentStart);

        // Accept "catch"
        AcceptAndMoveNext();
        AcceptWhile(IsValidStatementSpacingToken);

        // Parse the catch condition if present. If not present, let the C# compiler complain.
        if (TryParseCondition(builder))
        {
            AcceptWhile(IsValidStatementSpacingToken);

            if (At(CSharpSyntaxKind.WhenKeyword))
            {
                // Accept "when".
                AcceptAndMoveNext();
                AcceptWhile(IsValidStatementSpacingToken);

                // Parse the filter condition if present. If not present, let the C# compiler complain.
                if (!TryParseCondition(builder))
                {
                    // Incomplete condition.
                    return;
                }

                AcceptWhile(IsValidStatementSpacingToken);
            }

            ParseExpectedCodeBlock(builder, block);
        }
    }

    private void ParseDoStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        Assert(CSharpSyntaxKind.DoKeyword);
        if (transition != null)
        {
            builder.Add(transition);
        }

        ParseUnconditionalBlock(builder);
        ParseWhileClause(builder);
        var topLevel = transition != null;
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseWhileClause(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        SetAcceptedCharacters(AcceptedCharactersInternal.Any);
        using var whitespace = new PooledArrayBuilder<SyntaxToken>();
        SkipToNextImportantToken(builder, ref whitespace.AsRef());

        if (At(CSharpSyntaxKind.WhileKeyword))
        {
            Accept(in whitespace);
            Assert(CSharpSyntaxKind.WhileKeyword);
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);
            if (TryParseCondition(builder) && TryAccept(SyntaxKind.Semicolon))
            {
                SetAcceptedCharacters(AcceptedCharactersInternal.None);
            }
        }
        else
        {
            PutCurrentBack();
            PutBack(in whitespace);
        }
    }

    private void ParseUsingKeyword(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        Assert(CSharpSyntaxKind.UsingKeyword);
        var topLevel = transition != null;
        var block = new Block(GetBlockName(CurrentToken), CurrentStart);
        var usingToken = EatCurrentToken();
        using var whitespaceOrComments = new PooledArrayBuilder<SyntaxToken>();
        ReadWhile(IsSpacingTokenIncludingComments, ref whitespaceOrComments.AsRef());
        var atLeftParen = At(SyntaxKind.LeftParenthesis);
        var atIdentifier = At(SyntaxKind.Identifier);
        var atStaticOrGlobal = At(CSharpSyntaxKind.StaticKeyword, CSharpSyntaxKind.GlobalKeyword);

        // Put the read tokens back and let them be handled later.
        PutCurrentBack();
        PutBack(in whitespaceOrComments);
        PutBack(usingToken);
        EnsureCurrent();

        if (atLeftParen)
        {
            // using ( ==> Using Statement
            ParseUsingStatement(builder, transition, block);
        }
        else if (atIdentifier || atStaticOrGlobal)
        {
            // using Identifier ==> Using Declaration
            if (!topLevel)
            {
                // using Variable Declaration

                if (!Context.Options.AllowUsingVariableDeclarations)
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock(
                            new SourceSpan(block.Start, block.Name.Length)));
                }

                // There are cases when a user will do @using var x = 123; At which point we let C# notify the user
                // of their error like we do any other invalid expression.
                if (transition != null)
                {
                    builder.Add(transition);
                }
                AcceptAndMoveNext();
                AcceptWhile(IsSpacingTokenIncludingComments);
                ParseStandardStatement(builder, encounteredUnexpectedMarkupTransition: false);
            }
            else
            {
                ParseUsingDeclaration(builder, transition);
                return;
            }
        }
        else
        {
            if (transition != null)
            {
                builder.Add(transition);
            }

            AcceptAndMoveNext();
            AcceptWhile(IsSpacingTokenIncludingComments);
        }

        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseUsingStatement(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition, Block block)
    {
        Assert(CSharpSyntaxKind.UsingKeyword);
        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingComments);

        Assert(SyntaxKind.LeftParenthesis);
        if (transition != null)
        {
            builder.Add(transition);
        }

        // Parse condition
        if (TryParseCondition(builder))
        {
            AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);

            // Parse code block
            ParseExpectedCodeBlock(builder, block);
        }
    }

    private void ParseUsingDeclaration(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax? transition)
    {
        // Using declarations should always be top level. The error case is handled in a different code path.
        Debug.Assert(transition != null);
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var directiveBuilder = pooledResult.Builder;
            Assert(CSharpSyntaxKind.UsingKeyword);
            AcceptAndMoveNext();
            var isStatic = false;
            var nonNamespaceTokenCount = TokenBuilder.Count;
            AcceptWhile(IsSpacingTokenIncludingComments);
            var start = CurrentStart;
            if (At(SyntaxKind.Identifier) || At(CSharpSyntaxKind.GlobalKeyword))
            {
                // non-static using
                nonNamespaceTokenCount = TokenBuilder.Count;
                TryParseNamespaceOrTypeName(directiveBuilder);
                using var whitespace = new PooledArrayBuilder<SyntaxToken>();
                ReadWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives, ref whitespace.AsRef());
                if (At(SyntaxKind.Assign))
                {
                    // Alias
                    Accept(in whitespace);
                    Assert(SyntaxKind.Assign);
                    AcceptAndMoveNext();

                    AcceptWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives);

                    // One more namespace or type name
                    TryParseNamespaceOrTypeName(directiveBuilder);
                }
                else
                {
                    PutCurrentBack();
                    PutBack(in whitespace);
                }
            }
            else if (At(CSharpSyntaxKind.StaticKeyword))
            {
                // static using
                isStatic = true;
                AcceptAndMoveNext();
                AcceptWhile(IsSpacingTokenIncludingComments);
                nonNamespaceTokenCount = TokenBuilder.Count;
                TryParseNamespaceOrTypeName(directiveBuilder);
            }

            var usingStatementTokens = TokenBuilder.ToList().Nodes;

            SetAcceptedCharacters(AcceptedCharactersInternal.AnyExceptNewline);

            // Optional ";"
            bool hasExplicitSemicolon = false;
            if (EnsureCurrent())
            {
                hasExplicitSemicolon = TryAccept(SyntaxKind.Semicolon);
            }

            using var _1 = StringBuilderPool.GetPooledObject(out var usingContentBuilder);
            using var _2 = StringBuilderPool.GetPooledObject(out var parsedNamespaceBuilder);

            for (var i = 0; i < usingStatementTokens.Length; i++)
            {
                var token = usingStatementTokens[i];

                if (i >= 1)
                {
                    usingContentBuilder.Append(token.Content);
                }

                if (i >= nonNamespaceTokenCount &&
                    token.Kind != SyntaxKind.CSharpComment &&
                    token.Kind != SyntaxKind.Whitespace &&
                    token.Kind != SyntaxKind.NewLine)
                {
                    parsedNamespaceBuilder.Append(token.Content);
                }
            }

            chunkGenerator = new AddImportChunkGenerator(
                usingContentBuilder.ToString(),
                parsedNamespaceBuilder.ToString(),
                isStatic,
                hasExplicitSemicolon);

            Debug.Assert(directiveBuilder.Count == 0, "We should not have built any blocks so far.");
            var keywordTokens = OutputTokensAsStatementLiteral();
            var directiveBody = SyntaxFactory.RazorDirectiveBody(keywordTokens, null);
            builder.Add(SyntaxFactory.RazorUsingDirective(transition, directiveBody));

            if (!Context.DesignTimeMode)
            {
                CaptureWhitespaceToEndOfLine();
                builder.Add(OutputAsMetaCode(Output(), Context.CurrentAcceptedCharacters));
            }
        }
    }

    private bool TryParseNamespaceOrTypeName(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (TryAccept(SyntaxKind.LeftParenthesis))
        {
            while (!TryAccept(SyntaxKind.RightParenthesis) && !EndOfFile)
            {
                TryAccept(SyntaxKind.Whitespace);

                if (!TryParseNamespaceOrTypeName(builder))
                {
                    return false;
                }

                TryAccept(SyntaxKind.Whitespace);
                TryAccept(SyntaxKind.Identifier);
                TryAccept(SyntaxKind.Whitespace);
                TryAccept(SyntaxKind.Comma);
            }

            if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.QuestionMark))
            {
                // Only accept the whitespace if we are going to consume the next token.
                AcceptAndMoveNext();
            }

            TryAccept(SyntaxKind.QuestionMark); // Nullable

            return true;
        }
        else if (TryAccept(SyntaxKind.Identifier) || TryAccept(SyntaxKind.Keyword))
        {
            if (TryAccept(SyntaxKind.DoubleColon))
            {
                if (!TryAccept(SyntaxKind.Identifier))
                {
                    TryAccept(SyntaxKind.Keyword);
                }
            }
            if (At(SyntaxKind.LessThan))
            {
                ParseTypeArgumentList(builder);
            }
            if (TryAccept(SyntaxKind.Dot))
            {
                TryParseNamespaceOrTypeName(builder);
            }

            if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.QuestionMark))
            {
                // Only accept the whitespace if we are going to consume the next token.
                AcceptAndMoveNext();
            }

            TryAccept(SyntaxKind.QuestionMark); // Nullable

            if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.LeftBracket))
            {
                // Only accept the whitespace if we are going to consume the next token.
                AcceptAndMoveNext();
            }

            while (At(SyntaxKind.LeftBracket))
            {
                Balance(builder, BalancingModes.None);
                if (!TryAccept(SyntaxKind.RightBracket))
                {
                    Accept(SyntaxFactory.MissingToken(SyntaxKind.RightBracket));
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private void ParseTypeArgumentList(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        Assert(SyntaxKind.LessThan);
        Balance(builder, BalancingModes.None);
        if (!TryAccept(SyntaxKind.GreaterThan))
        {
            Accept(SyntaxFactory.MissingToken(SyntaxKind.GreaterThan));
        }
    }

    private void ParseReservedDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        Context.ErrorSink.OnError(
            RazorDiagnosticFactory.CreateParsing_ReservedWord(
                new SourceSpan(CurrentStart, CurrentToken.Content.Length), CurrentToken.Content));

        AcceptAndMoveNext();
        SetAcceptedCharacters(AcceptedCharactersInternal.None);
        chunkGenerator = SpanChunkGenerator.Null;
        CompleteBlock();
        var keyword = OutputAsMetaCode(Output());
        var directiveBody = SyntaxFactory.RazorDirectiveBody(keyword, csharpCode: null);

        // transition could be null if we're already inside a code block.
        transition = transition ?? SyntaxFactory.CSharpTransition(SyntaxFactory.MissingToken(SyntaxKind.Transition));
        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
    }

    protected void CompleteBlock()
    {
        AcceptMarkerTokenIfNecessary();
        CaptureWhitespaceToEndOfLine();
    }

    private void CaptureWhitespaceToEndOfLine()
    {
        EnsureCurrent();

        // Read whitespace, but not newlines
        // If we're not inserting a marker span, we don't need to capture whitespace
        if (!Context.WhiteSpaceIsSignificantToAncestorBlock &&
            !Context.DesignTimeMode &&
            !IsNested)
        {
            using var whitespace = new PooledArrayBuilder<SyntaxToken>();
            ReadWhile(static token => token.Kind == SyntaxKind.Whitespace, ref whitespace.AsRef());
            if (At(SyntaxKind.NewLine))
            {
                Accept(in whitespace);
                AcceptAndMoveNext();
                PutCurrentBack();
            }
            else
            {
                PutCurrentBack();
                PutBack(in whitespace);
            }
        }
        else
        {
            PutCurrentBack();
        }
    }

    private void SkipToNextImportantToken(
        in SyntaxListBuilder<RazorSyntaxNode> builder,
        ref PooledArrayBuilder<SyntaxToken> whitespace)
    {
        Debug.Assert(whitespace.Count == 0, "Expected empty builder.");

        while (!EndOfFile)
        {
            ReadWhile(IsSpacingTokenIncludingNewLinesAndCommentsAndCSharpDirectives, ref whitespace);
            if (At(SyntaxKind.RazorCommentTransition))
            {
                Accept(in whitespace);
                SetAcceptedCharacters(AcceptedCharactersInternal.Any);
                AcceptMarkerTokenIfNecessary();
                builder.Add(OutputTokensAsStatementLiteral());
                var comment = ParseRazorComment();
                builder.Add(comment);
            }
            else
            {
                return;
            }

            whitespace.Clear();
        }
    }

    private void DefaultSpanContextConfig(SpanEditHandlerBuilder? editHandlerBuilder, ref ISpanChunkGenerator? generator)
    {
        generator = StatementChunkGenerator.Instance;
        SetAcceptedCharacters(AcceptedCharactersInternal.Any);
        if (editHandlerBuilder == null)
        {
            return;
        }

        editHandlerBuilder.Reset();
        editHandlerBuilder.Tokenizer = LanguageTokenizeString;
    }

    private void ExplicitExpressionSpanContextConfig(SpanEditHandlerBuilder? editHandlerBuilder, ref ISpanChunkGenerator? generator)
    {
        generator = new ExpressionChunkGenerator();
        SetAcceptedCharacters(AcceptedCharactersInternal.Any);
        if (editHandlerBuilder == null)
        {
            return;
        }

        editHandlerBuilder.Reset();
        editHandlerBuilder.Tokenizer = LanguageTokenizeString;
    }

    private CSharpStatementLiteralSyntax? OutputTokensAsStatementLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return SyntaxFactory.CSharpStatementLiteral(tokens, chunkGenerator, GetEditHandler());
    }

    private CSharpExpressionLiteralSyntax? OutputTokensAsExpressionLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return SyntaxFactory.CSharpExpressionLiteral(tokens, chunkGenerator, GetEditHandler());
    }

    private CSharpEphemeralTextLiteralSyntax? OutputTokensAsEphemeralLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return SyntaxFactory.CSharpEphemeralTextLiteral(tokens, chunkGenerator, GetEditHandler());
    }

    private UnclassifiedTextLiteralSyntax? OutputTokensAsUnclassifiedLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return SyntaxFactory.UnclassifiedTextLiteral(tokens, chunkGenerator, GetEditHandler());
    }

    private void OtherParserBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // When transitioning to the HTML parser we no longer want to act as if we're in a nested C# state.
        // For instance, if <div>@hello.</div> is in a nested C# block we don't want the trailing '.' to be handled
        // as C#; it should be handled as a period because it's wrapped in markup.
        var wasNested = IsNested;
        IsNested = false;

        EndingBlock();

        RazorSyntaxNode? htmlBlock = null;
        using (PushSpanContextConfig())
        {
            htmlBlock = HtmlParser.ParseBlock();
        }

        builder.Add(htmlBlock);
        InitializeContext();

        StartingBlock();

        IsNested = wasNested;
        NextToken();
    }

    private bool Balance(SyntaxListBuilder<RazorSyntaxNode> builder, BalancingModes mode)
    {
        var left = CurrentToken.Kind;
        var right = Language.FlipBracket(left);
        var start = CurrentStart;
        AcceptAndMoveNext();
        if (EndOfFile && ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure))
        {
            Context.ErrorSink.OnError(
                RazorDiagnosticFactory.CreateParsing_ExpectedCloseBracketBeforeEOF(
                    new SourceSpan(start, contentLength: 1 /* { OR } */),
                    Language.GetSample(left),
                    Language.GetSample(right)));
        }

        return Balance(builder, mode, left, right, start);
    }

    private bool Balance(SyntaxListBuilder<RazorSyntaxNode> builder, BalancingModes mode, SyntaxKind left, SyntaxKind right, SourceLocation start)
    {
        var startPosition = CurrentStart.AbsoluteIndex;
        var nesting = 1;
        var stopAtEndOfLine = (mode & BalancingModes.StopAtEndOfLine) == BalancingModes.StopAtEndOfLine;
        if (!EndOfFile &&
            !(stopAtEndOfLine && At(SyntaxKind.NewLine)))
        {
            using var tokens = new PooledArrayBuilder<SyntaxToken>();
            do
            {
                if (IsAtEmbeddedTransition(
                    (mode & BalancingModes.AllowCommentsAndTemplates) == BalancingModes.AllowCommentsAndTemplates))
                {
                    Accept(in tokens);
                    tokens.Clear();
                    ParseEmbeddedTransition(builder);

                    // Reset backtracking since we've already outputted some spans.
                    startPosition = CurrentStart.AbsoluteIndex;
                }

                if (At(SyntaxKind.Transition))
                {
                    // We special case @@identifier because the old compiler behavior was to simply accept it and treat it as if it was @identifier. While
                    // this isn't legal, the runtime compiler doesn't handle @identifier correctly. We'll continue to accept this for now, but will potentially
                    // break it in the future when we move to the roslyn lexer and the runtime/compiletime split is much greater.
                    if (NextIs(SyntaxKind.Transition) && Lookahead(2) is { Kind: SyntaxKind.Identifier or SyntaxKind.Keyword })
                    {
                        Accept(in tokens);
                        tokens.Clear();
                        builder.Add(OutputTokensAsStatementLiteral());
                        AcceptAndMoveNext();
                        builder.Add(OutputTokensAsEphemeralLiteral());

                        // Reset backtracking since we've already outputted some spans.
                        startPosition = CurrentStart.AbsoluteIndex;
                        continue;
                    }
                    else if (NextIs(SyntaxKind.Keyword, SyntaxKind.Identifier))
                    {
                        tokens.Add(NextAsEscapedIdentifier());
                        continue;
                    }
                }

                if (At(left))
                {
                    nesting++;
                }
                else if (At(right))
                {
                    nesting--;
                }

                if (nesting > 0)
                {
                    tokens.Add(CurrentToken);
                    NextToken();
                }
            }
            while (nesting > 0 && EnsureCurrent() && !(stopAtEndOfLine && At(SyntaxKind.NewLine)));

            if (nesting > 0)
            {
                if ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure)
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_ExpectedCloseBracketBeforeEOF(
                            new SourceSpan(start, contentLength: 1 /* { OR } */),
                            Language.GetSample(left),
                            Language.GetSample(right)));
                }
                if ((mode & BalancingModes.BacktrackOnFailure) == BalancingModes.BacktrackOnFailure)
                {
                    _tokenizer.Reset(startPosition);
                    NextToken();
                }
                else
                {
                    Accept(in tokens);
                }
            }
            else
            {
                // Accept all the tokens we saw
                Accept(in tokens);
            }
        }
        return nesting == 0;
    }

    private bool IsAtEmbeddedTransition(bool allowTemplatesAndComments)
    {
        return allowTemplatesAndComments
               && ((Language.IsTransition(CurrentToken)
                    && NextIs(SyntaxKind.LessThan, SyntaxKind.Colon, SyntaxKind.DoubleColon))
                   || Language.IsCommentStart(CurrentToken));
    }

    private void ParseEmbeddedTransition(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (Language.IsTransition(CurrentToken))
        {
            PutCurrentBack();
            ParseTemplate(builder);
        }
        else if (Language.IsCommentStart(CurrentToken))
        {
            // Output tokens before parsing the comment.
            AcceptMarkerTokenIfNecessary();
            if (chunkGenerator is ExpressionChunkGenerator)
            {
                builder.Add(OutputTokensAsExpressionLiteral());
            }
            else
            {
                builder.Add(OutputTokensAsStatementLiteral());
            }

            var comment = ParseRazorComment();
            builder.Add(comment);
        }
    }

    private SyntaxToken NextAsEscapedIdentifier()
    {
        Debug.Assert(CurrentToken.Kind == SyntaxKind.Transition);
        var transition = CurrentToken;
        NextToken();
        Debug.Assert(CurrentToken.Kind is SyntaxKind.Identifier or SyntaxKind.Keyword);
        var identifier = CurrentToken;
        NextToken();

        var finalIdentifier = SyntaxFactory.Token(SyntaxKind.Identifier, $"{transition.Content}{identifier.Content}");
        return finalIdentifier;
    }

    [Conditional("DEBUG")]
    internal void Assert(CSharpSyntaxKind expectedKeyword)
    {
        var result = _tokenizer.Tokenizer.GetTokenKeyword(CurrentToken);
        Debug.Assert(CurrentToken.Kind == SyntaxKind.Keyword &&
            result.HasValue &&
            result.Value == expectedKeyword);
    }

    protected internal bool At(params ReadOnlySpan<CSharpSyntaxKind> keywords)
    {
        var result = _tokenizer.Tokenizer.GetTokenKeyword(CurrentToken);
        if (!At(SyntaxKind.Keyword) || result is not { } keywordKind)
        {
            return false;
        }

        foreach (var search in keywords)
        {
            if (keywordKind == search)
            {
                return true;
            }
        }

        return false;
    }

    private string GetBlockName(SyntaxToken token)
    {
        var result = _tokenizer.Tokenizer.GetTokenKeyword(token);
        if (result is not CSharpSyntaxKind.None and { } value && token.Kind == SyntaxKind.Keyword)
        {
            return CSharpSyntaxFacts.GetText(value);
        }
        return token.Content;
    }

    protected class Block
    {
        public Block(string name, SourceLocation start)
        {
            Name = name;
            Start = start;
        }

        public string Name { get; set; }
        public SourceLocation Start { get; set; }
    }

    internal class ParsedDirective
    {
        public required string DirectiveText { get; set; }

        public string? AssemblyName { get; set; }

        public string? TypePattern { get; set; }
    }
}
