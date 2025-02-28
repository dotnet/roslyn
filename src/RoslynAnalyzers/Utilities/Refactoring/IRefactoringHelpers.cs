// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities
{
    internal interface IRefactoringHelpers
    {
        /// <summary>
        /// <para>
        /// Returns an array of <typeparamref name="TSyntaxNode"/> instances for refactoring given specified selection in document.
        /// </para>
        /// <para>
        /// A <typeparamref name="TSyntaxNode"/> instance is returned if:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Selection is zero-width and inside/touching a Token with direct parent of type <typeparamref name="TSyntaxNode"/>.</description></item>
        /// <item><description>Selection is zero-width and touching a Token whose ancestor of type <typeparamref name="TSyntaxNode"/> ends/starts precisely on current selection.</description></item>
        /// <item><description>Selection is zero-width and in whitespace that corresponds to a Token whose direct ancestor is of type of type <typeparamref name="TSyntaxNode"/>.</description></item>
        /// <item><description>Selection is zero-width and in a header (defined by ISyntaxFacts helpers) of an node of type of type <typeparamref name="TSyntaxNode"/>.</description></item>
        /// <item><description>Token whose direct parent of type <typeparamref name="TSyntaxNode"/> is selected.</description></item>
        /// <item><description>Selection is zero-width and wanted node is an expression / argument with selection within such syntax node (arbitrarily deep) on its first line.</description></item>
        /// <item><description>Whole node of a type <typeparamref name="TSyntaxNode"/> is selected.</description></item>
        /// </list>
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
        Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(Document document, TextSpan selection, CancellationToken cancellationToken)
            where TSyntaxNode : SyntaxNode;
    }
}
