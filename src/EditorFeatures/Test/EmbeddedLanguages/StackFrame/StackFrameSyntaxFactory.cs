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
        public static readonly StackFrameToken GraveAccentToken = CreateToken(StackFrameKind.GraveAccentToken, "`");
        public static readonly StackFrameToken EOLToken = CreateToken(StackFrameKind.EndOfFrame, "");
        public static readonly StackFrameToken ColonToken = CreateToken(StackFrameKind.ColonToken, ":");
        public static readonly StackFrameToken DollarToken = CreateToken(StackFrameKind.DollarToken, "$");
        public static readonly StackFrameToken PipeToken = CreateToken(StackFrameKind.PipeToken, "|");
        public static readonly StackFrameToken ConstructorToken = CreateToken(StackFrameKind.ConstructorToken, ".ctor");
        public static readonly StackFrameToken StaticConstructorToken = CreateToken(StackFrameKind.ConstructorToken, ".cctor");

        public static readonly StackFrameTrivia AtTrivia = CreateTrivia(StackFrameKind.AtTrivia, "at ");
        public static readonly StackFrameTrivia LineTrivia = CreateTrivia(StackFrameKind.LineTrivia, "line ");
        public static readonly StackFrameTrivia InTrivia = CreateTrivia(StackFrameKind.InTrivia, " in ");

        public static readonly StackFrameConstructorNode Constructor = new(ConstructorToken);
        public static readonly StackFrameConstructorNode StaticConstructor = new(StaticConstructorToken);

        public static readonly StackFrameParameterList EmptyParams = ParameterList(OpenParenToken, CloseParenToken);

        public static StackFrameParameterDeclarationNode Parameter(StackFrameTypeNode type, StackFrameToken identifier)
            => new(type, identifier);

        public static StackFrameParameterList ParameterList(params StackFrameParameterDeclarationNode[] parameters)
            => ParameterList(OpenParenToken, CloseParenToken, parameters);

        public static StackFrameParameterList ParameterList(StackFrameToken openToken, StackFrameToken closeToken, params StackFrameParameterDeclarationNode[] parameters)
        {
            var separatedList = parameters.Length == 0
                ? EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameParameterDeclarationNode>.Empty
                : new(CommaSeparateList(parameters));

            return new(openToken, separatedList, closeToken);

            static ImmutableArray<StackFrameNodeOrToken> CommaSeparateList(StackFrameParameterDeclarationNode[] parameters)
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
            StackFrameQualifiedNameNode memberAccessExpression,
            StackFrameTypeArgumentList? typeArguments = null,
            StackFrameParameterList? argumentList = null)
        {
            return new StackFrameMethodDeclarationNode(memberAccessExpression, typeArguments, argumentList ?? ParameterList(OpenParenToken, CloseParenToken));
        }

        public static StackFrameGeneratedMethodNameNode GeneratedName(string name, bool endWithDollar = true)
            => new(LessThanToken, IdentifierToken(name), GreaterThanToken, endWithDollar ? DollarToken : null);

        public static StackFrameQualifiedNameNode QualifiedName(string s, StackFrameTrivia? leadingTrivia = null, StackFrameTrivia? trailingTrivia = null)
            => QualifiedName(s, leadingTrivia.ToImmutableArray(), trailingTrivia.ToImmutableArray());

        public static StackFrameQualifiedNameNode QualifiedName(string s, ImmutableArray<StackFrameTrivia> leadingTrivia, ImmutableArray<StackFrameTrivia> trailingTrivia)
        {
            StackFrameNameNode? current = null;
            Assert.True(s.Contains('.'));

            var identifiers = s.Split('.');
            for (var i = 0; i < identifiers.Length; i++)
            {
                var identifier = identifiers[i];

                if (current is null)
                {
                    current = Identifier(IdentifierToken(identifier, leadingTrivia: leadingTrivia, trailingTrivia: ImmutableArray<StackFrameTrivia>.Empty));
                }
                else if (i == identifiers.Length - 1)
                {
                    var rhs = Identifier(IdentifierToken(identifier, leadingTrivia: ImmutableArray<StackFrameTrivia>.Empty, trailingTrivia: trailingTrivia));
                    current = QualifiedName(current, rhs);
                }
                else
                {
                    current = QualifiedName(current, Identifier(identifier));
                }
            }

            AssertEx.NotNull(current);
            return (StackFrameQualifiedNameNode)current;
        }

        public static StackFrameTrivia SpaceTrivia(int count = 1)
            => CreateTrivia(StackFrameKind.WhitespaceTrivia, new string(' ', count));

        public static StackFrameQualifiedNameNode QualifiedName(StackFrameNameNode left, StackFrameSimpleNameNode right)
            => new(left, DotToken, right);

        public static StackFrameToken IdentifierToken(string identifierName)
            => IdentifierToken(identifierName, leadingTrivia: null, trailingTrivia: null);

        public static StackFrameToken IdentifierToken(string identifierName, StackFrameTrivia? leadingTrivia = null, StackFrameTrivia? trailingTrivia = null)
            => IdentifierToken(identifierName, leadingTrivia.ToImmutableArray(), trailingTrivia.ToImmutableArray());

        public static StackFrameToken IdentifierToken(string identifierName, ImmutableArray<StackFrameTrivia> leadingTrivia, ImmutableArray<StackFrameTrivia> trailingTrivia)
            => CreateToken(StackFrameKind.IdentifierToken, identifierName, leadingTrivia: leadingTrivia, trailingTrivia: trailingTrivia);

        public static StackFrameIdentifierNameNode Identifier(string name)
            => Identifier(IdentifierToken(name));

        public static StackFrameIdentifierNameNode Identifier(StackFrameToken identifier)
            => new(identifier);

        public static StackFrameIdentifierNameNode Identifier(string name, StackFrameTrivia? leadingTrivia = null, StackFrameTrivia? trailingTrivia = null)
            => Identifier(IdentifierToken(name, leadingTrivia, trailingTrivia));

        public static StackFrameArrayRankSpecifier ArrayRankSpecifier(int commaCount = 0, StackFrameTrivia? leadingTrivia = null, StackFrameTrivia? trailingTrivia = null)
            => new(OpenBracketToken.With(leadingTrivia: leadingTrivia.ToImmutableArray()), CloseBracketToken.With(trailingTrivia: trailingTrivia.ToImmutableArray()), Enumerable.Repeat(CommaToken, commaCount).ToImmutableArray());

        public static StackFrameArrayRankSpecifier ArrayRankSpecifier(StackFrameToken openToken, StackFrameToken closeToken, params StackFrameToken[] commaTokens)
            => new(openToken, closeToken, commaTokens.ToImmutableArray());

        public static StackFrameArrayTypeNode ArrayType(StackFrameNameNode identifier, params StackFrameArrayRankSpecifier[] arrayTokens)
            => new(identifier, arrayTokens.ToImmutableArray());

        public static StackFrameGenericNameNode GenericType(string identifierName, int arity)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifierName), GraveAccentToken, CreateToken(StackFrameKind.NumberToken, arity.ToString()));

        public static StackFrameTypeArgumentList TypeArgumentList(params StackFrameIdentifierNameNode[] typeArguments)
            => TypeArgumentList(useBrackets: true, typeArguments);

        public static StackFrameTypeArgumentList TypeArgumentList(bool useBrackets, params StackFrameIdentifierNameNode[] typeArguments)
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

            var typeArgumentsList = new EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameIdentifierNameNode>(builder.ToImmutable());

            return new(openToken, typeArgumentsList, closeToken);
        }

        public static StackFrameIdentifierNameNode TypeArgument(string identifier)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifier));

        public static StackFrameIdentifierNameNode TypeArgument(StackFrameToken identifier)
            => new(identifier);

        public static StackFrameFileInformationNode FileInformation(StackFrameToken path, StackFrameToken colon, StackFrameToken line, StackFrameTrivia? inTrivia = null)
            => new(path.With(leadingTrivia: ImmutableArray.Create(inTrivia.HasValue ? inTrivia.Value : InTrivia)), colon, line);

        public static StackFrameToken Path(string path)
            => CreateToken(StackFrameKind.PathToken, path);

        public static StackFrameToken Line(int lineNumber)
            => CreateToken(StackFrameKind.NumberToken, lineNumber.ToString(), leadingTrivia: ImmutableArray.Create(LineTrivia));

        public static StackFrameLocalMethodNameNode LocalMethod(StackFrameGeneratedMethodNameNode encapsulatingMethod, string identifier, string suffix)
            => new(
                encapsulatingMethod,
                CreateToken(StackFrameKind.GeneratedNameSeparatorToken, "g__"),
                IdentifierToken(identifier),
                PipeToken,
                CreateToken(StackFrameKind.GeneratedNameSuffixToken, suffix));
    }
}
