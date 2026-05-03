// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
internal abstract partial class SyntaxNode(GreenNode green, SyntaxNode parent, int position)
{
    internal GreenNode Green { get; } = green;
    public SyntaxNode Parent { get; } = parent;
    public int Position { get; } = position;

    public int EndPosition => Position + Width;

    public SyntaxKind Kind => Green.Kind;

    public int Width => Green.Width;

    public int SpanStart => Position;

    public TextSpan Span => new(Position, Green.Width);

    internal int SlotCount => Green.SlotCount;

    public bool IsList => Green.IsList;

    public bool IsMissing => Green.IsMissing;

    public bool IsToken => Green.IsToken;

    public bool ContainsDiagnostics => Green.ContainsDiagnostics;

    internal abstract SyntaxNode? GetNodeSlot(int index);

    internal abstract SyntaxNode? GetCachedSlot(int index);

    internal SyntaxNode? GetRed(ref SyntaxNode? field, int slot)
    {
        var result = field;

        if (result == null)
        {
            var green = Green.GetSlot(slot);
            if (green != null)
            {
                Interlocked.CompareExchange(ref field, green.CreateRed(this, GetChildPosition(slot)), null);
                result = field;
            }
        }

        return result;
    }

    // Special case of above function where slot = 0, does not need GetChildPosition
    internal SyntaxNode? GetRedAtZero(ref SyntaxNode? field)
    {
        var result = field;

        if (result == null)
        {
            var green = Green.GetSlot(0);
            if (green != null)
            {
                Interlocked.CompareExchange(ref field, green.CreateRed(this, Position), null);
                result = field;
            }
        }

        return result;
    }

    protected T? GetRed<T>(ref T field, int slot)
        where T : SyntaxNode
    {
        var result = field;

        if (result == null)
        {
            var green = Green.GetSlot(slot);
            if (green != null)
            {
                Interlocked.CompareExchange(ref field, (T)green.CreateRed(this, this.GetChildPosition(slot)), null);
                result = field;
            }
        }

        return result;
    }

    // special case of above function where slot = 0, does not need GetChildPosition
    protected T? GetRedAtZero<T>(ref T field)
        where T : SyntaxNode
    {
        var result = field;

        if (result == null)
        {
            var green = Green.GetSlot(0);
            if (green != null)
            {
                Interlocked.CompareExchange(ref field, (T)green.CreateRed(this, Position), null);
                result = field;
            }
        }

        return result;
    }

    internal SyntaxNode? GetRedElement(ref SyntaxNode? element, int slot)
    {
        Debug.Assert(IsList);

        var result = element;

        if (result == null)
        {
            var green = Green.GetRequiredSlot(slot);
            // passing list's parent
            Interlocked.CompareExchange(ref element, green.CreateRed(Parent, GetChildPosition(slot)), null);
            result = element;
        }

        return result;
    }

    internal int GetChildIndex(int slot)
    {
        var index = 0;

        for (var i = 0; i < slot; i++)
        {
            var item = Green.GetSlot(i);
            if (item != null)
            {
                if (item.IsList)
                {
                    index += item.SlotCount;
                }
                else
                {
                    index++;
                }
            }
        }

        return index;
    }

    internal int GetChildPosition(int index)
    {
        var offset = 0;
        var green = Green;

        while (index > 0)
        {
            index--;
            var prevSibling = GetCachedSlot(index);
            if (prevSibling != null)
            {
                return prevSibling.EndPosition + offset;
            }

            var greenChild = green.GetSlot(index);
            if (greenChild != null)
            {
                offset += greenChild.Width;
            }
        }

        return Position + offset;
    }

    internal SyntaxNodeOrToken ChildThatContainsPosition(int position)
    {
        //PERF: it is very important to keep this method fast.

        if (!Span.Contains(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        var childNodeOrToken = ChildSyntaxList.ChildThatContainsPosition(this, position);
        Debug.Assert(childNodeOrToken.Span.Contains(position), "ChildThatContainsPosition's return value does not contain the requested position.");
        return childNodeOrToken;
    }

    /// <summary>
    /// Gets the first token of the tree rooted by this node. Skips zero-width tokens.
    /// </summary>
    /// <returns>The first token or <c>default(SyntaxToken)</c> if it doesn't exist.</returns>
    public SyntaxToken GetFirstToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetFirstToken(this, includeZeroWidth);
    }

    /// <summary>
    /// Gets the last token of the tree rooted by this node. Skips zero-width tokens.
    /// </summary>
    /// <returns>The last token or <c>default(SyntaxToken)</c> if it doesn't exist.</returns>
    public SyntaxToken GetLastToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetLastToken(this, includeZeroWidth);
    }

    /// <summary>
    /// The list of child nodes of this node, where each element is a SyntaxNode instance.
    /// </summary>
    public ChildSyntaxList ChildNodesAndTokens()
    {
        return new ChildSyntaxList(this);
    }

