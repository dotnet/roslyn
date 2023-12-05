// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct SemanticEditInfo
    {
        public SemanticEditInfo(
            SemanticEditKind kind,
            SymbolKey symbol,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            SyntaxTree? syntaxMapTree,
            SymbolKey? partialType,
            SymbolKey? deletedSymbolContainer)
        {
            Debug.Assert(kind == SemanticEditKind.Delete || deletedSymbolContainer == null);
            Debug.Assert(partialType == null || syntaxMap is null == syntaxMapTree is null);

            Kind = kind;
            Symbol = symbol;
            SyntaxMap = syntaxMap;
            SyntaxMapTree = syntaxMapTree;
            PartialType = partialType;
            DeletedSymbolContainer = deletedSymbolContainer;
        }

        public static SemanticEditInfo CreateInsert(SymbolKey symbol, SymbolKey? partialType)
            => new(SemanticEditKind.Insert, symbol, syntaxMap: null, syntaxMapTree: null, partialType, deletedSymbolContainer: null);

        public static SemanticEditInfo CreateUpdate(SymbolKey symbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap, SyntaxTree? syntaxMapTree, SymbolKey? partialType)
            => new(SemanticEditKind.Update, symbol, syntaxMap, syntaxMapTree, partialType, deletedSymbolContainer: null);

        public static SemanticEditInfo CreateReplace(SymbolKey symbol, SymbolKey? partialType)
            => new(SemanticEditKind.Replace, symbol, syntaxMap: null, syntaxMapTree: null, partialType, deletedSymbolContainer: null);

        public static SemanticEditInfo CreateDelete(SymbolKey symbol, SymbolKey deletedSymbolContainer, SymbolKey? partialType)
            => new(SemanticEditKind.Delete, symbol, syntaxMap: null, syntaxMapTree: null, partialType, deletedSymbolContainer);

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
        /// The syntax map for nodes in the tree for this edit, which will be merged with other maps from other trees for this type.
        /// </summary>
        public Func<SyntaxNode, SyntaxNode?>? SyntaxMap { get; }

        /// <summary>
        /// The tree <see cref="SyntaxMap"/> operates on (the new tree, since the map is mapping from new nodes to old nodes).
        /// Only available when <see cref="PartialType"/> is not null.
        /// </summary>
        public SyntaxTree? SyntaxMapTree { get; }

        /// <summary>
        /// Specified if the edit needs to be merged with other edits of the same <see cref="PartialType"/>.
        /// 
        /// If specified, the <see cref="SyntaxMap"/> is either null or incomplete: it only provides mapping of the changed members of a single partial type declaration.
        /// </summary>
        public SymbolKey? PartialType { get; }
    }
}
