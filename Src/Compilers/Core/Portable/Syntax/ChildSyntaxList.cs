// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial struct ChildSyntaxList : IEquatable<ChildSyntaxList>, IReadOnlyList<SyntaxNodeOrToken>
    {
        private readonly SyntaxNode node;
        private readonly int count;

        internal ChildSyntaxList(SyntaxNode node)
        {
            this.node = node;
            this.count = CountNodes(node.Green);
        }

        /// <summary>
        /// Gets the number of children contained in the <see cref="ChildSyntaxList"/>.
        /// </summary>
        public int Count
        {
            get
            {
                return this.count;
            }
        }

        internal static int CountNodes(GreenNode green)
        {
            int n = 0;

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

        /// <summary>Gets the child at the specified index.</summary>
        /// <param name="index">The zero-based index of the child to get.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="ChildSyntaxList.Count"/>. </exception>
        public SyntaxNodeOrToken this[int index]
        {
            get
            {
                if (unchecked((uint)index < (uint)this.count))
                {
                    return ItemInternal(node, index);
                }

                throw new ArgumentOutOfRangeException("index");
            }
        }

        internal SyntaxNode Node
        {
            get { return this.node; }
        }

        private static int Occupancy(GreenNode green)
        {
            return green.IsList ? green.SlotCount : 1;
        }

        /// <summary>
        /// internal indexer that does not verify index.
        /// Used when caller has already ensured that index is within bounds.
        /// </summary>
        internal static SyntaxNodeOrToken ItemInternal(SyntaxNode node, int index)
        {
            GreenNode greenChild;
            var green = node.Green;
            var idx = index;
            var slotIndex = 0;
            var position = node.Position;

            // find a slot that contains the node or its parent list (if node is in a list)
            // we will be skipping whole slots here so we will not loop for long
            // the max possible number of slots is 11 (TypeDeclarationSyntax)
            // and typically much less than that
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
                    int currentOccupancy = Occupancy(greenChild);
                    if (idx < currentOccupancy)
                    {
                        break;
                    }

                    idx -= currentOccupancy;
                    position += greenChild.FullWidth;
                }

                slotIndex++;
            }

            // get node that represents this slot
            var red = node.GetNodeSlot(slotIndex);
            if (!greenChild.IsList)
            {
                // this is a single node or token
                // if it is a node, we are done
                // otherwise will have to make a token with current gChild and position
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
                // update gChild and position and let it be handled as a token
                greenChild = greenChild.GetSlot(idx);
                position = red.GetChildPosition(idx);
            }
            else
            {
                // it is a token from a token list, uncommon case
                // update gChild and position and let it be handled as a token
                position += greenChild.GetSlotOffset(idx);
                greenChild = greenChild.GetSlot(idx);
            }

            return new SyntaxNodeOrToken(node, greenChild, position, index);
        }

        /// <summary>
        /// internal indexer that does not verify index.
        /// Used when caller has already ensured that index is within bounds.
        /// </summary>
        internal static SyntaxNode ItemInternalAsNode(SyntaxNode node, int index)
        {
            GreenNode greenChild;
            var green = node.Green;
            var idx = index;
            var slotIndex = 0;

            // find a slot that contains the node or its parent list (if node is in a list)
            // we will be skipping whole slots here so we will not loop for long
            // the max possible number of slots is 11 (TypeDeclarationSyntax)
            // and typically much less than that
            //
            // at the end of this loop we will have
            // 1) slot index - slotIdx
            // 2) if the slot is a list, node index in the list - idx
            while (true)
            {
                greenChild = green.GetSlot(slotIndex);
                if (greenChild != null)
                {
                    int currentOccupancy = Occupancy(greenChild);
                    if (idx < currentOccupancy)
                    {
                        break;
                    }

                    idx -= currentOccupancy;
                }

                slotIndex++;
            }

            // get node that represents this slot
            var red = node.GetNodeSlot(slotIndex);
            if (greenChild.IsList && red != null)
            {
                // it is a red list of nodes (separated or not), most common case
                return red.GetNodeSlot(idx);
            }

            // this is a single node or token
            return red;
        }

        // for debugging
        private SyntaxNodeOrToken[] Nodes
        {
            get
            {
                return this.ToArray();
            }
        }

        public bool Any()
        {
            return this.count != 0;
        }

        /// <summary>
        /// Returns the first child in the list.
        /// </summary>
        /// <returns>The first child in the list.</returns>
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>    
        public SyntaxNodeOrToken First()
        {
            if (Any())
            {
                return this[0];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns the last child in the list.
        /// </summary>
        /// <returns>The last child in the list.</returns>
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>    
        public SyntaxNodeOrToken Last()
        {
            if (Any())
            {
                return this[this.count - 1];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns a list which contains all children of <see cref="ChildSyntaxList"/> in reversed order.
        /// </summary>
        /// <returns><see cref="Reversed"/> which contains all children of <see cref="ChildSyntaxList"/> in reversed order</returns>
        public Reversed Reverse()
        {
            return new Reversed(this.node, this.count);
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="ChildSyntaxList"/>.</summary>
        /// <returns>A <see cref="Enumerator"/> for the <see cref="ChildSyntaxList"/>.</returns>
        public Enumerator GetEnumerator()
        {
            if (this.node == null)
            {
                return default(Enumerator);
            }

            return new Enumerator(this.node, this.count);
        }

        IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
            }

            return new EnumeratorImpl(this.node, this.count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
            }

            return new EnumeratorImpl(this.node, this.count);
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <returns>true if the specified object is a <see cref="ChildSyntaxList" /> structure and is equal to the current instance; otherwise, false.</returns>
        /// <param name="obj">The object to be compared with the current instance.</param>
        public override bool Equals(object obj)
        {
            return obj is ChildSyntaxList && Equals((ChildSyntaxList)obj);
        }

        /// <summary>Determines whether the specified <see cref="ChildSyntaxList" /> structure is equal to the current instance.</summary>
        /// <returns>true if the specified <see cref="ChildSyntaxList" /> structure is equal to the current instance; otherwise, false.</returns>
        /// <param name="other">The <see cref="ChildSyntaxList" /> structure to be compared with the current instance.</param>
        public bool Equals(ChildSyntaxList other)
        {
            return this.node == other.node;
        }

        /// <summary>Returns the hash code for the current instance.</summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return node == null ? 0 : node.GetHashCode();
        }

        /// <summary>Indicates whether two <see cref="ChildSyntaxList" /> structures are equal.</summary>
        /// <returns>true if <paramref name="list1" /> is equal to <paramref name="list2" />; otherwise, false.</returns>
        /// <param name="list1">The <see cref="ChildSyntaxList" /> structure on the left side of the equality operator.</param>
        /// <param name="list2">The <see cref="ChildSyntaxList" /> structure on the right side of the equality operator.</param>
        public static bool operator ==(ChildSyntaxList list1, ChildSyntaxList list2)
        {
            return list1.Equals(list2);
        }

        /// <summary>Indicates whether two <see cref="ChildSyntaxList" /> structures are unequal.</summary>
        /// <returns>true if <paramref name="list1" /> is equal to <paramref name="list2" />; otherwise, false.</returns>
        /// <param name="list1">The <see cref="ChildSyntaxList" /> structure on the left side of the inequality operator.</param>
        /// <param name="list2">The <see cref="ChildSyntaxList" /> structure on the right side of the inequality operator.</param>
        public static bool operator !=(ChildSyntaxList list1, ChildSyntaxList list2)
        {
            return !list1.Equals(list2);
        }
    }
}