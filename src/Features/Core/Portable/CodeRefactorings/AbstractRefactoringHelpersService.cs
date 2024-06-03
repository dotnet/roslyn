// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal abstract class AbstractRefactoringHelpersService<TExpressionSyntax, TArgumentSyntax, TExpressionStatementSyntax> : IRefactoringHelpersService
    where TExpressionSyntax : SyntaxNode
    where TArgumentSyntax : SyntaxNode
    where TExpressionStatementSyntax : SyntaxNode
{
    protected abstract IHeaderFacts HeaderFacts { get; }

    public abstract bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? typeDeclaration);

    private static void AddNode<TSyntaxNode>(bool allowEmptyNodes, ref TemporaryArray<TSyntaxNode> result, TSyntaxNode node) where TSyntaxNode : SyntaxNode
    {
        if (!allowEmptyNodes && node.Span.IsEmpty)
            return;

        result.Add(node);
    }

    public void AddRelevantNodes<TSyntaxNode>(
        ParsedDocument document, TextSpan selectionRaw, bool allowEmptyNodes, int maxCount, ref TemporaryArray<TSyntaxNode> result, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        // Given selection is trimmed first to enable over-selection that spans multiple lines. Since trailing whitespace ends
        // at newline boundary over-selection to e.g. a line after LocalFunctionStatement would cause FindNode to find enclosing
        // block's Node. That is because in addition to LocalFunctionStatement the selection would also contain trailing trivia 
        // (whitespace) of following statement.

        var root = document.Root;

        var syntaxFacts = document.LanguageServices.GetRequiredService<ISyntaxFactsService>();
        var headerFacts = document.LanguageServices.GetRequiredService<IHeaderFactsService>();
        var selectionTrimmed = CodeRefactoringHelpers.GetTrimmedTextSpan(document, selectionRaw);

        // If user selected only whitespace we don't want to return anything. We could do following:
        //  1) Consider token that owns (as its trivia) the whitespace.
        //  2) Consider start/beginning of whitespace as location (empty selection)
        // Option 1) can't be used all the time and 2) can be confusing for users. Therefore bailing out is the
        // most consistent option.
        if (selectionTrimmed.IsEmpty && !selectionRaw.IsEmpty)
            return;

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
            AddRelevantNodesForSelection(syntaxFacts, root, selectionTrimmed, allowEmptyNodes, maxCount, ref result, cancellationToken);
        }
        else
        {
            var location = selectionTrimmed.Start;

            // No more selection -> Handle what current selection is touching:
            //
            // Consider touching only for empty selections. Otherwise `[|C|] methodName(){}` would be considered as
            // touching the Method's Node (through the left edge, see below) which is something the user probably
            // didn't want since they specifically selected only the return type.
            //
            // What the selection is touching is used in two ways. 
            // - Firstly, it is used to handle situation where it touches a Token whose direct ancestor is wanted
            //   Node. While having the (even empty) selection inside such token or to left of such Token is already
            //   handle by code above touching it from right `C methodName[||](){}` isn't (the FindNode for that
            //   returns Args node).
            // 
            // - Secondly, it is used for left/right edge climbing. E.g. `[||]C methodName(){}` the touching token's
            //   direct ancestor is TypeNode for the return type but it is still reasonable to expect that the user
            //   might want to be given refactorings for the whole method (as he has caret on the edge of it).
            //   Therefore we travel the Node tree upwards and as long as we're on the left edge of a Node's span we
            //   consider such node & potentially continue traveling upwards. The situation for right edge (`C
            //   methodName(){}[||]`) is analogical. E.g. for right edge `C methodName(){}[||]`: CloseBraceToken ->
            //   BlockSyntax -> LocalFunctionStatement -> null (higher node doesn't end on position anymore) Note:
            //   left-edge climbing needs to handle AttributeLists explicitly, see below for more information. 
            //
            // - Thirdly, if location isn't touching anything, we move the location to the token in whose trivia
            //   location is in. more about that below.
            // 
            // - Fourthly, if we're in an expression / argument we consider touching a parent expression whenever
            //   we're within it as long as it is on the first line of such expression (arbitrary heuristic).

            // In addition to per-node extr also check if current location (if selection is empty) is in a header of
            // higher level desired node once. We do that only for locations because otherwise `[|int|] A { get;
            // set; }) would trigger all refactorings for Property Decl. We cannot check this any sooner because the
            // above code could've changed current location.
            AddNonHiddenCorrectTypeNodes(ExtractNodesInHeader(root, location, headerFacts), allowEmptyNodes, maxCount, ref result, cancellationToken);
            if (result.Count >= maxCount)
                return;

            var (tokenToLeft, tokenToRight) = GetTokensToLeftAndRight(document, root, location);

            // Add Nodes for touching tokens as described above.
            AddNodesForTokenToRight(syntaxFacts, root, allowEmptyNodes, maxCount, ref result, tokenToRight, cancellationToken);
            if (result.Count >= maxCount)
                return;

            AddNodesForTokenToLeft(syntaxFacts, allowEmptyNodes, maxCount, ref result, tokenToLeft, cancellationToken);
            if (result.Count >= maxCount)
                return;

            // If the wanted node is an expression syntax -> traverse upwards even if location is deep within a SyntaxNode.
            // We want to treat more types like expressions, e.g.: ArgumentSyntax should still trigger even if deep-in.
            if (IsWantedTypeExpressionLike<TSyntaxNode>())
            {
                // Reason to treat Arguments (and potentially others) as Expression-like: 
                // https://github.com/dotnet/roslyn/pull/37295#issuecomment-516145904
                AddNodesDeepIn(document, location, allowEmptyNodes, maxCount, ref result, cancellationToken);
            }
        }
    }

    private static bool IsWantedTypeExpressionLike<TSyntaxNode>() where TSyntaxNode : SyntaxNode
    {
        var wantedType = typeof(TSyntaxNode);

        var expressionType = typeof(TExpressionSyntax);
        var argumentType = typeof(TArgumentSyntax);
        var expressionStatementType = typeof(TExpressionStatementSyntax);

        return IsAEqualOrSubclassOfB(wantedType, expressionType) ||
            IsAEqualOrSubclassOfB(wantedType, argumentType) ||
            IsAEqualOrSubclassOfB(wantedType, expressionStatementType);

        static bool IsAEqualOrSubclassOfB(Type a, Type b)
        {
            return a == b || a.IsSubclassOf(b);
        }
    }

    private static (SyntaxToken tokenToLeft, SyntaxToken tokenToRight) GetTokensToLeftAndRight(
        ParsedDocument document,
        SyntaxNode root,
        int location)
    {
        // get Token for current location
        var tokenOnLocation = root.FindToken(location);

        var syntaxKinds = document.LanguageServices.GetRequiredService<ISyntaxKindsService>();
        if (tokenOnLocation.RawKind == syntaxKinds.CommaToken && location >= tokenOnLocation.Span.End)
        {
            var commaToken = tokenOnLocation;

            // A couple of scenarios to care about:
            //
            //      X,$$ Y
            //
            // In this case, consider the user on the Y node.
            //
            //      X,$$
            //      Y
            //
            // In this case, consider the user on the X node.
            var nextToken = commaToken.GetNextToken();
            var previousToken = commaToken.GetPreviousToken();
            if (nextToken != default && !commaToken.TrailingTrivia.Any(t => t.RawKind == syntaxKinds.EndOfLineTrivia))
            {
                return (tokenToLeft: default, tokenToRight: nextToken);
            }
            else if (previousToken != default && previousToken.Span.End == commaToken.Span.Start)
            {
                return (tokenToLeft: previousToken, tokenToRight: default);
            }
        }

        // Gets a token that is directly to the right of current location or that encompasses current location (`[||]tokenToRightOrIn` or `tok[||]enToRightOrIn`)
        var tokenToRight = tokenOnLocation.Span.Contains(location)
            ? tokenOnLocation
            : default;

        // A token can be to the left only when there's either no tokenDirectlyToRightOrIn or there's one  directly starting at current location. 
        // Otherwise (otherwise tokenToRightOrIn is also left from location, e.g: `tok[||]enToRightOrIn`)
        var tokenToLeft = default(SyntaxToken);
        if (tokenToRight == default || tokenToRight.FullSpan.Start == location)
        {
            var previousToken = tokenOnLocation.Span.End == location
                ? tokenOnLocation
                : tokenOnLocation.GetPreviousToken(includeZeroWidth: true);

            tokenToLeft = previousToken.Span.End == location
                ? previousToken
                : default;
        }

        // If both tokens directly to left & right are empty -> we're somewhere in the middle of whitespace.
        // Since there wouldn't be (m)any other refactorings we can try to offer at least the ones for (semantically) 
        // closest token/Node. Thus, we move the location to the token in whose `.FullSpan` the original location was.
        if (tokenToLeft == default && tokenToRight == default)
        {
            var sourceText = document.Text;

            if (IsAcceptableLineDistanceAway(sourceText, tokenOnLocation, location))
            {
                // tokenOnLocation: token in whose trivia location is at
                if (tokenOnLocation.Span.Start >= location)
                {
                    tokenToRight = tokenOnLocation;
                }
                else
                {
                    tokenToLeft = tokenOnLocation;
                }
            }
        }

        return (tokenToLeft, tokenToRight);

        static bool IsAcceptableLineDistanceAway(
            SourceText sourceText, SyntaxToken tokenOnLocation, int location)
        {
            // assume non-trivia token can't span multiple lines
            var tokenLine = sourceText.Lines.GetLineFromPosition(tokenOnLocation.Span.Start);
            var locationLine = sourceText.Lines.GetLineFromPosition(location);

            // Change location to nearest token only if the token is off by one line or less
            var lineDistance = tokenLine.LineNumber - locationLine.LineNumber;
            if (lineDistance is not 0 and not 1)
                return false;

            // Note: being a line below a tokenOnLocation is impossible in current model as whitespace 
            // trailing trivia ends on new line. Which is fine because if you're a line _after_ some node
            // you usually don't want refactorings for what's above you.

            if (lineDistance == 1)
            {
                // position is one line above the node of interest.  This is fine if that
                // line is blank.  Otherwise, if it isn't (i.e. it contains comments,
                // directives, or other trivia), then it's not likely the user is selecting
                // this entry.
                return locationLine.IsEmptyOrWhitespace();
            }

            // On hte same line.  This position is acceptable.
            return true;
        }
    }

    private void AddNodesForTokenToLeft<TSyntaxNode>(
        ISyntaxFactsService syntaxFacts,
        bool allowEmptyNodes,
        int maxCount,
        ref TemporaryArray<TSyntaxNode> result,
        SyntaxToken tokenToLeft,
        CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        var location = tokenToLeft.Span.End;

        // there could be multiple (n) tokens to the left if first n-1 are Empty -> iterate over all of them
        while (tokenToLeft != default)
        {
            var leftNode = tokenToLeft.Parent;
            do
            {
                // Consider either a Node that is:
                // - Ancestor Node of such Token as long as their span ends on location (it's still on the edge)
                AddNonHiddenCorrectTypeNodes(ExtractNodesSimple(leftNode, syntaxFacts), allowEmptyNodes, maxCount, ref result, cancellationToken);
                if (result.Count >= maxCount)
                    return;

                leftNode = leftNode?.Parent;
                if (leftNode is null)
                    break;

                if (leftNode.GetLastToken().Span.End != location && leftNode.Span.End != location)
                    break;
            }
            while (true);

            // as long as current tokenToLeft is empty -> its previous token is also tokenToLeft
            tokenToLeft = tokenToLeft.Span.IsEmpty
                ? tokenToLeft.GetPreviousToken(includeZeroWidth: true)
                : default;
        }
    }

    private void AddNodesForTokenToRight<TSyntaxNode>(
        ISyntaxFactsService syntaxFacts,
        SyntaxNode root,
        bool allowEmptyNodes,
        int maxCount,
        ref TemporaryArray<TSyntaxNode> result,
        SyntaxToken tokenToRightOrIn,
        CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        var location = tokenToRightOrIn.Span.Start;

        if (tokenToRightOrIn != default)
        {
            var rightNode = tokenToRightOrIn.Parent;
            do
            {
                // Consider either a Node that is:
                // - Parent of touched Token (location can be within) 
                // - Ancestor Node of such Token as long as their span starts on location (it's still on the edge)
                AddNonHiddenCorrectTypeNodes(ExtractNodesSimple(rightNode, syntaxFacts), allowEmptyNodes, maxCount, ref result, cancellationToken);
                if (result.Count >= maxCount)
                    return;

                rightNode = rightNode?.Parent;
                if (rightNode == null)
                    break;

                // The edge climbing for node to the right needs to handle Attributes e.g.:
                // [Test1]
                // //Comment1
                // [||]object Property1 { get; set; }
                // In essence:
                // - On the left edge of the node (-> left edge of first AttributeLists)
                // - On the left edge of the node sans AttributeLists (& as everywhere comments)
                if (rightNode.Span.Start != location)
                {
                    var rightNodeSpanWithoutAttributes = syntaxFacts.GetSpanWithoutAttributes(root, rightNode);
                    if (rightNodeSpanWithoutAttributes.Start != location)
                        break;
                }
            }
            while (true);
        }
    }

    private void AddRelevantNodesForSelection<TSyntaxNode>(
        ISyntaxFactsService syntaxFacts,
        SyntaxNode root,
        TextSpan selectionTrimmed,
        bool allowEmptyNodes,
        int maxCount,
        ref TemporaryArray<TSyntaxNode> result,
        CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        var selectionNode = root.FindNode(selectionTrimmed, getInnermostNodeForTie: true);
        var prevNode = selectionNode;
        do
        {
            var nonHiddenExtractedSelectedNodes = ExtractNodesSimple(selectionNode, syntaxFacts).OfType<TSyntaxNode>().Where(n => !n.OverlapsHiddenPosition(cancellationToken));
            foreach (var nonHiddenExtractedNode in nonHiddenExtractedSelectedNodes)
            {
                // For selections we need to handle an edge case where only AttributeLists are within selection (e.g. `Func([|[in][out]|] arg1);`).
                // In that case the smallest encompassing node is still the whole argument node but it's hard to justify showing refactorings for it
                // if user selected only its attributes.

                // Selection contains only AttributeLists -> don't consider current Node
                var spanWithoutAttributes = syntaxFacts.GetSpanWithoutAttributes(root, nonHiddenExtractedNode);
                if (!selectionTrimmed.IntersectsWith(spanWithoutAttributes))
                {
                    break;
                }

                AddNode(allowEmptyNodes, ref result, nonHiddenExtractedNode);
                if (result.Count >= maxCount)
                    return;
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
    protected virtual IEnumerable<SyntaxNode> ExtractNodesSimple(SyntaxNode? node, ISyntaxFactsService syntaxFacts)
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

        // `var a = b;` | `var a = b`;
        if (syntaxFacts.IsLocalDeclarationStatement(node) || syntaxFacts.IsLocalDeclarationStatement(node.Parent))
        {
            var localDeclarationStatement = syntaxFacts.IsLocalDeclarationStatement(node) ? node : node.Parent!;

            // Check if there's only one variable being declared, otherwise following transformation
            // would go through which isn't reasonable since we can't say the first one specifically
            // is wanted.
            // `var a = 1, `c = 2, d = 3`;
            // -> `var a = 1`, c = 2, d = 3;
            var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
            if (variables.Count == 1)
            {
                var declaredVariable = variables.First();

                // -> `a = b`
                yield return declaredVariable;

                // -> `b`
                var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(declaredVariable);
                if (initializer != null)
                {
                    var value = syntaxFacts.GetValueOfEqualsValueClause(initializer);
                    if (value != null)
                    {
                        yield return value;
                    }
                }
            }
        }

        // var `a = b`;
        if (syntaxFacts.IsVariableDeclarator(node))
        {
            // -> `b`
            var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(node);
            if (initializer != null)
            {
                var value = syntaxFacts.GetValueOfEqualsValueClause(initializer);
                if (value != null)
                {
                    yield return value;
                }
            }
        }

        // `a = b;`
        // -> `b`
        if (syntaxFacts.IsSimpleAssignmentStatement(node))
        {
            syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(node, out _, out _, out var rightSide);
            yield return rightSide;
        }

        // `a();`
        // -> a()
        if (syntaxFacts.IsExpressionStatement(node))
        {
            yield return syntaxFacts.GetExpressionOfExpressionStatement(node);
        }

        // `a()`;
        // -> `a();`
        if (syntaxFacts.IsExpressionStatement(node.Parent))
        {
            yield return node.Parent;
        }
    }

    /// <summary>
    /// Extractor function that checks and retrieves all nodes current location is in a header.
    /// </summary>
    protected virtual IEnumerable<SyntaxNode> ExtractNodesInHeader(SyntaxNode root, int location, IHeaderFactsService headerFacts)
    {
        // Header: [Test] `public int a` { get; set; }
        if (headerFacts.IsOnPropertyDeclarationHeader(root, location, out var propertyDeclaration))
            yield return propertyDeclaration;

        // Header: public C([Test]`int a = 42`) {}
        if (headerFacts.IsOnParameterHeader(root, location, out var parameter))
            yield return parameter;

        // Header: `public I.C([Test]int a = 42)` {}
        if (headerFacts.IsOnMethodHeader(root, location, out var method))
            yield return method;

        // Header: `static C([Test]int a = 42)` {}
        if (headerFacts.IsOnLocalFunctionHeader(root, location, out var localFunction))
            yield return localFunction;

        // Header: `var a = `3,` b = `5,` c = `7 + 3``;
        if (headerFacts.IsOnLocalDeclarationHeader(root, location, out var localDeclaration))
            yield return localDeclaration;

        // Header: `if(...)`{ };
        if (headerFacts.IsOnIfStatementHeader(root, location, out var ifStatement))
            yield return ifStatement;

        // Header: `foreach (var a in b)` { }
        if (headerFacts.IsOnForeachHeader(root, location, out var foreachStatement))
            yield return foreachStatement;

        if (headerFacts.IsOnTypeHeader(root, location, out var typeDeclaration))
            yield return typeDeclaration;
    }

    private static void AddNodesDeepIn<TSyntaxNode>(
        ParsedDocument document,
        int position,
        bool allowEmptyNodes,
        int maxCount,
        ref TemporaryArray<TSyntaxNode> result,
        CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        // If we're deep inside we don't have to deal with being on edges (that gets dealt by TryGetSelectedNodeAsync)
        // -> can simply FindToken -> proceed testing its ancestors
        var root = document.Root;
        if (root is null)
            throw new NotSupportedException(WorkspacesResources.Document_does_not_support_syntax_trees);

        var token = root.FindTokenOnRightOfPosition(position, true);

        // traverse upwards and add all parents if of correct type
        var ancestor = token.Parent;
        while (ancestor != null)
        {
            if (ancestor is TSyntaxNode typedAncestor)
            {
                var sourceText = document.Text;

                var argumentStartLine = sourceText.Lines.GetLineFromPosition(typedAncestor.Span.Start).LineNumber;
                var caretLine = sourceText.Lines.GetLineFromPosition(position).LineNumber;

                if (argumentStartLine == caretLine && !typedAncestor.OverlapsHiddenPosition(cancellationToken))
                {
                    AddNode(allowEmptyNodes, ref result, typedAncestor);
                    if (result.Count >= maxCount)
                        return;
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

    private static void AddNonHiddenCorrectTypeNodes<TSyntaxNode>(
        IEnumerable<SyntaxNode> nodes, bool allowEmptyNodes, int maxCount, ref TemporaryArray<TSyntaxNode> result, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        foreach (var node in nodes)
        {
            if (node is TSyntaxNode typedNode &&
                !node.OverlapsHiddenPosition(cancellationToken))
            {
                AddNode(allowEmptyNodes, ref result, typedNode);
                if (result.Count >= maxCount)
                    return;
            }
        }
    }

    public bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
        => HeaderFacts.IsOnTypeHeader(root, position, fullHeader, out typeDeclaration);

    public bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? propertyDeclaration)
        => HeaderFacts.IsOnPropertyDeclarationHeader(root, position, out propertyDeclaration);

    public bool IsOnParameterHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? parameter)
        => HeaderFacts.IsOnParameterHeader(root, position, out parameter);

    public bool IsOnMethodHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? method)
        => HeaderFacts.IsOnMethodHeader(root, position, out method);

    public bool IsOnLocalFunctionHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localFunction)
        => HeaderFacts.IsOnLocalFunctionHeader(root, position, out localFunction);

    public bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localDeclaration)
        => HeaderFacts.IsOnLocalDeclarationHeader(root, position, out localDeclaration);

    public bool IsOnIfStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? ifStatement)
        => HeaderFacts.IsOnIfStatementHeader(root, position, out ifStatement);

    public bool IsOnWhileStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? whileStatement)
        => HeaderFacts.IsOnWhileStatementHeader(root, position, out whileStatement);

    public bool IsOnForeachHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? foreachStatement)
        => HeaderFacts.IsOnForeachHeader(root, position, out foreachStatement);
}
