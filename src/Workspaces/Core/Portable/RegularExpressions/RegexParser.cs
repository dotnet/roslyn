// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    using static RegexHelpers;

    internal static class RegexHelpers
    {
        public static TextSpan GetSpan(RegexToken token)
            => GetSpan(token.VirtualChars);

        public static TextSpan GetSpan(RegexToken token1, RegexToken token2)
            => GetSpan(token1.VirtualChars[0], token2.VirtualChars.Last());

        public static TextSpan GetSpan(ImmutableArray<VirtualChar> virtualChars)
            => GetSpan(virtualChars[0], virtualChars.Last());

        public static TextSpan GetSpan(VirtualChar firstChar, VirtualChar lastChar)
            => TextSpan.FromBounds(firstChar.Span.Start, lastChar.Span.End);

        public static bool HasOption(RegexOptions options, RegexOptions val)
            => (options & val) != 0;

        public static RegexOptions OptionFromCode(VirtualChar ch)
        {
            switch (ch)
            {
                case 'i': case 'I':
                    return RegexOptions.IgnoreCase;
                case 'm': case 'M':
                    return RegexOptions.Multiline;
                case 'n': case 'N':
                    return RegexOptions.ExplicitCapture;
                case 's': case 'S':
                    return RegexOptions.Singleline;
                case 'x': case 'X':
                    return RegexOptions.IgnorePatternWhitespace;
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    internal struct RegexParser
    {
        private RegexLexer _lexer;
        private readonly ArrayBuilder<RegexDiagnostic> _diagnostics;
        private RegexOptions _options;
        private RegexToken _currentToken;
        private readonly List<string> _captureNames;
        private readonly List<int> _captureNumbers;
        //private int currentAutoCapture;

        private RegexParser(
            ImmutableArray<VirtualChar> text, RegexOptions options,
            List<string> captureNames, List<int> captureNumbers) : this()
        {
            _lexer = new RegexLexer(text);
            _diagnostics = ArrayBuilder<RegexDiagnostic>.GetInstance();
            _options = options;

            _captureNames = captureNames;
            _captureNumbers = captureNumbers;

            ScanNextToken(allowTrivia: true);
        }

        private RegexToken ScanNextToken(bool allowTrivia)
        {
            _currentToken = _lexer.ScanNextToken(allowTrivia, _options);
            return _currentToken;
        }

        public static RegexTree Parse(ImmutableArray<VirtualChar> text, RegexOptions options)
        {
            // Parse the tree once, to figure out the capture groups.  These are needed
            // to then parse the tree again, as the captures will affect how we interpret
            // certain things (i.e. escape references) and what errors will be reported.
            //
            // This is necessary as .net regexes allow references to *future* captures.
            // As such, we don't know when we're seeing a reference if it's to something
            // that exists or not.
            var tree1 = new RegexParser(text, options, null, null).ParseTree();

            var captureNames = new List<string>();
            var captureNumbers = new List<int> { 0 };
            var autoNumber = 1;
            CollectCaptures(tree1.Root, captureNames, captureNumbers, ref autoNumber);
            AssignNumbersToCaptureNames(captureNames, captureNumbers, autoNumber);

            var tree2 = new RegexParser(text, options, captureNames, captureNumbers).ParseTree();
            return tree2;
        }

        private static void AssignNumbersToCaptureNames(List<string> captureNames, List<int> captureNumbers, int autoNumber)
        {
            foreach (var name in captureNames)
            {
                while (captureNumbers.Contains(autoNumber))
                {
                    autoNumber++;
                }

                captureNumbers.Add(autoNumber++);
            }
        }

        private static void CollectCaptures(
            RegexNode node, List<string> captureNames, List<int> captureNumbers, ref int autoNumber)
        {
            switch (node.Kind)
            {
                case RegexKind.CaptureGrouping:
                    var captureGrouping = (RegexCaptureGroupingNode)node;
                    CollectCapture(captureNames, captureNumbers, captureGrouping.CaptureToken);
                    break;

                case RegexKind.BalancingGrouping:
                    var balancingGroup = (RegexBalancingGroupingNode)node;
                    CollectCapture(captureNames, captureNumbers, balancingGroup.FirstCaptureToken);
                    break;

                case RegexKind.SimpleGrouping:
                    CollectCaptures(captureNumbers, ref autoNumber, (RegexSimpleGroupingNode)node);
                    break;
            }

            for (int i = 0, n = node.ChildCount; i < n; i++)
            {
                var child = node.ChildAt(i);
                if (child.IsNode)
                {
                    CollectCaptures(child.Node, captureNames, captureNumbers, ref autoNumber);
                }
            }
        }

        private static void CollectCaptures(List<int> captureNumbers, ref int autoNumber, RegexSimpleGroupingNode node)
        {
            // Don't count a bogus (? node as a capture node.
            var expr = node.Expression;
            while (expr is RegexAlternationNode alternation)
            {
                expr = alternation.Left;
            }

            if (expr is RegexSequenceNode sequence &&
                sequence.ChildCount > 0)
            {
                var leftMost = sequence.ChildAt(0);
                if (leftMost.Node is RegexTextNode text &&
                    IsTextChar(text.TextToken, '?'))
                {
                    return;
                }
            }

            AddIfMissing(captureNumbers, autoNumber++);
        }

        private static void CollectCapture(List<string> captureNames, List<int> captureNumbers, RegexToken token)
        {
            if (!token.IsMissing)
            {
                if (token.Kind == RegexKind.NumberToken)
                {
                    AddIfMissing(captureNumbers, (int)token.Value);
                }
                else
                {
                    AddIfMissing(captureNames, (string)token.Value);
                }
            }
        }

        private static void AddIfMissing<T>(List<T> captures, T val)
        {
            if (!captures.Contains(val))
            {
                captures.Add(val);
            }
        }

        private RegexTree ParseTree()
        {
            var expression = this.ParseExpression(consumeCloseParen: true);
            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(_currentToken.Kind == RegexKind.EndOfFile);

            var root = new RegexCompilationUnit(expression, _currentToken);
            CollectDiagnostics(root);
            var diagnostics = _diagnostics.Distinct().OrderBy(rd => rd.Span.Start).ToImmutableArray();
            _diagnostics.Free();
            return new RegexTree(root, diagnostics);
        }

        private void CollectDiagnostics(RegexNode node)
        {
            for (int i = 0, n = node.ChildCount; i < n; i++)
            {
                var child = node.ChildAt(i);
                if (child.IsNode)
                {
                    CollectDiagnostics(child.Node);
                }
                else
                {
                    CollectDiagnostics(child.Token);
                }
            }
        }

        private void CollectDiagnostics(RegexToken token)
        {
            _diagnostics.AddRange(token.Diagnostics);

            foreach (var trivia in token.LeadingTrivia)
            {
                _diagnostics.AddRange(trivia.Diagnostics);
            }
        }

        private RegexExpressionNode ParseExpression(bool consumeCloseParen)
        {
            RegexExpressionNode current = ParseSequence(consumeCloseParen);

            while (_currentToken.Kind == RegexKind.BarToken)
            {
                var barToken = _currentToken;
                ScanNextToken(allowTrivia: true);

                var right = ParseSequence(consumeCloseParen);
                current = new RegexAlternationNode(current, barToken, right);
            }

            return current;
        }

        private RegexSequenceNode ParseSequence(bool consumeCloseParen)
        {
            var list = ArrayBuilder<RegexExpressionNode>.GetInstance();

            if (ShouldConsumeSequenceToken(consumeCloseParen))
            {
                do
                {
                    var last = list.Count == 0 ? null : list.Last();
                    list.Add(ParsePrimaryExpressionAndQuantifiers(consumeCloseParen, last));
                }
                while (ShouldConsumeSequenceToken(consumeCloseParen));
            }

            return new RegexSequenceNode(list.ToImmutableAndFree());
        }

        private bool ShouldConsumeSequenceToken(bool consumeCloseParen)
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

        private RegexExpressionNode ParsePrimaryExpressionAndQuantifiers(
            bool consumeCloseParen, RegexExpressionNode lastExpression)
        {
            var current = ParsePrimaryExpression(lastExpression);
            if (current.Kind == RegexKind.SimpleOptionsGrouping)
            {
                // Simple options (i.e. "(?i-x)" can't have quantifiers attached to them).
                return current;
            }

            switch (_currentToken.Kind)
            {
            case RegexKind.AsteriskToken: return ParseZeroOrMoreQuantifier(current, _currentToken);
            case RegexKind.PlusToken: return ParseOneOrMoreQuantifier(current, _currentToken);
            case RegexKind.QuestionToken: return ParseZeroOrOneQuantifier(current, _currentToken);
            case RegexKind.OpenBraceToken: return TryParseNumericQuantifier(current, _currentToken);
            default: return current;
            }
        }

        private RegexQuantifierNode TryParseLazyQuantifier(RegexQuantifierNode quantifier)
        {
            if (_currentToken.Kind != RegexKind.QuestionToken)
            {
                return quantifier;
            }

            var questionToken = _currentToken;
            ScanNextToken(allowTrivia: true);

            return new RegexLazyQuantifierNode(quantifier, questionToken);
        }

        private RegexQuantifierNode ParseZeroOrMoreQuantifier(RegexPrimaryExpressionNode current, RegexToken asteriskToken)
        {
            // Whitespace allowed between the quantifier and the possible following ?.
            ScanNextToken(allowTrivia: true);
            return TryParseLazyQuantifier(new RegexZeroOrMoreQuantifierNode(current, asteriskToken));
        }

        private RegexQuantifierNode ParseOneOrMoreQuantifier(RegexPrimaryExpressionNode current, RegexToken plusToken)
        {
            // Whitespace allowed between the quantifier and the possible following ?.
            ScanNextToken(allowTrivia: true);
            return TryParseLazyQuantifier(new RegexOneOrMoreQuantifierNode(current, plusToken));
        }

        private RegexQuantifierNode ParseZeroOrOneQuantifier(RegexPrimaryExpressionNode current, RegexToken questionToken)
        {
            // Whitespace allowed between the quantifier and the possible following ?.
            ScanNextToken(allowTrivia: true);
            return TryParseLazyQuantifier(new RegexZeroOrOneQuantifierNode(current, questionToken));
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
            ScanNextToken(allowTrivia: false);

            if (IsTextChar(_currentToken, ','))
            {
                commaToken = _currentToken.With(kind: RegexKind.CommaToken);

                var start = _lexer.Position;
                secondNumberToken = _lexer.TryScanNumber();

                if (secondNumberToken == null)
                {
                    // Nothing allowed between {x,n}
                    ResetToPositionAndScanNextToken(start, allowTrivia: false);
                }
                else
                {
                    var secondNumberTokenLocal = secondNumberToken.Value;

                    // Nothing allowed between {x,n}
                    ScanNextToken(allowTrivia: false);

                    var val1 = (int)firstNumberToken.Value;
                    var val2 = (int)secondNumberTokenLocal.Value;

                    if (val2 < val1)
                    {
                        secondNumberTokenLocal = secondNumberTokenLocal.AddDiagnosticIfNone(new RegexDiagnostic(
                            WorkspacesResources.Illegal_x_y_with_x_less_than_y,
                            GetSpan(secondNumberTokenLocal)));
                        secondNumberToken = secondNumberTokenLocal;
                    }
                }
            }

            if (!IsTextChar(_currentToken, '}'))
            {
                return false;
            }

            closeBraceToken = _currentToken.With(kind: RegexKind.CloseBraceToken);
            ScanNextToken(allowTrivia: true);
            return true;
        }

        private void ResetToPositionAndScanNextToken(int position, bool allowTrivia)
        {
            _lexer.Position = position;
            ScanNextToken(allowTrivia);
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
            case RegexKind.TextToken:
                return ParseText();
            case RegexKind.BackslashToken:
                return ParseEscape();
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
                throw new InvalidOperationException();
            }
        }

        private RegexPrimaryExpressionNode ParsePossibleUnexpectedNumericQuantifier(RegexExpressionNode lastExpression)
        {
            var openBraceToken = _currentToken.With(kind: RegexKind.TextToken);
            var start = _lexer.Position;

            if (TryParseNumericQuantifierParts(
                    out _, out _, out _, out _))
            {
                // Report that a numeric quantifier isn't allowed here.
                CheckQuantifierExpression(lastExpression, ref openBraceToken);
            }

            ResetToPositionAndScanNextToken(start, allowTrivia: true);
            return new RegexTextNode(openBraceToken);
        }

        private RegexPrimaryExpressionNode ParseUnexpectedCloseParenToken()
        {
            var token = _currentToken.With(kind: RegexKind.TextToken).AddDiagnosticIfNone(
                new RegexDiagnostic(WorkspacesResources.Too_many_close_parens, GetSpan(_currentToken)));
            ScanNextToken(allowTrivia: true);
            return new RegexTextNode(token);
        }

        private RegexPrimaryExpressionNode ParseText()
        {
            var textToken = _currentToken;
            ScanNextToken(allowTrivia: true);
            return new RegexTextNode(textToken);
        }

        private RegexPrimaryExpressionNode ParseEndAnchor()
        {
            var dollarToken = _currentToken;
            ScanNextToken(allowTrivia: true);
            return new RegexAnchorNode(RegexKind.EndAnchor, dollarToken);
        }

        private RegexPrimaryExpressionNode ParseStartAnchor()
        {
            var caretToken = _currentToken;
            ScanNextToken(allowTrivia: true);
            return new RegexAnchorNode(RegexKind.StartAnchor, caretToken);
        }

        private RegexPrimaryExpressionNode ParseWildcard()
        {
            var dotToken = _currentToken;
            ScanNextToken(allowTrivia: true);
            return new RegexWildcardNode(dotToken);
        }

        private RegexGroupingNode ParseGrouping()
        {
            var openParenToken = _currentToken;
            var start = _lexer.Position;
            ScanNextToken(allowTrivia: false);

            switch (_currentToken.Kind)
            {
            case RegexKind.QuestionToken:
                return ParseGroupQuestion(openParenToken, _currentToken);
            default:
                _lexer.Position = start;
                return ParseSimpleGroup(openParenToken);
            }
        }

        private RegexToken ParseGroupingCloseParen()
        {
            switch (_currentToken.Kind)
            {
            case RegexKind.CloseParenToken:
                var closeParenToken = _currentToken;
                ScanNextToken(allowTrivia: true);
                return closeParenToken;
            default:
                var missingToken = RegexToken.CreateMissing(RegexKind.CloseParenToken).AddDiagnosticIfNone(
                    new RegexDiagnostic(WorkspacesResources.Not_enough_close_parens, GetTokenStartPositionSpan(_currentToken)));
                return missingToken;
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

            ScanNextToken(allowTrivia: true);
            var expression = this.ParseExpression(consumeCloseParen: false);
            _options = currentOptions;
            return expression;
        }

        private TextSpan GetTokenSpanIncludingEOF(RegexToken token)
            => token.Kind == RegexKind.EndOfFile
                ? GetTokenStartPositionSpan(token)
                : GetSpan(token);

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
            ScanNextToken(allowTrivia: false);
            if (IsTextChar(_currentToken, '<'))
            {
                // (?<=...) or (?<!...) or (?<...>...) or (?<...-...>...)
                return ParseLookbehindOrNamedCaptureOrBalancingGrouping(openParenToken, questionToken);
            }
            else if (IsTextChar(_currentToken, '\''))
            {
                //  (?'...'...) or (?'...-...'...)
                return ParseNamedCaptureOrBalancingGrouping(
                    openParenToken, questionToken, _currentToken.With(kind: RegexKind.QuoteToken));
            }
            else if (_currentToken.Kind == RegexKind.OpenParenToken)
            {
                // alternation construct (?(...) | )
                return ParseConditionalGrouping(openParenToken, questionToken);
            }
            else if (IsTextChar(_currentToken, ':'))
            {
                return ParseNonCapturingGroupingNode(openParenToken, questionToken);
            }
            else if (IsTextChar(_currentToken, '='))
            {
                return ParsePositiveLookaheadGrouping(openParenToken, questionToken);
            }
            else if (IsTextChar(_currentToken, '!'))
            {
                return ParseNegativeLookaheadGrouping(openParenToken, questionToken);
            }
            else if (IsTextChar(_currentToken, '>'))
            {
                return ParseNonBacktrackingGrouping(openParenToken, questionToken);
            }
            else if (_currentToken.Kind != RegexKind.CloseParenToken)
            {
                // Native parser reports "Unrecognized grouping construct", *except* for (?)
                openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Unrecognized_grouping_construct,
                    GetSpan(openParenToken)));
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
            else
            {
                var capture = captureToken.Value;

                RegexToken innerCloseParenToken;
                if (capture.Kind == RegexKind.NumberToken)
                {
                    // If it's a numeric group, it always has to be a real reference.

                    ScanNextToken(allowTrivia: false);
                    if (_currentToken.Kind == RegexKind.CloseParenToken)
                    {
                        innerCloseParenToken = _currentToken;
                        if (_captureNumbers?.Contains((int)capture.Value) == false)
                        {
                            capture = capture.AddDiagnosticIfNone(new RegexDiagnostic(
                                WorkspacesResources.Reference_to_undefined_group,
                                GetSpan(capture)));
                        }
                    }
                    else
                    {
                        innerCloseParenToken = RegexToken.CreateMissing(RegexKind.CloseParenToken);
                        capture = capture.AddDiagnosticIfNone(new RegexDiagnostic(
                            WorkspacesResources.Malformed,
                            GetSpan(capture)));
                        MoveBackBeforePreviousScan();
                    }
                }
                else
                {
                    // If its a capture name, its ok if it doesn't exist.
                    if (_captureNames?.Contains((string)capture.Value) != true)
                    {
                        _lexer.Position = afterInnerOpenParen;
                        return ParseConditionalExpressionGrouping(openParenToken, questionToken, innerOpenParenToken);
                    }

                    ScanNextToken(allowTrivia: false);
                    if (_currentToken.Kind != RegexKind.CloseParenToken)
                    {
                        _lexer.Position = afterInnerOpenParen;
                        return ParseConditionalExpressionGrouping(openParenToken, questionToken, innerOpenParenToken);
                    }

                    innerCloseParenToken = _currentToken;
                }


                ScanNextToken(allowTrivia: true);
                var result = ParseConditionalGroupingResult();

                return new RegexConditionalCaptureGroupingNode(
                    openParenToken, questionToken,
                    innerOpenParenToken, capture, innerCloseParenToken,
                    result, ParseGroupingCloseParen());
            }
        }

        private void MoveBackBeforePreviousScan()
        {
            if (_currentToken.Kind != RegexKind.EndOfFile)
            {
                // Move back to unconsume whatever we just consumed.
                _lexer.Position--;
            }
        }

        private RegexConditionalGroupingNode ParseConditionalExpressionGrouping(
            RegexToken openParenToken, RegexToken questionToken, RegexToken innerOpenParenToken)
        {
            // Reproduce very specific errors the .net regex parser looks for.
            _lexer.Position--;
            if (_lexer.IsAt("(?#"))
            {
                var pos = _lexer.Position;
                var comment = _lexer.ScanComment(default);
                _lexer.Position = pos;

                if (comment.Value.Diagnostics.Length > 0)
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(comment.Value.Diagnostics[0]);
                }
                else
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                        WorkspacesResources.Alternation_conditions_cannot_be_comments,
                        GetSpan(openParenToken)));
                }
            }
            else if (_lexer.IsAt("(?'"))
            {
                openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Alternation_conditions_do_not_capture_and_cannot_be_named,
                    GetSpan(openParenToken)));
            }
            else if (_lexer.IsAt("(?<"))
            {
                if (!_lexer.IsAt("(?<!") &&
                    !_lexer.IsAt("(?<="))
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                        WorkspacesResources.Alternation_conditions_do_not_capture_and_cannot_be_named,
                        GetSpan(openParenToken)));
                }
            }

            ScanNextToken(allowTrivia: false);
            Debug.Assert(_currentToken.Kind == RegexKind.OpenParenToken);
            var grouping = ParseGrouping();

            var result = ParseConditionalGroupingResult();

            return new RegexConditionalExpressionGroupingNode(
                openParenToken, questionToken,
                grouping, result, ParseGroupingCloseParen());
        }

        private RegexExpressionNode ParseConditionalGroupingResult()
        {
            var currentOptions = _options;
            var result = this.ParseExpression(consumeCloseParen: false);
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
                    topAlternation.BarToken.AddDiagnosticIfNone(new RegexDiagnostic(
                        WorkspacesResources.Too_many_bars_in_conditional_grouping,
                        GetSpan(topAlternation.BarToken))),
                    topAlternation.Right);
            }

            return result;
        }

        private RegexGroupingNode ParseLookbehindOrNamedCaptureOrBalancingGrouping(
            RegexToken openParenToken, RegexToken questionToken)
        {
            var lessThanToken = _currentToken.With(kind: RegexKind.LessThanToken);
            var start = _lexer.Position;
            ScanNextToken(allowTrivia: false);

            if (IsTextChar(_currentToken, '='))
            {
                return new RegexPositiveLookbehindGroupingNode(
                    openParenToken, questionToken, lessThanToken, _currentToken.With(kind: RegexKind.EqualsToken),
                    ParseGroupingEmbeddedExpression(_options | RegexOptions.RightToLeft), ParseGroupingCloseParen());
            }
            else if (IsTextChar(_currentToken, '!'))
            {
                return new RegexNegativeLookbehindGroupingNode(
                    openParenToken, questionToken, lessThanToken, _currentToken.With(kind: RegexKind.ExclamationToken),
                    ParseGroupingEmbeddedExpression(_options | RegexOptions.RightToLeft), ParseGroupingCloseParen());
            }
            else
            {
                _lexer.Position = start;
                return ParseNamedCaptureOrBalancingGrouping(openParenToken, questionToken, lessThanToken);
            }
        }

        private RegexGroupingNode ParseNamedCaptureOrBalancingGrouping(
            RegexToken openParenToken, RegexToken questionToken, RegexToken openToken)
        {
            if (_lexer.Position == _lexer.Text.Length)
            {
                openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Unrecognized_grouping_construct,
                    GetSpan(openParenToken, openToken)));
            }

            // (?<...>...) or (?<...-...>...)
            // (?'...'...) or (?'...-...'...)
            var captureToken = _lexer.TryScanNumberOrCaptureName();
            if (captureToken == null)
            {
                ScanNextToken(allowTrivia: false);
                captureToken = RegexToken.CreateMissing(RegexKind.CaptureNameToken);

                if (IsTextChar(_currentToken, '-'))
                {
                    return ParseBalancingGrouping(
                        openParenToken, questionToken, openToken, captureToken.Value);
                }
                else
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
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
                capture = capture.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Capture_number_cannot_be_zero,
                    GetSpan(capture)));
            }

            ScanNextToken(allowTrivia: false);

            if (IsTextChar(_currentToken, '-'))
            {
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
            if (openToken.Kind == RegexKind.LessThanToken && IsTextChar(_currentToken, '>'))
            {
                return _currentToken.With(kind: RegexKind.GreaterThanToken);
            }
            else if (openToken.Kind == RegexKind.QuoteToken && IsTextChar(_currentToken, '\''))
            {
                return _currentToken.With(kind: RegexKind.QuoteToken);
            }
            else
            {
                if (_currentToken.Kind == RegexKind.EndOfFile)
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                        WorkspacesResources.Unrecognized_grouping_construct,
                        GetSpan(openParenToken, openToken)));
                }
                else
                {
                    openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                        WorkspacesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character,
                        GetSpan(_currentToken)));

                    // Rewind to where we were before seeing this bogus character.
                    _lexer.Position--;
                }

                return RegexToken.CreateMissing(
                    openToken.Kind == RegexKind.LessThanToken 
                        ? RegexKind.GreaterThanToken : RegexKind.QuoteToken);
            }
        }

        private RegexGroupingNode ParseBalancingGrouping(
            RegexToken openParenToken, RegexToken questionToken,
            RegexToken openToken, RegexToken firstCapture)
        {
            var minusToken = _currentToken.With(kind: RegexKind.MinusToken);
            var secondCapture = _lexer.TryScanNumberOrCaptureName();
            if (secondCapture == null)
            {
                // Invalid group name: Group names must begin with a word character
                ScanNextToken(allowTrivia: false);

                openParenToken = openParenToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character,
                    GetTokenSpanIncludingEOF(_currentToken)));

                // If we weren't at the end of the text, go back to before whatever character
                // we just consumed.
                MoveBackBeforePreviousScan();
                secondCapture = RegexToken.CreateMissing(RegexKind.CaptureNameToken);
            }

            var second = secondCapture.Value;
            CheckCapture(ref second);

            ScanNextToken(allowTrivia: false);
            var closeToken = ParseCaptureGroupingCloseToken(ref openParenToken, openToken);

            return new RegexBalancingGroupingNode(
                openParenToken, questionToken,
                openToken, firstCapture, minusToken, second, closeToken,
                ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());
        }

        private void CheckCapture(ref RegexToken captureToken)
        {
            if (_captureNames == null)
            {
                // Doing the first pass.  Can't validate capture references.
                return;
            }

            if (captureToken.IsMissing)
            {
                // Don't need to check for a synthesized error capture token.
                return;
            }

            if (captureToken.Kind == RegexKind.NumberToken)
            {
                var val = (int)captureToken.Value;
                if (!_captureNumbers.Contains(val))
                {
                    captureToken = captureToken.AddDiagnosticIfNone(new RegexDiagnostic(
                        string.Format(WorkspacesResources.Reference_to_undefined_group_number_0, val),
                        GetSpan(captureToken)));
                }
            }
            else
            {
                var val = (string)captureToken.Value;
                if (!_captureNames.Contains(val))
                {
                    captureToken = captureToken.AddDiagnosticIfNone(new RegexDiagnostic(
                        string.Format(WorkspacesResources.Reference_to_undefined_group_name_0, val),
                        GetSpan(captureToken)));
                }
            }
        }

        private RegexNonCapturingGroupingNode ParseNonCapturingGroupingNode(RegexToken openParenToken, RegexToken questionToken)
            => new RegexNonCapturingGroupingNode(
                openParenToken, questionToken, _currentToken.With(kind: RegexKind.ColonToken),
                ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());

        private RegexPositiveLookaheadGroupingNode ParsePositiveLookaheadGrouping(RegexToken openParenToken, RegexToken questionToken)
            => new RegexPositiveLookaheadGroupingNode(
                openParenToken, questionToken, _currentToken.With(kind: RegexKind.EqualsToken),
                ParseGroupingEmbeddedExpression(_options & ~RegexOptions.RightToLeft), ParseGroupingCloseParen());

        private RegexNegativeLookaheadGroupingNode ParseNegativeLookaheadGrouping(RegexToken openParenToken, RegexToken questionToken)
            => new RegexNegativeLookaheadGroupingNode(
                openParenToken, questionToken, _currentToken.With(kind: RegexKind.ExclamationToken),
                ParseGroupingEmbeddedExpression(_options & ~RegexOptions.RightToLeft), ParseGroupingCloseParen());

        private RegexNonBacktrackingGroupingNode ParseNonBacktrackingGrouping(RegexToken openParenToken, RegexToken questionToken)
            => new RegexNonBacktrackingGroupingNode(
                openParenToken, questionToken, _currentToken.With(kind: RegexKind.GreaterThanToken),
                ParseGroupingEmbeddedExpression(_options), ParseGroupingCloseParen());

        private RegexGroupingNode ParseOptionsGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken)
        {
            ScanNextToken(allowTrivia: false);
            switch (_currentToken.Kind)
            {
            case RegexKind.CloseParenToken:
                var closeParenToken = _currentToken;
                _options = GetOptionsFromToken(optionsToken);
                ScanNextToken(allowTrivia: true);
                return new RegexSimpleOptionsGroupingNode(
                    openParenToken, questionToken, optionsToken, closeParenToken);
            case RegexKind.TextToken when IsTextChar(_currentToken, ':'):
                return ParseNestedOptionsGroupingNode(
                    openParenToken, questionToken, optionsToken);
            default:
                var missingToken = RegexToken.CreateMissing(RegexKind.CloseParenToken).AddDiagnosticIfNone(
                new RegexDiagnostic(WorkspacesResources.Unrecognized_grouping_construct, GetSpan(openParenToken)));
                return new RegexSimpleOptionsGroupingNode(
                    openParenToken, questionToken, optionsToken, missingToken);
            }
        }

        private RegexNestedOptionsGroupingNode ParseNestedOptionsGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken)
        {
            var colonToken = _currentToken.With(kind: RegexKind.ColonToken);

            return new RegexNestedOptionsGroupingNode(
                openParenToken, questionToken, optionsToken, colonToken, 
                ParseGroupingEmbeddedExpression(GetOptionsFromToken(optionsToken)), ParseGroupingCloseParen());
        }

        private static bool IsTextChar(RegexToken currentToken, char ch)
            => currentToken.Kind == RegexKind.TextToken && currentToken.VirtualChars.Length == 1 && currentToken.VirtualChars[0].Char == ch;

        private RegexOptions GetOptionsFromToken(RegexToken optionsToken)
        {
            var copy = _options;
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

        private RegexBaseCharacterClassNode ParseCharacterClass()
        {
            var openBracketToken = _currentToken;
            Debug.Assert(openBracketToken.Kind == RegexKind.OpenBracketToken);
            var caretToken = RegexToken.CreateMissing(RegexKind.CaretToken);
            var closeBracketToken = RegexToken.CreateMissing(RegexKind.CloseBracketToken);

            ScanNextToken(allowTrivia: false);
            if (IsTextChar(_currentToken, '^'))
            {
                caretToken = _currentToken.With(kind: RegexKind.CaretToken);
            }
            else
            {
                MoveBackBeforePreviousScan();
            }

            ScanNextToken(allowTrivia: false);

            var contents = ArrayBuilder<RegexExpressionNode>.GetInstance();
            while (_currentToken.Kind != RegexKind.EndOfFile)
            {
                Debug.Assert(_currentToken.VirtualChars.Length == 1);

                if (IsTextChar(_currentToken, ']') && contents.Count > 0)
                {
                    closeBracketToken = _currentToken.With(kind: RegexKind.CloseBracketToken);
                    ScanNextToken(allowTrivia: true);
                    break;
                }

                ParseCharacterClassComponents(contents);
            }

            if (closeBracketToken.IsMissing)
            {
                openBracketToken = openBracketToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Unterminated_character_class_set,
                    GetTokenStartPositionSpan(_currentToken)));
            }

            return caretToken.IsMissing
                ? (RegexBaseCharacterClassNode)new RegexCharacterClassNode(openBracketToken, new RegexSequenceNode(contents.ToImmutableAndFree()), closeBracketToken)
                : new RegexNegatedCharacterClassNode(openBracketToken, caretToken, new RegexSequenceNode(contents.ToImmutableAndFree()), closeBracketToken);
        }

        private void ParseCharacterClassComponents(ArrayBuilder<RegexExpressionNode> components)
        {
            var left = ParseCharacterClassComponentPiece(isFirst: components.Count == 0, afterRangeMinus: false);
            if (IsTextChar(_currentToken, '-') && !_lexer.IsAt("]"))
            {
                var minusToken = _currentToken.With(kind: RegexKind.MinusToken);
                ScanNextToken(allowTrivia: false);

                if (_currentToken.Kind == RegexKind.OpenBracketToken)
                {
                    components.Add(left);
                    components.Add(ParseCharacterClassSubtractionNode(minusToken));
                }
                else
                {
                    var right = ParseCharacterClassComponentPiece(isFirst: false, afterRangeMinus: true);
                    components.Add(new RegexCharacterClassRangeNode(left, minusToken, right));
                }
            }
            else
            {
                components.Add(left);
            }
        }

        private RegexPrimaryExpressionNode ParseCharacterClassComponentPiece(bool isFirst, bool afterRangeMinus)
        {
            if (_currentToken.Kind == RegexKind.BackslashToken && _lexer.Position < _lexer.Text.Length)
            {
                var backslashToken = _currentToken;
                var afterSlash = _lexer.Position;
                ScanNextToken(allowTrivia: false);
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
                        if (afterRangeMinus)
                        {
                            backslashToken = backslashToken.AddDiagnosticIfNone(new RegexDiagnostic(
                                string.Format(WorkspacesResources.Cannot_include_class_0_in_character_range, nextChar),
                                GetSpan(backslashToken, _currentToken)));
                        }

                        var typeToken = _currentToken;
                        ScanNextToken(allowTrivia: false);
                        return new RegexSimpleEscapeNode(backslashToken, typeToken);

                    case 'p':
                    case 'P':
                        if (afterRangeMinus)
                        {
                            backslashToken = backslashToken.AddDiagnosticIfNone(new RegexDiagnostic(
                                string.Format(WorkspacesResources.Cannot_include_class_0_in_character_range, nextChar),
                                GetSpan(backslashToken, _currentToken)));
                        }

                        return ParseCategoryEscape(backslashToken);

                    case '-':
                        var dashToken = _currentToken;
                        ScanNextToken(allowTrivia: false);
                        return new RegexSimpleEscapeNode(backslashToken, dashToken);

                    default:
                        _lexer.Position--;
                        return ScanCharEscape(backslashToken);
                }
            }

            if (!afterRangeMinus &&
                !isFirst &&
                IsTextChar(_currentToken, '-') &&
                _lexer.IsAt("["))
            {
                // have a trailing subtraction.
                var minusToken = _currentToken.With(kind: RegexKind.MinusToken);
                ScanNextToken(allowTrivia: false);

                return ParseCharacterClassSubtractionNode(minusToken);
            }

            // From the .net regex code:
            // This is code for Posix style properties - [:Ll:] or [:IsTibetan:].
            // It currently doesn't do anything other than skip the whole thing!
            if (!afterRangeMinus && _currentToken.Kind == RegexKind.OpenBracketToken && _lexer.IsAt(":"))
            {
                var beforeBracketPos = _lexer.Position - 1;
                ScanNextToken(allowTrivia: false);
                var captureName = _lexer.TryScanCaptureName();
                if (captureName.HasValue && _lexer.IsAt(":]"))
                {
                    _lexer.Position += 2;
                    var textChars = _lexer.GetSubPattern(beforeBracketPos, _lexer.Position);
                    var token = new RegexToken(ImmutableArray<RegexTrivia>.Empty, RegexKind.TextToken, textChars);

                    ScanNextToken(allowTrivia: false);
                    return new RegexPosixPropertyNode(token);
                }
                else
                {
                    // Reset to back where we were.
                    _lexer.Position = beforeBracketPos;
                    ScanNextToken(allowTrivia: false);
                    Debug.Assert(_currentToken.Kind == RegexKind.OpenBracketToken);
                }
            }

            var textToken = _currentToken.With(kind: RegexKind.TextToken);
            ScanNextToken(allowTrivia: false);
            return new RegexTextNode(textToken);
        }

        private RegexPrimaryExpressionNode ParseCharacterClassSubtractionNode(RegexToken minusToken)
        {
            var charClass = ParseCharacterClass();

            if (!IsTextChar(_currentToken, ']') && _currentToken.Kind != RegexKind.EndOfFile)
            {
                var closeBracketToken = charClass.CloseBracketToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.A_subtraction_must_be_the_last_element_in_a_character_class,
                    GetTokenStartPositionSpan(_currentToken)));
                charClass = new RegexCharacterClassNode(charClass.OpenBracketToken, charClass.Components, closeBracketToken);
            }

            return new RegexCharacterClassSubtractionNode(minusToken, charClass);
        }

        private RegexPrimaryExpressionNode ParseCharacterClassComponentRight()
        {
            throw new NotImplementedException();
        }

        private RegexEscapeNode ParseEscape()
        {
            var backslashToken = _currentToken;

            // No spaces between \ and next char.
            ScanNextToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                backslashToken = backslashToken.With(kind: RegexKind.TextToken).AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Illegal_backslash_at_end_of_pattern,
                    GetSpan(backslashToken)));
                return new RegexSimpleEscapeNode(backslashToken, RegexToken.CreateMissing(RegexKind.TextToken));
            }

            Debug.Assert(_currentToken.VirtualChars.Length == 1);
            var ch = _currentToken.VirtualChars[0].Char;
            switch (_currentToken.VirtualChars[0].Char)
            {
            case 'b':
            case 'B':
            case 'A':
            case 'G':
            case 'Z':
            case 'z':
            case 'w':
            case 'W':
            case 's':
            case 'S':
            case 'd':
            case 'D':
                var typeToken = _currentToken;
                ScanNextToken(allowTrivia: true);
                return new RegexSimpleEscapeNode(backslashToken, typeToken);
            case 'p':
            case 'P':
                return ParseCategoryEscape(backslashToken);
            }

            // Move back to after the backslash
            _lexer.Position--;
            return ScanBasicBackslash(backslashToken);
        }

        private RegexEscapeNode ScanBasicBackslash(RegexToken backslashToken)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');

            // No spaces between \ and next char.
            ScanNextToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                backslashToken = backslashToken.With(kind: RegexKind.TextToken).AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Illegal_backslash_at_end_of_pattern,
                    GetSpan(backslashToken)));
                return new RegexSimpleEscapeNode(backslashToken, RegexToken.CreateMissing(RegexKind.TextToken));
            }

            Debug.Assert(_currentToken.VirtualChars.Length == 1);
            var ch = _currentToken.VirtualChars[0].Char;
            if (ch == 'k')
            {
                return ParsePossibleKCaptureEscape(backslashToken);
            }

            if (ch == '<' || ch == '\'')
            {
                _lexer.Position--;
                return ParsePossibleCaptureEscape(backslashToken);
            }

            if (ch >= '1' && ch <= '9')
            {
                _lexer.Position--;
                return ParsePossibleBackreferenceEscape(backslashToken);
            }

            _lexer.Position--;
            return ScanCharEscape(backslashToken);
        }

        private RegexEscapeNode ParsePossibleBackreferenceEscape(RegexToken backslashToken)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1] == '\\');
            return HasOption(_options, RegexOptions.ECMAScript)
                ? ParsePossibleEcmascriptBackreferenceEscape(backslashToken)
                : ParsePossibleRegularBackreferenceEscape(backslashToken);
        }

        private RegexEscapeNode ParsePossibleEcmascriptBackreferenceEscape(RegexToken backslashToken)
        {
            // Small deviation: Ecmascript allows references only to captures that preceed
            // this position (unlike .net which allows references in any direction).  However,
            // because we don't track position, we just consume the entire backreference.
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
                capVal *= 10;
                capVal += (ch - '0');

                _lexer.Position++;

                if (_captureNumbers?.Contains(capVal) == true)
                {
                    bestPosition = _lexer.Position;
                }
            }

            if (bestPosition != -1)
            {
                var numberToken = new RegexToken(
                    ImmutableArray<RegexTrivia>.Empty, RegexKind.NumberToken,
                    _lexer.GetSubPattern(start, bestPosition));
                ResetToPositionAndScanNextToken(bestPosition, allowTrivia: true);
                return new RegexBackreferenceEscapeNode(backslashToken, numberToken);
            }

            _lexer.Position = start;
            return ScanCharEscape(backslashToken);
        }

        private RegexEscapeNode ParsePossibleRegularBackreferenceEscape(RegexToken backslashToken)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');
            var start = _lexer.Position;

            var numberToken = _lexer.TryScanNumber().Value;
            var capVal = (int)numberToken.Value;
            if (_captureNumbers?.Contains(capVal) == true ||
                capVal <= 9)
            {
                CheckCapture(ref numberToken);

                ScanNextToken(allowTrivia: true);
                return new RegexBackreferenceEscapeNode(backslashToken, numberToken);
            }

            _lexer.Position = start;
            return ScanCharEscape(backslashToken);
        }

        private RegexEscapeNode ParsePossibleCaptureEscape(RegexToken backslashToken)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');
            Debug.Assert(_lexer.Text[_lexer.Position].Char == '<' ||
                         _lexer.Text[_lexer.Position].Char == '\'');

            var afterBackslashPosition = _lexer.Position;
            ScanCaptureParts(out var openToken, out var capture, out var closeToken);

            if (openToken.IsMissing || capture.IsMissing || closeToken.IsMissing)
            {
                _lexer.Position = afterBackslashPosition;
                return ScanCharEscape(backslashToken);
            }

            return new RegexCaptureEscapeNode(
                backslashToken, openToken, capture, closeToken);
        }

        private RegexEscapeNode ParsePossibleKCaptureEscape(RegexToken backslashToken)
        {
            var typeToken = _currentToken;
            var afterBackslashPosition = _lexer.Position - @"k".Length;

            ScanCaptureParts(out var openToken, out var capture, out var closeToken);
            if (openToken.IsMissing)
            {
                backslashToken = backslashToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Malformed_named_back_reference,
                    GetSpan(backslashToken, typeToken)));
                return new RegexSimpleEscapeNode(backslashToken, typeToken);
            }

            if (capture.IsMissing || closeToken.IsMissing)
            {
                // Native parser falls back to normal escape scanning, if it doesn't see 
                // a capture, or close brace.  For normal .net regexes, this will then 
                // fail later (as \k is not a legal escape), but will succeed for for
                // ecmascript regexes.
                _lexer.Position = afterBackslashPosition;
                return ScanCharEscape(backslashToken);
            }

            return new RegexKCaptureEscapeNode(
                backslashToken, typeToken, openToken, capture, closeToken);
        }

        private void ScanCaptureParts(
            out RegexToken openToken, out RegexToken capture, out RegexToken closeToken)
        {
            openToken = RegexToken.CreateMissing(RegexKind.LessThanToken);
            capture = RegexToken.CreateMissing(RegexKind.CaptureNameToken);
            closeToken = RegexToken.CreateMissing(RegexKind.GreaterThanToken);

            ScanNextToken(allowTrivia: false);

            if (_lexer.Position < _lexer.Text.Length &&
                _currentToken.VirtualChars.Length > 0 &&
                _currentToken.VirtualChars[0].Char is var openCh &&
                (openCh == '<' || openCh == '\''))
            {
                openToken = openCh == '<'
                    ? _currentToken.With(kind: RegexKind.LessThanToken)
                    : _currentToken.With(kind: RegexKind.QuoteToken);
            }
            else
            {
                return;
            }

            var captureToken = _lexer.TryScanNumberOrCaptureName();
            capture = captureToken == null
                ? RegexToken.CreateMissing(RegexKind.CaptureNameToken)
                : captureToken.Value;

            ScanNextToken(allowTrivia: false);
            closeToken = RegexToken.CreateMissing(RegexKind.GreaterThanToken);

            if (!capture.IsMissing &&
                _currentToken.VirtualChars.Length > 0 &&
                _currentToken.VirtualChars[0].Char is var closeCh &&
                ((openCh == '<' && closeCh == '>') || (openCh == '\'' && closeCh == '\'')))
            {
                closeToken = closeCh == '>'
                    ? _currentToken.With(kind: RegexKind.GreaterThanToken)
                    : _currentToken.With(kind: RegexKind.QuoteToken);

                CheckCapture(ref capture);
                ScanNextToken(allowTrivia: true);
            }
        }

        private RegexEscapeNode ScanCharEscape(RegexToken backslashToken)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1].Char == '\\');

            ScanNextToken(allowTrivia: false);
            Debug.Assert(_currentToken.VirtualChars.Length == 1);

            var ch = _currentToken.VirtualChars[0];
            if (ch >= '0' && ch <= '7')
            {
                _lexer.Position--;
                var octalDigits = _lexer.ScanOctalCharacters(_options);
                Debug.Assert(octalDigits.VirtualChars.Length > 0);

                ScanNextToken(allowTrivia: true);
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
                {
                    var typeToken = _currentToken;
                    ScanNextToken(allowTrivia: true);
                    return new RegexSimpleEscapeNode(backslashToken, typeToken);
                }
                case 'x':
                    return ScanHexEscape(backslashToken);
                case 'u':
                    return ScanUnicodeEscape(backslashToken);
                case 'c':
                    return ScanControlEscape(backslashToken);
                default:
                {
                    var typeToken = _currentToken;
                    ScanNextToken(allowTrivia: true);

                    if (!HasOption(_options, RegexOptions.ECMAScript) && RegexCharClass.IsWordChar(ch))
                    {
                        typeToken = typeToken.AddDiagnosticIfNone(new RegexDiagnostic(
                            string.Format(WorkspacesResources.Unrecognized_escape_sequence_0, ch.Char),
                            GetSpan(typeToken)));
                    }

                    return new RegexSimpleEscapeNode(backslashToken, typeToken);
                }
            }
        }

        private RegexEscapeNode ScanUnicodeEscape(RegexToken backslashToken)
        {
            var typeToken = _currentToken;
            var hexChars = _lexer.ScanHexCharacters(4);
            ScanNextToken(allowTrivia: true);
            return new RegexUnicodeEscapeNode(backslashToken, typeToken, hexChars);
        }

        private RegexEscapeNode ScanHexEscape(RegexToken backslashToken)
        {
            var typeToken = _currentToken;
            var hexChars = _lexer.ScanHexCharacters(2);
            ScanNextToken(allowTrivia: true);
            return new RegexHexEscapeNode(backslashToken, typeToken, hexChars);
        }

        private RegexControlEscapeNode ScanControlEscape(RegexToken backslashToken)
        {
            var typeToken = _currentToken;
            ScanNextToken(allowTrivia: false);

            if (_currentToken.Kind == RegexKind.EndOfFile)
            {
                typeToken = typeToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Missing_control_character,
                    GetSpan(typeToken)));
                return new RegexControlEscapeNode(backslashToken, typeToken, RegexToken.CreateMissing(RegexKind.TextToken));
            }

            Debug.Assert(_currentToken.VirtualChars.Length == 1);

            var ch = _currentToken.VirtualChars[0].Char;

            // \ca interpreted as \cA
            if (ch >= 'a' && ch <= 'z')
            {
                ch = (char)(ch - ('a' - 'A'));
            }

            if (unchecked(ch = (char)(ch - '@')) < ' ')
            {
                var controlToken = _currentToken.With(kind: RegexKind.TextToken);
                ScanNextToken(allowTrivia: true);
                return new RegexControlEscapeNode(backslashToken, typeToken, controlToken);
            }
            else
            {
                typeToken = typeToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Unrecognized_control_character,
                    GetSpan(_currentToken)));

                // Don't consume the bogus control character.
                return new RegexControlEscapeNode(backslashToken, typeToken, RegexToken.CreateMissing(RegexKind.TextToken));
            }
        }

        private RegexEscapeNode ParseCategoryEscape(RegexToken backslash)
        {
            Debug.Assert(_lexer.Text[_lexer.Position - 1] is var ch && (ch == 'P' || ch == 'p'));
            var typeToken = _currentToken;

            var start = _lexer.Position;

            if (!TryGetCategoryEscapeParts(
                    out var openBraceToken,
                    out var categoryToken,
                    out var closeBraceToken,
                    out var message))
            {
                ResetToPositionAndScanNextToken(start, allowTrivia: true);
                typeToken = typeToken.AddDiagnosticIfNone(new RegexDiagnostic(
                    message, GetSpan(backslash, typeToken)));
                return new RegexSimpleEscapeNode(backslash, typeToken);
            } 

            return new RegexCategoryEscapeNode(backslash, typeToken, openBraceToken, categoryToken, closeBraceToken);
        }

        private bool TryGetCategoryEscapeParts(
            out RegexToken openBraceToken, out RegexToken categoryToken, out RegexToken closeBraceToken, out string message)
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
            ScanNextToken(allowTrivia: false);

            if (_currentToken.Kind != RegexKind.OpenBraceToken)
            {
                message = WorkspacesResources.Malformed_character_escape;
                return false;
            }

            openBraceToken = _currentToken;
            var category = _lexer.TryScanEscapeCategory();

            // no whitespace in \p{x}
            ScanNextToken(allowTrivia: false);
            if (!IsTextChar(_currentToken, '}'))
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

            closeBraceToken = _currentToken.With(kind: RegexKind.CloseBraceToken);
            ScanNextToken(allowTrivia: true);
            return true;
        }

        private RegexTextNode ParseUnexpectedQuantifier(RegexExpressionNode lastExpression)
        {
            var token = _currentToken;
            CheckQuantifierExpression(lastExpression, ref token);
            ScanNextToken(allowTrivia: true);
            token = token.With(kind: RegexKind.TextToken);
            return new RegexTextNode(token);
        }

        private void CheckQuantifierExpression(RegexExpressionNode current, ref RegexToken token)
        {
            if (current == null ||
                current.Kind == RegexKind.SimpleOptionsGrouping)
            {
                token = token.AddDiagnosticIfNone(new RegexDiagnostic(
                    WorkspacesResources.Quantifier_x_y_following_nothing, GetSpan(token)));
            }
            else if (current is RegexQuantifierNode)
            {
                token = token.AddDiagnosticIfNone(new RegexDiagnostic(
                    string.Format(WorkspacesResources.Nested_quantifier_0, token.VirtualChars.First().Char), GetSpan(token)));
            }
        }
    }
}
