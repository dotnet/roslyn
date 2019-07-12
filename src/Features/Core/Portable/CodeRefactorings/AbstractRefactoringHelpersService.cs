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
        /// Attempts extracting (and testing with <paramref name="predicate"/> a Node for each Node it considers (see above).
        /// By default extracts initializer expressions from declarations and assignments.
        /// </para>
        /// <para>
        /// Note: this function trims all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The trimmed version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully. Over-selection containing leading comments is also handled correctly. 
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
        /// that are under/above those that might be selected / considered (as described above). It is a <see cref="Func{SyntaxNode, SyntaxNode}"/> that
        /// should always return either given Node or a Node somewhere below it that should be tested with <paramref name="predicate"/> and
        /// potentially returned instead of current Node. 
        /// </para>
        /// <para>
        /// Note: this function trims all whitespace from both the beginning and the end of given <paramref name="selectionRaw"/>.
        /// The trimmed version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully. Over-selection containing leading comments is also handled correctly. 
        /// </para>
        /// </summary>
        protected async Task<SyntaxNode> TryGetSelectedNodeAsync(Document document, TextSpan selectionRaw, Predicate<SyntaxNode> predicate, Func<SyntaxNode, ISyntaxFactsService, bool, SyntaxNode> extractNode, CancellationToken cancellationToken)
        {
            // Given selection is trimmed first to enable over-selection that spans multiple lines. Since trailing whitespace ends
            // at newline boundary over-selection to e.g. a line after LocalFunctionStatement would cause FindNode to find enclosing
            // block's Node. That is because in addition to LocalFunctionStatement the selection would also contain trailing trivia 
            // (whitespace) of following statement.

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectionTrimmed = await CodeRefactoringHelpers.GetTrimmedTextSpan(document, selectionRaw, cancellationToken).ConfigureAwait(false);

            // Every time a Node is considered by following algorithm (and tested with predicate) and the predicate fails
            // extractNode is called on the node and the result is tested with predicate again. If any of those succeed
            // a respective Node gets returned.
            //
            // That enables us to e.g. return node `b` when Node `var a = b;` is being considered without a complex (and potentially 
            // lang. & situation dependent) into Children descending code here.  We can't just try extracted Node because we might 
            // want the whole node `var a = b;`
            //
            // While we want to do most extractions for both selections and just caret location (empty selection), extractions based 
            // on type's header node (caret is anywhere in a header of wanted type e.g. `in[||]t A { get; set; }) are limited to 
            // empty selections. Otherwise `[|int|] A { get; set; }) would trigger all refactorings for Property Decl.
            // Thus: selection -> extractParentsOfHeader: false; location -> extractParentsOfHeader
            //
            // See local function TryGetAcceptedNodeOrExtracted DefaultNodeExtractor for more info.

            // Handle selections:
            // - The smallest node whose FullSpan includes the whole (trimmed) selection
            //   - Using FullSpan is important because it handles over-selection with comments
            // - Travels upwards through same-sized (FullSpan) nodes, extracting and testing predicate
            // - Handles situations where:
            //  - Token with wanted Node as direct parent is selected (e.g. IdentifierToken for LocalFunctionStatement: `C [|Fun|]() {}`) 
            //  - Most/the whole wanted Node is selected (e.g. `C [|Fun() {}|]`
            var selectionNode = root.FindNode(selectionTrimmed, getInnermostNodeForTie: true);
            var prevNode = selectionNode;
            do
            {
                var wantedNode = TryGetAcceptedNodeOrExtracted(selectionNode, predicate, extractNode, syntaxFacts, extractParentsOfHeader: false);
                if (wantedNode != null)
                {
                    // For selections we need to handle an edge case where only AttributeLists are within selection (e.g. `Func([|[in][out]|] arg1);`).
                    // In that case the smallest encompassing node is still the whole argument node but it's hard to justify showing refactorings for it
                    // if user selected only its attributes.

                    // Selection contains only AttributeLists -> don't consider current Node
                    var spanWithoutAttributes = GetSpanWithoutAttributes(wantedNode, root, syntaxFacts);
                    if (!selectionTrimmed.IntersectsWith(spanWithoutAttributes))
                    {
                        break;
                    }

                    return wantedNode;
                }

                prevNode = selectionNode;
                selectionNode = selectionNode.Parent;
            }
            while (selectionNode != null && prevNode.FullWidth() == selectionNode.FullWidth());

            // Handle what current selection is touching:
            //
            // Consider touching only for empty selections. Otherwise `[|C|] methodName(){}` would be considered as 
            // touching the Method's Node (through the left edge, see below) which is something the user probably 
            // didn't want since they specifically selected only the return type.
            //
            // Whether we have selection of location has to be checked against original selection because selecting just
            // whitespace could collapse selectionTrimmed into and empty Location. But we don't want `[|   |]token`
            // registering as `   [||]token`.
            //
            // What the selection is touching is used in two ways. 
            // - Firstly, it is used to handle situation where it touches a Token whose direct ancestor is wanted Node.
            // While having the (even empty) selection inside such token or to left of such Token is already handle 
            // by code above touching it from right `C methodName[||](){}` isn't (the FindNode for that returns Args node).
            // - Secondly it is used for left/right edge climbing. E.g. `[||]C methodName(){}` the touching token's direct 
            // ancestor is TypeNode for the return type but it is still reasonable to expect that the user might want to 
            // be given refactorings for the whole method (as he has caret on the edge of it). Therefore we travel the 
            // Node tree upwards and as long as we're on the left edge of a Node's span we consider such node & potentially 
            // continue traveling upwards. The situation for right edge (`C methodName(){}[||]`) is analogical.
            // E.g. for right edge `C methodName(){}[||]`: CloseBraceToken -> BlockSyntax -> LocalFunctionStatement -> null (higher 
            // node doesn't end on position anymore)
            // Note: left-edge climbing needs to handle AttributeLists explicitly, see below for more information. 
            if (!selectionRaw.IsEmpty)
            {
                return null;
            }

            // get Token for current location
            var location = selectionTrimmed.Start;
            var tokenOnLocation = root.FindToken(location);

            // Gets a token that is directly to the right of current location or that encompasses current location (`[||]tokenToRightOrIn` or `tok[||]enToRightOrIn`)
            var tokenToRightOrIn = tokenOnLocation.Span.Contains(location)
                ? tokenOnLocation
                : default;

            if (tokenToRightOrIn != default)
            {
                var rightNode = tokenOnLocation.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Parent of touched Token (location can be within) 
                    // - Ancestor Node of such Token as long as their span starts on location (it's still on the edge)
                    var wantedNode = TryGetAcceptedNodeOrExtracted(rightNode, predicate, extractNode, syntaxFacts, extractParentsOfHeader: true);
                    if (wantedNode != null)
                    {
                        return wantedNode;
                    }

                    rightNode = rightNode.Parent;
                    if (rightNode == null)
                    {
                        break;
                    }

                    // The edge climbing for node to the right needs to handle Attributes e.g.:
                    // [Test1]
                    // //Comment1
                    // [||]object Property1 { get; set; }
                    // In essence:
                    // - On the left edge of the node (-> left edge of first AttributeLists)
                    // - On the left edge of the node sans AttributeLists (& as everywhere comments)
                    if (rightNode.Span.Start != location)
                    {
                        var rightNodeSpanWithoutAttributes = GetSpanWithoutAttributes(rightNode, root, syntaxFacts);
                        if (rightNodeSpanWithoutAttributes.Start != location)
                        {
                            break;
                        }
                    }
                }
                while (true);
            }

            // if the location is inside tokenToRightOrIn -> no Token can be to Left (tokenToRightOrIn is also left from location, e.g: `tok[||]enToRightOrIn`)
            if (tokenToRightOrIn != default && tokenToRightOrIn.Span.Start != location)
            {
                return null;
            }

            // Token to left: a token whose span ends on current location
            var tokenPreLocation = (tokenOnLocation.Span.End == location)
                ? tokenOnLocation
                : tokenOnLocation.GetPreviousToken();

            var tokenToLeft = (tokenPreLocation.Span.End == location)
                ? tokenPreLocation
                : default;

            if (tokenToLeft != default)
            {
                var leftNode = tokenToLeft.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Ancestor Node of such Token as long as their span ends on location (it's still on the edge)
                    var wantedNode = TryGetAcceptedNodeOrExtracted(leftNode, predicate, extractNode, syntaxFacts, extractParentsOfHeader: true);
                    if (wantedNode != null)
                    {
                        return wantedNode;
                    }

                    leftNode = leftNode.Parent;
                    if (leftNode == null || leftNode.Span.End != location)
                    {
                        break;
                    }
                }
                while (true);
            }

            // nothing found -> return null
            return null;

            static SyntaxNode TryGetAcceptedNodeOrExtracted(SyntaxNode node, Predicate<SyntaxNode> predicate, Func<SyntaxNode, ISyntaxFactsService, bool, SyntaxNode> extractNode, ISyntaxFactsService syntaxFacts, bool extractParentsOfHeader)
            {
                if (node == null)
                {
                    return null;
                }

                if (predicate(node))
                {
                    return node;
                }

                var extractedNode = extractNode(node, syntaxFacts, extractParentsOfHeader);
                return (extractedNode != null && predicate(extractedNode))
                    ? extractedNode
                    : null;
            }
        }

        private static TextSpan GetSpanWithoutAttributes(SyntaxNode node, SyntaxNode root, ISyntaxFactsService syntaxFacts)
        {
            // Span without AttributeLists
            // - No AttributeLists -> original .Span
            // - Some AttributeLists -> (first non-trivia/comment Token.Span.Begin, original.Span.End)
            //   - We need to be mindful about comments due to:
            //      // [Test1]
            //      //Comment1
            //      [||]object Property1 { get; set; }
            //     the comment node being part of the next token's (`object`) leading trivia and not the AttributeList's node.
            var attributeList = syntaxFacts.GetAttributeLists(node);
            if (attributeList.Any())
            {
                var endOfAttributeLists = attributeList.Last().Span.End;
                var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

                return TextSpan.FromBounds(afterAttributesToken.Span.Start, node.Span.End);
            }

            return node.Span;
        }

        /// <summary>
        /// <para>
        /// Extractor function for <see cref="TryGetSelectedNodeAsync(Document, TextSpan, Predicate{SyntaxNode}, Func{SyntaxNode, ISyntaxFactsService, bool, SyntaxNode}, CancellationToken)"/> method 
        /// that retrieves nodes that should also be considered as refactoring targets given <paramref name="node"/> is considered. 
        /// Can extract both nodes above and under given <paramref name="node"/>.
        /// </para>
        /// <para>
        /// E.g. extracts expressions from assignment statement (`a = b;` -> `b`) or lambda node for its body.
        /// </para>
        /// <para>
        /// The rationale is that when user selects e.g. entire local declaration statement [|var a = b;|] it is reasonable
        /// to provide refactoring for `b` node. Similarly for other types of refactorings.
        /// </para>
        /// <para>
        /// The rationale behind <paramref name="extractParentsOfHeader"/> is following. We want to extract a parent Node of header Node/Token
        /// if its just for location (empty selection) but not for selection. We assume that if user selects just one node he wants to work
        /// with that and only that node. On the other hand placing cursor anywhere in header should still count as selecting the node it's header of.
        /// </para>
        /// </summary>
        protected virtual SyntaxNode DefaultNodeExtractor(SyntaxNode node, ISyntaxFactsService syntaxFacts, bool extractParentsOfHeader)
        {
            // REMARKS: 
            // The set of currently attempted extractions is in no way exhaustive and covers only cases
            // that were found to be relevant for refactorings that were moved to `TryGetSelectedNodeAsync`.
            // Feel free to extend it / refine current heuristics. 

            // `var a = b`;
            // -> `b`
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

            // `a = b;`
            // -> `b`
            if (syntaxFacts.IsSimpleAssignmentStatement(node))
            {
                syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(node, out _, out _, out var rightSide);
                return rightSide;
            }

            if (extractParentsOfHeader)
            {
                // Header: [TestAttribute] `public int a` { get; set; }
                if (syntaxFacts.IsInPropertyDeclarationHeader(node))
                {
                    return syntaxFacts.GetContainingPropertyDeclaration(node);
                }
            }

            return node;
        }
    }
}
