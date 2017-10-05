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
        public static TypeDeclarationSyntax AddMembers(
            this TypeDeclarationSyntax node, params MemberDeclarationSyntax[] members)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).AddMembers(members);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).AddMembers(members);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).AddMembers(members);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithMembers(
            this TypeDeclarationSyntax node, SyntaxList<MemberDeclarationSyntax> members)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithMembers(members);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithMembers(members);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithMembers(members);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithAttributeLists(
            this TypeDeclarationSyntax node, SyntaxList<AttributeListSyntax> attributes)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithAttributeLists(attributes);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithAttributeLists(attributes);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithAttributeLists(attributes);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithIdentifier(
            this TypeDeclarationSyntax node, SyntaxToken identifier)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithIdentifier(identifier);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithIdentifier(identifier);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithIdentifier(identifier);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithModifiers(
            this TypeDeclarationSyntax node, SyntaxTokenList modifiers)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithModifiers(modifiers);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithModifiers(modifiers);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithModifiers(modifiers);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithTypeParameterList(
            this TypeDeclarationSyntax node, TypeParameterListSyntax list)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithTypeParameterList(list);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithTypeParameterList(list);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithTypeParameterList(list);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithBaseList(
            this TypeDeclarationSyntax node, BaseListSyntax list)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithBaseList(list);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithBaseList(list);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithBaseList(list);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithConstraintClauses(
            this TypeDeclarationSyntax node, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithConstraintClauses(constraintClauses);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithConstraintClauses(constraintClauses);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithConstraintClauses(constraintClauses);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithOpenBraceToken(
            this TypeDeclarationSyntax node, SyntaxToken openBrace)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithOpenBraceToken(openBrace);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithOpenBraceToken(openBrace);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithOpenBraceToken(openBrace);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        public static TypeDeclarationSyntax WithCloseBraceToken(
            this TypeDeclarationSyntax node, SyntaxToken closeBrace)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithCloseBraceToken(closeBrace);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithCloseBraceToken(closeBrace);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithCloseBraceToken(closeBrace);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

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

                for (int i = 0; i < members.Count - 1; i++)
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

            IEnumerable<BaseTypeSyntax> baseListTypes = SpecializedCollections.EmptyEnumerable<BaseTypeSyntax>();

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
                var leadingTrivia = prependNewLineIfMissing ? token.LeadingTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed) : token.LeadingTrivia;
                var trailingTrivia = appendNewLineIfMissing ? token.TrailingTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed) : token.TrailingTrivia;
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
                int index = -1;
                var leadingTrivia = closeBrace.LeadingTrivia;
                for (int i = leadingTrivia.Count - 1; i >= 0; i--)
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
