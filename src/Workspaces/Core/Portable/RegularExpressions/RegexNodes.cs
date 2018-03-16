// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal sealed class RegexCompilationUnit : RegexNode
    {
        public RegexCompilationUnit(RegexExpressionNode expression, EmbeddedSyntaxToken endOfFileToken)
            : base(RegexKind.CompilationUnit)
        {
            Debug.Assert(expression != null);
            Debug.Assert(endOfFileToken.Kind() == RegexKind.EndOfFile);
            Expression = expression;
            EndOfFileToken = endOfFileToken;
        }

        public RegexExpressionNode Expression { get; }
        public EmbeddedSyntaxToken EndOfFileToken { get; }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return Expression;
            case 1: return EndOfFileToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Represents a possibly-empty sequence of regex expressions.  For example, the regex ""
    /// will produe an empty RegexSequence nodes, and "a|" will produce an alternation with an
    /// empty sequence on the right side.  Having a node represent the empty sequence is actually
    /// appropriate as these are legal regexes and the empty sequence represents 'a pattern
    /// that will match any position'.  Not having a node for this would actually end up 
    /// complicating things in terms of dealing with nulls in the tree.
    /// 
    /// This does not deviate from roslyn principles.  While nodes for empty text are rare, they
    /// are allowed (for example, OmittedTypeArgument in C#).
    /// </summary>
    internal sealed class RegexSequenceNode : RegexExpressionNode
    {
        public ImmutableArray<RegexExpressionNode> Children { get; }

        public override int ChildCount => Children.Length;

        public RegexSequenceNode(ImmutableArray<RegexExpressionNode> children)
            : base(RegexKind.Sequence)
        {
            this.Children = children;
        }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
            => Children[index];

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Represents a chunk of text (usually just a single char) from the original pattern.
    /// </summary>
    internal sealed class RegexTextNode : RegexPrimaryExpressionNode
    {
        public RegexTextNode(EmbeddedSyntaxToken textToken)
            : base(RegexKind.Text)
        {
            Debug.Assert(textToken.Kind() == RegexKind.TextToken);
            TextToken = textToken;
        }

        public EmbeddedSyntaxToken TextToken { get; }

        public override int ChildCount => 1;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index) => TextToken;

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type for [...] and [^...] character classes.
    /// </summary>
    internal abstract class RegexBaseCharacterClassNode : RegexPrimaryExpressionNode
    {
        protected RegexBaseCharacterClassNode(
            RegexKind kind, EmbeddedSyntaxToken openBracketToken, RegexSequenceNode components, EmbeddedSyntaxToken closeBracketToken)
            : base(kind)
        {
            Debug.Assert(openBracketToken.Kind() == RegexKind.OpenBracketToken);
            Debug.Assert(components != null);
            Debug.Assert(closeBracketToken.Kind() == RegexKind.CloseBracketToken);
            OpenBracketToken = openBracketToken;
            Components = components;
            CloseBracketToken = closeBracketToken;
        }

        public EmbeddedSyntaxToken OpenBracketToken { get; }
        public RegexSequenceNode Components { get; }
        public EmbeddedSyntaxToken CloseBracketToken { get; }
    }

    /// <summary>
    /// [...] node.
    /// </summary>
    internal sealed class RegexCharacterClassNode : RegexBaseCharacterClassNode
    {
        public RegexCharacterClassNode(
            EmbeddedSyntaxToken openBracketToken, RegexSequenceNode components, EmbeddedSyntaxToken closeBracketToken)
            : base(RegexKind.CharacterClass, openBracketToken, components, closeBracketToken)
        {
        }

        public override int ChildCount => 3;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return OpenBracketToken;
                case 1: return Components;
                case 2: return CloseBracketToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// [^...] node
    /// </summary>
    internal sealed class RegexNegatedCharacterClassNode : RegexBaseCharacterClassNode
    {
        public RegexNegatedCharacterClassNode(
            EmbeddedSyntaxToken openBracketToken, EmbeddedSyntaxToken caretToken, RegexSequenceNode components, EmbeddedSyntaxToken closeBracketToken)
            : base(RegexKind.NegatedCharacterClass, openBracketToken, components, closeBracketToken)
        {
            Debug.Assert(caretToken.Kind() == RegexKind.CaretToken);
            CaretToken = caretToken;
        }

        public EmbeddedSyntaxToken CaretToken { get; }

        public override int ChildCount => 4;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```a-z``` node in a character class.
    /// </summary>
    internal sealed class RegexCharacterClassRangeNode : RegexPrimaryExpressionNode
    {
        public RegexCharacterClassRangeNode(
            RegexExpressionNode left, EmbeddedSyntaxToken minusToken, RegexExpressionNode right)
            : base(RegexKind.CharacterClassRange)
        {
            Debug.Assert(left != null);
            Debug.Assert(minusToken.Kind() == RegexKind.MinusToken);
            Debug.Assert(right != null);
            Left = left;
            MinusToken = minusToken;
            Right = right;
        }

        public RegexExpressionNode Left { get; }
        public EmbeddedSyntaxToken MinusToken { get; }
        public RegexExpressionNode Right { get; }

        public override int ChildCount => 3;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return Left;
                case 1: return MinusToken;
                case 2: return Right;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```-[f-m]``` in a pattern like ```[a-z-[f-m]]```.  A subtraction must come last in a 
    /// character class, and removes some range of chars from the claracter class built up
    /// so far.
    /// </summary>
    internal sealed class RegexCharacterClassSubtractionNode : RegexPrimaryExpressionNode
    {
        public RegexCharacterClassSubtractionNode(
            EmbeddedSyntaxToken minusToken, RegexBaseCharacterClassNode characterClass)
            : base(RegexKind.CharacterClassSubtraction)
        {
            Debug.Assert(minusToken.Kind() == RegexKind.MinusToken);
            Debug.Assert(characterClass != null);
            MinusToken = minusToken;
            CharacterClass = characterClass;
        }

        public EmbeddedSyntaxToken MinusToken { get; }
        public RegexBaseCharacterClassNode CharacterClass { get; }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return MinusToken;
                case 1: return CharacterClass;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Represents a ```[:...:]``` node in a character class.  Note: the .net regex parser
    /// simply treats this as the character ```[``` and ignores the rest of the ```:...:]```.
    /// They latter part has no impact on the actual match engine that is produced.
    /// </summary>
    internal sealed class RegexPosixPropertyNode : RegexPrimaryExpressionNode
    {
        public RegexPosixPropertyNode(EmbeddedSyntaxToken textToken)
            : base(RegexKind.PosixProperty)
        {
            Debug.Assert(textToken.Kind() == RegexKind.TextToken);
            TextToken = textToken;
        }

        public EmbeddedSyntaxToken TextToken { get; }

        public override int ChildCount => 1;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index) => TextToken;

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Root of all expression nodes.
    /// </summary>
    internal abstract class RegexExpressionNode : RegexNode
    {
        protected RegexExpressionNode(RegexKind kind)
            : base(kind)
        {
        }
    }

    /// <summary>
    /// Root of all the primary nodes (simular to unary nodes in C#).
    /// </summary>
    internal abstract class RegexPrimaryExpressionNode : RegexExpressionNode
    {
        protected RegexPrimaryExpressionNode(RegexKind kind)
            : base(kind)
        {
        }
    }

    /// <summary>
    /// A ```.``` expression.
    /// </summary>
    internal sealed class RegexWildcardNode : RegexPrimaryExpressionNode
    {
        public RegexWildcardNode(EmbeddedSyntaxToken dotToken)
            : base(RegexKind.Wildcard)
        {
            Debug.Assert(dotToken.Kind() == RegexKind.DotToken);
            DotToken = dotToken;
        }

        public EmbeddedSyntaxToken DotToken { get; }

        public override int ChildCount => 1;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index) => DotToken;

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Root of all quantifier nodes: ```?```, ```*``` etc.
    /// </summary>
    internal abstract class RegexQuantifierNode : RegexExpressionNode
    {
        protected RegexQuantifierNode(RegexKind kind)
            : base(kind)
        {
        }
    }

    /// <summary>
    /// ```expr*```
    /// </summary>
    internal sealed class RegexZeroOrMoreQuantifierNode : RegexQuantifierNode
    {
        public RegexZeroOrMoreQuantifierNode(
            RegexExpressionNode expression, EmbeddedSyntaxToken asteriskToken)
            : base(RegexKind.ZeroOrMoreQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(asteriskToken.Kind() == RegexKind.AsteriskToken);
            Expression = expression;
            AsteriskToken = asteriskToken;
        }

        public RegexExpressionNode Expression { get; }
        public EmbeddedSyntaxToken AsteriskToken { get; }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Expression;
            case 1: return this.AsteriskToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```expr+```
    /// </summary>
    internal sealed class RegexOneOrMoreQuantifierNode : RegexQuantifierNode
    {
        public RegexOneOrMoreQuantifierNode(
            RegexExpressionNode expression, EmbeddedSyntaxToken plusToken)
            : base(RegexKind.OneOrMoreQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(plusToken.Kind() == RegexKind.PlusToken);
            Expression = expression;
            PlusToken = plusToken;
        }

        public RegexExpressionNode Expression { get; }
        public EmbeddedSyntaxToken PlusToken { get; }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Expression;
            case 1: return this.PlusToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```expr?```
    /// </summary>
    internal sealed class RegexZeroOrOneQuantifierNode : RegexQuantifierNode
    {
        public RegexZeroOrOneQuantifierNode(
            RegexExpressionNode expression, EmbeddedSyntaxToken questionToken)
            : base(RegexKind.ZeroOrOneQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(questionToken.Kind() == RegexKind.QuestionToken);
            Expression = expression;
            QuestionToken = questionToken;
        }

        public RegexExpressionNode Expression { get; }
        public EmbeddedSyntaxToken QuestionToken { get; }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Expression;
            case 1: return this.QuestionToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Quantifiers can be optionally followed by a ? to make them lazy.  i.e. ```a*?``` or ```a+?```.
    /// You can even have ```a??```  (zero or one 'a', lazy).  However, only one lazy modifier is alloed
    /// ```a*??``` or ```a???``` is not allowed.
    /// </summary>
    internal sealed class RegexLazyQuantifierNode : RegexExpressionNode
    {
        public RegexLazyQuantifierNode(
            RegexQuantifierNode quantifier, EmbeddedSyntaxToken questionToken)
            : base(RegexKind.LazyQuantifier)
        {
            Debug.Assert(quantifier != null);
            Debug.Assert(questionToken.Kind() == RegexKind.QuestionToken);
            Quantifier = quantifier;
            QuestionToken = questionToken;
        }

        public RegexQuantifierNode Quantifier { get; }
        public EmbeddedSyntaxToken QuestionToken { get; }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return this.Quantifier;
            case 1: return this.QuestionToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```a{...}```
    /// </summary>
    internal abstract class RegexNumericQuantifierNode : RegexQuantifierNode
    {
        protected RegexNumericQuantifierNode(
            RegexKind kind, RegexPrimaryExpressionNode expression, EmbeddedSyntaxToken openBraceToken, EmbeddedSyntaxToken firstNumberToken, EmbeddedSyntaxToken closeBraceToken)
            : base(kind)
        {
            Debug.Assert(expression != null);
            Debug.Assert(openBraceToken.Kind() == RegexKind.OpenBraceToken);
            Debug.Assert(firstNumberToken.Kind() == RegexKind.NumberToken);
            Debug.Assert(closeBraceToken.Kind() == RegexKind.CloseBraceToken);
            Expression = expression;
            OpenBraceToken = openBraceToken;
            FirstNumberToken = firstNumberToken;
            CloseBraceToken = closeBraceToken;
        }

        public RegexExpressionNode Expression { get; }
        public EmbeddedSyntaxToken OpenBraceToken { get; }
        public EmbeddedSyntaxToken FirstNumberToken { get; }
        public EmbeddedSyntaxToken CloseBraceToken { get; }
    }

    /// <summary>
    /// ```a{5}```
    /// </summary>
    internal sealed class RegexExactNumericQuantifierNode : RegexNumericQuantifierNode
    {
        public RegexExactNumericQuantifierNode(
            RegexPrimaryExpressionNode expression, EmbeddedSyntaxToken openBraceToken, EmbeddedSyntaxToken numberToken, EmbeddedSyntaxToken closeBraceToken)
            : base(RegexKind.ExactNumericQuantifier, expression, openBraceToken, numberToken, closeBraceToken)
        {
        }

        public override int ChildCount => 4;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```a{5,}```
    /// </summary>
    internal sealed class RegexOpenNumericRangeQuantifierNode : RegexNumericQuantifierNode
    {
        public RegexOpenNumericRangeQuantifierNode(
            RegexPrimaryExpressionNode expression,
            EmbeddedSyntaxToken openBraceToken, EmbeddedSyntaxToken firstNumberToken,
            EmbeddedSyntaxToken commaToken, EmbeddedSyntaxToken closeBraceToken)
            : base(RegexKind.OpenRangeNumericQuantifier, expression, openBraceToken, firstNumberToken, closeBraceToken)
        {
            Debug.Assert(commaToken.Kind() == RegexKind.CommaToken);
            CommaToken = commaToken;
        }

        public EmbeddedSyntaxToken CommaToken { get; }

        public override int ChildCount => 5;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```a{5,10}```
    /// </summary>
    internal sealed class RegexClosedNumericRangeQuantifierNode : RegexNumericQuantifierNode
    {
        public RegexClosedNumericRangeQuantifierNode(
            RegexPrimaryExpressionNode expression,
            EmbeddedSyntaxToken openBraceToken, EmbeddedSyntaxToken firstNumberToken,
            EmbeddedSyntaxToken commaToken, EmbeddedSyntaxToken secondNumberToken, EmbeddedSyntaxToken closeBraceToken)
            : base(RegexKind.ClosedRangeNumericQuantifier, expression, openBraceToken, firstNumberToken, closeBraceToken)
        {
            Debug.Assert(commaToken.Kind() == RegexKind.CommaToken);
            Debug.Assert(secondNumberToken.Kind() == RegexKind.NumberToken);
            CommaToken = commaToken;
            SecondNumberToken = secondNumberToken;
        }

        public EmbeddedSyntaxToken CommaToken { get; }
        public EmbeddedSyntaxToken SecondNumberToken { get; }

        public override int ChildCount => 6;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```$``` or ```^```.
    /// </summary>
    internal sealed class RegexAnchorNode : RegexPrimaryExpressionNode
    {
        public RegexAnchorNode(RegexKind kind, EmbeddedSyntaxToken anchorToken)
            : base(kind)
        {
            Debug.Assert(anchorToken.Kind() == RegexKind.DollarToken || anchorToken.Kind() == RegexKind.CaretToken);
            AnchorToken = anchorToken;
        }

        public EmbeddedSyntaxToken AnchorToken { get; }

        public override int ChildCount => 1;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index) => AnchorToken;

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```expr1|expr2``` node.
    /// </summary>
    internal sealed class RegexAlternationNode : RegexExpressionNode
    {
        public RegexAlternationNode(
            RegexExpressionNode left, EmbeddedSyntaxToken barToken, RegexSequenceNode right)
            : base(RegexKind.Alternation)
        {
            Debug.Assert(left != null);
            Debug.Assert(barToken.Kind() == RegexKind.BarToken);
            Debug.Assert(right != null);
            Left = left;
            BarToken = barToken;
            Right = right;
        }

        public RegexExpressionNode Left { get; }
        public EmbeddedSyntaxToken BarToken { get; }
        public RegexSequenceNode Right { get; }

        public override int ChildCount => 3;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return Left;
            case 1: return BarToken;
            case 2: return Right;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type of all non-trivia ```(...)``` nodes
    /// </summary>
    internal abstract class RegexGroupingNode : RegexPrimaryExpressionNode
    {
        protected RegexGroupingNode(RegexKind kind, EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken closeParenToken)
            : base(kind)
        {
            Debug.Assert(openParenToken.Kind() == RegexKind.OpenParenToken);
            Debug.Assert(closeParenToken.Kind() == RegexKind.CloseParenToken);
            OpenParenToken = openParenToken;
            CloseParenToken = closeParenToken;
        }

        public EmbeddedSyntaxToken OpenParenToken { get; }
        public EmbeddedSyntaxToken CloseParenToken { get; }
    }

    /// <summary>
    /// The ```(...)``` node you get when the group does not start with ```(?```
    /// </summary>
    internal class RegexSimpleGroupingNode : RegexGroupingNode
    {
        public RegexSimpleGroupingNode(EmbeddedSyntaxToken openParenToken, RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.SimpleGrouping, openParenToken, closeParenToken)
        {
            Debug.Assert(expression != null);
            Expression = expression;
        }

        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 3;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return OpenParenToken;
            case 1: return Expression;
            case 2: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type of all ```(?...)``` groupings.
    /// </summary>
    internal abstract class RegexQuestionGroupingNode : RegexGroupingNode
    {
        protected RegexQuestionGroupingNode(RegexKind kind, EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken closeParenToken)
            : base(kind, openParenToken, closeParenToken)
        {
            Debug.Assert(questionToken.Kind() == RegexKind.QuestionToken);
            QuestionToken = questionToken;
        }

        public EmbeddedSyntaxToken QuestionToken { get; }
    }

    /// <summary>
    /// Base type of ```(?inmsx)``` or ```(?inmsx:...)``` nodes.
    /// </summary>
    internal abstract class RegexOptionsGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexOptionsGroupingNode(RegexKind kind, EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken optionsToken, EmbeddedSyntaxToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            OptionsToken = optionsToken;
        }

        public EmbeddedSyntaxToken OptionsToken { get; }
    }

    /// <summary>
    /// ```(?inmsx)``` node.  Changes options in a sequence for all subsequence nodes.
    /// </summary>
    internal class RegexSimpleOptionsGroupingNode : RegexOptionsGroupingNode
    {
        public RegexSimpleOptionsGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken optionsToken, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.SimpleOptionsGrouping, openParenToken, questionToken, optionsToken, closeParenToken)
        {
        }

        public override int ChildCount => 4;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?inmsx:expr)``` node.  Changes options for the parsing of 'expr'.
    /// </summary>
    internal class RegexNestedOptionsGroupingNode : RegexOptionsGroupingNode
    {
        public RegexNestedOptionsGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken optionsToken,
            EmbeddedSyntaxToken colonToken, RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.NestedOptionsGrouping, openParenToken, questionToken, optionsToken, closeParenToken)
        {
            Debug.Assert(colonToken.Kind() == RegexKind.ColonToken);
            Debug.Assert(expression != null);
            ColonToken = colonToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken ColonToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 6;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?:expr)``` node.
    /// </summary>
    internal sealed class RegexNonCapturingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNonCapturingGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken colonToken, 
            RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken) 
            : base(RegexKind.NonCapturingGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(colonToken.Kind() == RegexKind.ColonToken);
            Debug.Assert(expression != null);
            ColonToken = colonToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken ColonToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?=expr)``` node.
    /// </summary>
    internal sealed class RegexPositiveLookaheadGroupingNode : RegexQuestionGroupingNode
    {
        public RegexPositiveLookaheadGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken equalsToken,
            RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.PositiveLookaheadGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(equalsToken.Kind() == RegexKind.EqualsToken);
            Debug.Assert(expression != null);
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken EqualsToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?!expr)``` node.
    /// </summary>
    internal sealed class RegexNegativeLookaheadGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNegativeLookaheadGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken exclamationToken,
            RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.NegativeLookaheadGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(exclamationToken.Kind() == RegexKind.ExclamationToken);
            Debug.Assert(expression != null);
            ExclamationToken = exclamationToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken ExclamationToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal abstract class RegexLookbehindGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexLookbehindGroupingNode(
            RegexKind kind, EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken,
            EmbeddedSyntaxToken lessThanToken, EmbeddedSyntaxToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(lessThanToken.Kind() == RegexKind.LessThanToken);
            LessThanToken = lessThanToken;
        }

        public EmbeddedSyntaxToken LessThanToken { get; }
    }

    /// <summary>
    /// ```(?&lt;=expr)``` node.
    /// </summary>
    internal sealed class RegexPositiveLookbehindGroupingNode : RegexLookbehindGroupingNode
    {
        public RegexPositiveLookbehindGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken,EmbeddedSyntaxToken lessThanToken,
            EmbeddedSyntaxToken equalsToken, RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.PositiveLookbehindGrouping, openParenToken, questionToken, lessThanToken, closeParenToken)
        {
            Debug.Assert(equalsToken.Kind() == RegexKind.EqualsToken);
            Debug.Assert(expression != null);
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken EqualsToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 6;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?&lt;!expr)``` node.
    /// </summary>
    internal sealed class RegexNegativeLookbehindGroupingNode : RegexLookbehindGroupingNode
    {
        public RegexNegativeLookbehindGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken lessThanToken,
            EmbeddedSyntaxToken exclamationToken, RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.NegativeLookbehindGrouping, openParenToken, questionToken, lessThanToken, closeParenToken)
        {
            Debug.Assert(exclamationToken.Kind() == RegexKind.ExclamationToken);
            Debug.Assert(expression != null);
            ExclamationToken = exclamationToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken ExclamationToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 6;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?&gt;expr)``` node.
    /// </summary>
    internal sealed class RegexNonBacktrackingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNonBacktrackingGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken greaterThanToken,
            RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.NonBacktrackingGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(greaterThanToken.Kind() == RegexKind.GreaterThanToken);
            Debug.Assert(expression != null);
            GreaterThanToken = greaterThanToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken GreaterThanToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 5;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?'name'expr)``` or ```(?&lt;name&gt;expr)``` node.
    /// </summary>
    internal sealed class RegexCaptureGroupingNode : RegexQuestionGroupingNode
    {
        public RegexCaptureGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken openToken, 
            EmbeddedSyntaxToken captureToken, EmbeddedSyntaxToken closeToken, 
            RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken) 
            : base(RegexKind.CaptureGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(expression != null);
            OpenToken = openToken;
            CaptureToken = captureToken;
            CloseToken = closeToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken OpenToken { get; }
        public EmbeddedSyntaxToken CaptureToken { get; }
        public EmbeddedSyntaxToken CloseToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 7;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?'name1-name2'expr)``` or ```(?&lt;name1-name2&gt;expr)``` node.
    /// </summary>
    internal sealed class RegexBalancingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexBalancingGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, EmbeddedSyntaxToken openToken,
            EmbeddedSyntaxToken firstCaptureToken, EmbeddedSyntaxToken minusToken, EmbeddedSyntaxToken secondCaptureToken,
            EmbeddedSyntaxToken closeToken, RegexExpressionNode expression, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.BalancingGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(minusToken.Kind() == RegexKind.MinusToken);
            Debug.Assert(expression != null);
            OpenToken = openToken;
            FirstCaptureToken = firstCaptureToken;
            MinusToken = minusToken;
            SecondCaptureToken = secondCaptureToken;
            CloseToken = closeToken;
            Expression = expression;
        }

        public EmbeddedSyntaxToken OpenToken { get; }
        public EmbeddedSyntaxToken FirstCaptureToken { get; }
        public EmbeddedSyntaxToken MinusToken { get; }
        public EmbeddedSyntaxToken SecondCaptureToken { get; }
        public EmbeddedSyntaxToken CloseToken { get; }
        public RegexExpressionNode Expression { get; }

        public override int ChildCount => 9;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal abstract class RegexConditionalGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexConditionalGroupingNode(
            RegexKind kind, EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken,
            RegexExpressionNode result, EmbeddedSyntaxToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(result != null);
            Result = result;
        }

        public RegexExpressionNode Result { get; }
    }

    /// <summary>
    /// ```(?(capture_name)result)```
    /// </summary>
    internal sealed class RegexConditionalCaptureGroupingNode : RegexConditionalGroupingNode
    {
        public RegexConditionalCaptureGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken, 
            EmbeddedSyntaxToken innerOpenParenToken, EmbeddedSyntaxToken captureToken, EmbeddedSyntaxToken innerCloseParenToken,
            RegexExpressionNode result, EmbeddedSyntaxToken closeParenToken) 
            : base(RegexKind.ConditionalCaptureGrouping, openParenToken, questionToken, result, closeParenToken)
        {
            Debug.Assert(innerOpenParenToken.Kind() == RegexKind.OpenParenToken);
            Debug.Assert(innerCloseParenToken.Kind() == RegexKind.CloseParenToken);
            InnerOpenParenToken = innerOpenParenToken;
            CaptureToken = captureToken;
            InnerCloseParenToken = innerCloseParenToken;
        }

        public EmbeddedSyntaxToken InnerOpenParenToken { get; }
        public EmbeddedSyntaxToken CaptureToken { get; }
        public EmbeddedSyntaxToken InnerCloseParenToken { get; }

        public override int ChildCount => 7;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?(group)result)```
    /// </summary>
    internal sealed class RegexConditionalExpressionGroupingNode : RegexConditionalGroupingNode
    {
        public RegexConditionalExpressionGroupingNode(
            EmbeddedSyntaxToken openParenToken, EmbeddedSyntaxToken questionToken,
            RegexGroupingNode grouping,
            RegexExpressionNode result, EmbeddedSyntaxToken closeParenToken)
            : base(RegexKind.ConditionalExpressionGrouping, openParenToken, questionToken, result, closeParenToken)
        {
            Debug.Assert(grouping != null);
            Grouping = grouping;
        }

        public override int ChildCount => 5;

        public RegexGroupingNode Grouping { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type of all regex primitives that start with \
    /// </summary>
    internal abstract class RegexEscapeNode : RegexPrimaryExpressionNode
    {
        protected RegexEscapeNode(RegexKind kind, EmbeddedSyntaxToken backslashToken) : base(kind)
        {
            Debug.Assert(backslashToken.Kind() == RegexKind.BackslashToken);
            BackslashToken = backslashToken;
        }

        public EmbeddedSyntaxToken BackslashToken { get; }
    }

    /// <summary>
    /// Base type of all regex escapes that start with \ and some informative character (like \v \t \c etc.).
    /// </summary>
    internal abstract class RegexTypeEscapeNode : RegexEscapeNode
    {
        protected RegexTypeEscapeNode(RegexKind kind, EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken)
            : base(kind, backslashToken)
        {
            TypeToken = typeToken;
        }

        public EmbeddedSyntaxToken TypeToken { get; }
    }

    /// <summary>
    /// A basic escape that just has \ and one additional character and needs no further information.
    /// </summary>
    internal sealed class RegexSimpleEscapeNode : RegexTypeEscapeNode
    {
        public RegexSimpleEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken)
            : base(RegexKind.SimpleEscape, backslashToken, typeToken)
        {
            Debug.Assert(typeToken.Kind() == RegexKind.TextToken);
        }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
            case 0: return BackslashToken;
            case 1: return TypeToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// One of \b \B \A \G \z \Z
    /// </summary>
    internal sealed class RegexAnchorEscapeNode : RegexTypeEscapeNode
    {
        public RegexAnchorEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken)
            : base(RegexKind.AnchorEscape, backslashToken, typeToken)
        {
        }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// One of \s \S \d \D \w \W
    /// </summary>
    internal sealed class RegexCharacterClassEscapeNode : RegexTypeEscapeNode
    {
        public RegexCharacterClassEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken)
            : base(RegexKind.CharacterClassEscape, backslashToken, typeToken)
        {
        }

        public override int ChildCount => 2;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\cX``` escape
    /// </summary>
    internal sealed class RegexControlEscapeNode : RegexTypeEscapeNode
    {
        public RegexControlEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken, EmbeddedSyntaxToken controlToken)
            : base(RegexKind.ControlEscape, backslashToken, typeToken)
        {
            ControlToken = controlToken;
        }

        public override int ChildCount => 3;

        public EmbeddedSyntaxToken ControlToken { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
                case 2: return ControlToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\xFF``` escape.
    /// </summary>
    internal sealed class RegexHexEscapeNode : RegexTypeEscapeNode
    {
        public RegexHexEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken, EmbeddedSyntaxToken hexText)
            : base(RegexKind.HexEscape, backslashToken, typeToken)
        {
            HexText = hexText;
        }

        public override int ChildCount => 3;

        public EmbeddedSyntaxToken HexText { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
                case 2: return HexText;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\uFFFF``` escape.
    /// </summary>
    internal sealed class RegexUnicodeEscapeNode : RegexTypeEscapeNode
    {
        public RegexUnicodeEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken, EmbeddedSyntaxToken hexText)
            : base(RegexKind.UnicodeEscape, backslashToken, typeToken)
        {
            HexText = hexText;
        }

        public override int ChildCount => 3;

        public EmbeddedSyntaxToken HexText { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return TypeToken;
                case 2: return HexText;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\'name'``` or ```\&lt;name&gt;``` escape.
    /// </summary>
    internal sealed class RegexCaptureEscapeNode : RegexEscapeNode
    {
        public RegexCaptureEscapeNode(
            EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken openToken, EmbeddedSyntaxToken captureToken, EmbeddedSyntaxToken closeToken)
            : base(RegexKind.CaptureEscape, backslashToken)
        {
            OpenToken = openToken;
            CaptureToken = captureToken;
            CloseToken = closeToken;
        }

        public override int ChildCount => 4;

        public EmbeddedSyntaxToken OpenToken { get; }
        public EmbeddedSyntaxToken CaptureToken { get; }
        public EmbeddedSyntaxToken CloseToken { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\k'name'``` or ```\k&lt;name&gt;``` escape.
    /// </summary>
    internal sealed class RegexKCaptureEscapeNode : RegexTypeEscapeNode
    {
        public RegexKCaptureEscapeNode(
            EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken,
            EmbeddedSyntaxToken openToken, EmbeddedSyntaxToken captureToken, EmbeddedSyntaxToken closeToken)
            : base(RegexKind.KCaptureEscape, backslashToken, typeToken)
        {
            OpenToken = openToken;
            CaptureToken = captureToken;
            CloseToken = closeToken;
        }

        public override int ChildCount => 5;

        public EmbeddedSyntaxToken OpenToken { get; }
        public EmbeddedSyntaxToken CaptureToken { get; }
        public EmbeddedSyntaxToken CloseToken { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\1``` escape. In contexts where backreferences are not allowed.
    /// </summary>
    internal sealed class RegexOctalEscapeNode : RegexEscapeNode
    {
        public RegexOctalEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken octalText)
            : base(RegexKind.OctalEscape, backslashToken)
        {
            OctalText = octalText;
        }

        public override int ChildCount => 2;

        public EmbeddedSyntaxToken OctalText { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return OctalText;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\1```
    /// </summary>
    internal sealed class RegexBackreferenceEscapeNode : RegexEscapeNode
    {
        public RegexBackreferenceEscapeNode(EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken numberToken)
            : base(RegexKind.BackreferenceEscape, backslashToken)
        {
            NumberToken = numberToken;
        }

        public override int ChildCount => 2;

        public EmbeddedSyntaxToken NumberToken { get; }

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
        {
            switch (index)
            {
                case 0: return BackslashToken;
                case 1: return NumberToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\p{...}```
    /// </summary>
    internal sealed class RegexCategoryEscapeNode : RegexEscapeNode
    {
        public RegexCategoryEscapeNode(
            EmbeddedSyntaxToken backslashToken, EmbeddedSyntaxToken typeToken, EmbeddedSyntaxToken openBraceToken, EmbeddedSyntaxToken categoryToken, EmbeddedSyntaxToken closeBraceToken)
            : base(RegexKind.CategoryEscape, backslashToken)
        {
            Debug.Assert(openBraceToken.Kind() == RegexKind.OpenBraceToken);
            Debug.Assert(closeBraceToken.Kind() == RegexKind.CloseBraceToken);
            TypeToken = typeToken;
            OpenBraceToken = openBraceToken;
            CategoryToken = categoryToken;
            CloseBraceToken = closeBraceToken;
        }

        public EmbeddedSyntaxToken TypeToken { get; }
        public EmbeddedSyntaxToken OpenBraceToken { get; }
        public EmbeddedSyntaxToken CategoryToken { get; }
        public EmbeddedSyntaxToken CloseBraceToken { get; }

        public override int ChildCount => 5;

        public override EmbeddedSyntaxNodeOrToken<RegexNode> ChildAt(int index)
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

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }
}
