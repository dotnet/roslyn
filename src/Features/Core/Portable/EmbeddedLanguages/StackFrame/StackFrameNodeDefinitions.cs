// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    internal abstract class StackFrameNode : EmbeddedSyntaxNode<StackFrameKind, StackFrameNode>
    {
        protected StackFrameNode(StackFrameKind kind) : base(kind)
        {
        }

        public abstract void Accept(IStackFrameNodeVisitor visitor);
    }

    /// <summary>
    /// Root of all expression nodes.
    /// </summary>
    internal abstract class StackFrameExpressionNode : StackFrameNode
    {
        protected StackFrameExpressionNode(StackFrameKind kind) : base(kind)
        {
        }

        internal abstract StackFrameExpressionNode WithLeadingTrivia(ImmutableArray<StackFrameTrivia> trivia);
        internal abstract StackFrameExpressionNode WithTrailingTrivia(ImmutableArray<StackFrameTrivia> trivia);
    }

    internal sealed class StackFrameMethodDeclarationNode : StackFrameNode
    {
        public readonly StackFrameMemberAccessExpressionNode MemberAccessExpression;
        public readonly StackFrameTypeArgumentList? TypeArguments;
        public readonly StackFrameParameterList ArgumentList;

        internal StackFrameMethodDeclarationNode(
            StackFrameMemberAccessExpressionNode memberAccessExpression,
            StackFrameTypeArgumentList? typeArguments,
            StackFrameParameterList argumentList)
            : base(StackFrameKind.MethodDeclaration)
        {
            MemberAccessExpression = memberAccessExpression;
            TypeArguments = typeArguments;
            ArgumentList = argumentList;
        }

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
             => index switch
             {
                 0 => MemberAccessExpression,
                 1 => TypeArguments,
                 2 => ArgumentList,
                 _ => throw new InvalidOperationException(),
             };

        internal StackFrameMethodDeclarationNode WithLeadingTrivia(ImmutableArray<StackFrameTrivia> trivia)
            => new((StackFrameMemberAccessExpressionNode)MemberAccessExpression.WithLeadingTrivia(trivia), TypeArguments, ArgumentList);
    }

    internal sealed class StackFrameMemberAccessExpressionNode : StackFrameExpressionNode
    {
        public readonly StackFrameExpressionNode Expression;
        public readonly StackFrameToken Operator;
        public readonly StackFrameBaseIdentifierNode Identifier;

        public StackFrameMemberAccessExpressionNode(StackFrameExpressionNode expression, StackFrameToken operatorToken, StackFrameBaseIdentifierNode identifier) : base(StackFrameKind.MemberAccess)
        {
            Expression = expression;
            Operator = operatorToken;
            Identifier = identifier;
        }

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Expression,
                1 => Operator,
                2 => Identifier,
                _ => throw new InvalidOperationException()
            };

        internal StackFrameExpressionNode WithLeadingTrivia(StackFrameTrivia trivia)
            => WithLeadingTrivia(ImmutableArray.Create(trivia));

        internal override StackFrameExpressionNode WithLeadingTrivia(ImmutableArray<StackFrameTrivia> trivia)
            => new StackFrameMemberAccessExpressionNode(Expression.WithLeadingTrivia(trivia), Operator, Identifier);

        internal StackFrameExpressionNode WithTrailingTrivia(StackFrameTrivia trivia)
            => WithTrailingTrivia(ImmutableArray.Create(trivia));

        internal override StackFrameExpressionNode WithTrailingTrivia(ImmutableArray<StackFrameTrivia> trivia)
            => new StackFrameMemberAccessExpressionNode(Expression, Operator, (StackFrameBaseIdentifierNode)Identifier.WithTrailingTrivia(trivia));
    }

    internal abstract class StackFrameBaseIdentifierNode : StackFrameExpressionNode
    {
        protected StackFrameBaseIdentifierNode(StackFrameKind kind) : base(kind)
        {
        }

        internal StackFrameExpressionNode WithLeadingTrivia(StackFrameTrivia leadingTrivia)
            => WithLeadingTrivia(ImmutableArray.Create(leadingTrivia));

        internal StackFrameExpressionNode WithTrailingTrivia(StackFrameTrivia trailingTrivia)
            => WithTrailingTrivia(ImmutableArray.Create(trailingTrivia));
    }

    internal sealed class StackFrameIdentifierNode : StackFrameBaseIdentifierNode
    {
        public readonly StackFrameToken Identifier;

        internal override int ChildCount => 1;

        internal StackFrameIdentifierNode(StackFrameToken identifier)
            : base(StackFrameKind.Identifier)
        {
            Identifier = identifier;
        }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Identifier,
                _ => throw new InvalidOperationException()
            };

        public override string ToString()
            => Identifier.VirtualChars.CreateString();

        internal override StackFrameExpressionNode WithTrailingTrivia(ImmutableArray<StackFrameTrivia> trailingTrivia)
            => new StackFrameIdentifierNode(Identifier.With(trailingTrivia: trailingTrivia));

        internal override StackFrameExpressionNode WithLeadingTrivia(ImmutableArray<StackFrameTrivia> leadingTrivia)
            => new StackFrameIdentifierNode(Identifier.With(leadingTrivia: leadingTrivia));
    }

    internal sealed class StackFrameGenericTypeIdentifier : StackFrameBaseIdentifierNode
    {
        public readonly StackFrameToken Identifier;
        public readonly StackFrameToken ArityToken;
        public readonly StackFrameToken ArityNumericToken;

        internal override int ChildCount => 3;

        internal StackFrameGenericTypeIdentifier(StackFrameToken identifier, StackFrameToken arityToken, StackFrameToken arityNumericToken)
            : base(StackFrameKind.GenericTypeIdentifier)
        {
            Identifier = identifier;
            ArityToken = arityToken;
            ArityNumericToken = arityNumericToken;
        }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Identifier,
                1 => ArityToken,
                2 => ArityNumericToken,
                _ => throw new InvalidOperationException()
            };

        internal override StackFrameExpressionNode WithLeadingTrivia(ImmutableArray<StackFrameTrivia> leadingTrivia)
            => new StackFrameGenericTypeIdentifier(
                Identifier.With(leadingTrivia: leadingTrivia),
                ArityToken,
                ArityNumericToken);

        internal override StackFrameExpressionNode WithTrailingTrivia(ImmutableArray<StackFrameTrivia> trailingTrivia)
            => new StackFrameGenericTypeIdentifier(
                Identifier,
                ArityToken,
                ArityNumericToken.With(trailingTrivia: trailingTrivia));
    }

    internal sealed class StackFrameArrayExpressionNode : StackFrameExpressionNode
    {
        private readonly StackFrameExpressionNode _identifier;
        private readonly ImmutableArray<StackFrameToken> _arrayBrackets;

        public StackFrameArrayExpressionNode(StackFrameExpressionNode identifier, ImmutableArray<StackFrameToken> arrayBrackets)
            : base(StackFrameKind.ArrayExpression)
        {
            _identifier = identifier;
            _arrayBrackets = arrayBrackets;

            Debug.Assert(arrayBrackets.All(t => t.Kind is StackFrameKind.OpenBracketToken or StackFrameKind.CloseBracketToken));
        }

        internal override int ChildCount => _arrayBrackets.Length + 1;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => _identifier,
                _ => _arrayBrackets[index - 1]
            };

        internal override StackFrameExpressionNode WithLeadingTrivia(ImmutableArray<StackFrameTrivia> trivia)
            => new StackFrameArrayExpressionNode(_identifier.WithLeadingTrivia(trivia), _arrayBrackets);

        internal override StackFrameExpressionNode WithTrailingTrivia(ImmutableArray<StackFrameTrivia> trivia)
        {
            var lastBracket = _arrayBrackets.Last().With(trailingTrivia: trivia);
            var newBrackets = _arrayBrackets.RemoveAt(_arrayBrackets.Length - 1).Add(lastBracket);

            return new StackFrameArrayExpressionNode(_identifier, newBrackets);
        }
    }

    internal sealed class StackFrameTypeArgumentList : StackFrameNode
    {
        public readonly StackFrameToken OpenToken;
        public readonly StackFrameToken CloseToken;

        public StackFrameTypeArgumentList(StackFrameToken openToken, ImmutableArray<StackFrameNodeOrToken> childNodesOrTokens, StackFrameToken closeToken) : base(StackFrameKind.TypeArgument)
        {
            Debug.Assert(openToken.Kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken);
            Debug.Assert(openToken.Kind == StackFrameKind.OpenBracketToken ? closeToken.Kind == StackFrameKind.CloseBracketToken : closeToken.Kind == StackFrameKind.GreaterThanToken);
            Debug.Assert(childNodesOrTokens.All(nodeOrToken => nodeOrToken.IsNode
                ? nodeOrToken.Node is StackFrameTypeArgument
                : nodeOrToken.Token.Kind == StackFrameKind.CommaToken));

            OpenToken = openToken;
            CloseToken = closeToken;
            _childNodesOrTokens = childNodesOrTokens;
            ChildCount = childNodesOrTokens.Length + 2;
        }

        private readonly ImmutableArray<StackFrameNodeOrToken> _childNodesOrTokens;

        internal override int ChildCount { get; }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
        {
            if (index >= ChildCount)
            {
                throw new InvalidOperationException();
            }

            if (index == 0)
            {
                return OpenToken;
            }

            if (index == ChildCount - 1)
            {
                return CloseToken;
            }

            return _childNodesOrTokens[index - 1];
        }
    }

    internal sealed class StackFrameTypeArgument : StackFrameBaseIdentifierNode
    {
        public readonly StackFrameToken Identifier;

        internal override int ChildCount => 1;

        internal StackFrameTypeArgument(StackFrameToken identifier)
            : base(StackFrameKind.TypeIdentifier)
        {
            Identifier = identifier;
        }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Identifier,
                _ => throw new InvalidOperationException()
            };

        internal override StackFrameExpressionNode WithTrailingTrivia(ImmutableArray<StackFrameTrivia> trailingTrivia)
            => new StackFrameTypeArgument(Identifier.With(trailingTrivia: trailingTrivia));

        internal override StackFrameExpressionNode WithLeadingTrivia(ImmutableArray<StackFrameTrivia> leadingTrivia)
            => new StackFrameTypeArgument(Identifier.With(leadingTrivia: leadingTrivia));
    }

    internal sealed class StackFrameParameterList : StackFrameNode
    {
        public readonly StackFrameToken OpenParen;
        public readonly StackFrameToken CloseParen;
        private readonly ImmutableArray<StackFrameNodeOrToken> _childNodesOrTokens;

        public StackFrameParameterList(StackFrameToken openParen, ImmutableArray<StackFrameNodeOrToken> childNodesOrTokens, StackFrameToken closeParen) : base(StackFrameKind.ParameterList)
        {
            Debug.Assert(openParen.Kind == StackFrameKind.OpenParenToken);
            Debug.Assert(closeParen.Kind == StackFrameKind.CloseParenToken);
            Debug.Assert(childNodesOrTokens.IsDefaultOrEmpty || childNodesOrTokens.All(nodeOrToken => nodeOrToken.IsNode
                ? nodeOrToken.Node is StackFrameIdentifierNode or StackFrameMemberAccessExpressionNode or StackFrameArrayExpressionNode
                : nodeOrToken.Token.Kind is StackFrameKind.CommaToken));

            OpenParen = openParen;
            CloseParen = closeParen;
            _childNodesOrTokens = childNodesOrTokens;
            ChildCount = _childNodesOrTokens.IsDefault ? 2 : _childNodesOrTokens.Length + 2;
        }

        internal override int ChildCount { get; }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
        {
            if (index >= ChildCount)
            {
                throw new InvalidOperationException();
            }

            if (index == 0)
            {
                return OpenParen;
            }

            if (index == ChildCount - 1)
            {
                return CloseParen;
            }

            return _childNodesOrTokens[index - 1];
        }
    }

    internal sealed class StackFrameFileInformationNode : StackFrameNode
    {
        public readonly StackFrameToken Path;
        public readonly StackFrameToken? Colon;
        public readonly StackFrameToken? Line;

        public StackFrameFileInformationNode(StackFrameToken path, StackFrameToken? colon = null, StackFrameToken? line = null) : base(StackFrameKind.FileInformation)
        {
            Path = path;
            Colon = colon;
            Line = line;
        }

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Path,
                1 => Colon.HasValue ? Colon.Value : null,
                2 => Line.HasValue ? Line.Value : null,
                _ => throw new InvalidOperationException()
            };

        internal StackFrameFileInformationNode WithLeadingTrivia(StackFrameTrivia inTrivia)
            => new(Path.With(leadingTrivia: ImmutableArray.Create(inTrivia)),
                Colon,
                Line);
    }
}
