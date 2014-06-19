using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
#if REMOVE
    /// <summary>
    /// A list of child SyntaxNodeOrToken structs.
    /// </summary>
    public partial struct ChildSyntaxList : IReadOnlyCollection<SyntaxNodeOrToken>, IEquatable<ChildSyntaxList>
    {
        /// <summary>
        /// The underlying syntax node
        /// </summary>
        private readonly CSharpSyntaxNode node;

        /// <summary>
        /// The count of elements in this list
        /// </summary>
        private readonly int count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildSyntaxList"/> struct.
        /// </summary>
        /// <param name="node">The underlying syntax node.</param>
        internal ChildSyntaxList(CSharpSyntaxNode node)
        {
            Debug.Assert(node == null || node.Kind != SyntaxKind.List);
            this.node = node;
            this.count = CountNodes(node.Green);
        }

        /// <summary>
        /// Gets the count of elements in this list
        /// </summary>
        public int Count
        {
            get
            {
                return this.count;
            }
        }

        /// <summary>
        /// Gets the underlying node.
        /// </summary>
        internal CSharpSyntaxNode Node
        {
            get
            {
                return this.node;
            }
        }

        /// <summary>
        /// Counts the nodes.
        /// </summary>
        /// <param name="green">The green.</param>
        /// <returns></returns>
        private static int CountNodes(Syntax.InternalSyntax.CSharpSyntaxNode green)
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

        /// <summary>
        /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.SyntaxNodeOrToken"/> at the specified index.
        /// </summary>
        /// 
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
        public SyntaxNodeOrToken ElementAt(int index)
        {
            if (index < 0 || index >= this.count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return this[index];
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.CodeAnalysis.CSharp.SyntaxNodeOrToken"/> at the specified index.
        /// </summary>
        /// 
        /// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is out of range</exception>
        internal SyntaxNodeOrToken this[int index]
        {
            get
            {
                return ItemInternal(node, index);
            }
        }

        private static int Occupancy(Syntax.InternalSyntax.CSharpSyntaxNode greenChild)
        {
            return greenChild.IsList
                ? greenChild.SlotCount
                : 1;
        }

        /// <summary>
        /// internal indexer that does not verify index.
        /// Used when caller has already ensured that index is within bounds.
        /// </summary>
#if DEBUG
        internal static SyntaxNodeOrToken ItemInternal(CSharpSyntaxNode node, int index, bool fromTokenCtor = false)
#else
        internal static SyntaxNodeOrToken ItemInternal(CSharpSyntaxNode node, int index)
#endif
        {
            Syntax.InternalSyntax.CSharpSyntaxNode greenChild;
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

#if DEBUG
            return new SyntaxNodeOrToken(node, (Syntax.InternalSyntax.SyntaxToken)greenChild, position, index, fromTokenCtor);
#else
            return new SyntaxNodeOrToken(node, (Syntax.InternalSyntax.SyntaxToken)greenChild, position, index);
#endif
        }

        // for debugging
        private SyntaxNodeOrToken[] Nodes
        {
            get
            {
                return this.ToArray();
            }
        }

        /// <summary>
        /// Returns the reversed list.
        /// </summary>
        /// <returns></returns>
        public Reversed Reverse()
        {
            return new Reversed(this.node, this.count);
        }

        /// <summary>
        /// Gets an enumerator that iterates through the collection.
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            if (this.node == null)
            {
                return default(Enumerator);
            }

            return new Enumerator(node, count);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
            }

            return new EnumeratorImpl(node, count);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
            }

            return new EnumeratorImpl(node, count);
        }

        public override bool Equals(object obj)
        {
            return obj is ChildSyntaxList && Equals((ChildSyntaxList)obj);
        }

        public bool Equals(ChildSyntaxList other)
        {
            return this.node == other.node;
        }

        public override int GetHashCode()
        {
            return node == null ? 0 : node.GetHashCode();
        }
    }
#endif
}
