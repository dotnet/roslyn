using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal interface IRefactoringHelpersService : ILanguageService
    {


        /// <summary>
        /// <para>
        /// Returns a Node for refactoring given specified selection that passes <paramref name="predicate"/> 
        /// or null if no such instance exists.
        /// </para>
        /// <para>
        /// A node instance is return if:
        /// - Selection is zero-width and inside/touching a Token with direct parent passing <paramref name="predicate"/>.
        /// - Selection is zero-width and touching a Token whose ancestor Node passing <paramref name="predicate"/> ends/starts precisely on current selection.
        /// - Token whose direct parent passing <paramref name="predicate"/> is selected.
        /// - Whole node passing <paramref name="predicate"/> is selected.
        /// </para>
        /// <para>
        /// The <paramref name="extractNode"/> function enables testing with <paramref name="predicate"/> and potentially returning Nodes 
        /// that are under those that might be selected / considered (as described above). It is a <see cref="Func{SyntaxNode, SyntaxNode}"/> that
        /// should always return either given Node or a Node somewhere below it that should be tested with <paramref name="predicate"/> and
        /// potentially returned instead of current Node. 
        /// E.g. <see cref="ExtractNodeFromDeclarationAndAssignment{TNode}(SyntaxNode)"/>
        /// allows returning right side Expresion node even if whole AssignmentNode is selected.
        /// </para>
        /// <para>
        /// Note: this function strips all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The stripped version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully.
        /// </para>
        /// </summary>
        Task<SyntaxNode> TryGetSelectedNodeAsync(Document document, TextSpan selection, Func<SyntaxNode, SyntaxNode> extractNode, Predicate<SyntaxNode> predicate, CancellationToken cancellationToken);

        /// <summary>
        /// <para>
        /// Returns an instance of <typeparamref name="TSyntaxNode"/> for refactoring given specified selection in document or null
        /// if no such instance exists.
        /// </para>
        /// <para>
        /// A <typeparamref name="TSyntaxNode"/> instance is returned if:
        /// - Selection is zero-width and inside/touching a Token with direct parent of type <typeparamref name="TSyntaxNode"/>.
        /// - Selection is zero-width and touching a Token whose ancestor ends/starts precisely on current selection .
        /// - Token whose direct parent of type <typeparamref name="TSyntaxNode"/> is selected.
        /// - Whole node of a type <typeparamref name="TSyntaxNode"/> is selected.
        /// </para>
        /// <para>
        /// Note: this function strips all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The stripped version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully.
        /// </para>
        /// </summary>
        Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(Document document, TextSpan selection, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode;

        /// <summary>
        /// <para>
        /// Returns an instance of <typeparamref name="TSyntaxNode"/> for refactoring given specified selection in document or null
        /// if no such instance exists.
        /// </para>
        /// <para>
        /// A <typeparamref name="TSyntaxNode"/> instance is returned if:
        /// - Selection is zero-width and inside/touching a Token with direct parent of type <typeparamref name="TSyntaxNode"/>.
        /// - Selection is zero-width and touching a Token whose ancestor ends/starts precisely on current selection .
        /// - Token whose direct parent of type <typeparamref name="TSyntaxNode"/> is selected.
        /// - Whole node of a type <typeparamref name="TSyntaxNode"/> is selected.
        /// </para>
        /// <para>
        /// The <paramref name="extractNode"/> function enables testing with <paramref name="predicate"/> and potentially returning Nodes 
        /// that are under those that might be selected / considered (as described above). It is a <see cref="Func{SyntaxNode, SyntaxNode}"/> that
        /// should always return either given Node or a Node somewhere below it that should be tested with <paramref name="predicate"/> and
        /// potentially returned instead of current Node. 
        /// E.g. <see cref="ExtractNodeFromDeclarationAndAssignment{TNode}(SyntaxNode)"/>
        /// allows returning right side Expresion node even if whole AssignmentNode is selected.
        /// </para>
        /// <para>
        /// Note: this function strips all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The stripped version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully.
        /// </para>
        /// </summary>
        Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(Document document, TextSpan selection, Func<SyntaxNode, SyntaxNode> extractNode, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode;

        /// <summary>
        /// Extractor function for <see cref="TryGetSelectedNodeAsync"/> methods that retrieves expressions from 
        /// declarations and assignments. Otherwise returns unchanged <paramref name="node"/>.
        /// </summary>
        SyntaxNode ExtractNodeFromDeclarationAndAssignment<TNode>(SyntaxNode node) where TNode : SyntaxNode;
    }
}
