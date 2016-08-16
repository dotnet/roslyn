// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a read-only list of <see cref="SyntaxTrivia"/>.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public partial struct SyntaxTriviaList : IEquatable<SyntaxTriviaList>, IReadOnlyList<SyntaxTrivia>
    {
        public static SyntaxTriviaList Empty => default(SyntaxTriviaList);

        internal SyntaxTriviaList(SyntaxToken token, GreenNode node, int position, int index = 0)
        {
            Token = token;
            Node = node;
            Position = position;
            Index = index;
        }

        internal SyntaxTriviaList(SyntaxToken token, GreenNode node)
        {
            Token = token;
            Node = node;
            Position = token.Position;
            Index = 0;
        }

        internal SyntaxTriviaList(SyntaxTrivia trivia)
        {
            Token = default(SyntaxToken);
            Node = trivia.UnderlyingNode;
            Position = 0;
            Index = 0;
        }

        internal SyntaxToken Token { get; }

        internal GreenNode Node { get; }

        internal int Position { get; }

        internal int Index { get; }

        public int Count
        {
            get { return Node == null ? 0 : (Node.IsList ? Node.SlotCount : 1); }
        }

        public SyntaxTrivia ElementAt(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Gets the trivia at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the trivia to get.</param>
        /// <returns>The token at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="Count" />. </exception>
        public SyntaxTrivia this[int index]
        {
            get
            {
                if (Node != null)
                {
                    if (Node.IsList)
                    {
                        if (unchecked((uint)index < (uint)Node.SlotCount))
                        {
                            return new SyntaxTrivia(Token, Node.GetSlot(index), Position + Node.GetSlotOffset(index), Index + index);
                        }
                    }
                    else if (index == 0)
                    {
                        return new SyntaxTrivia(Token, Node, Position, Index);
                    }
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>
        /// The absolute span of the list elements in characters, including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan FullSpan
        {
            get
            {
                if (Node == null)
                {
                    return default(TextSpan);
                }

                return new TextSpan(this.Position, Node.FullWidth);
            }
        }

        /// <summary>
        /// The absolute span of the list elements in characters, not including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                if (Node == null)
                {
                    return default(TextSpan);
                }

                return TextSpan.FromBounds(Position + Node.GetLeadingTriviaWidth(),
                    Position + Node.FullWidth - Node.GetTrailingTriviaWidth());
            }
        }

        /// <summary>
        /// Returns the first trivia in the list.
        /// </summary>
        /// <returns>The first trivia in the list.</returns>
        /// <exception cref="InvalidOperationException">The list is empty.</exception>        
        public SyntaxTrivia First()
        {
            if (Any())
            {
                return this[0];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns the last trivia in the list.
        /// </summary>
        /// <returns>The last trivia in the list.</returns>
        /// <exception cref="InvalidOperationException">The list is empty.</exception>        
        public SyntaxTrivia Last()
        {
            if (Any())
            {
                return this[this.Count - 1];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Does this list have any items.
        /// </summary>
        public bool Any()
        {
            return Node != null;
        }

        /// <summary>
        /// Returns a list which contains all elements of <see cref="SyntaxTriviaList"/> in reversed order.
        /// </summary>
        /// <returns><see cref="Reversed"/> which contains all elements of <see cref="SyntaxTriviaList"/> in reversed order</returns>
        public Reversed Reverse()
        {
            return new Reversed(this);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        public int IndexOf(SyntaxTrivia triviaInList)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                var trivia = this[i];
                if (trivia == triviaInList)
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

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified trivia added to the end.
        /// </summary>
        /// <param name="trivia">The trivia to add.</param>
        public SyntaxTriviaList Add(SyntaxTrivia trivia)
        {
            return Insert(this.Count, trivia);
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified trivia added to the end.
        /// </summary>
        /// <param name="trivia">The trivia to add.</param>
        public SyntaxTriviaList AddRange(IEnumerable<SyntaxTrivia> trivia)
        {
            return InsertRange(this.Count, trivia);
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified trivia inserted at the index.
        /// </summary>
        /// <param name="index">The index in the list to insert the trivia at.</param>
        /// <param name="trivia">The trivia to insert.</param>
        public SyntaxTriviaList Insert(int index, SyntaxTrivia trivia)
        {
            if (trivia == default(SyntaxTrivia))
            {
                throw new ArgumentOutOfRangeException(nameof(trivia));
            }

            return InsertRange(index, new[] { trivia });
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified trivia inserted at the index.
        /// </summary>
        /// <param name="index">The index in the list to insert the trivia at.</param>
        /// <param name="trivia">The trivia to insert.</param>
        public SyntaxTriviaList InsertRange(int index, IEnumerable<SyntaxTrivia> trivia)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var items = trivia.ToList();
            if (items.Count == 0)
            {
                return this;
            }

            var list = this.ToList();
            list.InsertRange(index, items);

            if (list.Count == 0)
            {
                return this;
            }

            return new SyntaxTriviaList(default(SyntaxToken), list[0].UnderlyingNode.CreateList(list.Select(n => n.UnderlyingNode)), 0, 0);
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the element at the specified index removed.
        /// </summary>
        /// <param name="index">The index identifying the element to remove.</param>
        public SyntaxTriviaList RemoveAt(int index)
        {
            if (index < 0 || index >= this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var list = this.ToList();
            list.RemoveAt(index);
            return new SyntaxTriviaList(default(SyntaxToken), Node.CreateList(list.Select(n => n.UnderlyingNode)), 0, 0);
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified element removed.
        /// </summary>
        /// <param name="triviaInList">The trivia element to remove.</param>
        public SyntaxTriviaList Remove(SyntaxTrivia triviaInList)
        {
            var index = this.IndexOf(triviaInList);
            if (index >= 0 && index < this.Count)
            {
                return this.RemoveAt(index);
            }

            return this;
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified element replaced with new trivia.
        /// </summary>
        /// <param name="triviaInList">The trivia element to replace.</param>
        /// <param name="newTrivia">The trivia to replace the element with.</param>
        public SyntaxTriviaList Replace(SyntaxTrivia triviaInList, SyntaxTrivia newTrivia)
        {
            if (newTrivia == default(SyntaxTrivia))
            {
                throw new ArgumentOutOfRangeException(nameof(newTrivia));
            }

            return ReplaceRange(triviaInList, new[] { newTrivia });
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified element replaced with new trivia.
        /// </summary>
        /// <param name="triviaInList">The trivia element to replace.</param>
        /// <param name="newTrivia">The trivia to replace the element with.</param>
        public SyntaxTriviaList ReplaceRange(SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia)
        {
            var index = this.IndexOf(triviaInList);
            if (index >= 0 && index < this.Count)
            {
                var list = this.ToList();
                list.RemoveAt(index);
                list.InsertRange(index, newTrivia);
                return new SyntaxTriviaList(default(SyntaxToken), Node.CreateList(list.Select(n => n.UnderlyingNode)), 0, 0);
            }

            throw new ArgumentOutOfRangeException(nameof(triviaInList));
        }

        // for debugging
        private SyntaxTrivia[] Nodes => this.ToArray();

        IEnumerator<SyntaxTrivia> IEnumerable<SyntaxTrivia>.GetEnumerator()
        {
            if (Node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
            }

            return new EnumeratorImpl(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (Node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
            }

            return new EnumeratorImpl(ref this);
        }

        /// <summary>
        /// get the green node at the specific slot
        /// </summary>
        private GreenNode GetGreenNodeAt(int i)
        {
            return GetGreenNodeAt(Node, i);
        }

        private static GreenNode GetGreenNodeAt(GreenNode node, int i)
        {
            Debug.Assert(node.IsList || (i == 0 && !node.IsList));
            return node.IsList ? node.GetSlot(i) : node;
        }

        public bool Equals(SyntaxTriviaList other)
        {
            return Node == other.Node && Index == other.Index && Token.Equals(other.Token);
        }

        public static bool operator ==(SyntaxTriviaList left, SyntaxTriviaList right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SyntaxTriviaList left, SyntaxTriviaList right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return (obj is SyntaxTriviaList) && Equals((SyntaxTriviaList)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Token.GetHashCode(), Hash.Combine(Node, Index));
        }

        /// <summary>
        /// Copy <paramref name="count"/> number of items starting at <paramref name="offset"/> from this list into <paramref name="array"/> starting at <paramref name="arrayOffset"/>.
        /// </summary>
        internal void CopyTo(int offset, SyntaxTrivia[] array, int arrayOffset, int count)
        {
            if (offset < 0 || count < 0 || this.Count < offset + count)
            {
                throw new IndexOutOfRangeException();
            }

            if (count == 0)
            {
                return;
            }

            // get first one without creating any red node
            var first = this[offset];
            array[arrayOffset] = first;

            // calculate trivia position from the first ourselves from now on
            var position = first.Position;
            var current = first;

            for (int i = 1; i < count; i++)
            {
                position += current.FullWidth;
                current = new SyntaxTrivia(Token, GetGreenNodeAt(offset + i), position, Index + i);

                array[arrayOffset + i] = current;
            }
        }

        public override string ToString()
        {
            return Node != null ? Node.ToString() : string.Empty;
        }

        public string ToFullString()
        {
            return Node != null ? Node.ToFullString() : string.Empty;
        }

        public static SyntaxTriviaList Create(SyntaxTrivia trivia)
        {
            return new SyntaxTriviaList(trivia);
        }
    }
}
