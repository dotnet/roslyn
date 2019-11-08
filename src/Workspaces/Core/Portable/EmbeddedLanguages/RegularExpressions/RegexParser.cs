// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using static EmbeddedSyntaxHelpers;
    using static RegexHelpers;

    using RegexNodeOrToken = EmbeddedSyntaxNodeOrToken<RegexKind, RegexNode>;
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    /// <summary>
    /// Produces a <see cref="RegexTree"/> from a sequence of <see cref="VirtualChar"/> characters.
    ///
    /// Importantly, this parser attempts to replicate diagnostics with almost the exact same text
    /// as the native .NET regex parser.  This is important so that users get an understandable
    /// experience where it appears to them that this is all one cohesive system and that the IDE
    /// will let them discover and fix the same issues they would encounter when previously trying
    /// to just compile and execute these regexes.
    /// </summary>
    /// <remarks>
    /// Invariants we try to maintain (and should consider a bug if we do not): l 1. If the .NET
    /// regex parser does not report an error for a given pattern, we should not either. it would be
    /// very bad if we told the user there was something wrong with there pattern when there really
    /// wasn't.
    ///
    /// 2. If the .NET regex parser does report an error for a given pattern, we should either not
    /// report an error (not recommended) or report the same error at an appropriate location in the
    /// pattern.  Not reporting the error can be confusing as the user will think their pattern is
    /// ok, when it really is not.  However, it can be acceptable to do this as it's not telling
    /// them that something is actually wrong, and it may be too difficult to find and report the
    /// same error.  Note: there is only one time we do this in this parser (see the deviation
    /// documented in <see cref="ParsePossibleEcmascriptBackreferenceEscape"/>).
    ///
    /// Note1: "report the same error" means that we will attempt to report the error using the same
    /// text the .NET regex parser uses for its error messages.  This is so that the user is not
    /// confused when they use the IDE vs running the regex by getting different messages for the
    /// same issue.
    ///
    /// Note2: the above invariants make life difficult at times.  This happens due to the fact that
    /// the .NET parser is multi-pass.  Meaning it does a first scan (which may report errors), then
    /// does the full parse.  This means that it might report an error in a later location during
    /// the initial scan than it would during the parse.  We replicate that behavior to follow the
    /// second invariant.
    ///
    /// Note3: It would be nice if we could check these invariants at runtime, so we could control
    /// our behavior by the behavior of the real .NET regex engine.  For example, if the .NET regex
    /// engine did not report any issues, we could suppress any diagnostics we generated and we
    /// could log an NFW to record which pattern we deviated on so we could fix the issue for a
    /// future release.  However, we cannot do this as the .NET regex engine has no guarantees about
    /// its performance characteristics.  For example, certain regex patterns might end up causing
    /// that engine to consume unbounded amounts of CPU and memory.  This is because the .NET regex
    /// engine is not just a parser, but something that builds an actual recognizer using techniques
    /// that are not necessarily bounded.  As such, while we test ourselves around it during our
    /// tests, we cannot do the same at runtime as part of the IDE.
    ///
    /// This parser was based off the corefx RegexParser based at:
    /// https://github.com/dotnet/corefx/blob/f759243d724f462da0bcef54e86588f8a55352c6/src/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexParser.cs#L1
    ///
    /// Note4: The .NET parser itself changes over time (for example to fix behavior that even it
    /// thinks is buggy).  When this happens, we have to make a choice as to which behavior to
    /// follow. In general, the overall principle is that we should follow the more lenient
    /// behavior.  If we end up taking the more strict interpretation we risk giving people an error
    /// during design time that they would not get at runtime.  It's far worse to have that than to
    /// not report an error, even though one might happen later.
    /// </remarks>
    internal partial struct RegexParser
    {
        private readonly ImmutableDictionary<string, TextSpan> _captureNamesToSpan;
        private readonly ImmutableDictionary<int, TextSpan> _captureNumbersToSpan;

        private RegexLexer _lexer;
        private RegexOptions _options;
        private RegexToken _currentToken;
        private int _recursionDepth;

        private RegexParser(
            VirtualCharSequence text, RegexOptions options,
            ImmutableDictionary<string, TextSpan> captureNamesToSpan,
            ImmutableDictionary<int, TextSpan> captureNumbersToSpan) : this()
        {
            _lexer = new RegexLexer(text);
            _options = options;

            _captureNamesToSpan = captureNamesToSpan;
            _captureNumbersToSpan = captureNumbersToSpan;

            // Get the first token.  It is allowed to have trivia on it.
            ConsumeCurrentToken(allowTrivia: true);
        }

        /// <summary>
        /// Returns the latest token the lexer has produced, and then asks the lexer to 
        /// produce the next token after that.
        /// </summary>
        /// <param name="allowTrivia">Whether or not trivia is allowed on the next token
        /// produced.  In the .NET parser trivia is only allowed on a few constructs,
        /// and our parser mimics that behavior.  Note that even if trivia is allowed,
        /// the type of trivia that can be scanned depends on the current RegexOptions.
        /// For example, if <see cref="RegexOptions.IgnorePatternWhitespace"/> is currently
        /// enabled, then '#...' comments are allowed.  Otherwise, only '(?#...)' comments
        /// are allowed.</param>
        private RegexToken ConsumeCurrentToken(bool allowTrivia)
        {
            var previous = _currentToken;
            _currentToken = _lexer.ScanNextToken(allowTrivia, _options);
            return previous;
        }

        /// <summary>
        /// Given an input text, and set of options, parses out a fully representative syntax tree 
        /// and list of diagnostics.  Parsing should always succeed, except in the case of the stack 
        /// overflowing.
        /// </summary>
        public static RegexTree TryParse(VirtualCharSequence text, RegexOptions options)
        {
            if (text.IsDefault)
            {
                return null;
            }

            try
            {
                // Parse the tree once, to figure out the capture groups.  These are needed
                // to then parse the tree again, as the captures will affect how we interpret
                // certain things (i.e. escape references) and what errors will be reported.
                //
                // This is necessary as .NET regexes allow references to *future* captures.
                // As such, we don't know when we're seeing a reference if it's to something
                // that exists or not.
                var tree1 = new RegexParser(text, options,
                    ImmutableDictionary<string, TextSpan>.Empty,
                    ImmutableDictionary<int, TextSpan>.Empty).ParseTree();

                var (captureNames, captureNumbers) = CaptureInfoAnalyzer.Analyze(text, tree1.Root, options);

                var tree2 = new RegexParser(
                    text, options, captureNames, captureNumbers).ParseTree();
                return tree2;
            }
            catch (InsufficientExecutionStackException)
            {
                return null;
            }
        }

        private RegexTree ParseTree()
        {
            // Most callers to ParseAlternatingSequences are from group constructs.  As those
            // constructs will have already consumed the open paren, they don't want this sub-call
            // to consume through close-paren tokens as they want that token for themselves.
            // However, we're the topmost call and have not consumed an open paren.  And, we want
            // this call to consume all the way to the end, eating up excess close-paren tokens that
            // are encountered.
            var expression = this.ParseAlternatingSequences(consumeCloseParen: true);
            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(_currentToken.Kind == RegexKind.EndOfFile);

            var root = new RegexCompilationUnit(expression, _currentToken);

            var seenDiagnostics = new HashSet<EmbeddedDiagnostic>();
            var diagnostics = ArrayBuilder<EmbeddedDiagnostic>.GetInstance();
            CollectDiagnostics(root, seenDiagnostics, diagnostics);

            return new RegexTree(
                _lexer.Text, root, diagnostics.ToImmutableAndFree(),
                _captureNamesToSpan, _captureNumbersToSpan);
        }

        private static void CollectDiagnostics(
            RegexNode node, HashSet<EmbeddedDiagnostic> seenDiagnostics, ArrayBuilder<EmbeddedDiagnostic> diagnostics)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    CollectDiagnostics(child.Node, seenDiagnostics, diagnostics);
                }
                else
                {
                    var token = child.Token;
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddUniqueDiagnostics(seenDiagnostics, trivia.Diagnostics, diagnostics);
                    }

                    // We never place trailing trivia on regex tokens.
                    Debug.Assert(token.TrailingTrivia.IsEmpty);
                    AddUniqueDiagnostics(seenDiagnostics, token.Diagnostics, diagnostics);
                }
            }
        }

        /// <summary>
        /// It's very common to have duplicated diagnostics.  For example, consider "((". This will
        /// have two 'missing )' diagnostics, both at the end.  Reporting both isn't helpful, so we
        /// filter duplicates out here.
        /// </summary>
        private static void AddUniqueDiagnostics(
            HashSet<EmbeddedDiagnostic> seenDiagnostics, ImmutableArray<EmbeddedDiagnostic> from, ArrayBuilder<EmbeddedDiagnostic> to)
        {
            foreach (var diagnostic in from)
            {
                if (seenDiagnostics.Add(diagnostic))
                {
                    to.Add(diagnostic);
                }
            }
        }

        private RegexExpressionNode ParseAlternatingSequences(bool consumeCloseParen)
        {
            try
            {
                _recursionDepth++;
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                return ParseAlternatingSequencesWorker(consumeCloseParen);
            }
            finally
            {
                _recursionDepth--;
            }
        }

        /// <summary>
        /// Parses out code of the form: ...|...|...
        /// This is the type of code you have at the top level of a regex, or inside any grouping
        /// contruct.  Note that sequences can be empty in .NET regex.  i.e. the following is legal:
        /// 
        ///     ...||...
        /// 
        /// An empty sequence just means "match at every position in the test string".
        /// </summary>
        private RegexExpressionNode ParseAlternatingSequencesWorker(bool consumeCloseParen)
        {
            RegexExpressionNode current = ParseSequence(consumeCloseParen);

            while (_currentToken.Kind == RegexKind.BarToken)
            {
                // Trivia allowed between the | and the next token.
                current = new RegexAlternationNode(
                    current, ConsumeCurrentToken(allowTrivia: true), ParseSequence(consumeCloseParen));
            }

            return current;
        }

        private RegexSequenceNode ParseSequence(bool consumeCloseParen)
        {
            var builder = ArrayBuilder<RegexExpressionNode>.GetInstance();
            while (ShouldConsumeSequenceElement(consumeCloseParen))
            {
                var last = builder.Count == 0 ? null : builder.Last();
                builder.Add(ParsePrimaryExpressionAndQuantifiers(last));
            }

            // We wil commonly get tons of text nodes in a row.  For example, the
            // regex `abc` will be three text nodes in a row.  To help save on memory
            // try to merge that into one single text node.
            var sequence = ArrayBuilder<RegexExpressionNode>.GetInstance();
            MergeTextNodes(builder, sequence);
            builder.Free();

            return new RegexSequenceNode(sequence.ToImmutableAndFree());
        }

        private void MergeTextNodes(ArrayBuilder<RegexExpressionNode> list, ArrayBuilder<RegexExpressionNode> final)
        {
            // Iterate all the nodes in the sequence we have, adding them directly to
            // `final` if they are not text nodes.  If they are text nodes, we attempt
            // to keep merging them with any following text nodes as long as well.
            for (var index = 0; index < list.Count;)
            {
                var current = list[index];
                if (current.Kind != RegexKind.Text)
                {
                    // Not a text node.  Just add as-is, and move to the next node.
                    index++;
                    final.Add(current);
                    continue;
                }

                // Got a text node.  Try to combine it with all following nodes.
                index = MergeAndAddAdjacentTextNodes(list, final, index);
            }

            return;

            // local functions

            static int MergeAndAddAdjacentTextNodes(
                ArrayBuilder<RegexExpressionNode> list,
                ArrayBuilder<RegexExpressionNode> final,
                int index)
            {
                var startIndex = index;
                var startTextNode = (RegexTextNode)list[startIndex];

                // Keep walking forward as long as we hit text nodes and we can 
                // merge that text node with the previous text node.
                index++;
                var lastTextNode = startTextNode;
                for (; index < list.Count; index++)
                {
                    var currentNode = list[index];
                    if (!CanMerge(lastTextNode, currentNode))
                    {
                        // Hit something we couldn't merge with our last text node
                        // Break out and merge what we have so far.  'index' will
                        // be pointing at the right node for our caller.
                        break;
                    }

                    lastTextNode = (RegexTextNode)currentNode;
                }

                // If didn't have multiple text nodes in a row.  Just return the
                // starting node.  Otherwise, create one text node that has a token
                // that spans from the start of the first node to the end of the last node.
                final.Add(startTextNode == lastTextNode
                    ? startTextNode
                    : new RegexTextNode(CreateToken(
                        RegexKind.TextToken, startTextNode.TextToken.LeadingTrivia,
                        VirtualCharSequence.FromBounds(
                            startTextNode.TextToken.VirtualChars,
                            lastTextNode.TextToken.VirtualChars))));

                return index;
            }

            // Local functions
            static bool CanMerge(RegexTextNode lastNode, RegexExpressionNode next)
            {
                if (next.Kind == RegexKind.Text)
                {
                    var lastTextToken = lastNode.TextToken;
                    var nextTextToken = ((RegexTextNode)next).TextToken;

                    // Can't merge if the next text node has leading trivia. Also, conservatively 
                    // don't allow merging if there are diagnostics or values for these tokens.  
                    // We might be able to support that, but it's easier to not do anything that 
                    // might break an expectation someone might have downstream.                    /
                    if (lastTextToken is { Diagnostics: { Length: 0 }, Value: null } && nextTextToken is { Diagnostics: { Length: 0 }, Value: null, LeadingTrivia: { Length: 0 } })
                    {
                        lastTextToken.VirtualChars.AssertAdjacentTo(nextTextToken.VirtualChars);
                        return true;
                    }
                }

                return false;
            }
        }

        private bool ShouldConsumeSequenceElement(bool consumeCloseParen)
        {
            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                return false;
            }

            if (_currentToken.Kind == RegexKind.BarToken)
            {
                return false;
            }

            if (_currentToken.Kind == RegexKind.CloseParenToken)
            {
                return consumeCloseParen;
            }

            return true;
        }

        private RegexExpressionNode ParsePrimaryExpressionAndQuantifiers(RegexExpressionNode lastExpression)
        {
            var current = ParsePrimaryExpression(lastExpression);
            if (current.Kind == RegexKind.SimpleOptionsGrouping)
            {
                // Simple options (i.e. "(?i-x)" can't have quantifiers attached to them).
                return current;
            }

            switch (_currentToken.Kind)
            {
                case RegexKind.AsteriskToken: return ParseZeroOrMoreQuantifier(current);
                case RegexKind.PlusToken: return ParseOneOrMoreQuantifier(current);
                case RegexKind.QuestionToken: return ParseZeroOrOneQuantifier(current);
                case RegexKind.OpenBraceToken: return TryParseNumericQuantifier(current, _currentToken);
                default: return current;
            }
        }

        private RegexExpressionNode TryParseLazyQuantifier(RegexQuantifierNode quantifier)
        {
            if (_currentToken.Kind != RegexKind.QuestionToken)
            {
                return quantifier;
            }

            // Whitespace allowed after the question and the next sequence element.
            return new RegexLazyQuantifierNode(quantifier,
                ConsumeCurrentToken(allowTrivia: true));
        }

        private RegexExpressionNode ParseZeroOrMoreQuantifier(RegexPrimaryExpressionNode current)
        {
            // Whitespace allowed between the quantifier and the possible following ? or next sequence item.
            return TryParseLazyQuantifier(new RegexZeroOrMoreQuantifierNode(current, ConsumeCurrentToken(allowTrivia: true)));
        }

        private RegexExpressionNode ParseOneOrMoreQuantifier(RegexPrimaryExpressionNode current)
        {
            // Whitespace allowed between the quantifier and the possible following ? or next sequence item.
            return TryParseLazyQuantifier(new RegexOneOrMoreQuantifierNode(current, ConsumeCurrentToken(allowTrivia: true)));
        }

        private RegexExpressionNode ParseZeroOrOneQuantifier(RegexPrimaryExpressionNode current)
        {
            // Whitespace allowed between the quantifier and the possible following ? or next sequence item.
            return TryParseLazyQuantifier(new RegexZeroOrOneQuantifierNode(current, ConsumeCurrentToken(allowTrivia: true)));
        }

        private RegexExpressionNode TryParseNumericQuantifier(
            RegexPrimaryExpressionNode expression, RegexToken openBraceToken)
        {
            var start = _lexer.Position;

            if (!TryParseNumericQuantifierParts(
                    out var firstNumberToken,
                    out var commaToken,
                    out var secondNumberToken,
                    out var closeBraceToken))
            {
                _currentToken = openBraceToken;
                _lexer.Position = start;
                return expression;
            }

            var quantifier = CreateQuantifier(
                expression, openBraceToken, firstNumberToken, commaToken,
                secondNumberToken, closeBraceToken);

            return TryParseLazyQuantifier(quantifier);
        }

        private RegexQuantifierNode CreateQuantifier(
            RegexPrimaryExpressionNode expression,
            RegexToken openBraceToken, RegexToken firstNumberToken, RegexToken? commaToken,
            RegexToken? secondNumberToken, RegexToken closeBraceToken)
        {
            if (commaToken != null)
            {
                return secondNumberToken != null
                    ? new RegexClosedNumericRangeQuantifierNode(expression, openBraceToken, firstNumberToken, commaToken.Value, secondNumberToken.Value, closeBraceToken)
                    : (RegexQuantifierNode)new RegexOpenNumericRangeQuantifierNode(expression, openBraceToken, firstNumberToken, commaToken.Value, closeBraceToken);
            }

            return new RegexExactNumericQuantifierNode(expression, openBraceToken, firstNumberToken, closeBraceToken);
        }

        private bool TryParseNumericQuantifierParts(
            out RegexToken firstNumberToken, out RegexToken? commaToken,
            out RegexToken? secondNumberToken, out RegexToken closeBraceToken)
        {
            firstNumberToken = default;
            commaToken = default;
            secondNumberToken = default;
            closeBraceToken = default;

            var firstNumber = _lexer.TryScanNumber();
            if (firstNumber == null)
            {
                return false;
            }

            firstNumberToken = firstNumber.Value;

            // Nothing allowed between {x,n}
            ConsumeCurrentToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.CommaToken)
            {
                commaToken = _currentToken;

                var start = _lexer.Position;
                secondNumberToken = _lexer.TryScanNumber();

                if (secondNumberToken == null)
                {
                    // Nothing allowed between {x,n}
                    ResetToPositionAndConsumeCurrentToken(start, allowTrivia: false);
                }
                else
                {
                    var secondNumberTokenLocal = secondNumberToken.Value;

                    // Nothing allowed between {x,n}
                    ConsumeCurrentToken(allowTrivia: false);

                    var val1 = (int)firstNumberToken.Value;
                    var val2 = (int)secondNumberTokenLocal.Value;

                    if (val2 < val1)
                    {
                        secondNumberTokenLocal = secondNumberTokenLocal.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                            WorkspacesResources.Illegal_x_y_with_x_less_than_y,
                            secondNumberTokenLocal.GetSpan()));
                        secondNumberToken = secondNumberTokenLocal;
                    }
                }
            }

            if (_currentToken.Kind != RegexKind.CloseBraceToken)
            {
                return false;
            }

            // Whitespace allowed between the quantifier and the possible following ? or next sequence item.
            closeBraceToken = ConsumeCurrentToken(allowTrivia: true);
            return true;
        }

        private void ResetToPositionAndConsumeCurrentToken(int position, bool allowTrivia)
        {
            _lexer.Position = position;
            ConsumeCurrentToken(allowTrivia);
        }

        private RegexPrimaryExpressionNode ParsePrimaryExpression(RegexExpressionNode lastExpression)
        {
            switch (_currentToken.Kind)
            {
                case RegexKind.DotToken:
                    return ParseWildcard();
                case RegexKind.CaretToken:
                    return ParseStartAnchor();
                case RegexKind.DollarToken:
                    return ParseEndAnchor();
                case RegexKind.BackslashToken:
                    return ParseEscape(_currentToken, allowTriviaAfterEnd: true);
                case RegexKind.OpenBracketToken:
                    return ParseCharacterClass();
                case RegexKind.OpenParenToken:
                    return ParseGrouping();
                case RegexKind.CloseParenToken:
                    return ParseUnexpectedCloseParenToken();
                case RegexKind.OpenBraceToken:
                    return ParsePossibleUnexpectedNumericQuantifier(lastExpression);
                case RegexKind.AsteriskToken:
                case RegexKind.PlusToken:
                case RegexKind.QuestionToken:
                    return ParseUnexpectedQuantifier(lastExpression);
                default:
                    return ParseText();
            }
        }

        private RegexPrimaryExpressionNode ParsePossibleUnexpectedNumericQuantifier(RegexExpressionNode lastExpression)
        {
            // Native parser looks for something like {0,1} in a top level sequence and reports
            // an explicit error that that's not allowed.  However, something like {0, 1} is fine
            // and is treated as six textual tokens.
            var openBraceToken = _currentToken.With(kind: RegexKind.TextToken);
            var start = _lexer.Position;

            if (TryParseNumericQuantifierParts(
                    out _, out _, out _, out _))
            {
                // Report that a numeric quantifier isn't allowed here.
                CheckQuantifierExpression(lastExpression, ref openBraceToken);
            }

            // Started with { but wasn't a numeric quantifier.  This is totally legal and is just
            // a textual sequence.  Restart, scanning this token as a normal sequence element.
            ResetToPositionAndConsumeCurrentToken(start, allowTrivia: true);
            return new RegexTextNode(openBraceToken);
        }

        private RegexPrimaryExpressionNode ParseUnexpectedCloseParenToken()
        {
            var token = _currentToken.With(kind: RegexKind.TextToken).AddDiagnosticIfNone(
                new EmbeddedDiagnostic(WorkspacesResources.Too_many_close_parens, _currentToken.GetSpan()));

            // Technically, since an error occurred, we can do whatever we want here.  However,
            // the spirit of the native parser is that top level sequence elements are allowed
            // to have trivia.  So that's the behavior we mimic.
            ConsumeCurrentToken(allowTrivia: true);
            return new RegexTextNode(token);
        }

        private RegexPrimaryExpressionNode ParseText()
        {
            var token = ConsumeCurrentToken(allowTrivia: true);
            Debug.Assert(token.Value == null);

            // Allow trivia between this piece of text and the next sequence element
            return new RegexTextNode(token.With(kind: RegexKind.TextToken));
        }

        private RegexPrimaryExpressionNode ParseEndAnchor()
        {
            // Allow trivia between this anchor and the next sequence element
            return new RegexAnchorNode(RegexKind.EndAnchor, ConsumeCurrentToken(allowTrivia: true));
        }

        private RegexPrimaryExpressionNode ParseStartAnchor()
        {
            // Allow trivia between this anchor and the next sequence element
            return new RegexAnchorNode(RegexKind.StartAnchor, ConsumeCurrentToken(allowTrivia: true));
        }

        private RegexPrimaryExpressionNode ParseWildcard()
        {
            // Allow trivia between the . and the next sequence element
            return new RegexWildcardNode(ConsumeCurrentToken(allowTrivia: true));
        }

        private RegexGroupingNode ParseGrouping()
        {
            var start = _lexer.Position;

            // Check what immediately follows the (.  If we have (? it is processed specially.
            // However, we do not treat (? the same as ( ?
            var openParenToken = ConsumeCurrentToken(allowTrivia: false);

            switch (_currentToken.Kind)
            {
                case RegexKind.QuestionToken:
                    return ParseGroupQuestion(openParenToken, _currentToken);

                default:
                    // Wasn't (? just parse this as a normal group.
                    _lexer.Position = start;
                    return ParseSimpleGroup(openParenToken);
            }
        }

        private RegexToken ParseGroupingCloseParen()
        {
            switch (_currentToken.Kind)
            {
                case RegexKind.CloseParenToken:
                    // Grouping completed normally.  Allow trivia between it and the next sequence element.
                    return ConsumeCurrentToken(allowTrivia: true);

                default:
                    return CreateMissingToken(RegexKind.CloseParenToken).AddDiagnosticIfNone(
                        new EmbeddedDiagnostic(WorkspacesResources.Not_enough_close_parens, GetTokenStartPositionSpan(_currentToken)));
            }
        }

        private RegexSimpleGroupingNode ParseSimpleGroup(RegexToken openParenToken)
            => new RegexSimpleGroupingNode(
                openParenToken, ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());

        private RegexExpressionNode ParseGroupingEmbeddedExpression(RegexOptions embeddedOptions)
        {
            // Save and restore options when we go into, and pop out of a group node.
            var currentOptions = _options;
            _options = embeddedOptions;

            // We're parsing the embedded sequence inside the current group.  As this is a sequence
            // we want to allow trivia between the current token we're on, and the first token
            // of the embedded sequence.
            ConsumeCurrentToken(allowTrivia: true);

            // When parsing out the sequence don't grab the close paren, that will be for our caller
            // to get.
            var expression = this.ParseAlternatingSequences(consumeCloseParen: false);
            _options = currentOptions;
            return expression;
        }

        private TextSpan GetTokenSpanIncludingEOF(RegexToken token)
            => token.Kind == RegexKind.EndOfFile
                ? GetTokenStartPositionSpan(token)
                : token.GetSpan();

        private TextSpan GetTokenStartPositionSpan(RegexToken token)
        {
            return token.Kind == RegexKind.EndOfFile
                ? new TextSpan(_lexer.Text.Last().Span.End, 0)
                : new TextSpan(token.VirtualChars[0].Span.Start, 0);
        }

        private RegexGroupingNode ParseGroupQuestion(RegexToken openParenToken, RegexToken questionToken)
        {
            var optionsToken = _lexer.TryScanOptions();
            if (optionsToken != null)
            {
                return ParseOptionsGroupingNode(openParenToken, questionToken, optionsToken.Value);
            }

            var afterQuestionPos = _lexer.Position;

            // Lots of possible options when we see (?.  Look at the immediately following character
            // (without any allowed spaces) to decide what to parse out next.
            ConsumeCurrentToken(allowTrivia: false);
            switch (_currentToken.Kind)
            {
                case RegexKind.LessThanToken:
                    // (?<=...) or (?<!...) or (?<...>...) or (?<...-...>...)
                    return ParseLookbehindOrNamedCaptureOrBalancingGrouping(openParenToken, questionToken);

                case RegexKind.SingleQuoteToken:
                    //  (?'...'...) or (?'...-...'...)
                    return ParseNamedCaptureOrBalancingGrouping(
                        openParenToken, questionToken, _currentToken);

                case RegexKind.OpenParenToken:
                    // alternation construct (?(...) | )
                    return ParseConditionalGrouping(openParenToken, questionToken);

                case RegexKind.ColonToken:
                    return ParseNonCapturingGroupingNode(openParenToken, questionToken);

                case RegexKind.EqualsToken:
                    return ParsePositiveLookaheadGrouping(openParenToken, questionToken);

                case RegexKind.ExclamationToken:
                    return ParseNegativeLookaheadGrouping(openParenToken, questionToken);

                case RegexKind.GreaterThanToken:
                    return ParseNonBacktrackingGrouping(openParenToken, questionToken);

                default:
                    if (_currentToken.Kind != RegexKind.CloseParenToken)
                    {
                        // Native parser reports "Unrecognized grouping construct", *except* for (?)
                        openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                            WorkspacesResources.Unrecognized_grouping_construct,
                            openParenToken.GetSpan()));
                    }

                    break;
            }

            // (?)
            // Parse this as a normal group. The question will immediately error as it's a 
            // quantifier not following anything.
            _lexer.Position = afterQuestionPos - 1;
            return ParseSimpleGroup(openParenToken);
        }

        private RegexConditionalGroupingNode ParseConditionalGrouping(RegexToken openParenToken, RegexToken questionToken)
        {
            var innerOpenParenToken = _currentToken;
            var afterInnerOpenParen = _lexer.Position;

            var captureToken = _lexer.TryScanNumberOrCaptureName();
            if (captureToken == null)
            {
                return ParseConditionalExpressionGrouping(openParenToken, questionToken, innerOpenParenToken);
            }

            var capture = captureToken.Value;

            RegexToken innerCloseParenToken;
            if (capture.Kind == RegexKind.NumberToken)
            {
                // If it's a numeric group, it has to be immediately followed by a ) and the
                // numeric reference has to exist.
                //
                // That means that (?(4 ) is not treated as an embedded expression but as an
                // error.  This is different from (?(a ) which will be treated as an embedded
                // expression, and different from (?(a) will be treated as an embedded
                // expression or capture group depending on if 'a' is a existing capture name.

                ConsumeCurrentToken(allowTrivia: false);
                if (_currentToken.Kind == RegexKind.CloseParenToken)
                {
                    innerCloseParenToken = _currentToken;
                    if (!HasCapture((int)capture.Value))
                    {
                        capture = capture.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                            WorkspacesResources.Reference_to_undefined_group,
                            capture.GetSpan()));
                    }
                }
                else
                {
                    innerCloseParenToken = CreateMissingToken(RegexKind.CloseParenToken);
                    capture = capture.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Malformed,
                        capture.GetSpan()));
                    MoveBackBeforePreviousScan();
                }
            }
            else
            {
                // If it's a capture name, it's ok if that capture doesn't exist.  In that case we
                // will just treat this as an conditional expression.
                if (!HasCapture((string)capture.Value))
                {
                    _lexer.Position = afterInnerOpenParen;
                    return ParseConditionalExpressionGrouping(openParenToken, questionToken, innerOpenParenToken);
                }

                // Capture name existed.  For this to be a capture grouping it exactly has to
                // match (?(a)   anything other than a close paren after the ) will make this
                // into a conditional expression.
                ConsumeCurrentToken(allowTrivia: false);
                if (_currentToken.Kind != RegexKind.CloseParenToken)
                {
                    _lexer.Position = afterInnerOpenParen;
                    return ParseConditionalExpressionGrouping(openParenToken, questionToken, innerOpenParenToken);
                }

                innerCloseParenToken = _currentToken;
            }

            // Was (?(name) or (?(num)  and name/num was a legal capture name.  Parse
            // this out as a conditional grouping.  Because we're going to be parsing out
            // an embedded sequence, allow trivia before the first element.
            ConsumeCurrentToken(allowTrivia: true);
            var result = ParseConditionalGroupingResult();

            return new RegexConditionalCaptureGroupingNode(
                openParenToken, questionToken,
                innerOpenParenToken, capture, innerCloseParenToken,
                result, ParseGroupingCloseParen());
        }

        private bool HasCapture(int value)
            => _captureNumbersToSpan.ContainsKey(value);

        private bool HasCapture(string value)
            => _captureNamesToSpan.ContainsKey(value);

        private void MoveBackBeforePreviousScan()
        {
            if (_currentToken.Kind != RegexKind.EndOfFile)
            {
                // Move back to un-consume whatever we just consumed.
                _lexer.Position--;
            }
        }

        private RegexConditionalGroupingNode ParseConditionalExpressionGrouping(
            RegexToken openParenToken, RegexToken questionToken, RegexToken innerOpenParenToken)
        {
            // Reproduce very specific errors the .NET regex parser looks for.  Technically,
            // we would error out in these cases no matter what.  However, it means we can
            // stringently enforce that our parser produces the same errors as the native one.
            //
            // Move back before the (
            _lexer.Position--;
            if (_lexer.IsAt("(?#"))
            {
                var pos = _lexer.Position;
                var comment = _lexer.ScanComment(options: default);
                _lexer.Position = pos;

                if (comment.Value.Diagnostics.Length > 0)
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(comment.Value.Diagnostics[0]);
                }
                else
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Alternation_conditions_cannot_be_comments,
                        openParenToken.GetSpan()));
                }
            }
            else if (_lexer.IsAt("(?'"))
            {
                openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Alternation_conditions_do_not_capture_and_cannot_be_named,
                    openParenToken.GetSpan()));
            }
            else if (_lexer.IsAt("(?<"))
            {
                if (!_lexer.IsAt("(?<!") &&
                    !_lexer.IsAt("(?<="))
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Alternation_conditions_do_not_capture_and_cannot_be_named,
                        openParenToken.GetSpan()));
                }
            }

            // Consume the ( once more.
            ConsumeCurrentToken(allowTrivia: false);
            Debug.Assert(_currentToken.Kind == RegexKind.OpenParenToken);

            // Parse out the grouping that starts with the second open paren in (?(
            // this will get us to (?(...)
            var grouping = ParseGrouping();

            // Now parse out the embedded expression that follows that.  this will get us to
            // (?(...)...
            var result = ParseConditionalGroupingResult();

            // Finally, grab the close paren and produce (?(...)...)
            return new RegexConditionalExpressionGroupingNode(
                openParenToken, questionToken,
                grouping, result, ParseGroupingCloseParen());
        }

        private RegexExpressionNode ParseConditionalGroupingResult()
        {
            var currentOptions = _options;
            var result = this.ParseAlternatingSequences(consumeCloseParen: false);
            _options = currentOptions;

            result = CheckConditionalAlternation(result);
            return result;
        }

        private RegexExpressionNode CheckConditionalAlternation(RegexExpressionNode result)
        {
            if (result is RegexAlternationNode topAlternation &&
                topAlternation.Left is RegexAlternationNode innerAlternation)
            {
                return new RegexAlternationNode(
                    topAlternation.Left,
                    topAlternation.BarToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Too_many_bars_in_conditional_grouping,
                        topAlternation.BarToken.GetSpan())),
                    topAlternation.Right);
            }

            return result;
        }

        private RegexGroupingNode ParseLookbehindOrNamedCaptureOrBalancingGrouping(
            RegexToken openParenToken, RegexToken questionToken)
        {
            var start = _lexer.Position;

            // We have  (?<  Look for  (?<=  or  (?<!
            var lessThanToken = ConsumeCurrentToken(allowTrivia: false);

            switch (_currentToken.Kind)
            {
                case RegexKind.EqualsToken:
                    return new RegexPositiveLookbehindGroupingNode(
                        openParenToken, questionToken, lessThanToken, _currentToken,
                        ParseGroupingEmbeddedExpression(_options | RegexOptions.RightToLeft), ParseGroupingCloseParen());

                case RegexKind.ExclamationToken:
                    return new RegexNegativeLookbehindGroupingNode(
                        openParenToken, questionToken, lessThanToken, _currentToken,
                        ParseGroupingEmbeddedExpression(_options | RegexOptions.RightToLeft), ParseGroupingCloseParen());

                default:
                    // Didn't have a lookbehind group.  Parse out as  (?<...>  or  (?<...-...>
                    _lexer.Position = start;
                    return ParseNamedCaptureOrBalancingGrouping(openParenToken, questionToken, lessThanToken);
            }
        }

        private RegexGroupingNode ParseNamedCaptureOrBalancingGrouping(
            RegexToken openParenToken, RegexToken questionToken, RegexToken openToken)
        {
            if (_lexer.Position == _lexer.Text.Length)
            {
                openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Unrecognized_grouping_construct,
                    GetSpan(openParenToken, openToken)));
            }

            // (?<...>...) or (?<...-...>...)
            // (?'...'...) or (?'...-...'...)
            var captureToken = _lexer.TryScanNumberOrCaptureName();
            if (captureToken == null)
            {
                // Can't have any trivia between the elements in this grouping header.
                ConsumeCurrentToken(allowTrivia: false);
                captureToken = CreateMissingToken(RegexKind.CaptureNameToken);

                if (_currentToken.Kind == RegexKind.MinusToken)
                {
                    return ParseBalancingGrouping(
                        openParenToken, questionToken, openToken, captureToken.Value);
                }
                else
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character,
                        GetTokenSpanIncludingEOF(_currentToken)));

                    // If we weren't at the end of the text, go back to before whatever character
                    // we just consumed.
                    MoveBackBeforePreviousScan();
                }
            }

            var capture = captureToken.Value;
            if (capture.Kind == RegexKind.NumberToken && (int)capture.Value == 0)
            {
                capture = capture.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Capture_number_cannot_be_zero,
                    capture.GetSpan()));
            }

            // Can't have any trivia between the elements in this grouping header.
            ConsumeCurrentToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.MinusToken)
            {
                // Have  (?<...-  parse out the balancing group form.
                return ParseBalancingGrouping(
                    openParenToken, questionToken,
                    openToken, capture);
            }

            var closeToken = ParseCaptureGroupingCloseToken(ref openParenToken, openToken);

            return new RegexCaptureGroupingNode(
                openParenToken, questionToken,
                openToken, capture, closeToken,
                ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());
        }

        private RegexToken ParseCaptureGroupingCloseToken(ref RegexToken openParenToken, RegexToken openToken)
        {
            if ((openToken.Kind == RegexKind.LessThanToken && _currentToken.Kind == RegexKind.GreaterThanToken) ||
                (openToken.Kind == RegexKind.SingleQuoteToken && _currentToken.Kind == RegexKind.SingleQuoteToken))
            {
                return _currentToken;
            }

            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Unrecognized_grouping_construct,
                    GetSpan(openParenToken, openToken)));
            }
            else
            {
                openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character,
                    _currentToken.GetSpan()));

                // Rewind to where we were before seeing this bogus character.
                _lexer.Position--;
            }

            return CreateMissingToken(
                openToken.Kind == RegexKind.LessThanToken
                    ? RegexKind.GreaterThanToken : RegexKind.SingleQuoteToken);
        }

        private RegexBalancingGroupingNode ParseBalancingGrouping(
            RegexToken openParenToken, RegexToken questionToken,
            RegexToken openToken, RegexToken firstCapture)
        {
            var minusToken = _currentToken;
            var secondCapture = _lexer.TryScanNumberOrCaptureName();
            if (secondCapture == null)
            {
                // Invalid group name: Group names must begin with a word character
                ConsumeCurrentToken(allowTrivia: false);

                openParenToken = openParenToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character,
                    GetTokenSpanIncludingEOF(_currentToken)));

                // If we weren't at the end of the text, go back to before whatever character
                // we just consumed.
                MoveBackBeforePreviousScan();
                secondCapture = CreateMissingToken(RegexKind.CaptureNameToken);
            }

            var second = secondCapture.Value;
            CheckCapture(ref second);

            // Can't have any trivia between the elements in this grouping header.
            ConsumeCurrentToken(allowTrivia: false);
            var closeToken = ParseCaptureGroupingCloseToken(ref openParenToken, openToken);

            return new RegexBalancingGroupingNode(
                openParenToken, questionToken,
                openToken, firstCapture, minusToken, second, closeToken,
                ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());
        }

        private void CheckCapture(ref RegexToken captureToken)
        {
            if (captureToken.IsMissing)
            {
                // Don't need to check for a synthesized error capture token.
                return;
            }

            if (captureToken.Kind == RegexKind.NumberToken)
            {
                var val = (int)captureToken.Value;
                if (!HasCapture(val))
                {
                    captureToken = captureToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        string.Format(WorkspacesResources.Reference_to_undefined_group_number_0, val),
                        captureToken.GetSpan()));
                }
            }
            else
            {
                var val = (string)captureToken.Value;
                if (!HasCapture(val))
                {
                    captureToken = captureToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        string.Format(WorkspacesResources.Reference_to_undefined_group_name_0, val),
                        captureToken.GetSpan()));
                }
            }
        }

        private RegexNonCapturingGroupingNode ParseNonCapturingGroupingNode(RegexToken openParenToken, RegexToken questionToken)
            => new RegexNonCapturingGroupingNode(
                openParenToken, questionToken, _currentToken,
                ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());

        private RegexPositiveLookaheadGroupingNode ParsePositiveLookaheadGrouping(RegexToken openParenToken, RegexToken questionToken)
            => new RegexPositiveLookaheadGroupingNode(
                openParenToken, questionToken, _currentToken,
                ParseGroupingEmbeddedExpression(_options & ~RegexOptions.RightToLeft), ParseGroupingCloseParen());

        private RegexNegativeLookaheadGroupingNode ParseNegativeLookaheadGrouping(RegexToken openParenToken, RegexToken questionToken)
            => new RegexNegativeLookaheadGroupingNode(
                openParenToken, questionToken, _currentToken,
                ParseGroupingEmbeddedExpression(_options & ~RegexOptions.RightToLeft), ParseGroupingCloseParen());

        private RegexNonBacktrackingGroupingNode ParseNonBacktrackingGrouping(RegexToken openParenToken, RegexToken questionToken)
            => new RegexNonBacktrackingGroupingNode(
                openParenToken, questionToken, _currentToken,
                ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());

        private RegexGroupingNode ParseOptionsGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken)
        {
            // Only (?opts:...) or (?opts) are allowed.  After the opts must be a : or )
            ConsumeCurrentToken(allowTrivia: false);
            switch (_currentToken.Kind)
            {
                case RegexKind.CloseParenToken:
                    // Allow trivia after the options and the next element in the sequence.
                    _options = GetNewOptionsFromToken(_options, optionsToken);
                    return new RegexSimpleOptionsGroupingNode(
                        openParenToken, questionToken, optionsToken,
                        ConsumeCurrentToken(allowTrivia: true));

                case RegexKind.ColonToken:
                    return ParseNestedOptionsGroupingNode(openParenToken, questionToken, optionsToken);

                default:
                    return new RegexSimpleOptionsGroupingNode(
                        openParenToken, questionToken, optionsToken,
                        CreateMissingToken(RegexKind.CloseParenToken).AddDiagnosticIfNone(
                            new EmbeddedDiagnostic(WorkspacesResources.Unrecognized_grouping_construct, openParenToken.GetSpan())));
            }
        }

        private RegexNestedOptionsGroupingNode ParseNestedOptionsGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken)
            => new RegexNestedOptionsGroupingNode(
                openParenToken, questionToken, optionsToken, _currentToken,
                ParseGroupingEmbeddedExpression(GetNewOptionsFromToken(_options, optionsToken)), ParseGroupingCloseParen());

        private static bool IsTextChar(RegexToken currentToken, char ch)
            => currentToken.Kind == RegexKind.TextToken && currentToken.VirtualChars.Length == 1 && currentToken.VirtualChars[0].Char == ch;

        private static RegexOptions GetNewOptionsFromToken(RegexOptions currentOptions, RegexToken optionsToken)
        {
            var copy = currentOptions;
            var on = true;
            foreach (var ch in optionsToken.VirtualChars)
            {
                switch (ch.Char)
                {
                    case '-': on = false; break;
                    case '+': on = true; break;
                    default:
                        var newOption = OptionFromCode(ch);
                        if (on)
                        {
                            copy |= newOption;
                        }
                        else
                        {
                            copy &= ~newOption;
                        }
                        break;
                }
            }

            return copy;
        }

        private static RegexOptions OptionFromCode(VirtualChar ch)
        {
            switch (ch)
            {
                case 'i': case 'I': return RegexOptions.IgnoreCase;
                case 'm': case 'M': return RegexOptions.Multiline;
                case 'n': case 'N': return RegexOptions.ExplicitCapture;
                case 's': case 'S': return RegexOptions.Singleline;
                case 'x': case 'X': return RegexOptions.IgnorePatternWhitespace;
                default:
                    throw new InvalidOperationException();
            }
        }

        private RegexBaseCharacterClassNode ParseCharacterClass()
        {
            // Note: ScanCharClass is one of the strangest function in the .NET regex parser. Code
            // for it is here:
            // https://github.com/dotnet/corefx/blob/6ae0da1563e6e701bac61012c62ede8f8737f065/src/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexParser.cs#L498
            //
            // It has certain behaviors that were probably not intentional, but which we try to
            // replicate.  Specifically, it looks like it was *intended* to just read components
            // like simple characters ('a'), char-class-escape ('\s' and the like), ranges
            // ('component-component'), and subtractions ('-[charclass]').
            //
            // And, it *looks* like it intended that if it ran into a range, it would check that the
            // components on the left and right of the '-' made sense (i.e. you could have 'a-b' but
            // not 'b-a').
            //
            // *However*, the way it is actually written, it does not have that behavior.  Instead,
            // what it ends up doing is subtly different.  Specifically, in this switch:
            // https://github.com/dotnet/corefx/blob/6ae0da1563e6e701bac61012c62ede8f8737f065/src/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexParser.cs#L531
            //
            // In this switch, if it encounters a '\-' it immediately 'continues', effectively
            // ignoring that character on the right side of a character range.  So, if you had
            // ```[#-\-b]```, then this *should* be treated as the character class containing
            // the range of character from '#' to '-', unioned with the character 'b'.  However,
            // .NET will interpret this as the character class containing the range of characters
            // from '#' to 'b'.  We follow .NET here to keep our errors in sync with them.
            //
            // See the comment about this in ParseRightSideOfCharacterClassRange

            var openBracketToken = _currentToken;
            Debug.Assert(openBracketToken.Kind == RegexKind.OpenBracketToken);
            var caretToken = CreateMissingToken(RegexKind.CaretToken);
            var closeBracketToken = CreateMissingToken(RegexKind.CloseBracketToken);

            // trivia is not allowed anywhere in a character class
            ConsumeCurrentToken(allowTrivia: false);
            if (_currentToken.Kind == RegexKind.CaretToken)
            {
                caretToken = _currentToken;
            }
            else
            {
                MoveBackBeforePreviousScan();
            }

            // trivia is not allowed anywhere in a character class
            ConsumeCurrentToken(allowTrivia: false);

            var builder = ArrayBuilder<RegexExpressionNode>.GetInstance();
            while (_currentToken.Kind != RegexKind.EndOfFile)
            {
                Debug.Assert(_currentToken.VirtualChars.Length == 1);

                if (_currentToken.Kind == RegexKind.CloseBracketToken && builder.Count > 0)
                {
                    // Allow trivia after the character class, and whatever is next in the sequence.
                    closeBracketToken = ConsumeCurrentToken(allowTrivia: true);
                    break;
                }

                ParseCharacterClassComponents(builder);
            }

            // We wil commonly get tons of text nodes in a row.  For example, the
            // regex `[abc]` will be three text nodes in a row.  To help save on memory
            // try to merge that into one single text node.
            var contents = ArrayBuilder<RegexExpressionNode>.GetInstance();
            MergeTextNodes(builder, contents);
            builder.Free();

            if (closeBracketToken.IsMissing)
            {
                closeBracketToken = closeBracketToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Unterminated_character_class_set,
                    GetTokenStartPositionSpan(_currentToken)));
            }

            var components = new RegexSequenceNode(contents.ToImmutableAndFree());
            return caretToken.IsMissing
                ? (RegexBaseCharacterClassNode)new RegexCharacterClassNode(openBracketToken, components, closeBracketToken)
                : new RegexNegatedCharacterClassNode(openBracketToken, caretToken, components, closeBracketToken);
        }

        private void ParseCharacterClassComponents(ArrayBuilder<RegexExpressionNode> components)
        {
            var left = ParseSingleCharacterClassComponent(isFirst: components.Count == 0, afterRangeMinus: false);
            if (left.Kind == RegexKind.CharacterClassEscape ||
                left.Kind == RegexKind.CategoryEscape ||
                IsEscapedMinus(left))
            {
                // \s or \p{Lu} or \- on the left of a minus doesn't start a range. If there is a following
                // minus, it's just treated textually.
                components.Add(left);
                return;
            }

            if (_currentToken.Kind == RegexKind.MinusToken && !_lexer.IsAt("]"))
            {
                // trivia is not allowed anywhere in a character class
                var minusToken = ConsumeCurrentToken(allowTrivia: false);

                if (_currentToken.Kind == RegexKind.OpenBracketToken)
                {
                    components.Add(left);
                    components.Add(ParseCharacterClassSubtractionNode(minusToken));
                }
                else
                {
                    var right = ParseRightSideOfCharacterClassRange();

                    if (TryGetRangeComponentValue(left, out var leftCh) &&
                        TryGetRangeComponentValue(right, out var rightCh) &&
                        leftCh > rightCh)
                    {
                        minusToken = minusToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                            WorkspacesResources.x_y_range_in_reverse_order,
                            minusToken.GetSpan()));
                    }

                    components.Add(new RegexCharacterClassRangeNode(left, minusToken, right));
                }
            }
            else
            {
                components.Add(left);
            }
        }

        private bool IsEscapedMinus(RegexNode node)
            => node is RegexSimpleEscapeNode simple && IsTextChar(simple.TypeToken, '-');

        private bool TryGetRangeComponentValue(RegexExpressionNode component, out char ch)
        {
            // Don't bother examining the component if it has any errors already.  This also means
            // we don't have to worry about running into invalid escape sequences and the like.
            if (!HasProblem(component))
            {
                return TryGetRangeComponentValueWorker(component, out ch);
            }

            ch = default;
            return false;
        }

        private bool TryGetRangeComponentValueWorker(RegexNode component, out char ch)
        {
            switch (component.Kind)
            {
                case RegexKind.SimpleEscape:
                    var escapeNode = (RegexSimpleEscapeNode)component;
                    ch = MapEscapeChar(escapeNode.TypeToken.VirtualChars[0]);
                    return true;

                case RegexKind.ControlEscape:
                    var controlEscape = (RegexControlEscapeNode)component;
                    var controlCh = controlEscape.ControlToken.VirtualChars[0].Char;

                    // \ca interpreted as \cA
                    if (controlCh >= 'a' && controlCh <= 'z')
                    {
                        controlCh -= (char)('a' - 'A');
                    }

                    // The control characters have values mapping from the A-Z range to numeric
                    // values 1-26.  So, to map that, we subtract 'A' from the value (which would
                    // give us 0-25) and then add '1' back to it.
                    ch = (char)(controlCh - 'A' + 1);
                    return true;

                case RegexKind.OctalEscape:
                    ch = GetCharValue(((RegexOctalEscapeNode)component).OctalText, withBase: 8);
                    return true;

                case RegexKind.HexEscape:
                    ch = GetCharValue(((RegexHexEscapeNode)component).HexText, withBase: 16);
                    return true;

                case RegexKind.UnicodeEscape:
                    ch = GetCharValue(((RegexUnicodeEscapeNode)component).HexText, withBase: 16);
                    return true;

                case RegexKind.PosixProperty:
                    // When the native parser sees [:...:] it treats this as if it just saw '[' and skipped the 
                    // rest.
                    ch = '[';
                    return true;

                case RegexKind.Text:
                    ch = ((RegexTextNode)component).TextToken.VirtualChars[0];
                    return true;

                case RegexKind.Sequence:
                    var sequence = (RegexSequenceNode)component;
#if DEBUG
                    Debug.Assert(sequence.ChildCount > 0);
                    for (int i = 0, n = sequence.ChildCount - 1; i < n; i++)
                    {
                        Debug.Assert(IsEscapedMinus(sequence.ChildAt(i).Node));
                    }
#endif

                    var last = sequence.ChildAt(sequence.ChildCount - 1).Node;
                    if (IsEscapedMinus(last))
                    {
                        break;
                    }

                    return TryGetRangeComponentValueWorker(last, out ch);
            }

            ch = default;
            return false;
        }

        private char GetCharValue(RegexToken hexText, int withBase)
        {
            unchecked
            {
                var total = 0;
                foreach (var vc in hexText.VirtualChars)
                {
                    total *= withBase;
                    total += HexValue(vc.Char);
                }

                return (char)total;
            }
        }

        private int HexValue(char ch)
        {
            Debug.Assert(RegexLexer.IsHexChar(ch));
            unchecked
            {
                var temp = (uint)(ch - '0');
                if (temp <= 9)
                {
                    return (int)temp;
                }

                temp = (uint)(ch - 'a');
                if (temp <= 5)
                {
                    return (int)(temp + 10);
                }

                temp = (uint)(ch - 'A');
                if (temp <= 5)
                {
                    return (int)(temp + 10);
                }
            }

            throw new InvalidOperationException();
        }

        private bool HasProblem(RegexNodeOrToken component)
        {
            if (component.IsNode)
            {
                foreach (var child in component.Node)
                {
                    if (HasProblem(child))
                    {
                        return true;
                    }
                }
            }
            else
            {
                var token = component.Token;
                if (token.IsMissing ||
                    token.Diagnostics.Length > 0)
                {
                    return true;
                }

                foreach (var trivia in token.LeadingTrivia)
                {
                    if (trivia.Diagnostics.Length > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private RegexExpressionNode ParseRightSideOfCharacterClassRange()
        {
            // Parsing the right hand side of a - is extremely strange (and most likely buggy) in
            // the .NET parser. Specifically, the .NET parser will still consider itself on the
            // right side no matter how many escaped dashes it sees.  So, for example, the following
            // is legal [a-\-] (even though \- is less than 'a'). Similarly, the following are
            // *illegal* [b-\-a] and [b-\-\-a].  That's because the range that is checked is
            // actually "b-a", even though it has all the \- escapes in the middle.
            //
            // This is tracked with: https://github.com/dotnet/corefx/issues/31786

            var first = ParseSingleCharacterClassComponent(isFirst: false, afterRangeMinus: true);
            if (!IsEscapedMinus(first))
            {
                return first;
            }

            var builder = ArrayBuilder<RegexExpressionNode>.GetInstance();
            builder.Add(first);

            while (IsEscapedMinus(builder.Last()) && _currentToken.Kind != RegexKind.CloseBracketToken)
            {
                builder.Add(ParseSingleCharacterClassComponent(isFirst: false, afterRangeMinus: true));
            }

            return new RegexSequenceNode(builder.ToImmutableAndFree());
        }

        private RegexPrimaryExpressionNode ParseSingleCharacterClassComponent(bool isFirst, bool afterRangeMinus)
        {
            if (_currentToken.Kind == RegexKind.BackslashToken && _lexer.Position < _lexer.Text.Length)
            {
                var backslashToken = _currentToken;
                var afterSlash = _lexer.Position;

                // trivia is not allowed anywhere in a character class, and definitely not between
                // a \ and the following character.
                ConsumeCurrentToken(allowTrivia: false);
                Debug.Assert(_currentToken.VirtualChars.Length == 1);

                var nextChar = _currentToken.VirtualChars[0].Char;
                switch (nextChar)
                {
                    case 'D':
                    case 'd':
                    case 'S':
                    case 's':
                    case 'W':
                    case 'w':
                    case 'p':
                    case 'P':
                        if (afterRangeMinus)
                        {
                            backslashToken = backslashToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                                string.Format(WorkspacesResources.Cannot_include_class_0_in_character_range, nextChar),
                                GetSpan(backslashToken, _currentToken)));
                        }

                        // move back before the character we just scanned.
                        // trivia is not allowed anywhere in a character class.

                        // The above list are character class and category escapes.  ParseEscape can
                        // handle both of those, so we just defer to it.
                        _lexer.Position--;
                        return ParseEscape(backslashToken, allowTriviaAfterEnd: false);

                    case '-':
                        // trivia is not allowed anywhere in a character class.

                        // We just let the basic consumption code pull out a token for us, we then
                        // convert that to text since we treat all characters after the - as text no
                        // matter what.
                        return new RegexSimpleEscapeNode(
                            backslashToken, ConsumeCurrentToken(allowTrivia: false).With(kind: RegexKind.TextToken));

                    default:
                        // trivia is not allowed anywhere in a character class.

                        // Note: it is very intentional that we're calling ParseCharEscape and not
                        // ParseEscape.  Normal escapes are not interpreted the same way inside a
                        // character class.  For example \b is not an anchor in a character class.
                        // And things like \k'...' are not k-captures, etc. etc.  
                        _lexer.Position--;
                        return ParseCharEscape(backslashToken, allowTriviaAfterEnd: false);
                }
            }

            if (!afterRangeMinus &&
                !isFirst &&
                _currentToken.Kind == RegexKind.MinusToken &&
                _lexer.IsAt("["))
            {
                // have a trailing subtraction.
                // trivia is not allowed anywhere in a character class
                return ParseCharacterClassSubtractionNode(
                    ConsumeCurrentToken(allowTrivia: false));
            }

            // From the .NET regex code:
            // This is code for Posix style properties - [:Ll:] or [:IsTibetan:].
            // It currently doesn't do anything other than skip the whole thing!
            if (!afterRangeMinus && _currentToken.Kind == RegexKind.OpenBracketToken && _lexer.IsAt(":"))
            {
                var beforeBracketPos = _lexer.Position - 1;
                // trivia is not allowed anywhere in a character class
                ConsumeCurrentToken(allowTrivia: false);

                var captureName = _lexer.TryScanCaptureName();
                if (captureName.HasValue && _lexer.IsAt(":]"))
                {
                    _lexer.Position += 2;
                    var textChars = _lexer.GetSubPattern(beforeBracketPos, _lexer.Position);
                    var token = CreateToken(RegexKind.TextToken, ImmutableArray<RegexTrivia>.Empty, textChars);

                    // trivia is not allowed anywhere in a character class
                    ConsumeCurrentToken(allowTrivia: false);
                    return new RegexPosixPropertyNode(token);
                }
                else
                {
                    // Reset to back where we were.
                    // trivia is not allowed anywhere in a character class
                    _lexer.Position = beforeBracketPos;
                    ConsumeCurrentToken(allowTrivia: false);
                    Debug.Assert(_currentToken.Kind == RegexKind.OpenBracketToken);
                }
            }

            // trivia is not allowed anywhere in a character class
            return new RegexTextNode(
                ConsumeCurrentToken(allowTrivia: false).With(kind: RegexKind.TextToken));
        }

        private RegexPrimaryExpressionNode ParseCharacterClassSubtractionNode(RegexToken minusToken)
        {
            var charClass = ParseCharacterClass();

            if (_currentToken.Kind != RegexKind.CloseBracketToken && _currentToken.Kind != RegexKind.EndOfFile)
            {
                minusToken = minusToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.A_subtraction_must_be_the_last_element_in_a_character_class,
                    GetTokenStartPositionSpan(minusToken)));
            }

            return new RegexCharacterClassSubtractionNode(minusToken, charClass);
        }

        /// <summary>
        /// Parses out an escape sequence.  Escape sequences are allowed in top level sequences
        /// and in character classes.  In a top level sequence trivia will be allowed afterwards,
        /// but in a character class trivia is not allowed afterwards.
        /// </summary>
        private RegexEscapeNode ParseEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');

            // No spaces between \ and next char.
            ConsumeCurrentToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                backslashToken = backslashToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Illegal_backslash_at_end_of_pattern,
                    backslashToken.GetSpan()));
                return new RegexSimpleEscapeNode(backslashToken, CreateMissingToken(RegexKind.TextToken));
            }

            Debug.Assert(_currentToken.VirtualChars.Length == 1);
            switch (_currentToken.VirtualChars[0].Char)
            {
                case 'b':
                case 'B':
                case 'A':
                case 'G':
                case 'Z':
                case 'z':
                    return new RegexAnchorEscapeNode(
                        backslashToken, ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd));

                case 'w':
                case 'W':
                case 's':
                case 'S':
                case 'd':
                case 'D':
                    return new RegexCharacterClassEscapeNode(
                        backslashToken, ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd));

                case 'p':
                case 'P':
                    return ParseCategoryEscape(backslashToken, allowTriviaAfterEnd);
            }

            // Move back to after the backslash
            _lexer.Position--;
            return ParseBasicBackslash(backslashToken, allowTriviaAfterEnd);
        }

        private RegexEscapeNode ParseBasicBackslash(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');

            // No spaces between \ and next char.
            ConsumeCurrentToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                backslashToken = backslashToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Illegal_backslash_at_end_of_pattern,
                    backslashToken.GetSpan()));
                return new RegexSimpleEscapeNode(backslashToken, CreateMissingToken(RegexKind.TextToken));
            }

            Debug.Assert(_currentToken.VirtualChars.Length == 1);
            var ch = _currentToken.VirtualChars[0].Char;
            if (ch == 'k')
            {
                return ParsePossibleKCaptureEscape(backslashToken, allowTriviaAfterEnd);
            }

            if (ch == '<' || ch == '\'')
            {
                _lexer.Position--;
                return ParsePossibleCaptureEscape(backslashToken, allowTriviaAfterEnd);
            }

            if (ch >= '1' && ch <= '9')
            {
                _lexer.Position--;
                return ParsePossibleBackreferenceEscape(backslashToken, allowTriviaAfterEnd);
            }

            _lexer.Position--;
            return ParseCharEscape(backslashToken, allowTriviaAfterEnd);
        }

        private RegexEscapeNode ParsePossibleBackreferenceEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1] == '\\');
            return HasOption(_options, RegexOptions.ECMAScript)
                ? ParsePossibleEcmascriptBackreferenceEscape(backslashToken, allowTriviaAfterEnd)
                : ParsePossibleRegularBackreferenceEscape(backslashToken, allowTriviaAfterEnd);
        }

        private RegexEscapeNode ParsePossibleEcmascriptBackreferenceEscape(
            RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            // Small deviation: Ecmascript allows references only to captures that precede
            // this position (unlike .NET which allows references in any direction).  However,
            // because we don't track position, we just consume the entire back-reference.
            //
            // This is addressable if we add position tracking when we locate all the captures.

            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');
            var start = _lexer.Position;

            var bestPosition = -1;
            var capVal = 0;
            while (_lexer.Position < _lexer.Text.Length &&
                   _lexer.Text[_lexer.Position] is var ch &&
                   (ch >= '0' && ch <= '9'))
            {
                unchecked
                {
                    capVal *= 10;
                    capVal += (ch - '0');
                }

                _lexer.Position++;

                if (HasCapture(capVal))
                {
                    bestPosition = _lexer.Position;
                }
            }

            if (bestPosition != -1)
            {
                var numberToken = CreateToken(
                    RegexKind.NumberToken, ImmutableArray<RegexTrivia>.Empty,
                    _lexer.GetSubPattern(start, bestPosition)).With(value: capVal);
                ResetToPositionAndConsumeCurrentToken(bestPosition, allowTrivia: allowTriviaAfterEnd);
                return new RegexBackreferenceEscapeNode(backslashToken, numberToken);
            }

            _lexer.Position = start;
            return ParseCharEscape(backslashToken, allowTriviaAfterEnd);
        }

        private RegexEscapeNode ParsePossibleRegularBackreferenceEscape(
            RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');
            var start = _lexer.Position;

            var numberToken = _lexer.TryScanNumber().Value;
            var capVal = (int)numberToken.Value;
            if (HasCapture(capVal) ||
                capVal <= 9)
            {
                CheckCapture(ref numberToken);

                ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd);
                return new RegexBackreferenceEscapeNode(backslashToken, numberToken);
            }

            _lexer.Position = start;
            return ParseCharEscape(backslashToken, allowTriviaAfterEnd);
        }

        private RegexEscapeNode ParsePossibleCaptureEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');
            Debug.Assert(_lexer.Text[_lexer.Position].Char == '<' ||
                         _lexer.Text[_lexer.Position].Char == '\'');

            var afterBackslashPosition = _lexer.Position;
            ScanCaptureParts(allowTriviaAfterEnd, out var openToken, out var capture, out var closeToken);

            if (openToken.IsMissing || capture.IsMissing || closeToken.IsMissing)
            {
                _lexer.Position = afterBackslashPosition;
                return ParseCharEscape(backslashToken, allowTriviaAfterEnd);
            }

            return new RegexCaptureEscapeNode(
                backslashToken, openToken, capture, closeToken);
        }

        private RegexEscapeNode ParsePossibleKCaptureEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            var typeToken = _currentToken;
            var afterBackslashPosition = _lexer.Position - @"k".Length;

            ScanCaptureParts(allowTriviaAfterEnd, out var openToken, out var capture, out var closeToken);
            if (openToken.IsMissing)
            {
                backslashToken = backslashToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Malformed_named_back_reference,
                    GetSpan(backslashToken, typeToken)));
                return new RegexSimpleEscapeNode(backslashToken, typeToken.With(kind: RegexKind.TextToken));
            }

            if (capture.IsMissing || closeToken.IsMissing)
            {
                // Native parser falls back to normal escape scanning, if it doesn't see a capture,
                // or close brace.  For normal .NET regexes, this will then fail later (as \k is not
                // a legal escape), but will succeed for ecmascript regexes.
                _lexer.Position = afterBackslashPosition;
                return ParseCharEscape(backslashToken, allowTriviaAfterEnd);
            }

            return new RegexKCaptureEscapeNode(
                backslashToken, typeToken, openToken, capture, closeToken);
        }

        private void ScanCaptureParts(
            bool allowTriviaAfterEnd, out RegexToken openToken, out RegexToken capture, out RegexToken closeToken)
        {
            openToken = CreateMissingToken(RegexKind.LessThanToken);
            capture = CreateMissingToken(RegexKind.CaptureNameToken);
            closeToken = CreateMissingToken(RegexKind.GreaterThanToken);

            // No trivia allowed in <cap> or 'cap'
            ConsumeCurrentToken(allowTrivia: false);

            if (_lexer.Position < _lexer.Text.Length &&
                (_currentToken.Kind == RegexKind.LessThanToken || _currentToken.Kind == RegexKind.SingleQuoteToken))
            {
                openToken = _currentToken;
            }
            else
            {
                return;
            }

            var captureToken = _lexer.TryScanNumberOrCaptureName();
            capture = captureToken == null
                ? CreateMissingToken(RegexKind.CaptureNameToken)
                : captureToken.Value;

            // No trivia allowed in <cap> or 'cap'
            ConsumeCurrentToken(allowTrivia: false);
            closeToken = CreateMissingToken(RegexKind.GreaterThanToken);

            if (!capture.IsMissing &&
                ((openToken.Kind == RegexKind.LessThanToken && _currentToken.Kind == RegexKind.GreaterThanToken) ||
                 (openToken.Kind == RegexKind.SingleQuoteToken && _currentToken.Kind == RegexKind.SingleQuoteToken)))
            {
                CheckCapture(ref capture);
                closeToken = ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd);
            }
        }

        private RegexEscapeNode ParseCharEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');

            // no trivia between \ and the next char
            ConsumeCurrentToken(allowTrivia: false);
            Debug.Assert(_currentToken.VirtualChars.Length == 1);

            var ch = _currentToken.VirtualChars[0];
            if (ch >= '0' && ch <= '7')
            {
                _lexer.Position--;
                var octalDigits = _lexer.ScanOctalCharacters(_options);
                Debug.Assert(octalDigits.VirtualChars.Length > 0);

                ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd);
                return new RegexOctalEscapeNode(backslashToken, octalDigits);
            }

            switch (ch)
            {
                case 'a':
                case 'b':
                case 'e':
                case 'f':
                case 'n':
                case 'r':
                case 't':
                case 'v':
                    return new RegexSimpleEscapeNode(
                        backslashToken, ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd));
                case 'x':
                    return ParseHexEscape(backslashToken, allowTriviaAfterEnd);
                case 'u':
                    return ParseUnicodeEscape(backslashToken, allowTriviaAfterEnd);
                case 'c':
                    return ParseControlEscape(backslashToken, allowTriviaAfterEnd);
                default:
                    var typeToken = ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd).With(kind: RegexKind.TextToken);

                    if (!HasOption(_options, RegexOptions.ECMAScript) && RegexCharClass.IsWordChar(ch))
                    {
                        typeToken = typeToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                            string.Format(WorkspacesResources.Unrecognized_escape_sequence_0, ch.Char),
                            typeToken.GetSpan()));
                    }

                    return new RegexSimpleEscapeNode(backslashToken, typeToken);
            }
        }

        private RegexEscapeNode ParseUnicodeEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            var typeToken = _currentToken;
            var hexChars = _lexer.ScanHexCharacters(4);
            ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd);
            return new RegexUnicodeEscapeNode(backslashToken, typeToken, hexChars);
        }

        private RegexEscapeNode ParseHexEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            var typeToken = _currentToken;
            var hexChars = _lexer.ScanHexCharacters(2);
            ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd);
            return new RegexHexEscapeNode(backslashToken, typeToken, hexChars);
        }

        private RegexControlEscapeNode ParseControlEscape(RegexToken backslashToken, bool allowTriviaAfterEnd)
        {
            // Nothing allowed between \c and the next char
            var typeToken = ConsumeCurrentToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                typeToken = typeToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Missing_control_character,
                    typeToken.GetSpan()));
                return new RegexControlEscapeNode(backslashToken, typeToken, CreateMissingToken(RegexKind.TextToken));
            }

            Debug.Assert(_currentToken.VirtualChars.Length == 1);

            var ch = _currentToken.VirtualChars[0].Char;

            unchecked
            {
                // From: https://github.com/dotnet/corefx/blob/80e220fc7009de0f0611ee6b52d4d5ffd25eb6c7/src/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexParser.cs#L1450

                // Note: Roslyn accepts a control escape that current .NET parser does not.
                // Specifically: \c[
                //
                // It is a bug that the .NET parser does not support this construct.  The bug was
                // reported at: https://github.com/dotnet/corefx/issues/26501 and was fixed for
                // CoreFx with https://github.com/dotnet/corefx/commit/80e220fc7009de0f0611ee6b52d4d5ffd25eb6c7
                //
                // Because it was a bug, we follow the correct behavior.  That means we will not
                // report a diagnostic for a Regex that someone might run on a previous version of
                // .NET that ends up throwing at runtime.  That's acceptable.  Our goal is to match
                // the latest .NET 'correct' behavior.  Not intermediary points with bugs that have
                // since been fixed.

                // \ca interpreted as \cA
                if (ch >= 'a' && ch <= 'z')
                {
                    ch -= (char)('a' - 'A');
                }

                if (ch >= '@' && ch <= '_')
                {
                    var controlToken = ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd).With(kind: RegexKind.TextToken);
                    return new RegexControlEscapeNode(backslashToken, typeToken, controlToken);
                }
                else
                {
                    typeToken = typeToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Unrecognized_control_character,
                        _currentToken.GetSpan()));

                    // Don't consume the bogus control character.
                    return new RegexControlEscapeNode(backslashToken, typeToken, CreateMissingToken(RegexKind.TextToken));
                }
            }
        }

        private RegexEscapeNode ParseCategoryEscape(RegexToken backslash, bool allowTriviaAfterEnd)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1] is var ch && (ch == 'P' || ch == 'p'));
            var typeToken = _currentToken;

            var start = _lexer.Position;

            if (!TryGetCategoryEscapeParts(
                    allowTriviaAfterEnd,
                    out var openBraceToken,
                    out var categoryToken,
                    out var closeBraceToken,
                    out var message))
            {
                ResetToPositionAndConsumeCurrentToken(start, allowTrivia: allowTriviaAfterEnd);
                typeToken = typeToken.With(kind: RegexKind.TextToken).AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    message, GetSpan(backslash, typeToken)));
                return new RegexSimpleEscapeNode(backslash, typeToken);
            }

            return new RegexCategoryEscapeNode(backslash, typeToken, openBraceToken, categoryToken, closeBraceToken);
        }

        private bool TryGetCategoryEscapeParts(
            bool allowTriviaAfterEnd,
            out RegexToken openBraceToken,
            out RegexToken categoryToken,
            out RegexToken closeBraceToken,
            out string message)
        {
            openBraceToken = default;
            categoryToken = default;
            closeBraceToken = default;
            message = default;

            if (_lexer.Text.Length - _lexer.Position < "{x}".Length)
            {
                message = WorkspacesResources.Incomplete_character_escape;
                return false;
            }

            // no whitespace in \p{x}
            ConsumeCurrentToken(allowTrivia: false);

            if (_currentToken.Kind != RegexKind.OpenBraceToken)
            {
                message = WorkspacesResources.Malformed_character_escape;
                return false;
            }

            openBraceToken = _currentToken;
            var category = _lexer.TryScanEscapeCategory();

            // no whitespace in \p{x}
            ConsumeCurrentToken(allowTrivia: false);
            if (_currentToken.Kind != RegexKind.CloseBraceToken)
            {
                message = WorkspacesResources.Incomplete_character_escape;
                return false;
            }

            if (category == null)
            {
                message = WorkspacesResources.Unknown_property;
                return false;
            }

            categoryToken = category.Value;
            closeBraceToken = ConsumeCurrentToken(allowTrivia: allowTriviaAfterEnd);
            return true;
        }

        private RegexTextNode ParseUnexpectedQuantifier(RegexExpressionNode lastExpression)
        {
            // This is just a bogus element in the higher level sequence.  Allow trivia 
            // after this to abide by the spirit of the native parser.
            var token = ConsumeCurrentToken(allowTrivia: true);
            CheckQuantifierExpression(lastExpression, ref token);
            return new RegexTextNode(token.With(kind: RegexKind.TextToken));
        }

        private void CheckQuantifierExpression(RegexExpressionNode current, ref RegexToken token)
        {
            if (current == null ||
                current.Kind == RegexKind.SimpleOptionsGrouping)
            {
                token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    WorkspacesResources.Quantifier_x_y_following_nothing, token.GetSpan()));
            }
            else if (current is RegexQuantifierNode ||
                     current is RegexLazyQuantifierNode)
            {
                token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    string.Format(WorkspacesResources.Nested_quantifier_0, token.VirtualChars.First().Char), token.GetSpan()));
            }
        }
    }
}
