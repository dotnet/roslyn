// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly partial struct ChildSyntaxList : IEquatable<ChildSyntaxList>, IReadOnlyList<SyntaxNodeOrToken>
{
    private readonly SyntaxNode _node;
    private readonly int _count;

    internal ChildSyntaxList(SyntaxNode node)
    {
        _node = node;
        _count = CountNodes(node.Green);
    }

    /// <summary>
    ///  Gets the number of children contained in the <see cref="ChildSyntaxList"/>.
    /// </summary>
    public int Count => _count;

    internal static int CountNodes(GreenNode green)
    {
        var n = 0;

        for (int i = 0, s = green.SlotCount; i < s; i++)
        {
            var child = green.GetSlot(i);
            if (child != null)
            {
                if (!child.IsList)
                {
                    n++;
                }
                else
                {
                    n += child.SlotCount;
                }
            }
        }

        return n;
    }

    /// <summary>
    ///  Gets the child at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the child to get.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    ///  <paramref name="index"/> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="ChildSyntaxList.Count"/>.
    /// </exception>
    public SyntaxNodeOrToken this[int index]
    {
        get
        {
            if (unchecked((uint)index < (uint)_count))
            {
                return ItemInternal(_node, index);
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal SyntaxNode Node => _node;

    private static int Occupancy(GreenNode green)
    {
        return green.IsList ? green.SlotCount : 1;
    }

    internal readonly struct SlotData
    {
        /// <summary>
        /// The green node slot index at which to start the search
        /// </summary>
        public readonly int SlotIndex;

        /// <summary>
        /// Indicates the total number of occupants in preceding slots
        /// </summary>
        public readonly int PrecedingOccupantSlotCount;

        /// <summary>
        /// Indicates the node start position plus any prior slot full widths
        /// </summary>
        public readonly int PositionAtSlotIndex;

        public SlotData(SyntaxNode node)
            : this(slotIndex: 0, precedingOccupantSlotCount: 0, node.Position)
        {
        }

        public SlotData(int slotIndex, int precedingOccupantSlotCount, int positionAtSlotIndex)
        {
            SlotIndex = slotIndex;
            PrecedingOccupantSlotCount = precedingOccupantSlotCount;
            PositionAtSlotIndex = positionAtSlotIndex;
        }
    }

    internal static SyntaxNodeOrToken ItemInternal(SyntaxNode node, int index)
    {
        var slotData = new SlotData(node);

        return ItemInternal(node, index, ref slotData);
    }

    /// <summary>
    /// An internal indexer that does not verify index.
    /// Used when caller has already ensured that index is within bounds.
    /// </summary>
    internal static SyntaxNodeOrToken ItemInternal(SyntaxNode node, int index, ref SlotData slotData)
    {
        GreenNode? greenChild;
        var green = node.Green;

        // slotData may contain information that allows us to start the loop below using data
        // calculated during a previous call. As index represents the offset into all children of
        // node, idx represents the offset requested relative to the given slot index.
        var idx = index - slotData.PrecedingOccupantSlotCount;
        var slotIndex = slotData.SlotIndex;
        var position = slotData.PositionAtSlotIndex;

        Debug.Assert(idx >= 0);

        // find a slot that contains the node or its parent list (if node is in a list)
        // we will be skipping whole slots here so we will not loop for long
        //
        // at the end of this loop we will have
        // 1) slot index - slotIdx
        // 2) if the slot is a list, node index in the list - idx
        // 3) slot position - position
        while (true)
        {
            greenChild = green.GetSlot(slotIndex);
            if (greenChild != null)
            {
                var currentOccupancy = Occupancy(greenChild);
                if (idx < currentOccupancy)
                {
                    break;
                }

                idx -= currentOccupancy;
                position += greenChild.Width;
            }

            slotIndex++;
        }

        if (slotIndex != slotData.SlotIndex)
        {
            // (index - idx) represents the number of occupants prior to this new slotIndex
            slotData = new SlotData(slotIndex, index - idx, position);
        }

        // get node that represents this slot
        var red = node.GetNodeSlot(slotIndex);
        if (!greenChild.IsList)
        {
            // this is a single node
            // if it is a node, we are done
            if (red != null)
            {
                return red;
            }
        }
        else if (red != null)
        {
            // it is a red list of nodes (separated or not), most common case
            var redChild = red.GetNodeSlot(idx);
            if (redChild != null)
            {
                // this is our node
                return redChild;
            }

            // must be a separator
            // update greenChild and position and let it be handled as a token
            greenChild = greenChild.GetSlot(idx);
            position = red.GetChildPosition(idx);
        }
        else
        {
            // it is a token from a token list, uncommon case
            // update greenChild and position and let it be handled as a token
            position += greenChild.GetSlotOffset(idx);
            greenChild = greenChild.GetSlot(idx);
        }

        return new SyntaxNodeOrToken(node, greenChild, position, index);
    }

    /// <summary>
    ///  Locate the node that is a child of the given <see cref="SyntaxNode"/> and contains the given position.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to search.</param>
    /// <param name="targetPosition">The position.</param>
    /// <returns>
    ///  The node that spans the given position.
    /// </returns>
    /// <remarks>
    ///  Assumes that <paramref name="targetPosition"/> is within the span of <paramref name="node"/>.
    /// </remarks>
    internal static SyntaxNodeOrToken ChildThatContainsPosition(SyntaxNode node, int targetPosition)
    {
        // The targetPosition must already be within this node
        Debug.Assert(node.Span.Contains(targetPosition));

        var green = node.Green;
        var position = node.Position;
        var index = 0;

        Debug.Assert(!green.IsList);

        // Find the green node that spans the target position.
        // We will be skipping whole slots here so we will not loop for long
        int slot;
        for (slot = 0; ; slot++)
        {
            var greenChild = green.GetSlot(slot);
            if (greenChild != null)
            {
                var endPosition = position + greenChild.Width;
                if (targetPosition < endPosition)
                {
                    // Descend into the child element
                    green = greenChild;
                    break;
                }

                position = endPosition;
                index += Occupancy(greenChild);
            }
        }

        // Realize the red node (if any)
        var red = node.GetNodeSlot(slot);
        if (!green.IsList)
        {
            // This is a single node.
            // If it is a node, we are done.
            if (red != null)
            {
                return red;
            }

            // Otherwise will have to make a token with current green and position
        }
        else
        {
            slot = green.FindSlotIndexContainingOffset(targetPosition - position);

            // Realize the red node (if any)
            if (red != null)
            {
                // It is a red list of nodes
                red = red.GetNodeSlot(slot);
                if (red != null)
                {
                    return red;
                }
            }

            // Otherwise we have a token.
            position += green.GetSlotOffset(slot);
            green = green.GetSlot(slot);

            // Since we can't have "lists of lists", the Occupancy calculation for
            // child elements in a list is simple.
            index += slot;

        }

        // Make a token with current child and position.
        return new SyntaxNodeOrToken(node, green, position, index);
    }

    /// <summary>
    /// An internal indexer that does not verify index.
    /// Used when caller has already ensured that index is within bounds.
    /// </summary>
    internal static SyntaxNode? ItemInternalAsNode(SyntaxNode node, int index, ref SlotData slotData)
    {
        GreenNode? greenChild;
        var green = node.Green;
        var idx = index - slotData.PrecedingOccupantSlotCount;
        var slotIndex = slotData.SlotIndex;
        var position = slotData.PositionAtSlotIndex;

        Debug.Assert(idx >= 0);

        // find a slot that contains the node or its parent list (if node is in a list)
        // we will be skipping whole slots here so we will not loop for long
        //
        // at the end of this loop we will have
        // 1) slot index - slotIdx
        // 2) if the slot is a list, node index in the list - idx
        while (true)
        {
            greenChild = green.GetSlot(slotIndex);
            if (greenChild != null)
            {
                var currentOccupancy = Occupancy(greenChild);
                if (idx < currentOccupancy)
                {
                    break;
                }

                idx -= currentOccupancy;
                position += greenChild.Width;
            }

            slotIndex++;
        }

        if (slotIndex != slotData.SlotIndex)
        {
            // (index - idx) represents the number of occupants prior to this new slotIndex
            slotData = new SlotData(slotIndex, index - idx, position);
        }

        // get node that represents this slot
        var red = node.GetNodeSlot(slotIndex);
        if (greenChild.IsList && red != null)
        {
            // it is a red list of nodes, most common case
            return red.GetNodeSlot(idx);
        }

        // this is a single node
        return red;
    }

    // for debugging
#pragma warning disable IDE0051 // Remove unused private members
    private SyntaxNodeOrToken[] NodesAndTokens => [.. this];
#pragma warning restore IDE0051 // Remove unused private members

    public bool Any()
        => _count != 0;

    /// <summary>
    ///  Returns the first child in the list.
    /// </summary>
    /// <returns>
    ///  The first child in the list.
    /// </returns>
    /// <exception cref="InvalidOperationException">The list is empty.</exception>
    public SyntaxNodeOrToken First()
    {
        if (Any())
        {
            return this[0];
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    ///  Returns the last child in the list.
    /// </summary>
    /// <returns>
    ///  The last child in the list.
    /// </returns>
    /// <exception cref="InvalidOperationException">The list is empty.</exception>
    public SyntaxNodeOrToken Last()
    {
        if (Any())
        {
            return this[_count - 1];
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    ///  Returns a list which contains all children of <see cref="ChildSyntaxList"/> in reversed order.
    /// </summary>
    /// <returns>
    ///  <see cref="Reversed"/> which contains all children of <see cref="ChildSyntaxList"/> in reversed order.
    /// </returns>
    public Reversed Reverse()
    {
        Debug.Assert(_node != null);
        return new Reversed(_node, _count);
    }

    /// <summary>
    ///  Returns an enumerator that iterates through the <see cref="ChildSyntaxList"/>.
    /// </summary>
    /// <returns>
    ///  A <see cref="Enumerator"/> for the <see cref="ChildSyntaxList"/>.
    /// </returns>
    public Enumerator GetEnumerator()
        => _node != null ? new Enumerator(_node, _count) : default;

    IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
        => _node == null
            ? SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>()
            : new EnumeratorImpl(_node, _count);

    IEnumerator IEnumerable.GetEnumerator()
        => _node == null
            ? SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>()
            : new EnumeratorImpl(_node, _count);

    /// <summary>
    ///  Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to be compared with the current instance.</param>
    /// <returns>
    ///  <see langword="true"/> if the specified object is a <see cref="ChildSyntaxList" /> structure and is equal to the current instance;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ChildSyntaxList list && Equals(list);

    /// <summary>
    ///  Determines whether the specified <see cref="ChildSyntaxList" /> structure is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="ChildSyntaxList" /> structure to be compared with the current instance.</param>
    /// <returns>
    ///  <see langword="true"/> if the specified <see cref="ChildSyntaxList" /> structure is equal to the current instance;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(ChildSyntaxList other)
        => _node == other._node;

    /// <summary>
    ///  Returns the hash code for the current instance.
    /// </summary>
    /// <returns>
    ///  A 32-bit signed integer hash code.
    /// </returns>
    public override int GetHashCode()
        => _node?.GetHashCode() ?? 0;

    /// <summary>
    ///  Indicates whether two <see cref="ChildSyntaxList" /> structures are equal.
    /// </summary>
    /// <param name="list1">The <see cref="ChildSyntaxList" /> structure on the left side of the equality operator.</param>
    /// <param name="list2">The <see cref="ChildSyntaxList" /> structure on the right side of the equality operator.</param>
    /// <returns>
    ///  <see langword="true"/> if <paramref name="list1" /> is equal to <paramref name="list2" />;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(ChildSyntaxList list1, ChildSyntaxList list2)
        => list1.Equals(list2);

    /// <summary>
    ///  Indicates whether two <see cref="ChildSyntaxList" /> structures are unequal.
    /// </summary>
    /// <param name="list1">The <see cref="ChildSyntaxList" /> structure on the left side of the inequality operator.</param>
    /// <param name="list2">The <see cref="ChildSyntaxList" /> structure on the right side of the inequality operator.</param>
    /// <returns>
    ///  <see langword="true"/> if <paramref name="list1" /> is not equal to <paramref name="list2" />;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(ChildSyntaxList list1, ChildSyntaxList list2)
        => !list1.Equals(list2);
}
