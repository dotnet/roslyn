﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    ///  Represents a read-only list of <see cref="SyntaxTrivia"/>.
    /// </summary>
    public partial struct SyntaxTriviaList : IEquatable<SyntaxTriviaList>, IReadOnlyList<SyntaxTrivia>
    {
        private readonly SyntaxToken _token;
        private readonly GreenNode _node;
        private readonly int _position;
        private readonly int _index;

        public static readonly SyntaxTriviaList Empty = default(SyntaxTriviaList);

        internal SyntaxTriviaList(SyntaxToken token, GreenNode node, int position, int index = 0)
        {
            _token = token;
            _node = node;
            _position = position;
            _index = index;
        }

        internal SyntaxTriviaList(SyntaxToken token, GreenNode node)
        {
            _token = token;
            _node = node;
            _position = token.Position;
            _index = 0;
        }

        internal SyntaxTriviaList(SyntaxTrivia trivia)
        {
            _token = default(SyntaxToken);
            _node = trivia.UnderlyingNode;
            _position = 0;
            _index = 0;
        }

        internal SyntaxToken Token
        {
            get { return _token; }
        }

        internal GreenNode Node
        {
            get { return _node; }
        }

        internal int Position
        {
            get { return _position; }
        }

        internal int Index
        {
            get { return _index; }
        }

        public int Count
        {
            get { return _node == null ? 0 : (_node.IsList ? _node.SlotCount : 1); }
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
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///   <paramref name="index" /> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="SyntaxTriviaList.Count" />. </exception>
        public SyntaxTrivia this[int index]
        {
            get
            {
                if (_node != null)
                {
                    if (_node.IsList)
                    {
                        if (unchecked((uint)index < (uint)_node.SlotCount))
                        {
                            return new SyntaxTrivia(_token, _node.GetSlot(index), _position + _node.GetSlotOffset(index), _index + index);
                        }
                    }
                    else if (index == 0)
                    {
                        return new SyntaxTrivia(_token, _node, _position, _index);
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
                if (_node == null)
                {
                    return default(TextSpan);
                }
                else
                {
                    return new TextSpan(this.Position, _node.FullWidth);
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
                if (_node == null)
                {
                    return default(TextSpan);
                }
                else
                {
                    return TextSpan.FromBounds(_position + _node.GetLeadingTriviaWidth(),
                                               _position + _node.FullWidth - _node.GetTrailingTriviaWidth());
                }
            }
        }

        /// <summary>
        /// Returns the first trivia in the list.
        /// </summary>
        /// <returns>The first trivia in the list.</returns>
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>        
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
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>        
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
        /// <returns></returns>
        public bool Any()
        {
            return _node != null;
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
                throw new ArgumentException("trivia");
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
            else
            {
                return new SyntaxTriviaList(default(SyntaxToken), list[0].UnderlyingNode.CreateList(list.Select(n => n.UnderlyingNode)), 0, 0);
            }
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
            return new SyntaxTriviaList(default(SyntaxToken), _node.CreateList(list.Select(n => n.UnderlyingNode)), 0, 0);
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
            else
            {
                return this;
            }
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
                throw new ArgumentException("newTrivia");
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
                return new SyntaxTriviaList(default(SyntaxToken), _node.CreateList(list.Select(n => n.UnderlyingNode)), 0, 0);
            }
            else
            {
                throw new ArgumentException("triviaInList");
            }
        }

        // for debugging
        private SyntaxTrivia[] Nodes
        {
            get { return this.ToArray(); }
        }

        IEnumerator<SyntaxTrivia> IEnumerable<SyntaxTrivia>.GetEnumerator()
        {
            if (_node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
            }

            return new EnumeratorImpl(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_node == null)
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
            return GetGreenNodeAt(_node, i);
        }

        private static GreenNode GetGreenNodeAt(GreenNode node, int i)
        {
            Debug.Assert(node.IsList || (i == 0 && !node.IsList));
            return node.IsList ? node.GetSlot(i) : node;
        }

        public bool Equals(SyntaxTriviaList other)
        {
            return _node == other._node && _index == other._index && _token.Equals(other._token);
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
            return Hash.Combine(_token.GetHashCode(), Hash.Combine(_node, _index));
        }

        /// <summary>
        /// Copy count number of items starting at offset from this list into array starting at arrayOffset.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="array"></param>
        /// <param name="arrayOffset"></param>
        /// <param name="count"></param>
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
                current = new SyntaxTrivia(_token, GetGreenNodeAt(offset + i), position, _index + i);

                array[arrayOffset + i] = current;
            }
        }

        public override string ToString()
        {
            return _node != null ? _node.ToString() : String.Empty;
        }

        public string ToFullString()
        {
            return _node != null ? _node.ToFullString() : String.Empty;
        }

        public static SyntaxTriviaList Create(SyntaxTrivia trivia)
        {
            return new SyntaxTriviaList(trivia);
        }
    }
}
