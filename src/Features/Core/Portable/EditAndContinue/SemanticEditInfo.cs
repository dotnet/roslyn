// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly record struct SyntaxMaps
{
    /// <summary>
    /// The tree the maps operate on (the new tree, since the maps are mapping from new nodes to old nodes/rude edits).
    /// </summary>
    public readonly SyntaxTree NewTree;

    public readonly Func<SyntaxNode, SyntaxNode?>? MatchingNodes;
    public readonly Func<SyntaxNode, RuntimeRudeEdit?>? RuntimeRudeEdits;

    public SyntaxMaps(
        SyntaxTree newTree,
        Func<SyntaxNode, SyntaxNode?>? matchingNodes = null,
        Func<SyntaxNode, RuntimeRudeEdit?>? runtimeRudeEdits = null)
    {
        // if we have runtime rude edit map we should also have matching node map:
        Debug.Assert(runtimeRudeEdits == null || matchingNodes != null);

        NewTree = newTree;
        MatchingNodes = matchingNodes;
        RuntimeRudeEdits = runtimeRudeEdits;
    }

    [MemberNotNullWhen(true, nameof(MatchingNodes))]
    public bool HasMap => MatchingNodes != null;
}

internal readonly struct SemanticEditInfo
{
    public SemanticEditInfo(
        SemanticEditKind kind,
        SymbolKey symbol,
        SyntaxMaps syntaxMaps,
        SymbolKey? partialType,
        SymbolKey? deletedSymbolContainer)
    {
        Debug.Assert(kind == SemanticEditKind.Delete || deletedSymbolContainer == null);

        Kind = kind;
        Symbol = symbol;
        SyntaxMaps = syntaxMaps;
        PartialType = partialType;
        DeletedSymbolContainer = deletedSymbolContainer;
    }

    public static SemanticEditInfo CreateInsert(SymbolKey symbol, SymbolKey? partialType)
        => new(SemanticEditKind.Insert, symbol, syntaxMaps: default, partialType, deletedSymbolContainer: null);

    public static SemanticEditInfo CreateUpdate(SymbolKey symbol, SyntaxMaps syntaxMaps, SymbolKey? partialType)
        => new(SemanticEditKind.Update, symbol, syntaxMaps, partialType, deletedSymbolContainer: null);

    public static SemanticEditInfo CreateReplace(SymbolKey symbol, SymbolKey? partialType)
        => new(SemanticEditKind.Replace, symbol, syntaxMaps: default, partialType, deletedSymbolContainer: null);

    public static SemanticEditInfo CreateDelete(SymbolKey symbol, SymbolKey deletedSymbolContainer, SymbolKey? partialType)
        => new(SemanticEditKind.Delete, symbol, syntaxMaps: default, partialType, deletedSymbolContainer);

    /// <summary>
    /// <see cref="SemanticEditKind.Insert"/> or <see cref="SemanticEditKind.Update"/> or <see cref="SemanticEditKind.Delete"/>.
    /// </summary>
    public SemanticEditKind Kind { get; }

    /// <summary>
    /// If <see cref="Kind"/> is <see cref="SemanticEditKind.Insert"/> represents the inserted symbol in the new compilation.
    /// If <see cref="Kind"/> is <see cref="SemanticEditKind.Update"/> represents the updated symbol in both compilations.
    /// If <see cref="Kind"/> is <see cref="SemanticEditKind.Delete"/> represents the deleted symbol in the old compilation.
    /// 
    /// We use <see cref="SymbolKey"/> to represent the symbol rather then <see cref="ISymbol"/>,
    /// since different semantic edits might have been calculated against different solution snapshot and thus symbols are not directly comparable.
    /// When the edits are processed we map the <see cref="SymbolKey"/> to the current compilation.
    /// </summary>
    public SymbolKey Symbol { get; }

    /// <summary>
    /// If <see cref="Kind"/> is <see cref="SemanticEditKind.Delete"/> represents the containing symbol in the new compilation.
    /// 
    /// We use <see cref="SymbolKey"/> to represent the symbol rather then <see cref="ISymbol"/>,
    /// since different semantic edits might have been calculated against different solution snapshot and thus symbols are not directly comparable.
    /// When the edits are processed we map the <see cref="SymbolKey"/> to the current compilation.
    /// </summary>
    public SymbolKey? DeletedSymbolContainer { get; }

    /// <summary>
    /// Syntax maps for nodes in the tree for this edit, which will be merged with other maps from other trees for this type.
    /// </summary>
    public SyntaxMaps SyntaxMaps { get; }

    /// <summary>
    /// Specified if the edit needs to be merged with other edits of the same <see cref="PartialType"/>.
    /// 
    /// If specified, the <see cref="SyntaxMaps"/> is either null or incomplete: it only provides mapping of the changed members of a single partial type declaration.
    /// </summary>
    public SymbolKey? PartialType { get; }
}
