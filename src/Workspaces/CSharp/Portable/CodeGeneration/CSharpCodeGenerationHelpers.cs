// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class CSharpCodeGenerationHelpers
    {
        public static TDeclarationSyntax ConditionallyAddFormattingAnnotationTo<TDeclarationSyntax>(
            TDeclarationSyntax result,
            SyntaxList<MemberDeclarationSyntax> members) where TDeclarationSyntax : MemberDeclarationSyntax
        {
            return members.Count == 1
                ? result.WithAdditionalAnnotations(Formatter.Annotation)
                : result;
        }

        internal static void AddAccessibilityModifiers(
            Accessibility accessibility,
            ArrayBuilder<SyntaxToken> tokens,
            CodeGenerationOptions options,
            Accessibility defaultAccessibility)
        {
            options ??= CodeGenerationOptions.Default;
            if (!options.GenerateDefaultAccessibility && accessibility == defaultAccessibility)
            {
                return;
            }

            switch (accessibility)
            {
                case Accessibility.Public:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    break;
                case Accessibility.Protected:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Private:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    break;
                case Accessibility.ProtectedAndInternal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Internal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.ProtectedOrInternal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
            }
        }

        public static TypeDeclarationSyntax AddMembersTo(
            TypeDeclarationSyntax destination, SyntaxList<MemberDeclarationSyntax> members)
        {
            destination = ReplaceUnterminatedConstructs(destination);

            return ConditionallyAddFormattingAnnotationTo(
                destination.EnsureOpenAndCloseBraceTokens().WithMembers(members),
                members);
        }

        private static TypeDeclarationSyntax ReplaceUnterminatedConstructs(TypeDeclarationSyntax destination)
        {
            const string MultiLineCommentTerminator = "*/";
            var lastToken = destination.GetLastToken();
            var updatedToken = lastToken.ReplaceTrivia(lastToken.TrailingTrivia,
                (t1, t2) =>
                {
                    if (t1.Kind() == SyntaxKind.MultiLineCommentTrivia)
                    {
                        var text = t1.ToString();
                        if (!text.EndsWith(MultiLineCommentTerminator, StringComparison.Ordinal))
                        {
                            return SyntaxFactory.SyntaxTrivia(SyntaxKind.MultiLineCommentTrivia, text + MultiLineCommentTerminator);
                        }
                    }
                    else if (t1.Kind() == SyntaxKind.SkippedTokensTrivia)
                    {
                        return ReplaceUnterminatedConstructs(t1);
                    }

                    return t1;
                });

            return destination.ReplaceToken(lastToken, updatedToken);
        }

        private static SyntaxTrivia ReplaceUnterminatedConstructs(SyntaxTrivia skippedTokensTrivia)
        {
            var syntax = (SkippedTokensTriviaSyntax)skippedTokensTrivia.GetStructure();
            var tokens = syntax.Tokens;

            var updatedTokens = SyntaxFactory.TokenList(tokens.Select(ReplaceUnterminatedConstruct));
            var updatedSyntax = syntax.WithTokens(updatedTokens);

            return SyntaxFactory.Trivia(updatedSyntax);
        }

        private static SyntaxToken ReplaceUnterminatedConstruct(SyntaxToken token)
        {
            if (token.IsVerbatimStringLiteral())
            {
                var tokenText = token.ToString();
                if (tokenText.Length <= 2 || tokenText.Last() != '"')
                {
                    tokenText += '"';
                    return SyntaxFactory.Literal(token.LeadingTrivia, tokenText, token.ValueText, token.TrailingTrivia);
                }
            }
            else if (token.IsRegularStringLiteral())
            {
                var tokenText = token.ToString();
                if (tokenText.Length <= 1 || tokenText.Last() != '"')
                {
                    tokenText += '"';
                    return SyntaxFactory.Literal(token.LeadingTrivia, tokenText, token.ValueText, token.TrailingTrivia);
                }
            }

            return token;
        }

        public static MemberDeclarationSyntax FirstMember(SyntaxList<MemberDeclarationSyntax> members)
        {
            return members.FirstOrDefault();
        }

        public static MemberDeclarationSyntax FirstMethod(SyntaxList<MemberDeclarationSyntax> members)
        {
            return members.FirstOrDefault(m => m is MethodDeclarationSyntax);
        }

        public static MemberDeclarationSyntax LastField(SyntaxList<MemberDeclarationSyntax> members)
        {
            return members.LastOrDefault(m => m is FieldDeclarationSyntax);
        }

        public static MemberDeclarationSyntax LastConstructor(SyntaxList<MemberDeclarationSyntax> members)
        {
            return members.LastOrDefault(m => m is ConstructorDeclarationSyntax);
        }

        public static MemberDeclarationSyntax LastMethod(SyntaxList<MemberDeclarationSyntax> members)
        {
            return members.LastOrDefault(m => m is MethodDeclarationSyntax);
        }

        public static MemberDeclarationSyntax LastOperator(SyntaxList<MemberDeclarationSyntax> members)
        {
            return members.LastOrDefault(m => m is OperatorDeclarationSyntax || m is ConversionOperatorDeclarationSyntax);
        }

        public static StatementSyntax LastStatement(SyntaxList<StatementSyntax> statements)
        {
            return statements.LastOrDefault(s => s is StatementSyntax);
        }

        public static SyntaxList<TDeclaration> Insert<TDeclaration>(
            SyntaxList<TDeclaration> declarationList,
            TDeclaration declaration,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            Func<SyntaxList<TDeclaration>, TDeclaration> after = null,
            Func<SyntaxList<TDeclaration>, TDeclaration> before = null)
            where TDeclaration : SyntaxNode
        {
            var index = GetInsertionIndex(
                declarationList, declaration, options, availableIndices,
                CSharpDeclarationComparer.WithoutNamesInstance,
                CSharpDeclarationComparer.WithNamesInstance,
                after, before);

            if (availableIndices != null)
            {
                availableIndices.Insert(index, true);
            }

            if (index != 0 && declarationList[index - 1].ContainsDiagnostics && AreBracesMissing(declarationList[index - 1]))
            {
                return declarationList.Insert(index, declaration.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
            }

            return declarationList.Insert(index, declaration);
        }

        private static bool AreBracesMissing<TDeclaration>(TDeclaration declaration) where TDeclaration : SyntaxNode
        {
            return declaration.ChildTokens().Where(t => t.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken) && t.IsMissing).Any();
        }

        public static SyntaxNode GetContextNode(
            Location location, CancellationToken cancellationToken)
        {
            var contextLocation = location as Location;

            var contextTree = contextLocation != null && contextLocation.IsInSource
                ? contextLocation.SourceTree
                : null;

            return contextTree?.GetRoot(cancellationToken).FindToken(contextLocation.SourceSpan.Start).Parent;
        }

        public static ExplicitInterfaceSpecifierSyntax GenerateExplicitInterfaceSpecifier(
            IEnumerable<ISymbol> implementations)
        {
            var implementation = implementations.FirstOrDefault();
            if (implementation == null)
            {
                return null;
            }

            if (!(implementation.ContainingType.GenerateTypeSyntax() is NameSyntax name))
            {
                return null;
            }

            return SyntaxFactory.ExplicitInterfaceSpecifier(name);
        }

        public static CodeGenerationDestination GetDestination(SyntaxNode destination)
        {
            if (destination != null)
            {
                return destination.Kind() switch
                {
                    SyntaxKind.ClassDeclaration => CodeGenerationDestination.ClassType,
                    SyntaxKind.CompilationUnit => CodeGenerationDestination.CompilationUnit,
                    SyntaxKind.EnumDeclaration => CodeGenerationDestination.EnumType,
                    SyntaxKind.InterfaceDeclaration => CodeGenerationDestination.InterfaceType,
                    SyntaxKind.NamespaceDeclaration => CodeGenerationDestination.Namespace,
                    SyntaxKind.StructDeclaration => CodeGenerationDestination.StructType,
                    _ => CodeGenerationDestination.Unspecified,
                };
            }

            return CodeGenerationDestination.Unspecified;
        }

        public static TSyntaxNode ConditionallyAddDocumentationCommentTo<TSyntaxNode>(
            TSyntaxNode node,
            ISymbol symbol,
            CodeGenerationOptions options,
            CancellationToken cancellationToken = default)
            where TSyntaxNode : SyntaxNode
        {
            if (!options.GenerateDocumentationComments || node.GetLeadingTrivia().Any(t => t.IsDocComment()))
            {
                return node;
            }

            var result = TryGetDocumentationComment(symbol, "///", out var comment, cancellationToken)
                ? node.WithPrependedLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(comment))
                      .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)
                : node;
            return result;
        }
    }
}
