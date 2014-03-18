// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    ///  Represents a read-only list of <see cref="SyntaxToken"/>.
    /// </summary>
    public partial struct SyntaxTokenList : IEquatable<SyntaxTokenList>, IReadOnlyList<SyntaxToken>
    {
        private readonly SyntaxNode parent;
        private readonly GreenNode node;
        private readonly int position;
        private readonly int index;

        internal SyntaxTokenList(SyntaxNode parent, GreenNode tokenOrList, int position, int index)
        {
            Debug.Assert(tokenOrList != null || (position == 0 && index == 0 && parent == null));
            Debug.Assert(position >= 0);
            Debug.Assert(tokenOrList == null || (tokenOrList.IsToken) || (tokenOrList.IsList));
            this.parent = parent;
            this.node = tokenOrList;
            this.position = position;
            this.index = index;
        }

        internal SyntaxTokenList(SyntaxToken token)
        {
            this.parent = token.Parent;
            this.node = token.Node;
            this.position = token.Position;
            this.index = 0;
        }

        internal GreenNode Node
        {
            get
            {
                return this.node;
            }
        }

        internal int Position
        {
            get
            {
                return this.position;
            }
        }

        /// <summary>
        /// Returns the number of tokens in the list.
        /// </summary>
        public int Count
        {
            get
            {
                return node == null ? 0 : (node.IsList ? node.SlotCount : 1);
            }
        }

        /// <summary>
        /// Gets the token at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the token to get.</param>
        /// <returns>The token at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///   <paramref name="index" /> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="SyntaxTokenList.Count" />. </exception>
        public SyntaxToken this[int index]
        {
            get
            {
                if (node != null)
                {
                    if (node.IsList)
                    {
                        if (unchecked((uint)index < (uint)node.SlotCount))
                        {
                            return new SyntaxToken(this.parent, node.GetSlot(index), this.position + node.GetSlotOffset(index), this.index + index);
                        }
                    }
                    else if (index == 0)
                    {
                        return new SyntaxToken(this.parent, this.node, this.position, this.index);
                    }
                }

                throw new ArgumentOutOfRangeException("index");
            }
        }

        /// <summary>
        /// The absolute span of the list elements in characters, not including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan FullSpan
        {
            get
            {
                if (this.node == null)
                {
                    return default(TextSpan);
                }
                else
                {
                    return new TextSpan(this.Position, this.node.FullWidth);
                }
            }
        }

        /// <summary>
        /// The absolute span of the list elements in characters, including the leading and trailing trivia of the first and last elements.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                if (node == null)
                {
                    return default(TextSpan);
                }
                else
                {
                    return TextSpan.FromBounds(this.position + this.node.GetLeadingTriviaWidth(),
                                               this.position + this.node.FullWidth - this.node.GetTrailingTriviaWidth());
                }
            }
        }

        /// <summary>
        /// Returns the string representation of the tokens in this list, not including 
        /// the first token's leading trivia and the last token's trailing trivia.
        /// </summary>
        /// <returns>
        /// The string representation of the tokens in this list, not including 
        /// the first token's leading trivia and the last token's trailing trivia.
        /// </returns>
        public override string ToString()
        {
            return this.node != null ? this.node.ToString() : string.Empty;
        }

        /// <summary>
        /// Returns the full string representation of the tokens in this list including 
        /// the first token's leading trivia and the last token's trailing trivia.
        /// </summary>
        /// <returns>
        /// The full string representation of the tokens in this list including 
        /// the first token's leading trivia and the last token's trailing trivia.
        /// </returns>
        public string ToFullString()
        {
            return this.node != null ? this.node.ToFullString() : string.Empty;
        }

        /// <summary>
        /// Returns the first token in the list.
        /// </summary>
        /// <returns>The first token in the list.</returns>
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>        
        public SyntaxToken First()
        {
            if (Any())
            {
                return this[0];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns the last token in the list.
        /// </summary>
        /// <returns> The last token in the list.</returns>
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>        
        public SyntaxToken Last()
        {
            if (Any())
            {
                return this[this.Count - 1];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Tests whether the list is non-empty.
        /// </summary>
        /// <returns>True if the list contains any tokens.</returns>
        public bool Any()
        {
            return this.node != null;
        }

        /// <summary>
        /// Returns a list which contains all elements of <see cref="SyntaxTokenList"/> in reversed order.
        /// </summary>
        /// <returns><see cref="Reversed"/> which contains all elements of <see cref="SyntaxTokenList"/> in reversed order</returns>
        public Reversed Reverse()
        {
            return new Reversed(this);
        }

        internal void CopyTo(int offset, GreenNode[] array, int arrayOffset, int count)
        {
            Debug.Assert(this.Count >= offset + count);

            for (int i = 0; i < count; i++)
            {
                array[arrayOffset + i] = GetGreenNodeAt(offset + i);
            }
        }

        /// <summary>
        /// get the green node at the given slot
        /// </summary>
        private GreenNode GetGreenNodeAt(int i)
        {
            return GetGreenNodeAt(this.node, i);
        }

        /// <summary>
        /// get the green node at the given slot
        /// </summary>
        private static GreenNode GetGreenNodeAt(GreenNode node, int i)
        {
            Debug.Assert(node.IsList || (i == 0 && !node.IsList));
            return node.IsList ? node.GetSlot(i) : node;
        }

        public int IndexOf(SyntaxToken tokenInList)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                var token = this[i];
                if (token == tokenInList)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the specified token added to the end.
        /// </summary>
        /// <param name="token">The token to add.</param>
        public SyntaxTokenList Add(SyntaxToken token)
        {
            return Insert(this.Count, token);
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the specified tokens added to the end.
        /// </summary>
        /// <param name="tokens">The tokens to add.</param>
        public SyntaxTokenList AddRange(IEnumerable<SyntaxToken> tokens)
        {
            return InsertRange(this.Count, tokens);
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the specified token insert at the index.
        /// </summary>
        /// <param name="index">The index to insert the new token.</param>
        /// <param name="token">The token to insert.</param>
        public SyntaxTokenList Insert(int index, SyntaxToken token)
        {
            if (token == default(SyntaxToken))
            {
                throw new ArgumentException("token");
            }

            return InsertRange(index, new[] { token });
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the specified tokens insert at the index.
        /// </summary>
        /// <param name="index">The index to insert the new tokens.</param>
        /// <param name="tokens">The tokens to insert.</param>
        public SyntaxTokenList InsertRange(int index, IEnumerable<SyntaxToken> tokens)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (tokens == null)
            {
                throw new ArgumentNullException("tokens");
            }

            var items = tokens.ToList();
            if (items.Count == 0)
            {
                return this;
            }

            var list = this.ToList();
            list.InsertRange(index, tokens);

            if (list.Count == 0)
            {
                return this;
            }
            else
            {
                return new SyntaxTokenList(null, list[0].Node.CreateList(list.Select(n => n.Node)), 0, 0);
            }
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the token at the specified index removed.
        /// </summary>
        /// <param name="index">The index of the token to remove.</param>
        public SyntaxTokenList RemoveAt(int index)
        {
            if (index < 0 || index >= this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var list = this.ToList();
            list.RemoveAt(index);
            return new SyntaxTokenList(null, this.node.CreateList(list.Select(n => n.Node)), 0, 0);
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the specified token removed.
        /// </summary>
        /// <param name="tokenInList">The token to remove.</param>
        public SyntaxTokenList Remove(SyntaxToken tokenInList)
        {
            var index = this.IndexOf(tokenInList);
            if (index >= 0 && index <= this.Count)
            {
                return RemoveAt(index);
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the specified token replaced with a new token.
        /// </summary>
        /// <param name="tokenInList">The token to replace.</param>
        /// <param name="newToken">The new token.</param>
        public SyntaxTokenList Replace(SyntaxToken tokenInList, SyntaxToken newToken)
        {
            if (newToken == default(SyntaxToken))
            {
                throw new ArgumentException("newToken");
            }

            return ReplaceRange(tokenInList, new[] { newToken });
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTokenList"/> with the specified token replaced with new tokens.
        /// </summary>
        /// <param name="tokenInList">The token to replace.</param>
        /// <param name="newTokens">The new tokens.</param>
        public SyntaxTokenList ReplaceRange(SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
        {
            var index = this.IndexOf(tokenInList);
            if (index >= 0 && index <= this.Count)
            {
                var list = this.ToList();
                list.RemoveAt(index);
                list.InsertRange(index, newTokens);
                return new SyntaxTokenList(null, this.node.CreateList(list.Select(n => n.Node)), 0, 0);
            }
            else
            {
                throw new ArgumentException("tokenInList");
            }
        }

        // for debugging
        private SyntaxToken[] Nodes
        {
            get { return this.ToArray(); }
        }

        /// <summary>
        /// Returns an enumerator for the tokens in the <see cref="SyntaxTokenList"/>
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
            }

            return new EnumeratorImpl(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
            }

            return new EnumeratorImpl(ref this);
        }

        /// <summary>
        /// Compares <paramref name="left"/> and <paramref name="right"/> for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two <see cref="SyntaxTokenList"/>s are equal.</returns>
        public static bool operator ==(SyntaxTokenList left, SyntaxTokenList right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares <paramref name="left"/> and <paramref name="right"/> for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two <see cref="SyntaxTokenList"/>s are not equal.</returns>
        public static bool operator !=(SyntaxTokenList left, SyntaxTokenList right)
        {
            return !left.Equals(right);
        }

        public bool Equals(SyntaxTokenList other)
        {
            return this.node == other.node && this.parent == other.parent && this.index == other.index;
        }

        /// <summary>
        /// Compares this <see cref=" SyntaxTokenList"/> with the <paramref name="obj"/> for equality.
        /// </summary>
        /// <returns>True if the two objects are equal.</returns>
        public override bool Equals(object obj)
        {
            return (obj is SyntaxTokenList) && Equals((SyntaxTokenList)obj);
        }

        /// <summary>
        /// Serves as a hash function for the <see cref="SyntaxTokenList"/>
        /// </summary>
        public override int GetHashCode()
        {
            // Not call GHC on parent as it's expensive
            return Hash.Combine(this.node, this.index);
        }

        /// <summary>
        /// Create a new Token List
        /// </summary>
        /// <param name="token">Element of the return Token List</param>
        /// <returns></returns>
        public static SyntaxTokenList Create(SyntaxToken token)
        {
            return new SyntaxTokenList(token);
        }
    }
}