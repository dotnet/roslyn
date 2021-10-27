// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;

    internal static class StackFrameSyntaxFactory
    {
        public static StackFrameToken CreateToken(StackFrameKind kind, string s, ImmutableArray<StackFrameTrivia> leadingTrivia = default, ImmutableArray<StackFrameTrivia> trailingTrivia = default)
            => new(
                kind,
                leadingTrivia.IsDefaultOrEmpty ? ImmutableArray<StackFrameTrivia>.Empty : leadingTrivia,
                CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence.Create(0, s),
                trailingTrivia.IsDefaultOrEmpty ? ImmutableArray<StackFrameTrivia>.Empty : trailingTrivia,
                ImmutableArray<EmbeddedDiagnostic>.Empty,
                value: null!);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, string text)
            => new(kind, CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence.Create(0, text), ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static ImmutableArray<StackFrameTrivia> CreateTriviaArray(params StackFrameTrivia[] trivia)
            => ImmutableArray.Create(trivia);

        public static ImmutableArray<StackFrameTrivia> CreateTriviaArray(StackFrameTrivia? trivia)
            => trivia.HasValue ? ImmutableArray.Create(trivia.Value) : ImmutableArray<StackFrameTrivia>.Empty;

        public static ImmutableArray<StackFrameTrivia> CreateTriviaArray(params string[] strings)
            => strings.Select(s => CreateTrivia(StackFrameKind.SkippedTextTrivia, s)).ToImmutableArray();

        public static readonly StackFrameToken DotToken = CreateToken(StackFrameKind.DotToken, ".");
        public static readonly StackFrameToken CommaToken = CreateToken(StackFrameKind.CommaToken, ",");
        public static readonly StackFrameToken OpenParenToken = CreateToken(StackFrameKind.OpenParenToken, "(");
        public static readonly StackFrameToken CloseParenToken = CreateToken(StackFrameKind.CloseParenToken, ")");
        public static readonly StackFrameToken OpenBracketToken = CreateToken(StackFrameKind.OpenBracketToken, "[");
        public static readonly StackFrameToken CloseBracketToken = CreateToken(StackFrameKind.CloseBracketToken, "]");
        public static readonly StackFrameToken LessThanToken = CreateToken(StackFrameKind.LessThanToken, "<");
        public static readonly StackFrameToken GreaterThanToken = CreateToken(StackFrameKind.GreaterThanToken, ">");
        public static readonly StackFrameToken AccentGraveToken = CreateToken(StackFrameKind.GraveAccentToken, "`");
        public static readonly StackFrameToken EOLToken = CreateToken(StackFrameKind.EndOfLine, "");
        public static readonly StackFrameToken ColonToken = CreateToken(StackFrameKind.ColonToken, ":");

        public static readonly StackFrameTrivia AtTrivia = CreateTrivia(StackFrameKind.AtTrivia, "at ");
        public static readonly StackFrameTrivia LineTrivia = CreateTrivia(StackFrameKind.LineTrivia, "line ");
        public static readonly StackFrameTrivia InTrivia = CreateTrivia(StackFrameKind.InTrivia, " in ");

        public static readonly StackFrameParameterList EmptyParams = ParameterList(OpenParenToken, CloseParenToken);

        public static StackFrameParameterNode Parameter(StackFrameNodeOrToken type, StackFrameToken identifier)
            => new(type, identifier);

        public static StackFrameParameterList ParameterList(StackFrameToken openToken, StackFrameToken closeToken, params StackFrameParameterNode[] parameters)
        {
            var separatedList = parameters.Length == 0
                ? SeparatedStackFrameNodeList<StackFrameParameterNode>.Empty
                : new(CommaSeparateList(parameters));

            return new(openToken, closeToken, separatedList);

            static ImmutableArray<StackFrameNodeOrToken> CommaSeparateList(StackFrameParameterNode[] parameters)
            {
                var builder = ImmutableArray.CreateBuilder<StackFrameNodeOrToken>();
                builder.Add(parameters[0]);

                for (var i = 1; i < parameters.Length; i++)
                {
                    builder.Add(CommaToken);
                    builder.Add(parameters[i]);
                }

                return builder.ToImmutable();
            }
        }

        public static StackFrameMethodDeclarationNode MethodDeclaration(
            StackFrameMemberAccessExpressionNode memberAccessExpression,
            StackFrameTypeArgumentList? typeArguments = null,
            StackFrameParameterList? argumentList = null)
        {
            return new StackFrameMethodDeclarationNode(memberAccessExpression, typeArguments, argumentList ?? ParameterList(OpenParenToken, CloseParenToken));
        }

        public static StackFrameMemberAccessExpressionNode MemberAccessExpression(string s, StackFrameTrivia? leadingTrivia = null, StackFrameTrivia? trailingTrivia = null)
            => MemberAccessExpression(s, CreateTriviaArray(leadingTrivia), CreateTriviaArray(trailingTrivia));

        public static StackFrameMemberAccessExpressionNode MemberAccessExpression(string s, ImmutableArray<StackFrameTrivia> leadingTrivia, ImmutableArray<StackFrameTrivia> trailingTrivia)
        {
            StackFrameNodeOrToken? current = null;
            var identifiers = s.Split('.');
            for (var i = 0; i < identifiers.Length; i++)
            {
                var identifier = identifiers[i];

                if (!current.HasValue)
                {
                    current = Identifier(identifier, leadingTrivia: leadingTrivia, trailingTrivia: ImmutableArray<StackFrameTrivia>.Empty);
                }
                else if (i == identifiers.Length - 1)
                {
                    current = MemberAccessExpression(current.Value, Identifier(identifier, leadingTrivia: ImmutableArray<StackFrameTrivia>.Empty, trailingTrivia: trailingTrivia));
                }
                else
                {
                    current = MemberAccessExpression(current.Value, Identifier(identifier));
                }
            }

            Assert.True(current.HasValue);
            Assert.True(current!.Value.IsNode);

            var node = current.Value.Node;
            AssertEx.NotNull(node);
            return (StackFrameMemberAccessExpressionNode)node;
        }

        public static StackFrameTrivia SpaceTrivia(int count = 1)
            => CreateTrivia(StackFrameKind.WhitespaceTrivia, new string(' ', count));

        public static StackFrameMemberAccessExpressionNode MemberAccessExpression(StackFrameNodeOrToken left, StackFrameNodeOrToken right)
            => new(left, DotToken, right);

        public static StackFrameToken Identifier(string identifierName)
            => Identifier(identifierName, leadingTrivia: null, trailingTrivia: null);

        public static StackFrameToken Identifier(string identifierName, StackFrameTrivia? leadingTrivia = null, StackFrameTrivia? trailingTrivia = null)
            => Identifier(identifierName, CreateTriviaArray(leadingTrivia), CreateTriviaArray(trailingTrivia));

        public static StackFrameToken Identifier(string identifierName, ImmutableArray<StackFrameTrivia> leadingTrivia, ImmutableArray<StackFrameTrivia> trailingTrivia)
            => CreateToken(StackFrameKind.IdentifierToken, identifierName, leadingTrivia: leadingTrivia, trailingTrivia: trailingTrivia);

        public static StackFrameArrayRankSpecifier ArrayRankSpecifier(int commaCount = 0, StackFrameTrivia? leadingTrivia = null, StackFrameTrivia? trailingTrivia = null)
            => new(OpenBracketToken.With(leadingTrivia: CreateTriviaArray(leadingTrivia)), CloseBracketToken.With(trailingTrivia: CreateTriviaArray(trailingTrivia)), Enumerable.Repeat(CommaToken, commaCount).ToImmutableArray());

        public static StackFrameArrayTypeExpression ArrayExpression(StackFrameNodeOrToken identifier, params StackFrameArrayRankSpecifier[] arrayTokens)
            => new(identifier, arrayTokens.ToImmutableArray());

        public static StackFrameGenericTypeIdentifier GenericType(string identifierName, int arity)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifierName), AccentGraveToken, CreateToken(StackFrameKind.NumberToken, arity.ToString()));

        public static StackFrameTypeArgumentList TypeArgumentList(params StackFrameTypeArgumentNode[] typeArguments)
            => TypeArgumentList(useBrackets: true, typeArguments);

        public static StackFrameTypeArgumentList TypeArgumentList(bool useBrackets, params StackFrameTypeArgumentNode[] typeArguments)
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

            var typeArgumentsList = new SeparatedStackFrameNodeList<StackFrameTypeArgumentNode>(builder.ToImmutable());

            return new(openToken, typeArgumentsList, closeToken);
        }

        public static StackFrameTypeArgumentNode TypeArgument(string identifier)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifier));

        public static StackFrameTypeArgumentNode TypeArgument(StackFrameToken identifier)
            => new(identifier);

        public static StackFrameFileInformationNode FileInformation(StackFrameToken path, StackFrameToken colon, StackFrameToken line)
            => new(path.With(leadingTrivia: CreateTriviaArray(InTrivia)), colon, line);

        public static StackFrameToken Path(string path)
            => CreateToken(StackFrameKind.PathToken, path);

        public static StackFrameToken Line(int lineNumber)
            => CreateToken(StackFrameKind.NumberToken, lineNumber.ToString(), leadingTrivia: ImmutableArray.Create(LineTrivia));
    }
}
