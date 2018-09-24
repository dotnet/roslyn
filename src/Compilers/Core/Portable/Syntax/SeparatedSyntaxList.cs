// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public readonly partial struct SeparatedSyntaxList<TNode> : IEquatable<SeparatedSyntaxList<TNode>>, IReadOnlyList<TNode> where TNode : SyntaxNode
    {
        private readonly SyntaxNodeOrTokenList _list;
        private readonly int _count;
        private readonly int _separatorCount;

        internal SeparatedSyntaxList(SyntaxNodeOrTokenList list)
            : this()
        {
            Validate(list);

            // calculating counts is very cheap when list interleaves nodes and tokens
            // so lets just do it here.

            int allCount = list.Count;
            _count = (allCount + 1) >> 1;
            _separatorCount = allCount >> 1;

            _list = list;
        }

        [Conditional("DEBUG")]
        private static void Validate(SyntaxNodeOrTokenList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if ((i & 1) == 0)
                {
                    Debug.Assert(item.IsNode, "Node missing in separated list.");
                }
                else
                {
                    Debug.Assert(item.IsToken, "Separator token missing in separated list.");
                }
            }
        }

        internal SeparatedSyntaxList(SyntaxNode node, int index)
            : this(new SyntaxNodeOrTokenList(node, index))
        {
        }

        internal SyntaxNode Node
        {
            get
            {
                return _list.Node;
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public int SeparatorCount
        {
            get
            {
                return _separatorCount;
            }
        }

        public TNode this[int index]
        {
            get
            {
                var node = _list.Node;
                if (node != null)
                {
                    if (!node.IsList)
                    {
                        if (index == 0)
                        {
                            return (TNode)node;
                        }
                    }
                    else
                    {
                        if (unchecked((uint)index < (uint)_count))
                        {
                            return (TNode)node.GetNodeSlot(index << 1);
                        }
                    }
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>
        /// Gets the separator at the given index in this list.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public SyntaxToken GetSeparator(int index)
        {
            var node = _list.Node;
            if (node != null)
            {
                Debug.Assert(node.IsList, "separated list cannot be a singleton separator");
                if (unchecked((uint)index < (uint)_separatorCount))
                {
                    index = (index << 1) + 1;
                    var green = node.Green.GetSlot(index);
                    Debug.Assert(green.IsToken);
                    return new SyntaxToken(node.Parent, green, node.GetChildPosition(index), _list.index + index);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Returns the sequence of just the separator tokens.
        /// </summary>
        public IEnumerable<SyntaxToken> GetSeparators()
        {
            return _list.Where(n => n.IsToken).Select(n => n.AsToken());
        }

        /// <summary>
        /// The absolute span of the list elements in characters, including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan FullSpan
        {
            get { return _list.FullSpan; }
        }

        /// <summary>
        /// The absolute span of the list elements in characters, not including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan Span
        {
            get { return _list.Span; }
        }

        /// <summary>
        /// Returns the string representation of the nodes in this list including separators but not including 
        /// the first node's leading trivia and the last node or token's trailing trivia.
        /// </summary>
        /// <returns>
        /// The string representation of the nodes in this list including separators but not including 
        /// the first node's leading trivia and the last node or token's trailing trivia.
        /// </returns>
        public override string ToString()
        {
            return _list.ToString();
        }

        /// <summary>
        /// Returns the full string representation of the nodes in this list including separators, 
        /// the first node's leading trivia, and the last node or token's trailing trivia.
        /// </summary>
        /// <returns>
        /// The full string representation of the nodes in this list including separators including separators,
        /// the first node's leading trivia, and the last node or token's trailing trivia.
        /// </returns>
        public string ToFullString()
        {
            return _list.ToFullString();
        }

        public TNode First()
        {
            return this[0];
        }

        public TNode FirstOrDefault()
        {
            if (this.Any())
            {
                return this[0];
            }

            return null;
        }

        public TNode Last()
        {
            return this[this.Count - 1];
        }

        public TNode LastOrDefault()
        {
            if (this.Any())
            {
                return this[this.Count - 1];
            }

            return null;
        }

        public bool Contains(TNode node)
        {
            return this.IndexOf(node) >= 0;
        }

        public int IndexOf(TNode node)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (object.Equals(this[i], node))
                {
                    return i;
                }
            }

            return -1;
        }

        public int IndexOf(Func<TNode, bool> predicate)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (predicate(this[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        internal int IndexOf(int rawKind)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (this[i].RawKind == rawKind)
                {
                    return i;
                }
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

        public bool Any()
        {
            return _list.Any();
        }

        internal bool Any(Func<TNode, bool> predicate)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (predicate(this[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public SyntaxNodeOrTokenList GetWithSeparators()
        {
            return _list;
        }

        public static bool operator ==(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right)
        {
            return !left.Equals(right);
        }

        public bool Equals(SeparatedSyntaxList<TNode> other)
        {
            return _list == other._list;
        }

        public override bool Equals(object obj)
        {
            return (obj is SeparatedSyntaxList<TNode>) && Equals((SeparatedSyntaxList<TNode>)obj);
        }

        public override int GetHashCode()
        {
            return _list.GetHashCode();
        }

        /// <summary>
        /// Creates a new list with the specified node added to the end.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public SeparatedSyntaxList<TNode> Add(TNode node)
        {
            return Insert(this.Count, node);
        }

        /// <summary>
        /// Creates a new list with the specified nodes added to the end.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        public SeparatedSyntaxList<TNode> AddRange(IEnumerable<TNode> nodes)
        {
            return InsertRange(this.Count, nodes);
        }

        /// <summary>
        /// Creates a new list with the specified node inserted at the index.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="node">The node to insert.</param>
        public SeparatedSyntaxList<TNode> Insert(int index, TNode node)
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
        public SeparatedSyntaxList<TNode> InsertRange(int index, IEnumerable<TNode> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var nodesWithSeps = this.GetWithSeparators();
            int insertionIndex = index < this.Count ? nodesWithSeps.IndexOf(this[index]) : nodesWithSeps.Count;

            // determine how to deal with separators (commas)
            if (insertionIndex > 0 && insertionIndex < nodesWithSeps.Count)
            {
                var previous = nodesWithSeps[insertionIndex - 1];
                if (previous.IsToken && !KeepSeparatorWithPreviousNode(previous.AsToken()))
                {
                    // pull back so item in inserted before separator
                    insertionIndex--;
                }
            }

            var nodesToInsertWithSeparators = new List<SyntaxNodeOrToken>();
            foreach (var item in nodes)
            {
                if (item != null)
                {
                    // if item before insertion point is a node, add a separator
                    if (nodesToInsertWithSeparators.Count > 0 || (insertionIndex > 0 && nodesWithSeps[insertionIndex - 1].IsNode))
                    {
                        nodesToInsertWithSeparators.Add(item.Green.CreateSeparator<TNode>(item));
                    }

                    nodesToInsertWithSeparators.Add(item);
                }
            }

            // if item after last inserted node is a node, add separator
            if (insertionIndex < nodesWithSeps.Count && nodesWithSeps[insertionIndex].IsNode)
            {
                var node = nodesWithSeps[insertionIndex].AsNode();
                nodesToInsertWithSeparators.Add(node.Green.CreateSeparator<TNode>(node)); // separator
            }

            return new SeparatedSyntaxList<TNode>(nodesWithSeps.InsertRange(insertionIndex, nodesToInsertWithSeparators));
        }

        private static bool KeepSeparatorWithPreviousNode(in SyntaxToken separator)
        {
            // if the trivia after the separator contains an explicit end of line or a single line comment
            // then it should stay associated with previous node
            foreach (var tr in separator.TrailingTrivia)
            {
                if (tr.UnderlyingNode.IsTriviaWithEndOfLine())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new list with the element at the specified index removed.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        public SeparatedSyntaxList<TNode> RemoveAt(int index)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.Remove(this[index]);
        }

        /// <summary>
        /// Creates a new list with specified element removed.
        /// </summary>
        /// <param name="node">The element to remove.</param>
        public SeparatedSyntaxList<TNode> Remove(TNode node)
        {
            var nodesWithSeps = this.GetWithSeparators();
            int index = nodesWithSeps.IndexOf(node);

            if (index >= 0 && index <= nodesWithSeps.Count)
            {
                nodesWithSeps = nodesWithSeps.RemoveAt(index);

                // remove separator too
                if (index < nodesWithSeps.Count && nodesWithSeps[index].IsToken)
                {
                    nodesWithSeps = nodesWithSeps.RemoveAt(index);
                }
                else if (index > 0 && nodesWithSeps[index - 1].IsToken)
                {
                    nodesWithSeps = nodesWithSeps.RemoveAt(index - 1);
                }

                return new SeparatedSyntaxList<TNode>(nodesWithSeps);
            }

            return this;
        }

        /// <summary>
        /// Creates a new list with the specified element replaced by the new node.
        /// </summary>
        /// <param name="nodeInList">The element to replace.</param>
        /// <param name="newNode">The new node.</param>
        public SeparatedSyntaxList<TNode> Replace(TNode nodeInList, TNode newNode)
        {
            if (newNode == null)
            {
                throw new ArgumentNullException(nameof(newNode));
            }

            var index = this.IndexOf(nodeInList);
            if (index >= 0 && index < this.Count)
            {
                return new SeparatedSyntaxList<TNode>(this.GetWithSeparators().Replace(nodeInList, newNode));
            }

            throw new ArgumentOutOfRangeException(nameof(nodeInList));
        }

        /// <summary>
        /// Creates a new list with the specified element replaced by the new nodes.
        /// </summary>
        /// <param name="nodeInList">The element to replace.</param>
        /// <param name="newNodes">The new nodes.</param>
        public SeparatedSyntaxList<TNode> ReplaceRange(TNode nodeInList, IEnumerable<TNode> newNodes)
        {
            if (newNodes == null)
            {
                throw new ArgumentNullException(nameof(newNodes));
            }

            var index = this.IndexOf(nodeInList);
            if (index >= 0 && index < this.Count)
            {
                var newNodeList = newNodes.ToList();
                if (newNodeList.Count == 0)
                {
                    return this.Remove(nodeInList);
                }

                var listWithFirstReplaced = this.Replace(nodeInList, newNodeList[0]);

                if (newNodeList.Count > 1)
                {
                    newNodeList.RemoveAt(0);
                    return listWithFirstReplaced.InsertRange(index + 1, newNodeList);
                }

                return listWithFirstReplaced;
            }

            throw new ArgumentOutOfRangeException(nameof(nodeInList));
        }

        /// <summary>
        /// Creates a new list with the specified separator token replaced with the new separator.
        /// </summary>
        /// <param name="separatorToken">The separator token to be replaced.</param>
        /// <param name="newSeparator">The new separator token.</param>
        public SeparatedSyntaxList<TNode> ReplaceSeparator(SyntaxToken separatorToken, SyntaxToken newSeparator)
        {
            var nodesWithSeps = this.GetWithSeparators();
            var index = nodesWithSeps.IndexOf(separatorToken);
            if (index < 0)
            {
                throw new ArgumentException("separatorToken");
            }

            if (newSeparator.RawKind != nodesWithSeps[index].RawKind ||
                newSeparator.Language != nodesWithSeps[index].Language)
            {
                throw new ArgumentException("newSeparator");
            }

            return new SeparatedSyntaxList<TNode>(nodesWithSeps.Replace(separatorToken, newSeparator));
        }

        // for debugging
        private TNode[] Nodes
        {
            get { return this.ToArray(); }
        }

        private SyntaxNodeOrToken[] NodesWithSeparators
        {
            get { return _list.ToArray(); }
        }

        public Enumerator GetEnumerator()
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

        public static implicit operator SeparatedSyntaxList<SyntaxNode>(SeparatedSyntaxList<TNode> nodes)
        {
            return new SeparatedSyntaxList<SyntaxNode>(nodes._list);
        }

        public static implicit operator SeparatedSyntaxList<TNode>(SeparatedSyntaxList<SyntaxNode> nodes)
        {
            return new SeparatedSyntaxList<TNode>(nodes._list);
        }
    }
}
