// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class TypeDeclarationSyntaxExtensions
    {
        public static IList<bool> GetInsertionIndices(this TypeDeclarationSyntax destination, CancellationToken cancellationToken)
        {
            var members = destination.Members;

            var indices = new List<bool>();
            if (members.Count == 0)
            {
                var start = destination.OpenBraceToken.Span.End;
                var end = GetEndToken(destination).SpanStart;

                indices.Add(!destination.OverlapsHiddenPosition(TextSpan.FromBounds(start, end), cancellationToken));
            }
            else
            {
                var start = destination.OpenBraceToken.Span.End;
                var end = destination.Members.First().SpanStart;
                indices.Add(!destination.OverlapsHiddenPosition(TextSpan.FromBounds(start, end), cancellationToken));

                for (var i = 0; i < members.Count - 1; i++)
                {
                    var member1 = members[i];
                    var member2 = members[i + 1];

                    indices.Add(!destination.OverlapsHiddenPosition(member1, member2, cancellationToken));
                }

                start = members.Last().Span.End;
                end = GetEndToken(destination).SpanStart;
                indices.Add(!destination.OverlapsHiddenPosition(TextSpan.FromBounds(start, end), cancellationToken));
            }

            return indices;
        }

        private static SyntaxToken GetEndToken(SyntaxNode node)
        {
            var lastToken = node.GetLastToken(includeZeroWidth: true, includeSkipped: true);

            if (lastToken.IsMissing)
            {
                var nextToken = lastToken.GetNextToken(includeZeroWidth: true, includeSkipped: true);
                if (nextToken.RawKind != 0)
                {
                    return nextToken;
                }
            }

            return lastToken;
        }

        public static IEnumerable<BaseTypeSyntax> GetAllBaseListTypes(this TypeDeclarationSyntax typeNode, SemanticModel model, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(typeNode);

            var baseListTypes = SpecializedCollections.EmptyEnumerable<BaseTypeSyntax>();

            var isPartialType = typeNode.Modifiers.Any(m => m.Kind() == SyntaxKind.PartialKeyword);
            if (isPartialType)
            {
                var typeSymbol = model.GetDeclaredSymbol(typeNode, cancellationToken);
                if (typeSymbol != null)
                {
                    foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.GetSyntax(cancellationToken) is TypeDeclarationSyntax typeDecl && typeDecl.BaseList != null)
                        {
                            baseListTypes = baseListTypes.Concat(typeDecl.BaseList.Types);
                        }
                    }
                }
            }
            else if (typeNode.BaseList != null)
            {
                return typeNode.BaseList.Types;
            }

            return baseListTypes;
        }

        private static SyntaxToken EnsureToken(SyntaxToken token, bool prependNewLineIfMissing = false, bool appendNewLineIfMissing = false)
        {
            if (token.IsMissing)
            {
                var leadingTrivia = prependNewLineIfMissing ? token.LeadingTrivia.Insert(0, SyntaxFactory.ElasticCarriageReturnLineFeed) : token.LeadingTrivia;
                var trailingTrivia = appendNewLineIfMissing ? token.TrailingTrivia.Insert(0, SyntaxFactory.ElasticCarriageReturnLineFeed) : token.TrailingTrivia;
                return SyntaxFactory.Token(leadingTrivia, token.Kind(), trailingTrivia).WithAdditionalAnnotations(Formatter.Annotation);
            }

            return token;
        }

        private static void EnsureAndGetBraceTokens(
            BaseTypeDeclarationSyntax typeDeclaration,
            bool hasMembers,
            out SyntaxToken openBrace,
            out SyntaxToken closeBrace)
        {
            openBrace = EnsureToken(typeDeclaration.OpenBraceToken);
            closeBrace = EnsureToken(typeDeclaration.CloseBraceToken, appendNewLineIfMissing: true);

            if (!hasMembers)
            {
                // Bug 539673: If there are no members, take any trivia that
                // belongs to the end brace and attach it to the opening brace.
                var index = -1;
                var leadingTrivia = closeBrace.LeadingTrivia;
                for (var i = leadingTrivia.Count - 1; i >= 0; i--)
                {
                    if (!leadingTrivia[i].IsWhitespaceOrEndOfLine())
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    openBrace = openBrace.WithTrailingTrivia(
                        openBrace.TrailingTrivia.Concat(closeBrace.LeadingTrivia.Take(index + 1)));
                    closeBrace = closeBrace.WithLeadingTrivia(
                        closeBrace.LeadingTrivia.Skip(index + 1));
                }
            }
        }

        public static TypeDeclarationSyntax EnsureOpenAndCloseBraceTokens(
            this TypeDeclarationSyntax typeDeclaration)
        {
            EnsureAndGetBraceTokens(typeDeclaration, typeDeclaration.Members.Count > 0, out var openBrace, out var closeBrace);
            return typeDeclaration.WithOpenBraceToken(openBrace).WithCloseBraceToken(closeBrace);
        }

        public static EnumDeclarationSyntax EnsureOpenAndCloseBraceTokens(
            this EnumDeclarationSyntax typeDeclaration)
        {
            EnsureAndGetBraceTokens(typeDeclaration, typeDeclaration.Members.Count > 0, out var openBrace, out var closeBrace);
            return typeDeclaration.WithOpenBraceToken(openBrace).WithCloseBraceToken(closeBrace);
        }
    }
}
