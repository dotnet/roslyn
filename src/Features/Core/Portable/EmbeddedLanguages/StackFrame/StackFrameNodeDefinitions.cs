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
using Microsoft.CodeAnalysis.PooledObjects;
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

        public StackFrameNodeOrToken this[int index] => ChildAt(index);
        public StackFrameNodeOrToken this[Index index] => this[index.GetOffset(this.ChildCount)];
    }

    internal sealed class SeparatedStackFrameNodeList<TNode> where TNode : StackFrameNode
    {
        public SeparatedStackFrameNodeList(ImmutableArray<StackFrameNodeOrToken> nodesAndTokens)
        {
            Contract.ThrowIfTrue(nodesAndTokens.IsDefaultOrEmpty);
            NodesAndTokens = nodesAndTokens;

#if DEBUG
            // Length should represent (nodes.Length) + (nodes.Length - 1), where the latter 
            // represents the number of separator tokens
            Debug.Assert(nodesAndTokens.Length % 2 == 1);
            for (var i = 0; i < nodesAndTokens.Length; i++)
            {
                if (i % 2 == 0)
                {
                    // All even values should be TNode
                    Debug.Assert(nodesAndTokens[i].IsNode);
                    Debug.Assert(nodesAndTokens[i].Node is TNode);
                }
                else
                {
                    // All odd values should be separator tokens 
                    Debug.Assert(!nodesAndTokens[i].IsNode);
                    Debug.Assert(!nodesAndTokens[i].Token.IsMissing);
                }
            }
#endif
        }

        private SeparatedStackFrameNodeList()
        {
            NodesAndTokens = ImmutableArray<StackFrameNodeOrToken>.Empty;
        }

        public ImmutableArray<StackFrameNodeOrToken> NodesAndTokens { get; }
        public int Length => NodesAndTokens.Length;
        public StackFrameNodeOrToken this[int index] => NodesAndTokens[index];

        public static SeparatedStackFrameNodeList<TNode> Empty => new SeparatedStackFrameNodeList<TNode>();

        public ImmutableArray<TNode> GetNodes()
        {
            using var _ = ArrayBuilder<TNode>.GetInstance(out var builder);

            for (var i = 0; i < NodesAndTokens.Length; i = i + 2)
            {
                var node = NodesAndTokens[i].Node;
                RoslynDebug.AssertNotNull(node);
                builder.Add((TNode)node);
            }

            return builder.ToImmutable();
        }
    }

    /// <summary>
    /// Root of all expression nodes.
    /// </summary>
    internal abstract class StackFrameExpressionNode : StackFrameNode
    {
        protected StackFrameExpressionNode(StackFrameKind kind) : base(kind)
        {
        }
    }

    internal sealed class StackFrameMethodDeclarationNode : StackFrameNode
    {
        public readonly StackFrameMemberAccessExpressionNode MemberAccessExpression;
        public readonly StackFrameTypeArgumentList? TypeArguments;
        public readonly StackFrameParameterList ArgumentList;

        public StackFrameMethodDeclarationNode(
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
    }

    internal sealed class StackFrameMemberAccessExpressionNode : StackFrameExpressionNode
    {
        public readonly StackFrameNodeOrToken Left;
        public readonly StackFrameToken Operator;
        public readonly StackFrameNodeOrToken Right;

        public StackFrameMemberAccessExpressionNode(StackFrameNodeOrToken left, StackFrameToken operatorToken, StackFrameNodeOrToken right) : base(StackFrameKind.MemberAccess)
        {
            Debug.Assert(left.IsNode || left.Token.Kind == StackFrameKind.IdentifierToken);
            Debug.Assert(right.IsNode || right.Token.Kind == StackFrameKind.IdentifierToken);
            Left = left;
            Operator = operatorToken;
            Right = right;
        }

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Left,
                1 => Operator,
                2 => Right,
                _ => throw new InvalidOperationException()
            };
    }

    internal abstract class StackFrameBaseIdentifierNode : StackFrameExpressionNode
    {
        protected StackFrameBaseIdentifierNode(StackFrameKind kind) : base(kind)
        {
        }
    }

    internal sealed class StackFrameGenericTypeIdentifier : StackFrameBaseIdentifierNode
    {
        public readonly StackFrameToken Identifier;
        public readonly StackFrameToken ArityToken;
        public readonly StackFrameToken ArityNumericToken;

        internal override int ChildCount => 3;

        public StackFrameGenericTypeIdentifier(StackFrameToken identifier, StackFrameToken arityToken, StackFrameToken arityNumericToken)
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
    }

    /// <summary>
    /// Represents an array type declaration, such as string[,][]
    /// </summary>
    internal sealed class StackFrameArrayTypeExpression : StackFrameExpressionNode
    {
        /// <summary>
        /// The type identifier without the array indicators.
        /// string[][]
        /// ^----^
        /// </summary>
        public readonly StackFrameNodeOrToken TypeIdentifier;

        /// <summary>
        /// Each unique array identifier for the type
        /// string[,][]
        ///        ^--- First array expression = "[,]"
        ///           ^- Second array expression = "[]" 
        /// </summary>
        public ImmutableArray<StackFrameArrayRankSpecifier> ArrayExpressions;

        public StackFrameArrayTypeExpression(StackFrameNodeOrToken typeIdentifier, ImmutableArray<StackFrameArrayRankSpecifier> arrayExpressions) : base(StackFrameKind.ArrayTypeExpression)
        {
            Contract.ThrowIfTrue(arrayExpressions.IsDefaultOrEmpty);
            TypeIdentifier = typeIdentifier;
            ArrayExpressions = arrayExpressions;
        }

        internal override int ChildCount => 1 + ArrayExpressions.Length;

        public override void Accept(IStackFrameNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => TypeIdentifier,
                _ => ArrayExpressions[index - 1]
            };
    }
    internal sealed class StackFrameArrayRankSpecifier : StackFrameExpressionNode
    {
        public readonly StackFrameToken OpenBracket;
        public readonly StackFrameToken CloseBracket;
        public ImmutableArray<StackFrameToken> CommaTokens;

        public StackFrameArrayRankSpecifier(StackFrameToken openBracket, StackFrameToken closeBracket, ImmutableArray<StackFrameToken> commaTokens)
            : base(StackFrameKind.ArrayExpression)
        {
            Contract.ThrowIfTrue(commaTokens.IsDefault);
            Debug.Assert(openBracket.Kind == StackFrameKind.OpenBracketToken);
            Debug.Assert(closeBracket.Kind == StackFrameKind.CloseBracketToken);
            Debug.Assert(commaTokens.All(t => t.Kind == StackFrameKind.CommaToken));

            OpenBracket = openBracket;
            CloseBracket = closeBracket;
            CommaTokens = commaTokens;
        }

        internal override int ChildCount => 2 + CommaTokens.Length;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
        {
            if (index == 0)
            {
                return OpenBracket;
            }

            if (index == ChildCount - 1)
            {
                return CloseBracket;
            }

            return CommaTokens[index - 1];
        }
    }

    /// <summary>
    /// The type argument list for a method declaration. 
    /// Ex: MyType.MyMethod[T, U, V](T t, U u, V v) 
    ///                    ^-----------------------  "[" = Open Token 
    ///                     ^------^   ------------  "T, U, V" = SeparatedStackFrameNodeList&lt;StackFrameTypeArgumentNode&gt;
    ///                             ^--------------  "]" = Close Token
    /// </summary>
    internal sealed class StackFrameTypeArgumentList : StackFrameNode
    {
        public readonly StackFrameToken OpenToken;
        public readonly StackFrameToken CloseToken;
        public readonly SeparatedStackFrameNodeList<StackFrameTypeArgumentNode> TypeArguments;

        public StackFrameTypeArgumentList(StackFrameToken openToken, SeparatedStackFrameNodeList<StackFrameTypeArgumentNode> typeArguments, StackFrameToken closeToken) : base(StackFrameKind.TypeArgument)
        {
            Debug.Assert(openToken.Kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken);
            Debug.Assert(openToken.Kind == StackFrameKind.OpenBracketToken ? closeToken.Kind == StackFrameKind.CloseBracketToken : closeToken.Kind == StackFrameKind.GreaterThanToken);
            Debug.Assert(typeArguments.Length > 0);

            OpenToken = openToken;
            CloseToken = closeToken;
            TypeArguments = typeArguments;
        }

        internal override int ChildCount => TypeArguments.Length + 2;

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

            return TypeArguments[index - 1];
        }
    }

    internal sealed class StackFrameTypeArgumentNode : StackFrameBaseIdentifierNode
    {
        public readonly StackFrameToken Identifier;

        internal override int ChildCount => 1;

        public StackFrameTypeArgumentNode(StackFrameToken identifier)
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
    }

    internal sealed class StackFrameParameterNode : StackFrameExpressionNode
    {
        public readonly StackFrameNodeOrToken Type;
        public readonly StackFrameToken Identifier;

        internal override int ChildCount => 2;

        public StackFrameParameterNode(StackFrameNodeOrToken type, StackFrameToken identifier)
            : base(StackFrameKind.Parameter)
        {
            Debug.Assert(type.IsNode || !type.Token.IsMissing);
            Type = type;
            Identifier = identifier;
        }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Type,
                1 => Identifier,
                _ => throw new InvalidOperationException()
            };
    }

    internal sealed class StackFrameParameterList : StackFrameExpressionNode
    {
        public readonly StackFrameToken OpenParen;
        public readonly StackFrameToken CloseParen;
        public readonly SeparatedStackFrameNodeList<StackFrameParameterNode> Parameters;

        public StackFrameParameterList(StackFrameToken openToken, StackFrameToken closeToken, SeparatedStackFrameNodeList<StackFrameParameterNode> parameters)
            : base(StackFrameKind.ParameterList)
        {
            Debug.Assert(openToken.Kind == StackFrameKind.OpenParenToken);
            Debug.Assert(closeToken.Kind == StackFrameKind.CloseParenToken);

            OpenParen = openToken;
            CloseParen = closeToken;
            Parameters = parameters;
        }

        internal override int ChildCount => 2 + Parameters.Length;

        public override void Accept(IStackFrameNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override StackFrameNodeOrToken ChildAt(int index)
        {
            if (index == 0)
            {
                return OpenParen;
            }

            if (index == ChildCount - 1)
            {
                return CloseParen;
            }

            return Parameters[index - 1];
        }
    }

    internal sealed class StackFrameFileInformationNode : StackFrameNode
    {
        public readonly StackFrameToken Path;
        public readonly StackFrameToken? Colon;
        public readonly StackFrameToken? Line;

        public StackFrameFileInformationNode(StackFrameToken path, StackFrameToken? colon, StackFrameToken? line) : base(StackFrameKind.FileInformation)
        {
            Debug.Assert(colon.HasValue == line.HasValue);
            Debug.Assert(!line.HasValue || line.Value.Kind == StackFrameKind.NumberToken);

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
