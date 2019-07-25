// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class AbstractRefactoringHelpersService<TExpressionSyntax, TArgumentSyntax> : IRefactoringHelpersService
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
    {
        public async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(
            Document document,
            TextSpan selection,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            return await TryGetSelectedNodeAsync<TSyntaxNode>(
                document,
                selection, Functions<TSyntaxNode>.True,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(
            Document document, TextSpan selectionRaw,
            Func<TSyntaxNode, bool> predicate,
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
                return null;
            }

            // Every time a Node is considered by an extractNodes method is called to check & potentially return nodes around the original one
            // that should also be considered.
            //
            // That enables us to e.g. return node `b` when Node `var a = b;` is being considered without a complex (and potentially 
            // lang. & situation dependent) into Children descending code here.  We can't just try extracted Node because we might 
            // want the whole node `var a = b;`
            //
            // In addition to per-node extractions we also check if current location (if selection is empty) is in a header of higher level
            // desired node once. We do that only for locations because otherwise `[|int|] A { get; set; }) would trigger all refactorings for 
            // Property Decl.

            // Handle selections:
            // - The smallest node whose FullSpan includes the whole (trimmed) selection
            //   - Using FullSpan is important because it handles over-selection with comments
            // - Travels upwards through same-sized (FullSpan) nodes, extracting and testing predicate
            // - Handles situations where:
            //  - Token with wanted Node as direct parent is selected (e.g. IdentifierToken for LocalFunctionStatement: `C [|Fun|]() {}`) 
            //  - Most/the whole wanted Node is selected (e.g. `C [|Fun() {}|]`
            // Note: Whether we have selection or location has to be checked against original selection because selecting just
            // whitespace could collapse selectionTrimmed into and empty Location. But we don't want `[|   |]token`
            // registering as `   [||]token`.

            if (!selectionTrimmed.IsEmpty)
            {
                var selectionNode = root.FindNode(selectionTrimmed, getInnermostNodeForTie: true);
                var prevNode = selectionNode;
                do
                {
                    if (TryGetAcceptedNode(ExtractNodesSimple(selectionNode, syntaxFacts), predicate, out var acceptedNode, cancellationToken))
                    {
                        // For selections we need to handle an edge case where only AttributeLists are within selection (e.g. `Func([|[in][out]|] arg1);`).
                        // In that case the smallest encompassing node is still the whole argument node but it's hard to justify showing refactorings for it
                        // if user selected only its attributes.

                        // Selection contains only AttributeLists -> don't consider current Node
                        var spanWithoutAttributes = GetSpanWithoutAttributes(acceptedNode, root, syntaxFacts);
                        if (!selectionTrimmed.IntersectsWith(spanWithoutAttributes))
                        {
                            break;
                        }

                        return acceptedNode;
                    }

                    prevNode = selectionNode;
                    selectionNode = selectionNode.Parent;
                }
                while (selectionNode != null && prevNode.FullWidth() == selectionNode.FullWidth());

                return null;
            }

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

            // First check if we're in a header of some higher-level node what would pass predicate & if we are -> return it
            // We can't check any sooner because the code above (that figures out tokenToLeft, ...) can change current 
            // `location`.
            if (TryGetAcceptedNode(ExtractNodesIfInHeader(root, location, syntaxFacts), predicate, out var acceptedHeaderNode, cancellationToken))
            {
                return acceptedHeaderNode;
            }

            if (tokenToRightOrIn != default)
            {
                var rightNode = tokenOnLocation.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Parent of touched Token (location can be within) 
                    // - Ancestor Node of such Token as long as their span starts on location (it's still on the edge)
                    if (TryGetAcceptedNode(ExtractNodesSimple(rightNode, syntaxFacts), predicate, out var acceptedNode, cancellationToken))
                    {
                        return acceptedNode;
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

            // there could be multiple (n) tokens to the left if first n-1 are Empty -> iterate over all of them
            while (tokenToLeft != default)
            {
                var leftNode = tokenToLeft.Parent;
                do
                {
                    // Consider either a Node that is:
                    // - Ancestor Node of such Token as long as their span ends on location (it's still on the edge)
                    if (TryGetAcceptedNode(ExtractNodesSimple(leftNode, syntaxFacts), predicate, out var acceptedNode, cancellationToken))
                    {
                        return acceptedNode;
                    }

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

            // If the wanted node is an expression syntax -> traverse upwards even if location is deep within a SyntaxNode
            // ArgumentSyntax isn't an "expression" syntax but we want to treat it as such
            if (typeof(TSyntaxNode).IsSubclassOf(typeof(TExpressionSyntax)) || typeof(TSyntaxNode) == typeof(TArgumentSyntax))
            {
                return await TryGetDeepInNodeAsync(document, location, predicate, cancellationToken).ConfigureAwait(false);
            }

            // nothing found -> return null
            return null;

            static bool TryGetAcceptedNode(IEnumerable<SyntaxNode> extractedNodes, Func<TSyntaxNode, bool> predicate, out TSyntaxNode acceptedNode, CancellationToken cancellationToken)
            {
                acceptedNode = extractedNodes.OfType<TSyntaxNode>().FirstOrDefault(predicate);
                return acceptedNode != null && !acceptedNode.OverlapsHiddenPosition(cancellationToken);
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
        /// Extractor function that retrieves all nodes that should be considered for <see cref="TryGetSelectedNodeAsync{TSyntaxNode}(Document, TextSpan, Func{TSyntaxNode, bool}, CancellationToken)"/>
        /// given current node. 
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
        /// Extractor function that checks and retrieves all nodes current location is in a header of <see cref="TryGetSelectedNodeAsync{TSyntaxNode}(Document, TextSpan, Func{TSyntaxNode, bool}, CancellationToken)"/>.
        /// </summary>
        protected virtual IEnumerable<SyntaxNode> ExtractNodesIfInHeader(SyntaxNode root, int location, ISyntaxFactsService syntaxFacts)
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

        protected virtual async Task<TSyntaxNode> TryGetDeepInNodeAsync<TSyntaxNode>(
            Document document, int position,
            Func<TSyntaxNode, bool> predicate,
            CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {

            // If we're deep inside we don't have to deal with being on edges (that gets dealt by TryGetSelectedNodeAsync)
            // -> can simply FindToken -> proceed with its ancestors.
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindTokenOnRightOfPosition(position, true);


            var expression = token.Parent?.FirstAncestorOrSelf<TSyntaxNode>(predicate);

            if (expression == null)
            {
                return null;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var argumentStartLine = sourceText.Lines.GetLineFromPosition(expression.Span.Start).LineNumber;
            var caretLine = sourceText.Lines.GetLineFromPosition(position).LineNumber;

            if (argumentStartLine != caretLine)
            {
                return null;
            }

            return expression;
        }
    }
}
