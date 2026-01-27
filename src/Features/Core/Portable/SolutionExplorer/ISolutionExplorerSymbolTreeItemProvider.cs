// Licensed to the .NET Foundation under one or more agreements.
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
    ImmutableArray<SymbolTreeItemData> GetItems(DocumentId documentId, SyntaxNode declarationNode, bool includeNamespaces, CancellationToken cancellationToken);
}

internal abstract class AbstractSolutionExplorerSymbolTreeItemProvider<
    TCompilationUnitSyntax,
    TMemberDeclarationSyntax,
    TNamespaceDeclarationSyntax,
    TEnumDeclarationSyntax,
    TTypeDeclarationSyntax,
    TMemberStatement>
    : ISolutionExplorerSymbolTreeItemProvider
    where TCompilationUnitSyntax : SyntaxNode
    where TMemberDeclarationSyntax : SyntaxNode
    where TNamespaceDeclarationSyntax : TMemberDeclarationSyntax
    where TEnumDeclarationSyntax : TMemberDeclarationSyntax
    where TMemberStatement : SyntaxNode
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
    protected abstract void AddNamespace(DocumentId documentId, TNamespaceDeclarationSyntax namespaceMember, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder);
    protected abstract void AddMemberDeclaration(DocumentId documentId, TMemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder);
    protected abstract void AddEnumDeclarationMembers(DocumentId documentId, TEnumDeclarationSyntax enumDeclaration, ArrayBuilder<SymbolTreeItemData> items, CancellationToken cancellationToken);

    protected virtual ImmutableArray<TMemberStatement> GetMemberDeclarationMembers(TMemberDeclarationSyntax memberDeclaration) => [];
    protected virtual ImmutableArray<TMemberStatement> GetMemberStatementMembers(TMemberStatement memberDeclaration) => [];
    protected virtual void AddMemberStatement(DocumentId documentId, TMemberStatement statement, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder) { }

    public ImmutableArray<SymbolTreeItemData> GetItems(DocumentId documentId, SyntaxNode node, bool returnNamespaces, CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<SymbolTreeItemData>.GetInstance(out var items);
        using var _2 = PooledStringBuilder.GetInstance(out var nameBuilder);

        switch (node)
        {
            case TCompilationUnitSyntax compilationUnit:
                AddTopLevelMembers(documentId, compilationUnit, items, nameBuilder, returnNamespaces, cancellationToken);
                break;

            case TEnumDeclarationSyntax enumDeclaration:
                AddEnumDeclarationMembers(documentId, enumDeclaration, items, cancellationToken);
                break;

            case TNamespaceDeclarationSyntax namespaceDeclaration:
                AddNamespaceDeclarationMembers(namespaceDeclaration);
                break;

            case TTypeDeclarationSyntax typeDeclaration:
                AddTypeDeclarationMembers(typeDeclaration);
                break;

            case TMemberDeclarationSyntax memberDeclaration:
                AddMemberDeclarationMembers(memberDeclaration);
                break;

            case TMemberStatement statement:
                AddMemberStatementMembers(statement);
                break;
        }

        return items.ToImmutableAndClear();

        void AddNamespaceDeclarationMembers(TNamespaceDeclarationSyntax namespaceDeclaration)
        {
            foreach (var member in GetMembers(namespaceDeclaration))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (returnNamespaces && member is TNamespaceDeclarationSyntax namespaceMember)
                {
                    AddNamespace(documentId, namespaceMember, items, nameBuilder);
                }
                else
                {
                    TryAddType(documentId, member, items, nameBuilder);
                }
            }
        }

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

        void AddMemberDeclarationMembers(TMemberDeclarationSyntax memberDeclaration)
        {
            foreach (var statement in GetMemberDeclarationMembers(memberDeclaration))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddMemberStatement(documentId, statement, items, nameBuilder);
            }
        }

        void AddMemberStatementMembers(TMemberStatement memberDeclaration)
        {
            foreach (var statement in GetMemberStatementMembers(memberDeclaration))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddMemberStatement(documentId, statement, items, nameBuilder);
            }
        }
    }

    private void AddTopLevelMembers(
        DocumentId documentId,
        TCompilationUnitSyntax root,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder,
        bool returnNamespaces,
        CancellationToken cancellationToken)
    {
        foreach (var member in GetMembers(root))
            RecurseIntoMemberDeclaration(member);

        return;

        void RecurseIntoMemberDeclaration(TMemberDeclarationSyntax member)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (returnNamespaces && member is TNamespaceDeclarationSyntax namespaceMember)
            {
                AddNamespace(documentId, namespaceMember, items, nameBuilder);
                return;
            }
            else if (member is TNamespaceDeclarationSyntax baseNamespace)
            {
                foreach (var childMember in GetMembers(baseNamespace))
                    RecurseIntoMemberDeclaration(childMember);

                return;
            }

            TryAddType(documentId, member, items, nameBuilder);
        }
    }
}
