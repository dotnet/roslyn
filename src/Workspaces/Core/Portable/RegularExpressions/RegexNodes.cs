// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal sealed class RegexCompilationUnit : RegexNode
    {
        public RegexCompilationUnit(RegexExpressionNode expression, RegexToken endOfFileToken)
            : base(RegexKind.CompilationUnit)
        {
            Debug.Assert(expression != null);
            Expression = expression;
            EndOfFileToken = endOfFileToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken EndOfFileToken { get; }

        public override int ChildCount => 2;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return Expression;
            case 1: return EndOfFileToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexSequenceNode : RegexExpressionNode
    {
        public ImmutableArray<RegexExpressionNode> Children { get; }

        public override int ChildCount => Children.Length;

        public RegexSequenceNode(ImmutableArray<RegexExpressionNode> children)
            : base(RegexKind.Sequence)
        {
            this.Children = children;
        }

        public override RegexNodeOrToken ChildAt(int index)
            => Children[index];
    }

    internal sealed class RegexTextNode : RegexPrimaryExpressionNode
    {
        public RegexTextNode(RegexToken textToken)
            : base(RegexKind.Text)
        {
            Debug.Assert(textToken.Kind == RegexKind.TextToken);
            TextToken = textToken;
        }

        public RegexToken TextToken { get; }

        public override int ChildCount => 1;

        public override RegexNodeOrToken ChildAt(int index) => TextToken;
    }

    //internal sealed class RegexCharacterClassSeparatorNode : RegexPrimaryExpressionNode
    //{
    //    public RegexCharacterClassSeparatorNode(RegexToken minusToken)
    //        : base(RegexKind.CharacterClassSeparator)
    //    {
    //        Debug.Assert(minusToken.Kind == RegexKind.MinusToken);
    //        MinusToken = minusToken;
    //    }

    //    public RegexToken MinusToken { get; }

    //    public override int ChildCount => 1;

    //    public override RegexNodeOrToken ChildAt(int index) => MinusToken;
    //}

    internal abstract class RegexBaseCharacterClassNode : RegexPrimaryExpressionNode
    {
        protected RegexBaseCharacterClassNode(
            RegexKind kind, RegexToken openBracketToken, RegexSequenceNode components, RegexToken closeBracketToken)
            : base(kind)
        {
            Debug.Assert(openBracketToken.Kind == RegexKind.OpenBracketToken);
            Debug.Assert(components != null);
            Debug.Assert(closeBracketToken.Kind == RegexKind.CloseBracketToken);
            OpenBracketToken = openBracketToken;
            Components = components;
            CloseBracketToken = closeBracketToken;
        }

        public RegexToken OpenBracketToken { get; }
        public RegexSequenceNode Components { get; }
        public RegexToken CloseBracketToken { get; }
    }

    internal sealed class RegexCharacterClassNode : RegexBaseCharacterClassNode
    {
        public RegexCharacterClassNode(
            RegexToken openBracketToken, RegexSequenceNode components, RegexToken closeBracketToken)
            : base(RegexKind.CharacterClass, openBracketToken, components, closeBracketToken)
        {
        }

        public override int ChildCount => 3;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return OpenBracketToken;
                case 1: return Components;
                case 2: return CloseBracketToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexNegatedCharacterClassNode : RegexBaseCharacterClassNode
    {
        public RegexNegatedCharacterClassNode(
            RegexToken openBracketToken, RegexToken caretToken, RegexSequenceNode components, RegexToken closeBracketToken)
            : base(RegexKind.NegatedCharacterClass, openBracketToken, components, closeBracketToken)
        {
            Debug.Assert(caretToken.Kind == RegexKind.CaretToken);
            CaretToken = caretToken;
        }

        public RegexToken CaretToken { get; }

        public override int ChildCount => 4;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return OpenBracketToken;
                case 1: return CaretToken;
                case 2: return Components;
                case 3: return CloseBracketToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexCharacterClassRangeNode : RegexPrimaryExpressionNode
    {
        public RegexCharacterClassRangeNode(
            RegexExpressionNode left, RegexToken minusToken, RegexExpressionNode right)
            : base(RegexKind.CharacterClassRange)
        {
            Debug.Assert(left != null);
            Debug.Assert(minusToken.Kind == RegexKind.MinusToken);
            Debug.Assert(right != null);
            Left = left;
            MinusToken = minusToken;
            Right = right;
        }

        public RegexExpressionNode Left { get; }
        public RegexToken MinusToken { get; }
        public RegexExpressionNode Right { get; }

        public override int ChildCount => 3;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return Left;
                case 1: return MinusToken;
                case 2: return Right;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexCharacterClassSubtractionNode : RegexPrimaryExpressionNode
    {
        public RegexCharacterClassSubtractionNode(
            RegexToken minusToken, RegexBaseCharacterClassNode characterClass)
            : base(RegexKind.CharacterClassSubtraction)
        {
            Debug.Assert(minusToken.Kind == RegexKind.MinusToken);
            Debug.Assert(characterClass != null);
            MinusToken = minusToken;
            CharacterClass = characterClass;
        }

        public RegexToken MinusToken { get; }
        public RegexBaseCharacterClassNode CharacterClass { get; }

        public override int ChildCount => 2;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return MinusToken;
                case 1: return CharacterClass;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexPosixPropertyNode : RegexPrimaryExpressionNode
    {
        public RegexPosixPropertyNode(RegexToken textToken)
            : base(RegexKind.PosixProperty)
        {
            Debug.Assert(textToken.Kind == RegexKind.TextToken);
            TextToken = textToken;
        }

        public RegexToken TextToken { get; }

        public override int ChildCount => 1;

        public override RegexNodeOrToken ChildAt(int index) => TextToken;
    }

    internal sealed class RegexWildcardNode : RegexPrimaryExpressionNode
    {
        public RegexWildcardNode(RegexToken dotToken)
            : base(RegexKind.Wildcard)
        {
            Debug.Assert(dotToken.Kind != RegexKind.None);
            DotToken = dotToken;
        }

        public RegexToken DotToken { get; }

        public override int ChildCount => 1;

        public override RegexNodeOrToken ChildAt(int index) => DotToken;
    }

    internal abstract class RegexExpressionNode : RegexNode
    {
        protected RegexExpressionNode(RegexKind kind)
            : base(kind)
        {
        }
    }

    internal abstract class RegexPrimaryExpressionNode : RegexExpressionNode
    {
        protected RegexPrimaryExpressionNode(RegexKind kind)
            : base(kind)
        {
        }
    }

    internal abstract class RegexQuantifierNode : RegexExpressionNode
    {
        protected RegexQuantifierNode(RegexKind kind)
            : base(kind)
        {
        }
    }

    internal sealed class RegexZeroOrMoreQuantifierNode : RegexQuantifierNode
    {
        public RegexZeroOrMoreQuantifierNode(
            RegexExpressionNode expression, RegexToken asteriskToken)
            : base(RegexKind.ZeroOrMoreQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(asteriskToken.Kind != RegexKind.None);
            Expression = expression;
            AsteriskToken = asteriskToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken AsteriskToken { get; }

        public override int ChildCount => 2;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Expression;
            case 1: return this.AsteriskToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexOneOrMoreQuantifierNode : RegexQuantifierNode
    {
        public RegexOneOrMoreQuantifierNode(
            RegexExpressionNode expression, RegexToken plusToken)
            : base(RegexKind.OneOrMoreQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(plusToken.Kind != RegexKind.None);
            Expression = expression;
            PlusToken = plusToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken PlusToken { get; }

        public override int ChildCount => 2;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Expression;
            case 1: return this.PlusToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexZeroOrOneQuantifierNode : RegexQuantifierNode
    {
        public RegexZeroOrOneQuantifierNode(
            RegexExpressionNode expression, RegexToken questionToken)
            : base(RegexKind.ZeroOrOneQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(questionToken.Kind != RegexKind.None);
            Expression = expression;
            QuestionToken = questionToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken QuestionToken { get; }

        public override int ChildCount => 2;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Expression;
            case 1: return this.QuestionToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexLazyQuantifierNode : RegexQuantifierNode
    {
        public RegexLazyQuantifierNode(
            RegexQuantifierNode quantifier, RegexToken questionToken)
            : base(RegexKind.LazyQuantifier)
        {
            Debug.Assert(quantifier != null);
            Debug.Assert(questionToken.Kind != RegexKind.None);
            Quantifier = quantifier;
            QuestionToken = questionToken;
        }

        public RegexQuantifierNode Quantifier { get; }
        public RegexToken QuestionToken { get; }

        public override int ChildCount => 2;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Quantifier;
            case 1: return this.QuestionToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal abstract class RegexNumericQuantifierNode : RegexQuantifierNode
    {
        protected RegexNumericQuantifierNode(
            RegexKind kind, RegexPrimaryExpressionNode expression, RegexToken openBraceToken, RegexToken firstNumberToken, RegexToken closeBraceToken)
            : base(kind)
        {
            Debug.Assert(expression != null);
            Debug.Assert(openBraceToken.Kind != RegexKind.None);
            Debug.Assert(firstNumberToken.Kind != RegexKind.None);
            Debug.Assert(closeBraceToken.Kind != RegexKind.None);
            Expression = expression;
            OpenBraceToken = openBraceToken;
            FirstNumberToken = firstNumberToken;
            CloseBraceToken = closeBraceToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken OpenBraceToken { get; }
        public RegexToken FirstNumberToken { get; }
        public RegexToken CloseBraceToken { get; }
    }

    internal sealed class RegexExactNumericQuantifierNode : RegexNumericQuantifierNode
    {
        public RegexExactNumericQuantifierNode(
            RegexPrimaryExpressionNode expression, RegexToken openBraceToken, RegexToken numberToken, RegexToken closeBraceToken)
            : base(RegexKind.ExactNumericQuantifier, expression, openBraceToken, numberToken, closeBraceToken)
        {
        }

        public override int ChildCount => 4;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return Expression;
            case 1: return OpenBraceToken;
            case 2: return FirstNumberToken;
            case 3: return CloseBraceToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexOpenNumericRangeQuantifierNode : RegexNumericQuantifierNode
    {
        public RegexOpenNumericRangeQuantifierNode(
            RegexPrimaryExpressionNode expression,
            RegexToken openBraceToken, RegexToken firstNumberToken,
            RegexToken commaToken, RegexToken closeBraceToken)
            : base(RegexKind.OpenRangeNumericQuantifier, expression, openBraceToken, firstNumberToken, closeBraceToken)
        {
            Debug.Assert(commaToken.Kind != RegexKind.None);
            CommaToken = commaToken;
        }

        public RegexToken CommaToken { get; }

        public override int ChildCount => 5;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return Expression;
            case 1: return OpenBraceToken;
            case 2: return FirstNumberToken;
            case 3: return CommaToken;
            case 4: return CloseBraceToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexClosedNumericRangeQuantifierNode : RegexNumericQuantifierNode
    {
        public RegexClosedNumericRangeQuantifierNode(
            RegexPrimaryExpressionNode expression,
            RegexToken openBraceToken, RegexToken firstNumberToken,
            RegexToken commaToken, RegexToken secondNumberToken, RegexToken closeBraceToken)
            : base(RegexKind.ClosedRangeNumericQuantifier, expression, openBraceToken, firstNumberToken, closeBraceToken)
        {
            Debug.Assert(commaToken.Kind != RegexKind.None);
            Debug.Assert(secondNumberToken.Kind != RegexKind.None);
            CommaToken = commaToken;
            SecondNumberToken = secondNumberToken;
        }

        public RegexToken CommaToken { get; }
        public RegexToken SecondNumberToken { get; }

        public override int ChildCount => 6;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return Expression;
            case 1: return OpenBraceToken;
            case 2: return FirstNumberToken;
            case 3: return CommaToken;
            case 4: return SecondNumberToken;
            case 5: return CloseBraceToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexAnchorNode : RegexPrimaryExpressionNode
    {
        public RegexAnchorNode(RegexKind kind, RegexToken anchorToken)
            : base(kind)
        {
            Debug.Assert(anchorToken.Kind != RegexKind.None);
            AnchorToken = anchorToken;
        }

        public RegexToken AnchorToken { get; }

        public override int ChildCount => 1;

        public override RegexNodeOrToken ChildAt(int index) => AnchorToken;
    }

    internal sealed class RegexAlternationNode : RegexExpressionNode
    {
        public RegexAlternationNode(
            RegexExpressionNode left, RegexToken barToken, RegexSequenceNode right)
            : base(RegexKind.Alternation)
        {
            Debug.Assert(left != null);
            Debug.Assert(barToken.Kind != RegexKind.None);
            Debug.Assert(right != null);
            Left = left;
            BarToken = barToken;
            Right = right;
        }

        public RegexExpressionNode Left { get; }
        public RegexToken BarToken { get; }
        public RegexSequenceNode Right { get; }

        public override int ChildCount => 3;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return Left;
            case 1: return BarToken;
            case 2: return Right;
            }

            throw new InvalidOperationException();
        }
    }

    internal abstract class RegexGroupingNode : RegexPrimaryExpressionNode
    {
        protected RegexGroupingNode(RegexKind kind, RegexToken openParenToken, RegexToken closeParenToken)
            : base(kind)
        {
            OpenParenToken = openParenToken;
            CloseParenToken = closeParenToken;
        }

        public RegexToken OpenParenToken { get; }
        public RegexToken CloseParenToken { get; }
    }

    internal class RegexSimpleGroupingNode : RegexGroupingNode
    {
        public RegexSimpleGroupingNode(RegexToken openParenToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.SimpleGrouping, openParenToken, closeParenToken)
        {
            Debug.Assert(expression != null);
            Expression = expression;
        }

        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 3;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return Expression;
            case 2: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal abstract class RegexQuestionGroupingNode : RegexGroupingNode
    {
        protected RegexQuestionGroupingNode(RegexKind kind, RegexToken openParenToken, RegexToken questionToken, RegexToken closeParenToken)
            : base(kind, openParenToken, closeParenToken)
        {
            QuestionToken = questionToken;
        }

        public RegexToken QuestionToken { get; }
    }

    internal abstract class RegexOptionsGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexOptionsGroupingNode(RegexKind kind, RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken, RegexToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            OptionsToken = optionsToken;
        }

        public RegexToken OptionsToken { get; }
    }

    internal class RegexSimpleOptionsGroupingNode : RegexOptionsGroupingNode
    {
        public RegexSimpleOptionsGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken, RegexToken closeParenToken)
            : base(RegexKind.SimpleOptionsGrouping, openParenToken, questionToken, optionsToken, closeParenToken)
        {
        }

        public override int ChildCount => 4;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return OptionsToken;
            case 3: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal class RegexNestedOptionsGroupingNode : RegexOptionsGroupingNode
    {
        public RegexNestedOptionsGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken,
            RegexToken colonToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NestedOptionsGrouping, openParenToken, questionToken, optionsToken, closeParenToken)
        {
            Debug.Assert(expression != null);
            ColonToken = colonToken;
            Expression = expression;
        }

        public RegexToken ColonToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 6;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return OptionsToken;
            case 3: return ColonToken;
            case 4: return Expression;
            case 5: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexNonCapturingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNonCapturingGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken colonToken, 
            RegexExpressionNode expression, RegexToken closeParenToken) 
            : base(RegexKind.NonCapturingGrouping, openParenToken, questionToken, closeParenToken)
        {
            ColonToken = colonToken;
            Expression = expression;
        }

        public RegexToken ColonToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return ColonToken;
            case 3: return Expression;
            case 4: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexPositiveLookaheadGroupingNode : RegexQuestionGroupingNode
    {
        public RegexPositiveLookaheadGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken equalsToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.PositiveLookaheadGrouping, openParenToken, questionToken, closeParenToken)
        {
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public RegexToken EqualsToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return EqualsToken;
            case 3: return Expression;
            case 4: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexNegativeLookaheadGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNegativeLookaheadGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken exclamationToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NegativeLookaheadGrouping, openParenToken, questionToken, closeParenToken)
        {
            ExclamationToken = exclamationToken;
            Expression = expression;
        }

        public RegexToken ExclamationToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return ExclamationToken;
            case 3: return Expression;
            case 4: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal abstract class RegexLookbehindGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexLookbehindGroupingNode(
            RegexKind kind, RegexToken openParenToken, RegexToken questionToken,
            RegexToken lessThanToken, RegexToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            LessThanToken = lessThanToken;
        }

        public RegexToken LessThanToken { get; }
    }

    internal sealed class RegexPositiveLookbehindGroupingNode : RegexLookbehindGroupingNode
    {
        public RegexPositiveLookbehindGroupingNode(
            RegexToken openParenToken, RegexToken questionToken,RegexToken lessThanToken,
            RegexToken equalsToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.PositiveLookbehindGrouping, openParenToken, questionToken, lessThanToken, closeParenToken)
        {
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public RegexToken EqualsToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 6;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return LessThanToken;
            case 3: return EqualsToken;
            case 4: return Expression;
            case 5: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexNegativeLookbehindGroupingNode : RegexLookbehindGroupingNode
    {
        public RegexNegativeLookbehindGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken lessThanToken,
            RegexToken exclamationToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NegativeLookbehindGrouping, openParenToken, questionToken, lessThanToken, closeParenToken)
        {
            ExclamationToken = exclamationToken;
            Expression = expression;
        }

        public RegexToken ExclamationToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 6;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return LessThanToken;
            case 3: return ExclamationToken;
            case 4: return Expression;
            case 5: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexNonBacktrackingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNonBacktrackingGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken greaterThanToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NonBacktrackingGrouping, openParenToken, questionToken, closeParenToken)
        {
            GreaterThanToken = greaterThanToken;
            Expression = expression;
        }

        public RegexToken GreaterThanToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return GreaterThanToken;
            case 3: return Expression;
            case 4: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexCaptureGroupingNode : RegexQuestionGroupingNode
    {
        public RegexCaptureGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken openToken, 
            RegexToken captureToken, RegexToken closeToken, 
            RegexExpressionNode expression, RegexToken closeParenToken) 
            : base(RegexKind.CaptureGrouping, openParenToken, questionToken, closeParenToken)
        {
            OpenToken = openToken;
            CaptureToken = captureToken;
            CloseToken = closeToken;
            Expression = expression;
        }

        public RegexToken OpenToken { get; }
        public RegexToken CaptureToken { get; }
        public RegexToken CloseToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 7;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return OpenToken;
            case 3: return CaptureToken;
            case 4: return CloseToken;
            case 5: return Expression;
            case 6: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexBalancingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexBalancingGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken openToken,
            RegexToken firstCaptureToken, RegexToken minusToken, RegexToken secondCaptureToken,
            RegexToken closeToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.BalancingGrouping, openParenToken, questionToken, closeParenToken)
        {
            OpenToken = openToken;
            FirstCaptureToken = firstCaptureToken;
            MinusToken = minusToken;
            SecondCaptureToken = secondCaptureToken;
            CloseToken = closeToken;
            Expression = expression;
        }

        public RegexToken OpenToken { get; }
        public RegexToken FirstCaptureToken { get; }
        public RegexToken MinusToken { get; }
        public RegexToken SecondCaptureToken { get; }
        public RegexToken CloseToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 9;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return OpenToken;
            case 3: return FirstCaptureToken;
            case 4: return MinusToken;
            case 5: return SecondCaptureToken;
            case 6: return CloseToken;
            case 7: return Expression;
            case 8: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal abstract class RegexConditionalGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexConditionalGroupingNode(
            RegexKind kind, RegexToken openParenToken, RegexToken questionToken,
            RegexExpressionNode result, RegexToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            Result = result;
        }

        public RegexExpressionNode Result { get; }
    }

    internal sealed class RegexConditionalCaptureGroupingNode : RegexConditionalGroupingNode
    {
        public RegexConditionalCaptureGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, 
            RegexToken innerOpenParenToken, RegexToken captureToken, RegexToken innerCloseParenToken,
            RegexExpressionNode result, RegexToken closeParenToken) 
            : base(RegexKind.ConditionalCaptureGrouping, openParenToken, questionToken, result, closeParenToken)
        {
            InnerOpenParenToken = innerOpenParenToken;
            CaptureToken = captureToken;
            InnerCloseParenToken = innerCloseParenToken;
        }

        public RegexToken InnerOpenParenToken { get; }
        public RegexToken CaptureToken { get; }
        public RegexToken InnerCloseParenToken { get; }

        public override int ChildCount => 7;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return InnerOpenParenToken;
            case 3: return CaptureToken;
            case 4: return InnerCloseParenToken;
            case 5: return Result;
            case 6: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexConditionalExpressionGroupingNode : RegexConditionalGroupingNode
    {
        public RegexConditionalExpressionGroupingNode(
            RegexToken openParenToken, RegexToken questionToken,
            RegexGroupingNode grouping,
            RegexExpressionNode result, RegexToken closeParenToken)
            : base(RegexKind.ConditionalExpressionGrouping, openParenToken, questionToken, result, closeParenToken)
        {
            Grouping = grouping;
        }

        public override int ChildCount => 5;

        public RegexGroupingNode Grouping { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return QuestionToken;
            case 2: return Grouping;
            case 3: return Result;
            case 4: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal abstract class RegexEscapeNode : RegexPrimaryExpressionNode
    {
        protected RegexEscapeNode(RegexKind kind, RegexToken backslashToken) : base(kind)
        {
            BackslashToken = backslashToken;
        }

        public RegexToken BackslashToken { get; }
    }

    internal abstract class RegexTypeEscapeNode : RegexEscapeNode
    {
        protected RegexTypeEscapeNode(RegexKind kind, RegexToken backslashToken, RegexToken typeToken)
            : base(kind, backslashToken)
        {
            TypeToken = typeToken;
        }

        public RegexToken TypeToken { get; }
    }

    internal sealed class RegexSimpleEscapeNode : RegexTypeEscapeNode
    {
        public RegexSimpleEscapeNode(RegexToken backslashToken, RegexToken typeToken)
            : base(RegexKind.SimpleEscape, backslashToken, typeToken)
        {
        }

        public override int ChildCount => 2;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return BackslashToken;
            case 1: return TypeToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexControlEscapeNode : RegexTypeEscapeNode
    {
        public RegexControlEscapeNode(RegexToken backslashToken, RegexToken typeToken, RegexToken controlToken)
            : base(RegexKind.ControlEscape, backslashToken, typeToken)
        {
            ControlToken = controlToken;
        }

        public override int ChildCount => 3;

        public RegexToken ControlToken { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
                case 2: return ControlToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexHexEscapeNode : RegexTypeEscapeNode
    {
        public RegexHexEscapeNode(RegexToken backslashToken, RegexToken typeToken, RegexToken hexText)
            : base(RegexKind.HexEscape, backslashToken, typeToken)
        {
            HexText = hexText;
        }

        public override int ChildCount => 3;

        public RegexToken HexText { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
                case 2: return HexText;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexUnicodeEscapeNode : RegexTypeEscapeNode
    {
        public RegexUnicodeEscapeNode(RegexToken backslashToken, RegexToken typeToken, RegexToken hexText)
            : base(RegexKind.UnicodeEscape, backslashToken, typeToken)
        {
            HexText = hexText;
        }

        public override int ChildCount => 3;

        public RegexToken HexText { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
                case 2: return HexText;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexCaptureEscapeNode : RegexEscapeNode
    {
        public RegexCaptureEscapeNode(
            RegexToken backslashToken, RegexToken openToken, RegexToken captureToken, RegexToken closeToken)
            : base(RegexKind.CaptureEscape, backslashToken)
        {
            OpenToken = openToken;
            CaptureToken = captureToken;
            CloseToken = closeToken;
        }

        public override int ChildCount => 4;

        public RegexToken OpenToken { get; }
        public RegexToken CaptureToken { get; }
        public RegexToken CloseToken { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return OpenToken;
                case 2: return CaptureToken;
                case 3: return CloseToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexKCaptureEscapeNode : RegexTypeEscapeNode
    {
        public RegexKCaptureEscapeNode(
            RegexToken backslashToken, RegexToken typeToken,
            RegexToken openToken, RegexToken captureToken, RegexToken closeToken)
            : base(RegexKind.KCaptureEscape, backslashToken, typeToken)
        {
            OpenToken = openToken;
            CaptureToken = captureToken;
            CloseToken = closeToken;
        }

        public override int ChildCount => 5;

        public RegexToken OpenToken { get; }
        public RegexToken CaptureToken { get; }
        public RegexToken CloseToken { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
                case 2: return OpenToken;
                case 3: return CaptureToken;
                case 4: return CloseToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexOctalEscapeNode : RegexEscapeNode
    {
        public RegexOctalEscapeNode(RegexToken backslashToken, RegexToken octalText)
            : base(RegexKind.OctalEscape, backslashToken)
        {
            OctalText = octalText;
        }

        public override int ChildCount => 2;

        public RegexToken OctalText { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return OctalText;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexBackreferenceEscapeNode : RegexEscapeNode
    {
        public RegexBackreferenceEscapeNode(RegexToken backslashToken, RegexToken numberToken)
            : base(RegexKind.BackreferenceEscape, backslashToken)
        {
            NumberToken = numberToken;
        }

        public override int ChildCount => 2;

        public RegexToken NumberToken { get; }

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return NumberToken;
            }

            throw new InvalidOperationException();
        }
    }

    internal sealed class RegexCategoryEscapeNode : RegexEscapeNode
    {
        public RegexCategoryEscapeNode(
            RegexToken backslashToken, RegexToken typeToken, RegexToken openBraceToken, RegexToken categoryToken, RegexToken closeBraceToken)
            : base(RegexKind.CategoryEscape, backslashToken)
        {
            TypeToken = typeToken;
            OpenBraceToken = openBraceToken;
            CategoryToken = categoryToken;
            CloseBraceToken = closeBraceToken;
        }

        public RegexToken TypeToken { get; }
        public RegexToken OpenBraceToken { get; }
        public RegexToken CategoryToken { get; }
        public RegexToken CloseBraceToken { get; }

        public override int ChildCount => 5;

        public override RegexNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
            case 0: return BackslashToken;
            case 1: return TypeToken;
            case 2: return OpenBraceToken;
            case 3: return CategoryToken;
            case 4: return CloseBraceToken;
            }

            throw new InvalidOperationException();
        }
    }
}
