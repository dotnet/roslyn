// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class AbstractRefactoringHelpersService : IRefactoringHelpersService
    {
        public async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(
            Document document, TextSpan selection, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            return (TSyntaxNode)await TryGetSelectedNodeAsync(document, selection, n => n is TSyntaxNode, cancellationToken).ConfigureAwait(false);
        }

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
        /// Attempts extracting (and testing with <paramref name="predicate"/> a Node for each Node it consideres (see above).
        /// By default extracts initializer expressions from declarations and assignments.
        /// </para>
        /// <para>
        /// Note: this function trims all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The trimmed version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully.
        /// </para>
        /// </summary>
        protected Task<SyntaxNode> TryGetSelectedNodeAsync(Document document, TextSpan selection, Predicate<SyntaxNode> predicate, CancellationToken cancellationToken)
            => TryGetSelectedNodeAsync(document, selection, predicate, DefaultNodeExtractor, cancellationToken);

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
        /// The <paramref name="extractNode"/> enables testing with <paramref name="predicate"/> and potentially returning Nodes 
        /// that are under those that might be selected / considered (as described above). It is a <see cref="Func{SyntaxNode, SyntaxNode}"/> that
        /// should always return either given Node or a Node somewhere below it that should be tested with <paramref name="predicate"/> and
        /// potentially returned instead of current Node. 
        /// </para>
        /// <para>
        /// Note: this function trims all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The trimmed version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully.
        /// </para>
        /// </summary>
        protected async Task<SyntaxNode> TryGetSelectedNodeAsync(Document document, TextSpan selection, Predicate<SyntaxNode> predicate, Func<SyntaxNode, ISyntaxFactsService, SyntaxNode> extractNode, CancellationToken cancellationToken)
        {
            // Given selection is trimmed first to enable overselection that spans multiple lines. Since trailing whitespace ends
            // at newline boundary overselection to e.g. a line after LocalFunctionStatement would cause FindNode to find enclosing
            // block's Node. That is because in addition to LocalFunctionStatement the selection would also contain trailing trivia 
            // (whitespace) of following statement.

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectionTrimmed = await CodeRefactoringHelpers.GetTrimmedTextSpan(document, selection, cancellationToken).ConfigureAwait(false);

            // Everytime a Node is considered by following algorithm (and tested with predicate) and the predicate fails
            // extractNode is called on the node and the result is tested with predicate again. If any of those succeed
            // a respective Node gets returned.
            //
            // That enables us to e.g. return node `b` when Node `var a = b;` is being considered without a complex (and potentially 
            // lang. & situation dependant) into Children descending code here.  We can't just try extracted Node because we might 
            // want the whole node `var a = b;`
            //
            // See local function TryGetAcceptedNodeOrExtracted DefaultNodeExtractor for more info.


            // Handle selections:
            // - The smallest node whose FullSpan inlcudes the whole (trimmed) selection
            // - Travels upwards through same-sized (FullSpan) nodes, extracting and testing predicate
            // - Handles situations where:
            //  - Token with wanted Node as direct parent is selected (e.g. IdentifierToken for LocalFunctionStatement: `C [|Fun|]() {}`) 
            //  - Most/the whole wanted Node is seleted (e.g. `C [|Fun() {}|]`
            var node = root.FindNode(selectionTrimmed, getInnermostNodeForTie: true);
            SyntaxNode prevNode;
            do
            {
                var wantedNode = TryGetAcceptedNodeOrExtracted(node, predicate, extractNode, syntaxFacts);
                if (wantedNode != null)
                {
                    return wantedNode;
                }

                prevNode = node;
                node = node.Parent;
            }
            while (node != null && prevNode.FullWidth() == node.FullWidth());

            // Handle what current selection is touching:
            //
            // Consider touching only for empty selections. Otherwise `[|C|] methodName(){}` would be considered as 
            // touching the Method's Node (through the left edge, see below) which is something the user probably 
            // didn't want since they specifically selected only the return type.
            //
            // What the selection is touching is used in two ways. 
            // - Firstly, it is used to handle situation where it touches a Token whose direct ancestor is wanted Node.
            // While having the (even empty) selection inside such token or to left of such Token is already handle 
            // by code above touching it from right `C methodName[||](){}` isn't (the FindNode for that returns Args node).
            // - Secondly it is used for left/right edge climbing. E.g. `[||]C methodName(){}` the touching token's direct 
            // ancestor is TypeNode for the return type but it is still resonable to expect that the user might want to 
            // be given refactorings for the whole method (as he has caret on the edge of it). Therefore we travel the 
            // Node tree upwards and as long as we're on the left edge of a Node's span we consider such node & potentially 
            // continue travelling upwards. The situation for right edge (`C methodName(){}[||]`) is analogical.
            // E.g. for right edge `C methodName(){}[||]`: CloseBraceToken -> BlockSyntax -> LocalFunctionStatement -> null (higher 
            // node doesn't end on position anymore)
            if (!selection.IsEmpty)
            {
                return null;
            }

            // get Token for current selection (empty) location
            var tokenOnSelection = root.FindToken(selectionTrimmed.Start);

            // Gets a token that is directly to the right of current (empty) selection or that encompases current selection (`[||]tokenITORightOrIn` or `tok[||]enITORightOrIn`)
            var tokenToRightOrIn = tokenOnSelection.Span.Contains(selectionTrimmed.Start)
                ? tokenOnSelection
                : default;

            if (tokenToRightOrIn != default)
            {
                var rightNode = tokenOnSelection.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Parent of touched Token (selection can be within) 
                    // - Ancestor Node of such Token as long as their their span starts on selection (it's still on the edge)
                    var wantedNode = TryGetAcceptedNodeOrExtracted(rightNode, predicate, extractNode, syntaxFacts);
                    if (wantedNode != null)
                    {
                        return wantedNode;
                    }

                    rightNode = rightNode?.Parent;
                }
                while (rightNode != null && rightNode.Span.Start == selection.Start);
            }

            // if the selection is inside tokenToRightOrIn -> no Token can be to Left (tokenToRightOrIn is also left from selection, e.g: `tok[||]enITORightOrIn`)
            if (tokenToRightOrIn != default && tokenToRightOrIn.Span.Start != selectionTrimmed.Start)
            {
                return null;
            }

            // Token to left: a token whose span ends on current (empty) selection
            var tokenPreSelection = (tokenOnSelection.Span.End == selectionTrimmed.Start)
                ? tokenOnSelection
                : tokenOnSelection.GetPreviousToken();

            var tokenToLeft = (tokenPreSelection.Span.End == selectionTrimmed.Start)
                ? tokenPreSelection
                : default;

            if (tokenToLeft != default)
            {
                var leftNode = tokenToLeft.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Ancestor Node of such Token as long as their their span ends on selection (it's still on the edge)
                    var wantedNode = TryGetAcceptedNodeOrExtracted(leftNode, predicate, extractNode, syntaxFacts);
                    if (wantedNode != null)
                    {
                        return wantedNode;
                    }

                    leftNode = leftNode?.Parent;
                }
                while (leftNode != null && leftNode.Span.End == selection.Start);
            }

            // nothing found -> return null
            return null;

            static SyntaxNode TryGetAcceptedNodeOrExtracted(SyntaxNode node, Predicate<SyntaxNode> predicate, Func<SyntaxNode, ISyntaxFactsService, SyntaxNode> extractNode, ISyntaxFactsService syntaxFacts)
            {
                if (node == null)
                {
                    return null;
                }

                if (predicate(node))
                {
                    return node;
                }

                var extractedNode = extractNode(node, syntaxFacts);
                return (extractedNode != null && predicate(extractedNode))
                    ? extractedNode
                    : null;
            }
        }

        /// <summary>
        /// <para>
        /// Extractor function for <see cref="TryGetSelectedNodeAsync(Document, TextSpan, Predicate{SyntaxNode}, Func{SyntaxNode, ISyntaxFactsService, SyntaxNode}, CancellationToken)"/> methods 
        /// that retrieves expressions from  declarations and assignments. Otherwise returns unchanged <paramref name="node"/>.
        /// </para>
        /// <para>
        /// The rationale is that when user selects e.g. entire local delaration statement [|var a = b;|] it is reasonable
        /// to provide refactoring for `b` node. Similarly for other types of refactorings.
        /// </para>
        /// </summary>
        protected virtual SyntaxNode DefaultNodeExtractor(SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            // REMARKS: 
            // The set of currently attempted extractions is in no way exhaustive and covers only cases
            // that were found to be relevant for refactorings that were moved to `TryGetSelectedNodeAsync`.
            // Feel free to extend it / refine current heuristics. 

            // var a = b;
            // -> b
            if (syntaxFacts.IsLocalDeclarationStatement(node))
            {
                var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(node);
                if (variables.Count == 1)
                {
                    var declaredVariable = variables.First();
                    var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(declaredVariable);

                    if (initializer != default)
                    {
                        var value = syntaxFacts.GetValueOfEqualsValueClause(initializer);
                        if (value != default)
                        {
                            return value;
                        }
                    }
                }
            }

            // a = b;
            // -> b
            if (syntaxFacts.IsSimpleAssignmentStatement(node))
            {
                syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(node, out _, out _, out var rightSide);
                return rightSide;
            }

            return node;
        }
    }
}
