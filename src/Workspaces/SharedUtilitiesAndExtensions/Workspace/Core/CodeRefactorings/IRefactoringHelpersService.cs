// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Contains helpers related to asking intuitive semantic questions about a users intent
/// based on the position of their caret or span of their selection.
/// </summary>
internal interface IRefactoringHelpersService : IHeaderFactsService, ILanguageService
{
    /// <summary>
    /// True if the user is on a blank line where a member could go inside a type declaration.
    /// This will be between members and not ever inside a member.
    /// </summary>
    bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? typeDeclaration);

    /// <summary>
    /// <para>
    /// Returns an array of <typeparamref name="TSyntaxNode"/> instances for refactoring given specified selection
    /// in document. <paramref name="allowEmptyNodes"/> determines if the returned nodes will can have empty spans
    /// or not.
    /// </para>
    /// <para>
    /// A <typeparamref name="TSyntaxNode"/> instance is returned if: - Selection is zero-width and inside/touching
    /// a Token with direct parent of type <typeparamref name="TSyntaxNode"/>. - Selection is zero-width and
    /// touching a Token whose ancestor of type <typeparamref name="TSyntaxNode"/> ends/starts precisely on current
    /// selection. - Selection is zero-width and in whitespace that corresponds to a Token whose direct ancestor is
    /// of type of type <typeparamref name="TSyntaxNode"/>. - Selection is zero-width and in a header (defined by
    /// ISyntaxFacts helpers) of an node of type of type <typeparamref name="TSyntaxNode"/>. - Token whose direct
    /// parent of type <typeparamref name="TSyntaxNode"/> is selected. - Selection is zero-width and wanted node is
    /// an expression / argument with selection within such syntax node (arbitrarily deep) on its first line. -
    /// Whole node of a type <typeparamref name="TSyntaxNode"/> is selected.
    /// </para>
    /// <para>
    /// Attempts extracting a Node of type <typeparamref name="TSyntaxNode"/> for each Node it considers (see
    /// above). E.g. extracts initializer expressions from declarations and assignments, Property declaration from
    /// any header node, etc.
    /// </para>
    /// <para>
    /// Note: this function trims all whitespace from both the beginning and the end of given <paramref
    /// name="selection"/>. The trimmed version is then used to determine relevant <see cref="SyntaxNode"/>. It also
    /// handles incomplete selections of tokens gracefully. Over-selection containing leading comments is also
    /// handled correctly. 
    /// </para>
    /// </summary>
    void AddRelevantNodes<TSyntaxNode>(
        ParsedDocument document, TextSpan selection, bool allowEmptyNodes, int maxCount, ref TemporaryArray<TSyntaxNode> result, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode;
}
