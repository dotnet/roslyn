﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SolutionExplorer;

internal interface ISolutionExplorerSymbolTreeItemProvider : ILanguageService
{
    ImmutableArray<SymbolTreeItemData> GetItems(DocumentId documentId, SyntaxNode declarationNode, CancellationToken cancellationToken);
}

internal abstract class AbstractSolutionExplorerSymbolTreeItemProvider<
    TCompilationUnitSyntax,
    TMemberDeclarationSyntax,
    TNamespaceDeclarationSyntax,
    TEnumDeclarationSyntax,
    TTypeDeclarationSyntax>
    : ISolutionExplorerSymbolTreeItemProvider
    where TCompilationUnitSyntax : SyntaxNode
    where TMemberDeclarationSyntax : SyntaxNode
    where TNamespaceDeclarationSyntax : TMemberDeclarationSyntax
    where TEnumDeclarationSyntax : TMemberDeclarationSyntax
{
    protected static void AppendCommaSeparatedList<TArgumentList, TArgument>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        TArgumentList? argumentList,
        Func<TArgumentList, SeparatedSyntaxList<TArgument>> getArguments,
        Action<TArgument, StringBuilder> append,
        string separator = ", ")
        where TArgumentList : SyntaxNode
        where TArgument : SyntaxNode
    {
        if (argumentList is null)
            return;

        AppendCommaSeparatedList(builder, openBrace, closeBrace, getArguments(argumentList), append, separator);
    }

    protected static void AppendCommaSeparatedList<TNode>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        SeparatedSyntaxList<TNode> arguments,
        Action<TNode, StringBuilder> append,
        string separator = ", ")
        where TNode : SyntaxNode
    {
        builder.Append(openBrace);
        builder.AppendJoinedValues(separator, arguments, append);
        builder.Append(closeBrace);
    }

    protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TCompilationUnitSyntax root);
    protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TNamespaceDeclarationSyntax baseNamespace);
    protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TTypeDeclarationSyntax typeDeclaration);

    protected abstract bool TryAddType(DocumentId documentId, TMemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder);
    protected abstract void AddMemberDeclaration(DocumentId documentId, TMemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder);
    protected abstract void AddEnumDeclarationMembers(DocumentId documentId, TEnumDeclarationSyntax enumDeclaration, ArrayBuilder<SymbolTreeItemData> items, CancellationToken cancellationToken);

    public ImmutableArray<SymbolTreeItemData> GetItems(DocumentId documentId, SyntaxNode node, CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<SymbolTreeItemData>.GetInstance(out var items);
        using var _2 = PooledStringBuilder.GetInstance(out var nameBuilder);

        switch (node)
        {
            case TCompilationUnitSyntax compilationUnit:
                AddTopLevelTypes(documentId, compilationUnit, items, nameBuilder, cancellationToken);
                break;

            case TEnumDeclarationSyntax enumDeclaration:
                AddEnumDeclarationMembers(documentId, enumDeclaration, items, cancellationToken);
                break;

            case TTypeDeclarationSyntax typeDeclaration:
                AddTypeDeclarationMembers(typeDeclaration);
                break;
        }

        return items.ToImmutableAndClear();

        void AddTypeDeclarationMembers(TTypeDeclarationSyntax typeDeclaration)
        {
            foreach (var member in GetMembers(typeDeclaration))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (TryAddType(documentId, member, items, nameBuilder))
                    continue;

                AddMemberDeclaration(documentId, member, items, nameBuilder);
            }
        }
    }

    private void AddTopLevelTypes(
        DocumentId documentId,
        TCompilationUnitSyntax root,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder,
        CancellationToken cancellationToken)
    {
        foreach (var member in GetMembers(root))
            RecurseIntoMemberDeclaration(member);

        return;

        void RecurseIntoMemberDeclaration(TMemberDeclarationSyntax member)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is TNamespaceDeclarationSyntax baseNamespace)
            {
                foreach (var childMember in GetMembers(baseNamespace))
                    RecurseIntoMemberDeclaration(childMember);
            }
            else
            {
                TryAddType(documentId, member, items, nameBuilder);
            }
        }
    }
}
