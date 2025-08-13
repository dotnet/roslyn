// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class TypeDeclarationSyntaxExtensions
{
    public static IList<bool> GetInsertionIndices(this TypeDeclarationSyntax destination, CancellationToken cancellationToken)
    {
        var members = destination.Members;

        var indices = new List<bool>();
        if (members is not [var firstMember, ..])
        {
            var start = destination.OpenBraceToken.Span.End;
            var end = GetEndToken(destination).SpanStart;

            indices.Add(!destination.OverlapsHiddenPosition(TextSpan.FromBounds(start, end), cancellationToken));
        }
        else
        {
            var start = destination.OpenBraceToken.Span.End;
            var end = firstMember.SpanStart;
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

        if (typeNode.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            var typeSymbol = model.GetRequiredDeclaredSymbol(typeNode, cancellationToken);
            if (typeSymbol.DeclaringSyntaxReferences.Length >= 2)
            {
                using var _ = ArrayBuilder<BaseTypeSyntax>.GetInstance(out var baseListTypes);

                foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
                {
                    if (syntaxRef.GetSyntax(cancellationToken) is TypeDeclarationSyntax { BaseList.Types: var baseTypes })
                        baseListTypes.AddRange(baseTypes);
                }

                return baseListTypes.ToImmutableAndClear();
            }
        }

        if (typeNode.BaseList != null)
            return typeNode.BaseList.Types;

        return [];
    }

    private static SyntaxToken EnsureToken(SyntaxToken token, SyntaxKind kind, bool prependNewLineIfMissing = false, bool appendNewLineIfMissing = false)
    {
        if (token.IsMissing || token.IsKind(SyntaxKind.None))
        {
            var leadingTrivia = prependNewLineIfMissing ? token.LeadingTrivia.Insert(0, SyntaxFactory.ElasticCarriageReturnLineFeed) : token.LeadingTrivia;
            var trailingTrivia = appendNewLineIfMissing ? token.TrailingTrivia.Insert(0, SyntaxFactory.ElasticCarriageReturnLineFeed) : token.TrailingTrivia;
            return SyntaxFactory.Token(leadingTrivia, kind, trailingTrivia).WithAdditionalAnnotations(Formatter.Annotation);
        }

        return token;
    }

    private static BaseTypeDeclarationSyntax EnsureHasBraces(BaseTypeDeclarationSyntax typeDeclaration, bool hasMembers)
    {
        var openBrace = EnsureToken(typeDeclaration.OpenBraceToken, SyntaxKind.OpenBraceToken);
        var closeBrace = EnsureToken(typeDeclaration.CloseBraceToken, SyntaxKind.CloseBraceToken, appendNewLineIfMissing: true);

        // If we are adding braces, then remove any semicolon to we convert something like `record class X();` to
        // `record class X { }`
        var addedBraces = openBrace != typeDeclaration.OpenBraceToken || closeBrace != typeDeclaration.CloseBraceToken;
        if (addedBraces && typeDeclaration.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
            typeDeclaration = typeDeclaration.WithSemicolonToken(default).WithTrailingTrivia(typeDeclaration.SemicolonToken.TrailingTrivia);

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

        return typeDeclaration.WithOpenBraceToken(openBrace).WithCloseBraceToken(closeBrace);
    }

    public static TypeDeclarationSyntax EnsureOpenAndCloseBraceTokens(this TypeDeclarationSyntax typeDeclaration)
    {
        return (TypeDeclarationSyntax)EnsureHasBraces(typeDeclaration, typeDeclaration.Members.Count > 0);
    }

    public static EnumDeclarationSyntax EnsureOpenAndCloseBraceTokens(this EnumDeclarationSyntax typeDeclaration)
    {
        return (EnumDeclarationSyntax)EnsureHasBraces(typeDeclaration, typeDeclaration.Members.Count > 0);
    }
}