    /// <summary>
    /// Gets a list of the child nodes in prefix document order.
    /// </summary>
    public IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var nodeOrToken in ChildNodesAndTokens())
        {
            if (nodeOrToken.IsNode)
            {
                yield return nodeOrToken.AsNode()!;
            }
        }
    }

    /// <summary>
    /// Gets a list of the direct child tokens of this node.
    /// </summary>
    public IEnumerable<SyntaxToken> ChildTokens()
    {
        foreach (var nodeOrToken in ChildNodesAndTokens())
        {
            if (nodeOrToken.IsToken)
            {
                yield return nodeOrToken.AsToken();
            }
        }
    }

    /// <summary>
    /// Gets a list of ancestor nodes
    /// </summary>
    public IEnumerable<SyntaxNode> Ancestors()
        => Parent?.AncestorsAndSelf() ?? [];

    /// <summary>
    /// Gets a list of ancestor nodes (including this node)
    /// </summary>
    public IEnumerable<SyntaxNode> AncestorsAndSelf()
    {
        for (var node = this; node != null; node = node.Parent)
        {
            yield return node;
        }
    }

    /// <summary>
    /// Gets the first node of type TNode that matches the predicate.
    /// </summary>
    public TNode? FirstAncestorOrSelf<TNode>(Func<TNode, bool>? predicate = null)
        where TNode : SyntaxNode
    {
        for (var node = this; node != null; node = node.Parent)
        {
            if (node is TNode typedNode && (predicate == null || predicate(typedNode)))
            {
                return typedNode;
            }
        }

        return default;
    }

    /// <summary>
    /// Gets a list of descendant nodes in prefix document order.
    /// </summary>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNode> DescendantNodes(Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesImpl(Span, descendIntoChildren, includeSelf: false);
    }

    /// <summary>
    /// Gets a list of descendant nodes in prefix document order.
    /// </summary>
    /// <param name="span">The span the node's full span must intersect.</param>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNode> DescendantNodes(TextSpan span, Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesImpl(span, descendIntoChildren, includeSelf: false);
    }

    /// <summary>
    /// Gets a list of descendant nodes (including this node) in prefix document order.
    /// </summary>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNode> DescendantNodesAndSelf(Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesImpl(Span, descendIntoChildren, includeSelf: true);
    }

    /// <summary>
    /// Gets a list of descendant nodes (including this node) in prefix document order.
    /// </summary>
    /// <param name="span">The span the node's full span must intersect.</param>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNode> DescendantNodesAndSelf(TextSpan span, Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesImpl(span, descendIntoChildren, includeSelf: true);
    }

    /// <summary>
    /// Gets a list of descendant nodes and tokens in prefix document order.
    /// </summary>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens(Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesAndTokensImpl(Span, descendIntoChildren, includeSelf: false);
    }

    //// <summary>
    /// Gets a list of the descendant nodes and tokens in prefix document order.
    /// </summary>
    /// <param name="span">The span the node's full span must intersect.</param>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens(TextSpan span, Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesAndTokensImpl(span, descendIntoChildren, includeSelf: false);
    }

    /// <summary>
    /// Gets a list of descendant nodes (including this node) in prefix document order.
    /// </summary>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNodeOrToken> DescendandNodesAndTokensAndSelf(Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesAndTokensImpl(Span, descendIntoChildren, includeSelf: true);
    }

    /// <summary>
    /// Gets a list of descendant nodes (including this node) in prefix document order.
    /// </summary>
    /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
    public IEnumerable<SyntaxNodeOrToken> DescendandNodesAndTokensAndSelf(TextSpan span, Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesAndTokensImpl(span, descendIntoChildren, includeSelf: true);
    }

    /// <summary>
    /// Gets a list of all the tokens in the span of this node.
    /// </summary>
    public IEnumerable<SyntaxToken> DescendantTokens(Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesAndTokens(Span, descendIntoChildren)
            .Where(static x => x.IsToken)
            .Select(static x => x.AsToken());
    }

    /// <summary>
    /// Gets a list of all the tokens in the span of this node.
    /// </summary>
    public IEnumerable<SyntaxToken> DescendantTokens(TextSpan span, Func<SyntaxNode, bool>? descendIntoChildren = null)
    {
        return DescendantNodesAndTokens(span, descendIntoChildren)
            .Where(static x => x.IsToken)
            .Select(static x => x.AsToken());
    }

    protected internal abstract SyntaxNode ReplaceCore<TNode>(
        IEnumerable<TNode>? nodes = null,
        Func<TNode, TNode, SyntaxNode>? computeReplacementNode = null,
        IEnumerable<SyntaxToken>? tokens = null,
        Func<SyntaxToken, SyntaxToken, SyntaxToken>? computeReplacementToken = null)
        where TNode : SyntaxNode;

    protected internal abstract SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes);
    protected internal abstract SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore);
    protected internal abstract SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens);
    protected internal abstract SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore);

    public RazorDiagnostic[] GetDiagnostics()
    {
        return Green.GetDiagnostics();
    }

    public bool IsEquivalentTo(SyntaxNode other)
    {
        if (this == other)
        {
            return true;
        }

        if (other == null)
        {
            return false;
        }

        return Green.IsEquivalentTo(other.Green);
    }

    /// <summary>
    /// Finds a descendant token of this node whose span includes the supplied position.
    /// </summary>
    /// <param name="position">The character position of the token relative to the beginning of the file.</param>
    /// <param name="includeWhitespace">
    /// True to return whitespace or newline tokens. If false, finds the closest non-whitespace, non-newline token that matches the following algorithm:
    /// <list type="number">
    /// <item>
    /// <description>
    /// Scan backwards until a non-whitespace token is found. If a newline is found, continue to the next step. Otherwise, return the found token.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Scan forwards until a non-whitespace, non-newline token is found. Return the found token.
    /// </description>
    /// </item>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when requested position is out of range of the root token requested. This includes the whitespace scanning: calling FindToken(0, false)
    /// on a whitespace token will throw.
    /// </exception>
    public SyntaxToken FindToken(int position, bool includeWhitespace = false)
    {
        if (position == EndPosition && this is RazorDocumentSyntax document)
        {
            return document.EndOfFile;
        }

        if (!Span.Contains(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        SyntaxNodeOrToken curNode = this;

        while (true)
        {
            Debug.Assert(curNode.Kind is < SyntaxKind.FirstAvailableTokenKind and >= 0);
            Debug.Assert(curNode.Span.Contains(position));

            var node = curNode.AsNode();

            if (node != null)
            {
                curNode = node.ChildThatContainsPosition(position);
            }
            else
            {
                // Once we've found the token that covers the exact position, we potentially need to account for whitespace to
                // partially emulate Roslyn's behavior. The rule is pretty simple:
                //
                //  After a non-whitespace token, all whitespace up to and including the next newline is considered part of the previous token.
                //  All whitespace after it is considered part of the next token.
                //
                // Roslyn, of course, includes all trivia in this rule, and also uses trivia to represent comments. Razor does neither of these things,
                // and we only want to skip past whitespace. Therefore, the algorithm we implement is:
                //
                //  Walk backwards until we find a non-whitespace token. If we find something that isn't a newline, that is the node requested.
                //  If we find a newline, we need to walk forwards until we find the first non-whitespace or newline token. That is the node requested.
                var foundToken = curNode.AsToken();
                if (includeWhitespace || foundToken.Kind is not (SyntaxKind.Whitespace or SyntaxKind.NewLine))
                {
                    return foundToken;
                }

                var originalFoundToken = foundToken;

                // Walk backwards until we find a non-whitespace token. We accomplish this by looking up the stack and walking nodes backwards from where we
                // were located.
                if (tryWalkBackwards(originalFoundToken, out foundToken))
                {
                    return foundToken;
                }

                // Encountered a newline while backtracking, so we need to walk forward instead.
                return walkForward(originalFoundToken);
            }

            bool tryWalkBackwards(SyntaxToken originalFoundToken, out SyntaxToken foundToken)
            {
                foundToken = originalFoundToken;
                do
                {
                    foundToken = foundToken.GetPreviousToken(includeZeroWidth: true);
                    if (foundToken.Kind is SyntaxKind.None or SyntaxKind.NewLine)
                    {
                        return false;
                    }

                    if (foundToken.SpanStart < this.SpanStart)
                    {
                        // User requested a position that is out of range of the root token requested.
                        throw new ArgumentOutOfRangeException(nameof(position));
                    }
                }
                while (foundToken.Kind is SyntaxKind.Whitespace);

                return true;
            }

            SyntaxToken walkForward(SyntaxToken originalFoundToken)
            {
                var currentToken = originalFoundToken;
                do
                {
                    currentToken = currentToken.GetNextToken(includeZeroWidth: true);

                    if (currentToken.Kind == SyntaxKind.None || currentToken.Span.End > this.Span.End)
                    {
                        // Walked all the way forward to the end of the root that was requested and did not find any non-whitespace tokens. The user requested
                        // something out of range.
                        throw new ArgumentOutOfRangeException(nameof(position));
                    }
                }
                while (currentToken is { Kind: SyntaxKind.NewLine or SyntaxKind.Whitespace });

                return currentToken;
            }
        }
    }

    public override string ToString()
    {
        return Green.ToString();
    }

    protected virtual string GetDebuggerDisplay()
    {
        if (IsToken)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0};[{1}]", Kind, ToString());
        }

        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}..{2})", Kind, Position, EndPosition);
    }
}
