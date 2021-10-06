// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Roslyn.Test.Utilities;
using Xunit;
using System;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;

    public partial class StackFrameParserTests
    {
        private static StackFrameToken CreateToken(StackFrameKind kind, string s, ImmutableArray<StackFrameTrivia> leadingTrivia = default, ImmutableArray<StackFrameTrivia> trailingTrivia = default)
            => new(
                kind,
                leadingTrivia.IsDefaultOrEmpty ? ImmutableArray<StackFrameTrivia>.Empty : leadingTrivia,
                CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence.Create(0, s),
                trailingTrivia.IsDefaultOrEmpty ? ImmutableArray<StackFrameTrivia>.Empty : trailingTrivia,
                ImmutableArray<EmbeddedDiagnostic>.Empty,
                value: null!);

        private static StackFrameTrivia CreateTrivia(StackFrameKind kind, string s)
            => new(kind, CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence.Create(0, s), ImmutableArray<EmbeddedDiagnostic>.Empty);

        private static ImmutableArray<StackFrameTrivia> CreateTriviaArray(params StackFrameTrivia[] trivia)
            => ImmutableArray.Create(trivia);

        private static readonly StackFrameToken DotToken = CreateToken(StackFrameKind.DotToken, ".");
        private static readonly StackFrameToken CommaToken = CreateToken(StackFrameKind.CommaToken, ",");
        private static readonly StackFrameToken OpenParenToken = CreateToken(StackFrameKind.OpenParenToken, "(");
        private static readonly StackFrameToken CloseParenToken = CreateToken(StackFrameKind.CloseParenToken, ")");
        private static readonly StackFrameToken OpenBracketToken = CreateToken(StackFrameKind.OpenBracketToken, "[");
        private static readonly StackFrameToken CloseBracketToken = CreateToken(StackFrameKind.CloseBracketToken, "]");
        private static readonly StackFrameToken LessThanToken = CreateToken(StackFrameKind.LessThanToken, "<");
        private static readonly StackFrameToken GreaterThanToken = CreateToken(StackFrameKind.GreaterThanToken, ">");
        private static readonly StackFrameToken AccentGraveToken = CreateToken(StackFrameKind.GraveAccentToken, "`");

        private static readonly StackFrameTrivia AtTrivia = CreateTrivia(StackFrameKind.AtTrivia, "at ");

        private static StackFrameParameterList ArgumentList(params StackFrameNodeOrToken[] nodesOrTokens)
            => new(OpenParenToken, nodesOrTokens.ToImmutableArray(), CloseParenToken);

        private static StackFrameParameterList ArgumentList(StackFrameToken openToken, StackFrameToken closeToken, params StackFrameNodeOrToken[] nodesOrTokens)
            => new(openToken, nodesOrTokens.ToImmutableArray(), closeToken);

        private static StackFrameMethodDeclarationNode MethodDeclaration(
            StackFrameMemberAccessExpressionNode memberAccessExpression,
            StackFrameTypeArgumentList? typeArguments = null,
            StackFrameParameterList? argumentList = null)
        {
            return new StackFrameMethodDeclarationNode(memberAccessExpression, typeArguments, argumentList ?? ArgumentList(OpenParenToken, CloseParenToken));
        }

        private static StackFrameMemberAccessExpressionNode MemberAccessExpression(string s, ImmutableArray<StackFrameTrivia> leadingTrivia = default, ImmutableArray<StackFrameTrivia> trailingTrivia = default)
        {
            StackFrameExpressionNode? current = null;
            var identifiers = s.Split('.');
            for (var i = 0; i < identifiers.Length; i++)
            {
                var identifier = identifiers[i];

                if (current is null)
                {
                    current = Identifier(identifier, leadingTrivia: leadingTrivia);
                }
                else if (i == identifiers.Length - 1)
                {
                    current = MemberAccessExpression(current, Identifier(identifier, trailingTrivia: trailingTrivia));
                }
                else
                {
                    current = MemberAccessExpression(current, Identifier(identifier));
                }
            }

            AssertEx.NotNull(current);
            return (StackFrameMemberAccessExpressionNode)current;
        }

        private static StackFrameTrivia SpaceTrivia(int count = 1)
            => CreateTrivia(StackFrameKind.WhitespaceTrivia, new string(' ', count));

        private static StackFrameMemberAccessExpressionNode MemberAccessExpression(StackFrameExpressionNode expressionNode, StackFrameBaseIdentifierNode identifierNode)
            => new(expressionNode, DotToken, identifierNode);

        private static StackFrameIdentifierNode Identifier(string identifierName, ImmutableArray<StackFrameTrivia> leadingTrivia = default, ImmutableArray<StackFrameTrivia> trailingTrivia = default)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifierName, leadingTrivia: leadingTrivia, trailingTrivia: trailingTrivia));

        private static StackFrameArrayExpressionNode ArrayExpression(StackFrameExpressionNode identifier, params StackFrameToken[] arrayTokens)
            => new(identifier, arrayTokens.ToImmutableArray());

        private static StackFrameGenericTypeIdentifier GenericType(string identifierName, int arity)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifierName), AccentGraveToken, CreateToken(StackFrameKind.TextToken, arity.ToString()));

        private static StackFrameTypeArgumentList TypeArgumentList(params StackFrameTypeArgument[] typeArguments)
            => TypeArgumentList(useBrackets: true, typeArguments);

        private static StackFrameTypeArgumentList TypeArgumentList(bool useBrackets, params StackFrameTypeArgument[] typeArguments)
        {
            using var _ = PooledObjects.ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            var openToken = useBrackets ? OpenBracketToken : LessThanToken;
            var closeToken = useBrackets ? CloseBracketToken : GreaterThanToken;

            var isFirst = true;
            foreach (var typeArgument in typeArguments)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    builder.Add(CommaToken);
                }

                builder.Add(typeArgument);
            }

            return new(openToken, builder.ToImmutable(), closeToken);
        }

        private static StackFrameTypeArgument TypeArgument(string identifier)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifier));
    }
}
