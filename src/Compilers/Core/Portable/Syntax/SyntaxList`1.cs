// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class SyntaxList
    {
        public static SyntaxList<TNode> Create<TNode>(ReadOnlySpan<TNode> nodes) where TNode : SyntaxNode
        {
            if (nodes.Length == 0)
                return default;

            return new SyntaxList<TNode>(createGreenNode(nodes).CreateRed());

            static GreenNode createGreenNode(ReadOnlySpan<TNode> nodes)
            {
                switch (nodes.Length)
                {
                    case 1: return nodes[0].Green;
                    case 2: return Syntax.InternalSyntax.SyntaxList.List(nodes[0].Green, nodes[1].Green);
                    case 3: return Syntax.InternalSyntax.SyntaxList.List(nodes[0].Green, nodes[1].Green, nodes[2].Green);
                    default:
                        {
                            var copy = new ArrayElement<GreenNode>[nodes.Length];
                            for (int i = 0, n = nodes.Length; i < n; i++)
                                copy[i].Value = nodes[i].Green;

                            return Syntax.InternalSyntax.SyntaxList.List(copy);
                        }
                }
            }
        }
    }

    /// <summary>
    /// A list of <see cref="SyntaxNode"/>.
    /// </summary>
    [CollectionBuilder(typeof(SyntaxList), methodName: "Create")]
    public readonly partial struct SyntaxList<TNode> : IReadOnlyList<TNode>, IEquatable<SyntaxList<TNode>>
        where TNode : SyntaxNode
    {
        private readonly SyntaxNode? _node;

        internal SyntaxList(SyntaxNode? node)
        {
            _node = node;
        }

        /// <summary>
        /// Creates a singleton list of syntax nodes.
        /// </summary>
        /// <param name="node">The single element node.</param>
        public SyntaxList(TNode? node)
            : this((SyntaxNode?)node)
        {
        }

        /// <summary>
        /// Creates a list of syntax nodes.
        /// </summary>
        /// <param name="nodes">A sequence of element nodes.</param>
        public SyntaxList(IEnumerable<TNode>? nodes)
            : this(CreateNode(nodes))
        {
        }

        private static SyntaxNode? CreateNode(IEnumerable<TNode>? nodes)
        {
            if (nodes == null)
            {
                return null;
            }

            var collection = nodes as ICollection<TNode>;
            var builder = (collection != null) ? new SyntaxListBuilder<TNode>(collection.Count) : SyntaxListBuilder<TNode>.Create();

            foreach (TNode node in nodes)
            {
                builder.Add(node);
            }

            return builder.ToList().Node;
        }

        internal SyntaxNode? Node
        {
            get
            {
                return _node;
            }
        }

        /// <summary>
        /// The number of nodes in the list.
        /// </summary>
        public int Count
        {
            get
            {
                return _node == null ? 0 : (_node.IsList ? _node.SlotCount : 1);
            }
        }

        /// <summary>
        /// Gets the node at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the node to get or set.</param>
        /// <returns>The node at the specified index.</returns>
        public TNode this[int index]
        {
            get
            {
                if (_node != null)
                {
                    if (_node.IsList)
                    {
                        if (unchecked((uint)index < (uint)_node.SlotCount))
                        {
                            return (TNode)_node.GetNodeSlot(index)!;
                        }
                    }
                    else if (index == 0)
                    {
                        return (TNode)_node;
                    }
                }
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        internal SyntaxNode? ItemInternal(int index)
        {
            if (_node?.IsList == true)
            {
                return _node.GetNodeSlot(index);
            }

            Debug.Assert(index == 0);
            return _node;
        }

        /// <summary>
        /// The absolute span of the list elements in characters, including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan FullSpan
        {
            get
            {
                if (this.Count == 0)
                {
                    return default(TextSpan);
                }
                else
                {
                    return TextSpan.FromBounds(this[0].FullSpan.Start, this[this.Count - 1].FullSpan.End);
                }
            }
        }

        /// <summary>
        /// The absolute span of the list elements in characters, not including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                if (this.Count == 0)
                {
                    return default(TextSpan);
                }
                else
                {
                    return TextSpan.FromBounds(this[0].Span.Start, this[this.Count - 1].Span.End);
                }
            }
        }

        /// <summary>
        /// Returns the string representation of the nodes in this list, not including 
        /// the first node's leading trivia and the last node's trailing trivia.
        /// </summary>
        /// <returns>
        /// The string representation of the nodes in this list, not including 
        /// the first node's leading trivia and the last node's trailing trivia.
        /// </returns>
        public override string ToString()
        {
            return _node != null ? _node.ToString() : string.Empty;
        }

        /// <summary>
        /// Returns the full string representation of the nodes in this list including 
        /// the first node's leading trivia and the last node's trailing trivia.
        /// </summary>
        /// <returns>
        /// The full string representation of the nodes in this list including 
        /// the first node's leading trivia and the last node's trailing trivia.
        /// </returns>
        public string ToFullString()
        {
            return _node != null ? _node.ToFullString() : string.Empty;
        }

        /// <summary>
        /// Creates a new list with the specified node added at the end.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public SyntaxList<TNode> Add(TNode node)
        {
            return this.Insert(this.Count, node);
        }

        /// <summary>
        /// Creates a new list with the specified nodes added at the end.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        public SyntaxList<TNode> AddRange(IEnumerable<TNode> nodes)
        {
            return this.InsertRange(this.Count, nodes);
        }

        /// <summary>
        /// Creates a new list with the specified node inserted at the index.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="node">The node to insert.</param>
        public SyntaxList<TNode> Insert(int index, TNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return InsertRange(index, new[] { node });
        }

        /// <summary>
        /// Creates a new list with the specified nodes inserted at the index.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="nodes">The nodes to insert.</param>
        public SyntaxList<TNode> InsertRange(int index, IEnumerable<TNode> nodes)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            var list = this.ToList();
            list.InsertRange(index, nodes);

            if (list.Count == 0)
            {
                return this;
            }
            else
            {
                return CreateList(list);
            }
        }

        /// <summary>
        /// Creates a new list with the element at specified index removed.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        public SyntaxList<TNode> RemoveAt(int index)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.Remove(this[index]);
        }

        /// <summary>
        /// Creates a new list with the element removed.
        /// </summary>
        /// <param name="node">The element to remove.</param>
        public SyntaxList<TNode> Remove(TNode node)
        {
            return CreateList(this.Where(x => x != node).ToList());
        }

        /// <summary>
        /// Creates a new list with the specified element replaced with the new node.
        /// </summary>
        /// <param name="nodeInList">The element to replace.</param>
        /// <param name="newNode">The new node.</param>
        public SyntaxList<TNode> Replace(TNode nodeInList, TNode newNode)
        {
            return ReplaceRange(nodeInList, new[] { newNode });
        }

        /// <summary>
        /// Creates a new list with the specified element replaced with new nodes.
        /// </summary>
        /// <param name="nodeInList">The element to replace.</param>
        /// <param name="newNodes">The new nodes.</param>
        public SyntaxList<TNode> ReplaceRange(TNode nodeInList, IEnumerable<TNode> newNodes)
        {
            if (nodeInList == null)
            {
                throw new ArgumentNullException(nameof(nodeInList));
            }

            if (newNodes == null)
            {
                throw new ArgumentNullException(nameof(newNodes));
            }

            var index = this.IndexOf(nodeInList);
            if (index >= 0 && index < this.Count)
            {
                var list = this.ToList();
                list.RemoveAt(index);
                list.InsertRange(index, newNodes);
                return CreateList(list);
            }
            else
            {
                throw new ArgumentException(nameof(nodeInList));
            }
        }

        private static SyntaxList<TNode> CreateList(List<TNode> items)
        {
            if (items.Count == 0)
            {
                return default(SyntaxList<TNode>);
            }

            var newGreen = GreenNode.CreateList(items, static n => n.Green);
            return new SyntaxList<TNode>(newGreen!.CreateRed());
        }

        /// <summary>
        /// The first node in the list.
        /// </summary>
        public TNode First()
        {
            return this[0];
        }

        /// <summary>
        /// The first node in the list or default if the list is empty.
        /// </summary>
        public TNode? FirstOrDefault()
        {
            if (this.Any())
            {
                return this[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// The last node in the list.
        /// </summary>
        public TNode Last()
        {
            return this[this.Count - 1];
        }

        /// <summary>
        /// The last node in the list or default if the list is empty.
        /// </summary>
        public TNode? LastOrDefault()
        {
            if (this.Any())
            {
                return this[this.Count - 1];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// True if the list has at least one node.
        /// </summary>
        public bool Any()
        {
            Debug.Assert(_node == null || Count != 0);
            return _node != null;
        }

        internal bool All(Func<TNode, bool> predicate)
        {
            foreach (var item in this)
            {
                if (!predicate(item))
                {
                    return false;
                }
            }

            return true;
        }

        // for debugging
#pragma warning disable IDE0051 // Remove unused private members
        private TNode[] Nodes
#pragma warning restore IDE0051 // Remove unused private members
        {
            get { return this.ToArray(); }
        }

        /// <summary>
        /// Get's the enumerator for this list.
        /// </summary>
#pragma warning disable RS0041 // uses oblivious reference types
        public Enumerator GetEnumerator()
#pragma warning restore RS0041 // uses oblivious reference types
        {
            return new Enumerator(this);
        }

        IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
        {
            if (this.Any())
            {
                return new EnumeratorImpl(this);
            }

            return SpecializedCollections.EmptyEnumerator<TNode>();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.Any())
            {
                return new EnumeratorImpl(this);
            }

            return SpecializedCollections.EmptyEnumerator<TNode>();
        }

        public static bool operator ==(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return left._node == right._node;
        }

        public static bool operator !=(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return left._node != right._node;
        }

        public bool Equals(SyntaxList<TNode> other)
        {
            return _node == other._node;
        }

        public override bool Equals(object? obj)
        {
            return obj is SyntaxList<TNode> && Equals((SyntaxList<TNode>)obj);
        }

        public override int GetHashCode()
        {
            return _node?.GetHashCode() ?? 0;
        }

        [Obsolete("This method is preserved for binary compatibility only. Use explicit cast instead.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static SyntaxList<TNode> op_Implicit(SyntaxList<SyntaxNode> nodes)
        {
            return new SyntaxList<TNode>(nodes._node);
        }

        public static implicit operator SyntaxList<SyntaxNode>(SyntaxList<TNode> nodes)
        {
            return new SyntaxList<SyntaxNode>(nodes.Node);
        }

        public static explicit operator SyntaxList<TNode>(SyntaxList<SyntaxNode> nodes)
        {
            return new SyntaxList<TNode>(nodes._node);
        }

        /// <summary>
        /// The index of the node in this list, or -1 if the node is not in the list.
        /// </summary>
        public int IndexOf(TNode node)
        {
            var index = 0;
            foreach (var child in this)
            {
                if (object.Equals(child, node))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public int IndexOf(Func<TNode, bool> predicate)
        {
            var index = 0;
            foreach (var child in this)
            {
                if (predicate(child))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        internal int IndexOf(int rawKind)
        {
            var index = 0;
            foreach (var child in this)
            {
                if (child.RawKind == rawKind)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public int LastIndexOf(TNode node)
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (object.Equals(this[i], node))
                {
                    return i;
                }
            }

            return -1;
        }

        public int LastIndexOf(Func<TNode, bool> predicate)
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (predicate(this[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
