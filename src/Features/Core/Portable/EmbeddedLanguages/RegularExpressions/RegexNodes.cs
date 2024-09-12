// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using RegexNodeOrToken = EmbeddedSyntaxNodeOrToken<RegexKind, RegexNode>;
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexAlternatingSequenceList = EmbeddedSeparatedSyntaxNodeList<RegexKind, RegexNode, RegexSequenceNode>;

    internal sealed class RegexCompilationUnit : RegexNode
    {
        public RegexCompilationUnit(RegexExpressionNode expression, RegexToken endOfFileToken)
            : base(RegexKind.CompilationUnit)
        {
            Debug.Assert(expression != null);
            Debug.Assert(endOfFileToken.Kind == RegexKind.EndOfFile);
            Expression = expression;
            EndOfFileToken = endOfFileToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken EndOfFileToken { get; }

        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Expression,
                1 => EndOfFileToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Represents a possibly-empty sequence of regex expressions.  For example, the regex ""
    /// will produce an empty RegexSequence nodes, and "a|" will produce an alternation with an
    /// empty sequence on the right side.  Having a node represent the empty sequence is actually
    /// appropriate as these are legal regexes and the empty sequence represents 'a pattern
    /// that will match any position'.  Not having a node for this would actually end up 
    /// complicating things in terms of dealing with nulls in the tree.
    /// 
    /// This does not deviate from Roslyn principles.  While nodes for empty text are rare, they
    /// are allowed (for example, OmittedTypeArgument in C#).
    /// </summary>
    internal sealed class RegexSequenceNode(ImmutableArray<RegexExpressionNode> children) : RegexExpressionNode(RegexKind.Sequence)
    {
        public ImmutableArray<RegexExpressionNode> Children { get; } = children;

        internal override int ChildCount => Children.Length;

        internal override RegexNodeOrToken ChildAt(int index)
            => Children[index];

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Represents a chunk of text (usually just a single char) from the original pattern.
    /// </summary>
    internal sealed class RegexTextNode : RegexPrimaryExpressionNode
    {
        public RegexTextNode(RegexToken textToken)
            : base(RegexKind.Text)
        {
            Debug.Assert(textToken.Kind == RegexKind.TextToken);
            TextToken = textToken;
        }

        public RegexToken TextToken { get; }

        internal override int ChildCount => 1;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => TextToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type for [...] and [^...] character classes.
    /// </summary>
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

    /// <summary>
    /// [...] node.
    /// </summary>
    internal sealed class RegexCharacterClassNode(
        RegexToken openBracketToken, RegexSequenceNode components, RegexToken closeBracketToken) : RegexBaseCharacterClassNode(RegexKind.CharacterClass, openBracketToken, components, closeBracketToken)
    {
        internal override int ChildCount => 3;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenBracketToken,
                1 => Components,
                2 => CloseBracketToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// [^...] node
    /// </summary>
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

        internal override int ChildCount => 4;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenBracketToken,
                1 => CaretToken,
                2 => Components,
                3 => CloseBracketToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```a-z``` node in a character class.
    /// </summary>
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

        internal override int ChildCount => 3;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Left,
                1 => MinusToken,
                2 => Right,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```-[f-m]``` in a pattern like ```[a-z-[f-m]]```.  A subtraction must come last in a 
    /// character class, and removes some range of chars from the character class built up
    /// so far.
    /// </summary>
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

        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => MinusToken,
                1 => CharacterClass,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Represents a ```[:...:]``` node in a character class.  Note: the .NET regex parser
    /// simply treats this as the character ```[``` and ignores the rest of the ```:...:]```.
    /// They latter part has no impact on the actual match engine that is produced.
    /// </summary>
    internal sealed class RegexPosixPropertyNode : RegexPrimaryExpressionNode
    {
        public RegexPosixPropertyNode(RegexToken textToken)
            : base(RegexKind.PosixProperty)
        {
            Debug.Assert(textToken.Kind == RegexKind.TextToken);
            TextToken = textToken;
        }

        public RegexToken TextToken { get; }

        internal override int ChildCount => 1;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => TextToken,
                _ => throw new InvalidOperationException(),
            };

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
    /// Root of all the primary nodes (similar to unary nodes in C#).
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
        public RegexWildcardNode(RegexToken dotToken)
            : base(RegexKind.Wildcard)
        {
            Debug.Assert(dotToken.Kind == RegexKind.DotToken);
            DotToken = dotToken;
        }

        public RegexToken DotToken { get; }

        internal override int ChildCount => 1;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => DotToken,
                _ => throw new InvalidOperationException(),
            };

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
            RegexExpressionNode expression, RegexToken asteriskToken)
            : base(RegexKind.ZeroOrMoreQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(asteriskToken.Kind == RegexKind.AsteriskToken);
            Expression = expression;
            AsteriskToken = asteriskToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken AsteriskToken { get; }

        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => this.Expression,
                1 => this.AsteriskToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```expr+```
    /// </summary>
    internal sealed class RegexOneOrMoreQuantifierNode : RegexQuantifierNode
    {
        public RegexOneOrMoreQuantifierNode(
            RegexExpressionNode expression, RegexToken plusToken)
            : base(RegexKind.OneOrMoreQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(plusToken.Kind == RegexKind.PlusToken);
            Expression = expression;
            PlusToken = plusToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken PlusToken { get; }

        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => this.Expression,
                1 => this.PlusToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```expr?```
    /// </summary>
    internal sealed class RegexZeroOrOneQuantifierNode : RegexQuantifierNode
    {
        public RegexZeroOrOneQuantifierNode(
            RegexExpressionNode expression, RegexToken questionToken)
            : base(RegexKind.ZeroOrOneQuantifier)
        {
            Debug.Assert(expression != null);
            Debug.Assert(questionToken.Kind == RegexKind.QuestionToken);
            Expression = expression;
            QuestionToken = questionToken;
        }

        public RegexExpressionNode Expression { get; }
        public RegexToken QuestionToken { get; }

        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => this.Expression,
                1 => this.QuestionToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Quantifiers can be optionally followed by a ? to make them lazy.  i.e. ```a*?``` or ```a+?```.
    /// You can even have ```a??```  (zero or one 'a', lazy).  However, only one lazy modifier is allowed
    /// ```a*??``` or ```a???``` is not allowed.
    /// </summary>
    internal sealed class RegexLazyQuantifierNode : RegexExpressionNode
    {
        public RegexLazyQuantifierNode(
            RegexQuantifierNode quantifier, RegexToken questionToken)
            : base(RegexKind.LazyQuantifier)
        {
            Debug.Assert(quantifier != null);
            Debug.Assert(quantifier.Kind != RegexKind.LazyQuantifier);
            Debug.Assert(questionToken.Kind == RegexKind.QuestionToken);
            Quantifier = quantifier;
            QuestionToken = questionToken;
        }

        public RegexQuantifierNode Quantifier { get; }
        public RegexToken QuestionToken { get; }

        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => this.Quantifier,
                1 => this.QuestionToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type of all regex numeric quantifier nodes.  i.e.  
    /// ```a{5}```,  ```a{5,}``` and ```a{5,10}```
    /// </summary>
    internal abstract class RegexNumericQuantifierNode : RegexQuantifierNode
    {
        protected RegexNumericQuantifierNode(
            RegexKind kind, RegexPrimaryExpressionNode expression, RegexToken openBraceToken, RegexToken firstNumberToken, RegexToken closeBraceToken)
            : base(kind)
        {
            Debug.Assert(expression != null);
            Debug.Assert(openBraceToken.Kind == RegexKind.OpenBraceToken);
            Debug.Assert(firstNumberToken.Kind == RegexKind.NumberToken);
            Debug.Assert(closeBraceToken.Kind == RegexKind.CloseBraceToken);
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

    /// <summary>
    /// ```a{5}```
    /// </summary>
    internal sealed class RegexExactNumericQuantifierNode(
        RegexPrimaryExpressionNode expression, RegexToken openBraceToken, RegexToken numberToken, RegexToken closeBraceToken) : RegexNumericQuantifierNode(RegexKind.ExactNumericQuantifier, expression, openBraceToken, numberToken, closeBraceToken)
    {
        internal override int ChildCount => 4;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Expression,
                1 => OpenBraceToken,
                2 => FirstNumberToken,
                3 => CloseBraceToken,
                _ => throw new InvalidOperationException(),
            };

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
            RegexToken openBraceToken, RegexToken firstNumberToken,
            RegexToken commaToken, RegexToken closeBraceToken)
            : base(RegexKind.OpenRangeNumericQuantifier, expression, openBraceToken, firstNumberToken, closeBraceToken)
        {
            Debug.Assert(commaToken.Kind == RegexKind.CommaToken);
            CommaToken = commaToken;
        }

        public RegexToken CommaToken { get; }

        internal override int ChildCount => 5;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Expression,
                1 => OpenBraceToken,
                2 => FirstNumberToken,
                3 => CommaToken,
                4 => CloseBraceToken,
                _ => throw new InvalidOperationException(),
            };

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
            RegexToken openBraceToken, RegexToken firstNumberToken,
            RegexToken commaToken, RegexToken secondNumberToken, RegexToken closeBraceToken)
            : base(RegexKind.ClosedRangeNumericQuantifier, expression, openBraceToken, firstNumberToken, closeBraceToken)
        {
            Debug.Assert(commaToken.Kind == RegexKind.CommaToken);
            Debug.Assert(secondNumberToken.Kind == RegexKind.NumberToken);
            CommaToken = commaToken;
            SecondNumberToken = secondNumberToken;
        }

        public RegexToken CommaToken { get; }
        public RegexToken SecondNumberToken { get; }

        internal override int ChildCount => 6;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Expression,
                1 => OpenBraceToken,
                2 => FirstNumberToken,
                3 => CommaToken,
                4 => SecondNumberToken,
                5 => CloseBraceToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```$``` or ```^```.
    /// </summary>
    internal sealed class RegexAnchorNode : RegexPrimaryExpressionNode
    {
        public RegexAnchorNode(RegexKind kind, RegexToken anchorToken)
            : base(kind)
        {
            Debug.Assert(anchorToken.Kind is RegexKind.DollarToken or RegexKind.CaretToken);
            AnchorToken = anchorToken;
        }

        public RegexToken AnchorToken { get; }

        internal override int ChildCount => 1;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => AnchorToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```expr1|expr2``` node.
    /// </summary>
    internal sealed class RegexAlternationNode : RegexExpressionNode
    {
        public RegexAlternationNode(RegexAlternatingSequenceList sequenceList)
            : base(RegexKind.Alternation)
        {
            Debug.Assert(sequenceList.NodesAndTokens.Length > 0);
            for (var i = 1; i < sequenceList.NodesAndTokens.Length; i += 2)
                Debug.Assert(sequenceList.NodesAndTokens[i].Kind == RegexKind.BarToken);

            SequenceList = sequenceList;
        }

        public RegexAlternatingSequenceList SequenceList { get; }

        internal override int ChildCount => SequenceList.NodesAndTokens.Length;

        internal override RegexNodeOrToken ChildAt(int index)
            => SequenceList.NodesAndTokens[index];

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type of all non-trivia ```(...)``` nodes
    /// </summary>
    internal abstract class RegexGroupingNode : RegexPrimaryExpressionNode
    {
        protected RegexGroupingNode(RegexKind kind, RegexToken openParenToken, RegexToken closeParenToken)
            : base(kind)
        {
            Debug.Assert(openParenToken.Kind == RegexKind.OpenParenToken);
            Debug.Assert(closeParenToken.Kind == RegexKind.CloseParenToken);
            OpenParenToken = openParenToken;
            CloseParenToken = closeParenToken;
        }

        public RegexToken OpenParenToken { get; }
        public RegexToken CloseParenToken { get; }
    }

    /// <summary>
    /// The ```(...)``` node you get when the group does not start with ```(?```
    /// </summary>
    internal sealed class RegexSimpleGroupingNode : RegexGroupingNode
    {
        public RegexSimpleGroupingNode(RegexToken openParenToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.SimpleGrouping, openParenToken, closeParenToken)
        {
            Debug.Assert(expression != null);
            Expression = expression;
        }

        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 3;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => Expression,
                2 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type of all ```(?...)``` groupings.
    /// </summary>
    internal abstract class RegexQuestionGroupingNode : RegexGroupingNode
    {
        protected RegexQuestionGroupingNode(RegexKind kind, RegexToken openParenToken, RegexToken questionToken, RegexToken closeParenToken)
            : base(kind, openParenToken, closeParenToken)
        {
            Debug.Assert(questionToken.Kind == RegexKind.QuestionToken);
            QuestionToken = questionToken;
        }

        public RegexToken QuestionToken { get; }
    }

    /// <summary>
    /// Base type of ```(?inmsx)``` or ```(?inmsx:...)``` nodes.
    /// </summary>
    internal abstract class RegexOptionsGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexOptionsGroupingNode(RegexKind kind, RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken, RegexToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            OptionsToken = optionsToken;
        }

        public RegexToken OptionsToken { get; }
    }

    /// <summary>
    /// ```(?inmsx)``` node.  Changes options in a sequence for all subsequence nodes.
    /// </summary>
    internal sealed class RegexSimpleOptionsGroupingNode(
        RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken, RegexToken closeParenToken) : RegexOptionsGroupingNode(RegexKind.SimpleOptionsGrouping, openParenToken, questionToken, optionsToken, closeParenToken)
    {
        internal override int ChildCount => 4;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => OptionsToken,
                3 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?inmsx:expr)``` node.  Changes options for the parsing of 'expr'.
    /// </summary>
    internal sealed class RegexNestedOptionsGroupingNode : RegexOptionsGroupingNode
    {
        public RegexNestedOptionsGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken optionsToken,
            RegexToken colonToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NestedOptionsGrouping, openParenToken, questionToken, optionsToken, closeParenToken)
        {
            Debug.Assert(colonToken.Kind == RegexKind.ColonToken);
            Debug.Assert(expression != null);
            ColonToken = colonToken;
            Expression = expression;
        }

        public RegexToken ColonToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 6;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => OptionsToken,
                3 => ColonToken,
                4 => Expression,
                5 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?:expr)``` node.
    /// </summary>
    internal sealed class RegexNonCapturingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNonCapturingGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken colonToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NonCapturingGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(colonToken.Kind == RegexKind.ColonToken);
            Debug.Assert(expression != null);
            ColonToken = colonToken;
            Expression = expression;
        }

        public RegexToken ColonToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 5;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => ColonToken,
                3 => Expression,
                4 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?=expr)``` node.
    /// </summary>
    internal sealed class RegexPositiveLookaheadGroupingNode : RegexQuestionGroupingNode
    {
        public RegexPositiveLookaheadGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken equalsToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.PositiveLookaheadGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(equalsToken.Kind == RegexKind.EqualsToken);
            Debug.Assert(expression != null);
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public RegexToken EqualsToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 5;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => EqualsToken,
                3 => Expression,
                4 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?!expr)``` node.
    /// </summary>
    internal sealed class RegexNegativeLookaheadGroupingNode : RegexQuestionGroupingNode
    {
        public RegexNegativeLookaheadGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken exclamationToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NegativeLookaheadGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(exclamationToken.Kind == RegexKind.ExclamationToken);
            Debug.Assert(expression != null);
            ExclamationToken = exclamationToken;
            Expression = expression;
        }

        public RegexToken ExclamationToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 5;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => ExclamationToken,
                3 => Expression,
                4 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal abstract class RegexLookbehindGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexLookbehindGroupingNode(
            RegexKind kind, RegexToken openParenToken, RegexToken questionToken,
            RegexToken lessThanToken, RegexToken closeParenToken)
            : base(kind, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(lessThanToken.Kind == RegexKind.LessThanToken);
            LessThanToken = lessThanToken;
        }

        public RegexToken LessThanToken { get; }
    }

    /// <summary>
    /// ```(?&lt;=expr)``` node.
    /// </summary>
    internal sealed class RegexPositiveLookbehindGroupingNode : RegexLookbehindGroupingNode
    {
        public RegexPositiveLookbehindGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken lessThanToken,
            RegexToken equalsToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.PositiveLookbehindGrouping, openParenToken, questionToken, lessThanToken, closeParenToken)
        {
            Debug.Assert(equalsToken.Kind == RegexKind.EqualsToken);
            Debug.Assert(expression != null);
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public RegexToken EqualsToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 6;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => LessThanToken,
                3 => EqualsToken,
                4 => Expression,
                5 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?&lt;!expr)``` node.
    /// </summary>
    internal sealed class RegexNegativeLookbehindGroupingNode : RegexLookbehindGroupingNode
    {
        public RegexNegativeLookbehindGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken lessThanToken,
            RegexToken exclamationToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.NegativeLookbehindGrouping, openParenToken, questionToken, lessThanToken, closeParenToken)
        {
            Debug.Assert(exclamationToken.Kind == RegexKind.ExclamationToken);
            Debug.Assert(expression != null);
            ExclamationToken = exclamationToken;
            Expression = expression;
        }

        public RegexToken ExclamationToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 6;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => LessThanToken,
                3 => ExclamationToken,
                4 => Expression,
                5 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?&gt;expr)``` node.
    /// </summary>
    internal sealed class RegexAtomicGroupingNode : RegexQuestionGroupingNode
    {
        public RegexAtomicGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken greaterThanToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.AtomicGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(greaterThanToken.Kind == RegexKind.GreaterThanToken);
            Debug.Assert(expression != null);
            GreaterThanToken = greaterThanToken;
            Expression = expression;
        }

        public RegexToken GreaterThanToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 5;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => GreaterThanToken,
                3 => Expression,
                4 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?'name'expr)``` or ```(?&lt;name&gt;expr)``` node.
    /// </summary>
    internal sealed class RegexCaptureGroupingNode : RegexQuestionGroupingNode
    {
        public RegexCaptureGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken openToken,
            RegexToken captureToken, RegexToken closeToken,
            RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.CaptureGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(expression != null);
            OpenToken = openToken;
            CaptureToken = captureToken;
            CloseToken = closeToken;
            Expression = expression;
        }

        public RegexToken OpenToken { get; }
        public RegexToken CaptureToken { get; }
        public RegexToken CloseToken { get; }
        public RegexExpressionNode Expression { get; }

        internal override int ChildCount => 7;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => OpenToken,
                3 => CaptureToken,
                4 => CloseToken,
                5 => Expression,
                6 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?'name1-name2'expr)``` or ```(?&lt;name1-name2&gt;expr)``` node.
    /// </summary>
    internal sealed class RegexBalancingGroupingNode : RegexQuestionGroupingNode
    {
        public RegexBalancingGroupingNode(
            RegexToken openParenToken, RegexToken questionToken, RegexToken openToken,
            RegexToken firstCaptureToken, RegexToken minusToken, RegexToken secondCaptureToken,
            RegexToken closeToken, RegexExpressionNode expression, RegexToken closeParenToken)
            : base(RegexKind.BalancingGrouping, openParenToken, questionToken, closeParenToken)
        {
            Debug.Assert(minusToken.Kind == RegexKind.MinusToken);
            Debug.Assert(expression != null);
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

        internal override int ChildCount => 9;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => OpenToken,
                3 => FirstCaptureToken,
                4 => MinusToken,
                5 => SecondCaptureToken,
                6 => CloseToken,
                7 => Expression,
                8 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal abstract class RegexConditionalGroupingNode : RegexQuestionGroupingNode
    {
        protected RegexConditionalGroupingNode(
            RegexKind kind, RegexToken openParenToken, RegexToken questionToken,
            RegexExpressionNode result, RegexToken closeParenToken)
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
            RegexToken openParenToken, RegexToken questionToken,
            RegexToken innerOpenParenToken, RegexToken captureToken, RegexToken innerCloseParenToken,
            RegexExpressionNode result, RegexToken closeParenToken)
            : base(RegexKind.ConditionalCaptureGrouping, openParenToken, questionToken, result, closeParenToken)
        {
            Debug.Assert(innerOpenParenToken.Kind == RegexKind.OpenParenToken);
            Debug.Assert(innerCloseParenToken.Kind == RegexKind.CloseParenToken);
            InnerOpenParenToken = innerOpenParenToken;
            CaptureToken = captureToken;
            InnerCloseParenToken = innerCloseParenToken;
        }

        public RegexToken InnerOpenParenToken { get; }
        public RegexToken CaptureToken { get; }
        public RegexToken InnerCloseParenToken { get; }

        internal override int ChildCount => 7;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => InnerOpenParenToken,
                3 => CaptureToken,
                4 => InnerCloseParenToken,
                5 => Result,
                6 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```(?(group)result)```
    /// </summary>
    internal sealed class RegexConditionalExpressionGroupingNode : RegexConditionalGroupingNode
    {
        public RegexConditionalExpressionGroupingNode(
            RegexToken openParenToken, RegexToken questionToken,
            RegexGroupingNode grouping,
            RegexExpressionNode result, RegexToken closeParenToken)
            : base(RegexKind.ConditionalExpressionGrouping, openParenToken, questionToken, result, closeParenToken)
        {
            Debug.Assert(grouping != null);
            Grouping = grouping;
        }

        internal override int ChildCount => 5;

        public RegexGroupingNode Grouping { get; }

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => OpenParenToken,
                1 => QuestionToken,
                2 => Grouping,
                3 => Result,
                4 => CloseParenToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Base type of all regex primitives that start with \
    /// </summary>
    internal abstract class RegexEscapeNode : RegexPrimaryExpressionNode
    {
        protected RegexEscapeNode(RegexKind kind, RegexToken backslashToken) : base(kind)
        {
            Debug.Assert(backslashToken.Kind == RegexKind.BackslashToken);
            BackslashToken = backslashToken;
        }

        public RegexToken BackslashToken { get; }
    }

    /// <summary>
    /// Base type of all regex escapes that start with \ and some informative character (like \v \t \c etc.).
    /// </summary>
    internal abstract class RegexTypeEscapeNode : RegexEscapeNode
    {
        protected RegexTypeEscapeNode(RegexKind kind, RegexToken backslashToken, RegexToken typeToken)
            : base(kind, backslashToken)
        {
            TypeToken = typeToken;
        }

        public RegexToken TypeToken { get; }
    }

    /// <summary>
    /// A basic escape that just has \ and one additional character and needs no further information.
    /// </summary>
    internal sealed class RegexSimpleEscapeNode : RegexTypeEscapeNode
    {
        public RegexSimpleEscapeNode(RegexToken backslashToken, RegexToken typeToken)
            : base(RegexKind.SimpleEscape, backslashToken, typeToken)
        {
            Debug.Assert(typeToken.Kind == RegexKind.TextToken);
        }

        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// One of \b \B \A \G \z \Z
    /// </summary>
    internal sealed class RegexAnchorEscapeNode(RegexToken backslashToken, RegexToken typeToken) : RegexTypeEscapeNode(RegexKind.AnchorEscape, backslashToken, typeToken)
    {
        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// One of \s \S \d \D \w \W
    /// </summary>
    internal sealed class RegexCharacterClassEscapeNode(RegexToken backslashToken, RegexToken typeToken) : RegexTypeEscapeNode(RegexKind.CharacterClassEscape, backslashToken, typeToken)
    {
        internal override int ChildCount => 2;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\cX``` escape
    /// </summary>
    internal sealed class RegexControlEscapeNode(RegexToken backslashToken, RegexToken typeToken, RegexToken controlToken) : RegexTypeEscapeNode(RegexKind.ControlEscape, backslashToken, typeToken)
    {
        internal override int ChildCount => 3;

        public RegexToken ControlToken { get; } = controlToken;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                2 => ControlToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\xFF``` escape.
    /// </summary>
    internal sealed class RegexHexEscapeNode(RegexToken backslashToken, RegexToken typeToken, RegexToken hexText) : RegexTypeEscapeNode(RegexKind.HexEscape, backslashToken, typeToken)
    {
        internal override int ChildCount => 3;

        public RegexToken HexText { get; } = hexText;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                2 => HexText,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\uFFFF``` escape.
    /// </summary>
    internal sealed class RegexUnicodeEscapeNode(RegexToken backslashToken, RegexToken typeToken, RegexToken hexText) : RegexTypeEscapeNode(RegexKind.UnicodeEscape, backslashToken, typeToken)
    {
        internal override int ChildCount => 3;

        public RegexToken HexText { get; } = hexText;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                2 => HexText,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\'name'``` or ```\&lt;name&gt;``` escape.
    /// </summary>
    internal sealed class RegexCaptureEscapeNode(
        RegexToken backslashToken, RegexToken openToken, RegexToken captureToken, RegexToken closeToken) : RegexEscapeNode(RegexKind.CaptureEscape, backslashToken)
    {
        internal override int ChildCount => 4;

        public RegexToken OpenToken { get; } = openToken;
        public RegexToken CaptureToken { get; } = captureToken;
        public RegexToken CloseToken { get; } = closeToken;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => OpenToken,
                2 => CaptureToken,
                3 => CloseToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\k'name'``` or ```\k&lt;name&gt;``` escape.
    /// </summary>
    internal sealed class RegexKCaptureEscapeNode(
        RegexToken backslashToken, RegexToken typeToken,
        RegexToken openToken, RegexToken captureToken, RegexToken closeToken) : RegexTypeEscapeNode(RegexKind.KCaptureEscape, backslashToken, typeToken)
    {
        internal override int ChildCount => 5;

        public RegexToken OpenToken { get; } = openToken;
        public RegexToken CaptureToken { get; } = captureToken;
        public RegexToken CloseToken { get; } = closeToken;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                2 => OpenToken,
                3 => CaptureToken,
                4 => CloseToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\1``` escape. In contexts where back-references are not allowed.
    /// </summary>
    internal sealed class RegexOctalEscapeNode(RegexToken backslashToken, RegexToken octalText) : RegexEscapeNode(RegexKind.OctalEscape, backslashToken)
    {
        internal override int ChildCount => 2;

        public RegexToken OctalText { get; } = octalText;

        internal override RegexNodeOrToken ChildAt(int index)
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
    internal sealed class RegexBackreferenceEscapeNode(RegexToken backslashToken, RegexToken numberToken) : RegexEscapeNode(RegexKind.BackreferenceEscape, backslashToken)
    {
        internal override int ChildCount => 2;

        public RegexToken NumberToken { get; } = numberToken;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => NumberToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// ```\p{...}```
    /// </summary>
    internal sealed class RegexCategoryEscapeNode : RegexEscapeNode
    {
        public RegexCategoryEscapeNode(
            RegexToken backslashToken, RegexToken typeToken, RegexToken openBraceToken, RegexToken categoryToken, RegexToken closeBraceToken)
            : base(RegexKind.CategoryEscape, backslashToken)
        {
            Debug.Assert(openBraceToken.Kind == RegexKind.OpenBraceToken);
            Debug.Assert(closeBraceToken.Kind == RegexKind.CloseBraceToken);
            TypeToken = typeToken;
            OpenBraceToken = openBraceToken;
            CategoryToken = categoryToken;
            CloseBraceToken = closeBraceToken;
        }

        public RegexToken TypeToken { get; }
        public RegexToken OpenBraceToken { get; }
        public RegexToken CategoryToken { get; }
        public RegexToken CloseBraceToken { get; }

        internal override int ChildCount => 5;

        internal override RegexNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => BackslashToken,
                1 => TypeToken,
                2 => OpenBraceToken,
                3 => CategoryToken,
                4 => CloseBraceToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IRegexNodeVisitor visitor)
            => visitor.Visit(this);
    }
}
