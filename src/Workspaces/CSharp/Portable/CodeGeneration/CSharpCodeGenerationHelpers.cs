// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
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
            IList<SyntaxToken> tokens,
            CodeGenerationOptions options,
            Accessibility defaultAccessibility)
        {
            options = options ?? CodeGenerationOptions.Default;
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

        public static SyntaxList<TDeclaration> Insert<TDeclaration>(
            SyntaxList<TDeclaration> declarationList,
            TDeclaration declaration,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            Func<SyntaxList<TDeclaration>, TDeclaration> after = null,
            Func<SyntaxList<TDeclaration>, TDeclaration> before = null)
            where TDeclaration : SyntaxNode
        {
            var index = GetInsertionIndex(declarationList, declaration, options, availableIndices, after, before);
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

        public static int GetInsertionIndex<TDeclaration>(
            SyntaxList<TDeclaration> declarationList,
            TDeclaration declaration,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            Func<SyntaxList<TDeclaration>, TDeclaration> after = null,
            Func<SyntaxList<TDeclaration>, TDeclaration> before = null)
            where TDeclaration : SyntaxNode
        {
            Contract.ThrowIfTrue(availableIndices != null && availableIndices.Count != declarationList.Count + 1);

            if (options != null)
            {
                // Try to strictly obey the after option by inserting immediately after the member containing the location
                if (options.AfterThisLocation != null)
                {
                    var afterMember = declarationList.LastOrDefault(m => m.SpanStart <= options.AfterThisLocation.SourceSpan.Start);
                    if (afterMember != null)
                    {
                        var index = declarationList.IndexOf(afterMember);
                        index = GetPreferredIndex(index + 1, availableIndices, forward: true);
                        if (index != -1)
                        {
                            return index;
                        }
                    }
                }

                // Try to strictly obey the before option by inserting immediately before the member containing the location
                if (options.BeforeThisLocation != null)
                {
                    var beforeMember = declarationList.FirstOrDefault(m => m.Span.End >= options.BeforeThisLocation.SourceSpan.End);
                    if (beforeMember != null)
                    {
                        var index = declarationList.IndexOf(beforeMember);
                        index = GetPreferredIndex(index, availableIndices, forward: false);
                        if (index != -1)
                        {
                            return index;
                        }
                    }
                }

                if (options.AutoInsertionLocation)
                {
                    if (declarationList.IsEmpty())
                    {
                        return 0;
                    }
                    else if (declarationList.IsSorted(CSharpDeclarationComparer.Instance))
                    {
                        var result = Array.BinarySearch(declarationList.ToArray(), declaration, CSharpDeclarationComparer.Instance);
                        var index = GetPreferredIndex(result < 0 ? ~result : result, availableIndices, forward: true);
                        if (index != -1)
                        {
                            return index;
                        }
                    }

                    if (after != null)
                    {
                        var member = after(declarationList);
                        if (member != null)
                        {
                            var index = declarationList.IndexOf(member);
                            if (index >= 0)
                            {
                                index = GetPreferredIndex(index + 1, availableIndices, forward: true);
                                if (index != -1)
                                {
                                    return index;
                                }
                            }
                        }
                    }

                    if (before != null)
                    {
                        var member = before(declarationList);
                        if (member != null)
                        {
                            var index = declarationList.IndexOf(member);

                            if (index >= 0)
                            {
                                index = GetPreferredIndex(index, availableIndices, forward: false);
                                if (index != -1)
                                {
                                    return index;
                                }
                            }
                        }
                    }
                }
            }

            // Otherwise, add the declaration to the end.
            {
                var index = GetPreferredIndex(declarationList.Count, availableIndices, forward: false);
                if (index != -1)
                {
                    return index;
                }
            }

            return declarationList.Count;
        }

        public static SyntaxNode GetContextNode(
            Location location, CancellationToken cancellationToken)
        {
            var contextLocation = location as Location;

            var contextTree = contextLocation != null && contextLocation.IsInSource
                ? contextLocation.SourceTree
                : null;

            return contextTree == null
                ? null
                : contextTree.GetRoot(cancellationToken).FindToken(contextLocation.SourceSpan.Start).Parent;
        }

        public static ExplicitInterfaceSpecifierSyntax GenerateExplicitInterfaceSpecifier(
            IEnumerable<ISymbol> implementations)
        {
            var implementation = implementations.FirstOrDefault();
            if (implementation == null)
            {
                return null;
            }

            var name = implementation.ContainingType.GenerateTypeSyntax() as NameSyntax;
            if (name == null)
            {
                return null;
            }

            return SyntaxFactory.ExplicitInterfaceSpecifier(name);
        }

        public static CodeGenerationDestination GetDestination(SyntaxNode destination)
        {
            if (destination != null)
            {
                switch (destination.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                        return CodeGenerationDestination.ClassType;
                    case SyntaxKind.CompilationUnit:
                        return CodeGenerationDestination.CompilationUnit;
                    case SyntaxKind.EnumDeclaration:
                        return CodeGenerationDestination.EnumType;
                    case SyntaxKind.InterfaceDeclaration:
                        return CodeGenerationDestination.InterfaceType;
                    case SyntaxKind.NamespaceDeclaration:
                        return CodeGenerationDestination.Namespace;
                    case SyntaxKind.StructDeclaration:
                        return CodeGenerationDestination.StructType;
                    default:
                        return CodeGenerationDestination.Unspecified;
                }
            }

            return CodeGenerationDestination.Unspecified;
        }

        public static TSyntaxNode ConditionallyAddDocumentationCommentTo<TSyntaxNode>(
            TSyntaxNode node,
            ISymbol symbol,
            CodeGenerationOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
            where TSyntaxNode : SyntaxNode
        {
            if (!options.GenerateDocumentationComments || node.GetLeadingTrivia().Any(t => t.IsDocComment()))
            {
                return node;
            }

            string comment;
            var result = TryGetDocumentationComment(symbol, "///", out comment, cancellationToken)
                ? node.WithPrependedLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(comment))
                      .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)
                : node;
            return result;
        }
    }
}
