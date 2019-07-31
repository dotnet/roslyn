// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class AbstractRefactoringHelpersService<TExpressionSyntax, TArgumentSyntax> : IRefactoringHelpersService
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
    {
        public async Task<ImmutableArray<TSyntaxNode>> GetRelevantNodesAsync<TSyntaxNode>(
            Document document, TextSpan selectionRaw,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            // Given selection is trimmed first to enable over-selection that spans multiple lines. Since trailing whitespace ends
            // at newline boundary over-selection to e.g. a line after LocalFunctionStatement would cause FindNode to find enclosing
            // block's Node. That is because in addition to LocalFunctionStatement the selection would also contain trailing trivia 
            // (whitespace) of following statement.

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectionTrimmed = await CodeRefactoringHelpers.GetTrimmedTextSpan(document, selectionRaw, cancellationToken).ConfigureAwait(false);

            // If user selected only whitespace we don't want to return anything. We could do following:
            //  1) Consider token that owns (as its trivia) the whitespace.
            //  2) Consider start/beginning of whitespace as location (empty selection)
            // Option 1) can't be used all the time and 2) can be confusing for users. Therefore bailing out is the
            // most consistent option.
            if (selectionTrimmed.IsEmpty && !selectionRaw.IsEmpty)
            {
                return ImmutableArray<TSyntaxNode>.Empty;
            }

            var relevantNodesBuilder = ArrayBuilder<TSyntaxNode>.GetInstance();
            try
            {
                // Every time a Node is considered an extractNodes method is called to add all nodes around the original one
                // that should also be considered.
                //
                // That enables us to e.g. return node `b` when Node `var a = b;` is being considered without a complex (and potentially 
                // lang. & situation dependent) into Children descending code here.  We can't just try extracted Node because we might 
                // want the whole node `var a = b;`

                // Handle selections:
                // - Most/the whole wanted Node is selected (e.g. `C [|Fun() {}|]`
                //   - The smallest node whose FullSpan includes the whole (trimmed) selection
                //   - Using FullSpan is important because it handles over-selection with comments
                //   - Travels upwards through same-sized (FullSpan) nodes, extracting
                // - Token with wanted Node as direct parent is selected (e.g. IdentifierToken for LocalFunctionStatement: `C [|Fun|]() {}`) 
                // Note: Whether we have selection or location has to be checked against original selection because selecting just
                // whitespace could collapse selectionTrimmed into and empty Location. But we don't want `[|   |]token`
                // registering as `   [||]token`.
                if (!selectionTrimmed.IsEmpty)
                {
                    AddRelevantNodesForSelection(syntaxFacts, root, selectionTrimmed, relevantNodesBuilder, cancellationToken);
                }
                else
                {
                    // No more selection -> Handle what current selection is touching:
                    //
                    // Consider touching only for empty selections. Otherwise `[|C|] methodName(){}` would be considered as 
                    // touching the Method's Node (through the left edge, see below) which is something the user probably 
                    // didn't want since they specifically selected only the return type.
                    //
                    // What the selection is touching is used in two ways. 
                    // - Firstly, it is used to handle situation where it touches a Token whose direct ancestor is wanted Node.
                    // While having the (even empty) selection inside such token or to left of such Token is already handle 
                    // by code above touching it from right `C methodName[||](){}` isn't (the FindNode for that returns Args node).
                    // - Secondly, it is used for left/right edge climbing. E.g. `[||]C methodName(){}` the touching token's direct 
                    // ancestor is TypeNode for the return type but it is still reasonable to expect that the user might want to 
                    // be given refactorings for the whole method (as he has caret on the edge of it). Therefore we travel the 
                    // Node tree upwards and as long as we're on the left edge of a Node's span we consider such node & potentially 
                    // continue traveling upwards. The situation for right edge (`C methodName(){}[||]`) is analogical.
                    // E.g. for right edge `C methodName(){}[||]`: CloseBraceToken -> BlockSyntax -> LocalFunctionStatement -> null (higher 
                    // node doesn't end on position anymore)
                    // Note: left-edge climbing needs to handle AttributeLists explicitly, see below for more information. 
                    // - Thirdly, if location isn't touching anything, we move the location to the token in whose trivia location is in.
                    // more about that below.
                    // - Fourthly, if we're in an expression / argument we consider touching a parent expression whenever we're within it
                    // as long as it is on the first line of such expression (arbitrary heuristic).

                    // First we need to get tokens we might potentially be touching, tokenToRightOrIn and tokenToLeft.
                    var (tokenToRightOrIn, tokenToLeft, location) = await GetTokensToRightOrInToLeftAndUpdatedLocation(
                        document, root, selectionTrimmed, cancellationToken).ConfigureAwait(false);


                    // In addition to per-node extr also check if current location (if selection is empty) is in a header of higher level
                    // desired node once. We do that only for locations because otherwise `[|int|] A { get; set; }) would trigger all refactorings for 
                    // Property Decl. 
                    // We cannot check this any sooner because the above code could've changed current location.
                    AddNonHiddenCorrectTypeNodes(ExtractNodesInHeader(root, location, syntaxFacts), relevantNodesBuilder, cancellationToken);

                    // Add Nodes for touching tokens as described above.
                    AddNodesForTokenToRightOrIn(syntaxFacts, root, relevantNodesBuilder, location, tokenToRightOrIn, cancellationToken);
                    AddNodesForTokenToLeft(syntaxFacts, relevantNodesBuilder, location, tokenToLeft, cancellationToken);

                    // If the wanted node is an expression syntax -> traverse upwards even if location is deep within a SyntaxNode.
                    // We want to treat more types like expressions, e.g.: ArgumentSyntax should still trigger even if deep-in.
                    if (IsWantedTypeExpressionLike<TSyntaxNode>())
                    {
                        await AddNodesDeepIn(document, location, relevantNodesBuilder, cancellationToken).ConfigureAwait(false);
                    }
                }

                return relevantNodesBuilder.ToImmutable();
            }
            finally
            {
                relevantNodesBuilder.Free();
            }
        }

        private static bool IsWantedTypeExpressionLike<TSyntaxNode>() where TSyntaxNode : SyntaxNode
        {
            var wantedType = typeof(TSyntaxNode);
            var expressionType = typeof(TExpressionSyntax);
            var argumentType = typeof(TArgumentSyntax);

            return IsAEqualOrSubclassOfB(wantedType, expressionType) || IsAEqualOrSubclassOfB(wantedType, argumentType);

            static bool IsAEqualOrSubclassOfB(Type a, Type b)
            {
                return a.IsSubclassOf(b) || a == b;
            }
        }

        private async Task<(SyntaxToken tokenToRightOrIn, SyntaxToken tokenToLeft, int location)> GetTokensToRightOrInToLeftAndUpdatedLocation(
            Document document,
            SyntaxNode root,
            TextSpan selectionTrimmed,
            CancellationToken cancellationToken)
        {
            // get Token for current location
            var location = selectionTrimmed.Start;
            var tokenOnLocation = root.FindToken(location);

            // Gets a token that is directly to the right of current location or that encompasses current location (`[||]tokenToRightOrIn` or `tok[||]enToRightOrIn`)
            var tokenToRightOrIn = tokenOnLocation.Span.Contains(location)
                ? tokenOnLocation
                : default;

            // A token can be to the left only when there's either no tokenDirectlyToRightOrIn or there's one  directly starting at current location. 
            // Otherwise (otherwise tokenToRightOrIn is also left from location, e.g: `tok[||]enToRightOrIn`)
            var tokenToLeft = default(SyntaxToken);
            if (tokenToRightOrIn == default || tokenToRightOrIn.FullSpan.Start == location)
            {
                var tokenPreLocation = (tokenOnLocation.Span.End == location)
                    ? tokenOnLocation
                    : tokenOnLocation.GetPreviousToken(includeZeroWidth: true);

                tokenToLeft = (tokenPreLocation.Span.End == location)
                    ? tokenPreLocation
                    : default;
            }

            // If both tokens directly to left & right are empty -> we're somewhere in the middle of whitespace.
            // Since there wouldn't be (m)any other refactorings we can try to offer at least the ones for (semantically) 
            // closest token/Node. Thus, we move the location to the token in whose `.FullSpan` the original location was.
            if (tokenToLeft == default && tokenToRightOrIn == default)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                // assume non-trivia token can't span multiple lines
                var tokenLine = sourceText.Lines.GetLineFromPosition(tokenOnLocation.Span.Start);
                var locationLine = sourceText.Lines.GetLineFromPosition(location);

                // Change location to nearest token only if the token is off by one line or less
                if (Math.Abs(tokenLine.LineNumber - locationLine.LineNumber) <= 1)
                {
                    // Note: being a line below a tokenOnLocation is impossible in current model as whitespace 
                    // trailing trivia ends on new line. Which is fine because if you're a line _after_ some node
                    // you usually don't want refactorings for what's above you.

                    // tokenOnLocation: token in whose trivia location is at
                    if (tokenOnLocation.Span.Start >= location)
                    {
                        tokenToRightOrIn = tokenOnLocation;
                        location = tokenToRightOrIn.Span.Start;
                    }
                    else
                    {
                        tokenToLeft = tokenOnLocation;
                        location = tokenToLeft.Span.End;
                    }
                }
            }

            return (tokenToRightOrIn, tokenToLeft, location);
        }

        private void AddNodesForTokenToLeft<TSyntaxNode>(ISyntaxFactsService syntaxFacts, ArrayBuilder<TSyntaxNode> relevantNodesBuilder, int location, SyntaxToken tokenToLeft, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            // there could be multiple (n) tokens to the left if first n-1 are Empty -> iterate over all of them
            while (tokenToLeft != default)
            {
                var leftNode = tokenToLeft.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Ancestor Node of such Token as long as their span ends on location (it's still on the edge)
                    AddNonHiddenCorrectTypeNodes(ExtractNodesSimple(leftNode, syntaxFacts), relevantNodesBuilder, cancellationToken);

                    leftNode = leftNode.Parent;
                    if (leftNode == null || !(leftNode.GetLastToken().Span.End == location || leftNode.Span.End == location))
                    {
                        break;
                    }
                }
                while (true);

                // as long as current tokenToLeft is empty -> its previous token is also tokenToLeft
                tokenToLeft = tokenToLeft.Span.IsEmpty
                    ? tokenToLeft.GetPreviousToken(includeZeroWidth: true)
                    : default;
            }
        }

        private void AddNodesForTokenToRightOrIn<TSyntaxNode>(ISyntaxFactsService syntaxFacts, SyntaxNode root, ArrayBuilder<TSyntaxNode> relevantNodesBuilder, int location, SyntaxToken tokenToRightOrIn, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            if (tokenToRightOrIn != default)
            {
                var rightNode = tokenToRightOrIn.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Parent of touched Token (location can be within) 
                    // - Ancestor Node of such Token as long as their span starts on location (it's still on the edge)
                    AddNonHiddenCorrectTypeNodes(ExtractNodesSimple(rightNode, syntaxFacts), relevantNodesBuilder, cancellationToken);

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
        }

        private void AddRelevantNodesForSelection<TSyntaxNode>(ISyntaxFactsService syntaxFacts, SyntaxNode root, TextSpan selectionTrimmed, ArrayBuilder<TSyntaxNode> relevantNodesBuilder, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            var selectionNode = root.FindNode(selectionTrimmed, getInnermostNodeForTie: true);
            var prevNode = selectionNode;
            do
            {
                var nonHiddenExtractedSelectedNodes = ExtractNodesSimple(selectionNode, syntaxFacts).OfType<TSyntaxNode>().Where(n => !n.OverlapsHiddenPosition(cancellationToken));
                foreach (var selectedNode in nonHiddenExtractedSelectedNodes)
                {
                    // For selections we need to handle an edge case where only AttributeLists are within selection (e.g. `Func([|[in][out]|] arg1);`).
                    // In that case the smallest encompassing node is still the whole argument node but it's hard to justify showing refactorings for it
                    // if user selected only its attributes.

                    // Selection contains only AttributeLists -> don't consider current Node
                    var spanWithoutAttributes = GetSpanWithoutAttributes(selectedNode, root, syntaxFacts);
                    if (!selectionTrimmed.IntersectsWith(spanWithoutAttributes))
                    {
                        break;
                    }

                    relevantNodesBuilder.Add(selectedNode);
                }

                prevNode = selectionNode;
                selectionNode = selectionNode.Parent;
            }
            while (selectionNode != null && prevNode.FullWidth() == selectionNode.FullWidth());
        }

        /// <summary>
        /// Extractor function that retrieves all nodes that should be considered for extraction of given current node. 
        /// <para>
        /// The rationale is that when user selects e.g. entire local declaration statement [|var a = b;|] it is reasonable
        /// to provide refactoring for `b` node. Similarly for other types of refactorings.
        /// </para>
        /// </summary>
        /// <remark>
        /// Should also return given node. 
        /// </remark>
        protected virtual IEnumerable<SyntaxNode> ExtractNodesSimple(SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            if (node == null)
            {
                yield break;
            }

            // First return the node itself so that it is considered
            yield return node;

            // REMARKS: 
            // The set of currently attempted extractions is in no way exhaustive and covers only cases
            // that were found to be relevant for refactorings that were moved to `TryGetSelectedNodeAsync`.
            // Feel free to extend it / refine current heuristics. 

            // `var a = b;`
            if (syntaxFacts.IsLocalDeclarationStatement(node))
            {
                // Check if there's only one variable being declared, otherwise following transformation
                // would go through which isn't reasonable since we can't say the first one specifically
                // is wanted.
                // `var a = 1, `c = 2, d = 3`;
                // -> `var a = 1`, c = 2, d = 3;
                var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(node);
                if (variables.Count == 1)
                {
                    var declaredVariable = variables.First();

                    // -> `a = b`
                    yield return declaredVariable;

                    // -> `b`
                    var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(declaredVariable);
                    if (initializer != default)
                    {
                        var value = syntaxFacts.GetValueOfEqualsValueClause(initializer);
                        if (value != default)
                        {
                            yield return value;
                        }
                    }
                }
            }

            // var `a = b`;
            // -> `var a = b`;
            if (syntaxFacts.IsLocalDeclarationStatement(node?.Parent))
            {
                // Check if there's only one variable being declared, otherwise following transformation
                // would go through which isn't reasonable. If there's specifically selected just one, 
                // we don't want to return LocalDeclarationStatement that contains multiple.
                // var a = 1, `c = 2`, d = 3;
                // -> `var a = 1, c = 2, d = 3`;
                if (syntaxFacts.GetVariablesOfLocalDeclarationStatement(node.Parent).Count == 1)
                {
                    yield return node.Parent;
                }
            }

            // `a = b;`
            // -> `b`
            if (syntaxFacts.IsSimpleAssignmentStatement(node))
            {
                syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(node, out _, out _, out var rightSide);
                yield return rightSide;
            }
        }

        /// <summary>
        /// Extractor function that checks and retrieves all nodes current location is in a header.
        /// </summary>
        protected virtual IEnumerable<SyntaxNode> ExtractNodesInHeader(SyntaxNode root, int location, ISyntaxFactsService syntaxFacts)
        {
            // Header: [Test] `public int a` { get; set; }
            if (syntaxFacts.IsOnPropertyDeclarationHeader(root, location, out var propertyDeclaration))
            {
                yield return propertyDeclaration;
            }

            // Header: public C([Test]`int a = 42`) {}
            if (syntaxFacts.IsOnParameterHeader(root, location, out var parameter))
            {
                yield return parameter;
            }

            // Header: `public I.C([Test]int a = 42)` {}
            if (syntaxFacts.IsOnMethodHeader(root, location, out var method))
            {
                yield return method;
            }

            // Header: `static C([Test]int a = 42)` {}
            if (syntaxFacts.IsOnLocalFunctionHeader(root, location, out var localFunction))
            {
                yield return localFunction;
            }

            // Header: `var a = `3,` b = `5,` c = `7 + 3``;
            if (syntaxFacts.IsOnLocalDeclarationHeader(root, location, out var localDeclaration))
            {
                yield return localDeclaration;
            }

            // Header: `if(...)`{ };
            if (syntaxFacts.IsOnIfStatementHeader(root, location, out var ifStatement))
            {
                yield return ifStatement;
            }

            // Header: `foreach (var a in b)` { }
            if (syntaxFacts.IsOnForeachHeader(root, location, out var foreachStatement))
            {
                yield return foreachStatement;
            }
        }

        protected virtual async Task AddNodesDeepIn<TSyntaxNode>(
            Document document, int position,
            ArrayBuilder<TSyntaxNode> relevantNodesBuilder,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            // If we're deep inside we don't have to deal with being on edges (that gets dealt by TryGetSelectedNodeAsync)
            // -> can simply FindToken -> proceed testing its ancestors
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindTokenOnRightOfPosition(position, true);

            // traverse upwards and add all parents if of correct type
            var ancestor = token.Parent;
            while (ancestor != null)
            {
                if (ancestor is TSyntaxNode correctTypeNode)
                {
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var argumentStartLine = sourceText.Lines.GetLineFromPosition(correctTypeNode.Span.Start).LineNumber;
                    var caretLine = sourceText.Lines.GetLineFromPosition(position).LineNumber;

                    if (argumentStartLine == caretLine && !correctTypeNode.OverlapsHiddenPosition(cancellationToken))
                    {
                        relevantNodesBuilder.Add(correctTypeNode);
                    }
                    else if (argumentStartLine < caretLine)
                    {
                        // higher level nodes will have Span starting at least on the same line -> can bail out
                        return;
                    }
                }
                ancestor = ancestor.Parent;
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
            // - In case only attribute is written we need to be careful to not to use next (unrelated) token as beginning current the node.
            var attributeList = syntaxFacts.GetAttributeLists(node);
            if (attributeList.Any())
            {
                var endOfAttributeLists = attributeList.Last().Span.End;
                var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

                var endOfNode = node.Span.End;
                var startOfTokenAfterAttributes = afterAttributesToken.Span.Start;
                var startOfNodeWithoutAttributes = endOfNode >= startOfTokenAfterAttributes
                    ? afterAttributesToken.Span.Start
                    : endOfNode;

                return TextSpan.FromBounds(startOfNodeWithoutAttributes, endOfNode);
            }

            return node.Span;
        }

        void AddNonHiddenCorrectTypeNodes<TSyntaxNode>(IEnumerable<SyntaxNode> nodes, ArrayBuilder<TSyntaxNode> resultBuilder, CancellationToken cancellationToken)
            where TSyntaxNode : SyntaxNode
        {
            var correctTypeNonHiddenNodes = nodes.OfType<TSyntaxNode>().Where(n => !n.OverlapsHiddenPosition(cancellationToken));
            foreach (var nodeToBeAdded in correctTypeNonHiddenNodes)
            {
                resultBuilder.Add(nodeToBeAdded);
            }
        }
    }
}
