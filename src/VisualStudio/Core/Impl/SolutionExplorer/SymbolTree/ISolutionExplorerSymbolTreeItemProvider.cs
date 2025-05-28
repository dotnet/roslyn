// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal interface ISolutionExplorerSymbolTreeItemProvider : ILanguageService
{
    ImmutableArray<SymbolTreeItemData> GetItems(SyntaxNode declarationNode, CancellationToken cancellationToken);
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
        Func<TArgumentList, IEnumerable<TArgument>> getArguments,
        Action<TArgument, StringBuilder> append,
        string separator = ", ")
        where TArgumentList : SyntaxNode
        where TArgument : SyntaxNode
    {
        if (argumentList is null)
            return;

        AppendCommaSeparatedList(builder, openBrace, closeBrace, getArguments(argumentList), append, separator);
    }

    protected static void AppendCommaSeparatedList<TArgument>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        IEnumerable<TArgument> arguments,
        Action<TArgument, StringBuilder> append,
        string separator = ", ")
        where TArgument : SyntaxNode
    {
        builder.Append(openBrace);
        builder.AppendJoinedValues(separator, arguments, append);
        builder.Append(closeBrace);
    }

    protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TCompilationUnitSyntax root);
    protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TNamespaceDeclarationSyntax baseNamespace);
    protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TTypeDeclarationSyntax typeDeclaration);

    protected abstract bool TryAddType(TMemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder);
    protected abstract void AddMemberDeclaration(TMemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder);
    protected abstract void AddEnumDeclarationMembers(TEnumDeclarationSyntax enumDeclaration, ArrayBuilder<SymbolTreeItemData> items, CancellationToken cancellationToken);

    public ImmutableArray<SymbolTreeItemData> GetItems(SyntaxNode node, CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<SymbolTreeItemData>.GetInstance(out var items);
        using var _2 = PooledStringBuilder.GetInstance(out var nameBuilder);

        switch (node)
        {
            case TCompilationUnitSyntax compilationUnit:
                AddTopLevelTypes(compilationUnit, items, nameBuilder, cancellationToken);
                break;

            case TEnumDeclarationSyntax enumDeclaration:
                AddEnumDeclarationMembers(enumDeclaration, items, cancellationToken);
                break;

            case TTypeDeclarationSyntax typeDeclaration:
                AddTypeDeclarationMembers(typeDeclaration, items, nameBuilder, cancellationToken);
                break;
        }

        return items.ToImmutableAndClear();
    }

    private void AddTopLevelTypes(
        TCompilationUnitSyntax root, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder, CancellationToken cancellationToken)
    {
        foreach (var member in GetMembers(root))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is TNamespaceDeclarationSyntax baseNamespace)
                AddTopLevelTypes(baseNamespace, items, nameBuilder, cancellationToken);
            else
                TryAddType(member, items, nameBuilder);
        }
    }

    private void AddTopLevelTypes(
        TNamespaceDeclarationSyntax baseNamespace, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder, CancellationToken cancellationToken)
    {
        foreach (var member in GetMembers(baseNamespace))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is TNamespaceDeclarationSyntax childNamespace)
                AddTopLevelTypes(childNamespace, items, nameBuilder, cancellationToken);
            else
                TryAddType(member, items, nameBuilder);
        }
    }

    private void AddTypeDeclarationMembers(
        TTypeDeclarationSyntax typeDeclaration, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder, CancellationToken cancellationToken)
    {
        foreach (var member in GetMembers(typeDeclaration))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryAddType(member, items, nameBuilder))
                continue;

            AddMemberDeclaration(member, items, nameBuilder);
        }
    }
}
