// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

#nullable enable
namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal interface IRefactoringHelpersService : ILanguageService
    {
        /// <summary>
        /// <para>
        /// Returns an array of <typeparamref name="TSyntaxNode"/> instances for refactoring given specified selection in document.
        /// </para>
        /// <para>
        /// A <typeparamref name="TSyntaxNode"/> instance is returned if:
        /// - Selection is zero-width and inside/touching a Token with direct parent of type <typeparamref name="TSyntaxNode"/>.
        /// - Selection is zero-width and touching a Token whose ancestor of type <typeparamref name="TSyntaxNode"/> ends/starts precisely on current selection.
        /// - Selection is zero-width and in whitespace that corresponds to a Token whose direct ancestor is of type of type <typeparamref name="TSyntaxNode"/>.
        /// - Selection is zero-width and in a header (defined by ISyntaxFacts helpers) of an node of type of type <typeparamref name="TSyntaxNode"/>.
        /// - Token whose direct parent of type <typeparamref name="TSyntaxNode"/> is selected.
        /// - Selection is zero-width and wanted node is an expression / argument with selection within such syntax node (arbitrarily deep) on its first line.
        /// - Whole node of a type <typeparamref name="TSyntaxNode"/> is selected.
        /// </para>
        /// <para>
        /// Attempts extracting a Node of type <typeparamref name="TSyntaxNode"/> for each Node it considers (see above).
        /// E.g. extracts initializer expressions from declarations and assignments, Property declaration from any header node, etc.
        /// </para>
        /// <para>
        /// Note: this function trims all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The trimmed version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully. Over-selection containing leading comments is also handled correctly. 
        /// </para>
        /// </summary>
        Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(Document document, TextSpan selection, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode;
    }
}
