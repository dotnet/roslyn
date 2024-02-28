// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

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
        CSharpCodeGenerationContextInfo info,
        Accessibility defaultAccessibility)
    {
        if (!info.Context.GenerateDefaultAccessibility && accessibility == defaultAccessibility)
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
        TypeDeclarationSyntax destination, SyntaxList<MemberDeclarationSyntax> members, CancellationToken cancellationToken)
    {
        var syntaxTree = destination.SyntaxTree;
        destination = ReplaceUnterminatedConstructs(destination);

        var node = ConditionallyAddFormattingAnnotationTo(
            destination.EnsureOpenAndCloseBraceTokens().WithMembers(members),
            members);

        // Make sure the generated syntax node has same parse option.
        // e.g. If add syntax member to a C# 5 destination, we should return a C# 5 syntax node.
        var tree = node.SyntaxTree.WithRootAndOptions(node, syntaxTree.Options);
        return (TypeDeclarationSyntax)tree.GetRoot(cancellationToken);
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
        var syntax = (SkippedTokensTriviaSyntax?)skippedTokensTrivia.GetStructure();
        Contract.ThrowIfNull(syntax);

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

    public static MemberDeclarationSyntax? FirstMember(SyntaxList<MemberDeclarationSyntax> members)
        => members.FirstOrDefault();

    public static MemberDeclarationSyntax? FirstMethod(SyntaxList<MemberDeclarationSyntax> members)
        => members.FirstOrDefault(m => m is MethodDeclarationSyntax);

    public static MemberDeclarationSyntax? LastField(SyntaxList<MemberDeclarationSyntax> members)
        => members.LastOrDefault(m => m is FieldDeclarationSyntax);

    public static MemberDeclarationSyntax? LastConstructor(SyntaxList<MemberDeclarationSyntax> members)
        => members.LastOrDefault(m => m is ConstructorDeclarationSyntax);

    public static MemberDeclarationSyntax? LastMethod(SyntaxList<MemberDeclarationSyntax> members)
        => members.LastOrDefault(m => m is MethodDeclarationSyntax);

    public static MemberDeclarationSyntax? LastOperator(SyntaxList<MemberDeclarationSyntax> members)
        => members.LastOrDefault(m => m is OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax);

    public static SyntaxList<TDeclaration> Insert<TDeclaration>(
        SyntaxList<TDeclaration> declarationList,
        TDeclaration declaration,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        Func<SyntaxList<TDeclaration>, TDeclaration?>? after = null,
        Func<SyntaxList<TDeclaration>, TDeclaration?>? before = null)
        where TDeclaration : SyntaxNode
    {
        var index = GetInsertionIndex(
            declarationList, declaration, info, availableIndices,
            CSharpDeclarationComparer.WithoutNamesInstance,
            CSharpDeclarationComparer.WithNamesInstance,
            after, before);

        availableIndices?.Insert(index, true);

        if (index != 0 && declarationList[index - 1].ContainsDiagnostics && AreBracesMissing(declarationList[index - 1]))
        {
            return declarationList.Insert(index, declaration.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
        }

        return declarationList.Insert(index, declaration);
    }

    private static bool AreBracesMissing<TDeclaration>(TDeclaration declaration) where TDeclaration : SyntaxNode
        => declaration.ChildTokens().Where(t => t.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.CloseBraceToken && t.IsMissing).Any();

    public static SyntaxNode? GetContextNode(
        Location location, CancellationToken cancellationToken)
    {
        var contextLocation = location;

        var contextTree = contextLocation != null && contextLocation.IsInSource
            ? contextLocation.SourceTree
            : null;

        return contextTree?.GetRoot(cancellationToken).FindToken(contextLocation!.SourceSpan.Start).Parent;
    }

    public static ExplicitInterfaceSpecifierSyntax? GenerateExplicitInterfaceSpecifier(
        IEnumerable<ISymbol> implementations)
    {
        var implementation = implementations.FirstOrDefault();
        if (implementation == null)
        {
            return null;
        }

        if (implementation.ContainingType.GenerateTypeSyntax() is not NameSyntax name)
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
                SyntaxKind.FileScopedNamespaceDeclaration => CodeGenerationDestination.Namespace,
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
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
        where TSyntaxNode : SyntaxNode
    {
        if (!info.Context.GenerateDocumentationComments || node.GetLeadingTrivia().Any(t => t.IsDocComment()))
        {
            return node;
        }

        var result = TryGetDocumentationComment(symbol, "///", out var comment, cancellationToken)
            ? node.WithPrependedLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(comment))
                  .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)
            : node;
        return result;
    }

    /// <summary>
    /// Try use the existing syntax node and generate a new syntax node for the given <param name="symbol"/>.
    /// Note: the returned syntax node might be modified, which means its parent information might be missing.
    /// </summary>
    public static T? GetReuseableSyntaxNodeForSymbol<T>(ISymbol symbol, CSharpCodeGenerationContextInfo info) where T : SyntaxNode
    {
        Contract.ThrowIfNull(symbol);

        if (info.Context.ReuseSyntax && symbol.DeclaringSyntaxReferences.Length == 1)
        {
            var reusableSyntaxNode = symbol.DeclaringSyntaxReferences[0].GetSyntax();

            if (symbol is IFieldSymbol
                && typeof(T) == typeof(FieldDeclarationSyntax)
                && reusableSyntaxNode is VariableDeclaratorSyntax variableDeclaratorNode
                && reusableSyntaxNode.Parent is VariableDeclarationSyntax variableDeclarationNode
                && reusableSyntaxNode.Parent.Parent is FieldDeclarationSyntax fieldDeclarationNode)
            {
                return RemoveLeadingDirectiveTrivia(
                    fieldDeclarationNode.WithDeclaration(
                        variableDeclarationNode.WithVariables([variableDeclaratorNode]))) as T;
            }

            return RemoveLeadingDirectiveTrivia(reusableSyntaxNode) as T;
        }

        return null;
    }
}
