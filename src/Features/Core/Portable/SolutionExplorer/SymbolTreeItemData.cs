// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SolutionExplorer;

internal readonly record struct SymbolTreeItemKey(
    DocumentId DocumentId,
    string Name,
    Glyph Glyph,
    bool HasItems);

internal readonly record struct SymbolTreeItemSyntax(
    SyntaxNode DeclarationNode,
    SyntaxToken NavigationToken);

internal readonly record struct SymbolTreeItemData(
    SymbolTreeItemKey ItemKey,
    SymbolTreeItemSyntax ItemSyntax)
{
    public SymbolTreeItemData(
        DocumentId documentId,
        string name,
        Glyph glyph,
        bool hasItems,
        SyntaxNode declarationNode,
        SyntaxToken navigationToken)
        : this(new(documentId, name, glyph, hasItems), new(declarationNode, navigationToken))
    {
    }

    public override string ToString()
        => $"""Name="{ItemKey.Name}" Glyph={ItemKey.Glyph} HasItems={ItemKey.HasItems}""";
}
