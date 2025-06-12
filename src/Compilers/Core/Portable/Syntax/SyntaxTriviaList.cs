// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a read-only list of <see cref="SyntaxTrivia"/>.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    [CollectionBuilder(typeof(SyntaxTriviaList), methodName: "Create")]
    public readonly partial struct SyntaxTriviaList : IEquatable<SyntaxTriviaList>, IReadOnlyList<SyntaxTrivia>
    {
        public static SyntaxTriviaList Empty => default(SyntaxTriviaList);

        internal SyntaxTriviaList(in SyntaxToken token, GreenNode? node, int position, int index = 0)
        {
            Token = token;
            Node = node;
            Position = position;
            Index = index;
        }

        internal SyntaxTriviaList(in SyntaxToken token, GreenNode? node)
        {
            Token = token;
            Node = node;
            Position = token.Position;
            Index = 0;
        }

        public SyntaxTriviaList(SyntaxTrivia trivia)
        {
            Token = default(SyntaxToken);
            Node = trivia.UnderlyingNode;
            Position = 0;
            Index = 0;
        }

        /// <summary>
        /// Creates a list of trivia.
        /// </summary>
        /// <param name="trivias">An array of trivia.</param>
        public SyntaxTriviaList(params SyntaxTrivia[] trivias)
            : this(default, CreateNodeFromSpan(trivias), 0, 0)
        {
        }

        /// <summary>
        /// Creates a list of trivia.
        /// </summary>
        /// <param name="trivias">A sequence of trivia.</param>
        public SyntaxTriviaList(IEnumerable<SyntaxTrivia>? trivias)
            : this(default, SyntaxTriviaListBuilder.Create(trivias).Node, 0, 0)
        {
        }

        public static SyntaxTriviaList Create(ReadOnlySpan<SyntaxTrivia> trivias)
        {
            if (trivias.Length == 0)
                return default;

            return new SyntaxTriviaList(token: default, CreateNodeFromSpan(trivias), position: 0, index: 0);
        }

        private static GreenNode? CreateNodeFromSpan(ReadOnlySpan<SyntaxTrivia> trivias)
        {
            switch (trivias.Length)
            {
                // Also handles case where trivias is `null`.
                case 0: return null;
                case 1: return trivias[0].UnderlyingNode!;
                case 2: return Syntax.InternalSyntax.SyntaxList.List(trivias[0].UnderlyingNode!, trivias[1].UnderlyingNode!);
                case 3: return Syntax.InternalSyntax.SyntaxList.List(trivias[0].UnderlyingNode!, trivias[1].UnderlyingNode!, trivias[2].UnderlyingNode!);
                default:
                    {
                        var copy = new ArrayElement<GreenNode>[trivias.Length];
                        for (int i = 0, n = trivias.Length; i < n; i++)
                            copy[i].Value = trivias[i].UnderlyingNode!;

                        return Syntax.InternalSyntax.SyntaxList.List(copy);
                    }
            }
        }

        internal SyntaxToken Token { get; }

        internal GreenNode? Node { get; }

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
            return new Enumerator(in this);
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

        private static readonly ObjectPool<SyntaxTriviaListBuilder> s_builderPool =
            new ObjectPool<SyntaxTriviaListBuilder>(() => SyntaxTriviaListBuilder.Create());

        private static SyntaxTriviaListBuilder GetBuilder()
            => s_builderPool.Allocate();

        private static void ClearAndFreeBuilder(SyntaxTriviaListBuilder builder)
        {
            // It's possible someone might create a list with a huge amount of trivia
            // in it.  We don't want to hold onto such items forever.  So only cache
            // reasonably sized lists.  In IDE testing, around 99% of all trivia lists
            // were 16 or less elements.
            const int MaxBuilderCount = 16;
            if (builder.Count <= MaxBuilderCount)
            {
                builder.Clear();
                s_builderPool.Free(builder);
            }
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTriviaList"/> with the specified trivia inserted at the index.
        /// </summary>
        /// <param name="index">The index in the list to insert the trivia at.</param>
        /// <param name="trivia">The trivia to insert.</param>
        public SyntaxTriviaList InsertRange(int index, IEnumerable<SyntaxTrivia> trivia)
        {
            var thisCount = this.Count;
            if (index < 0 || index > thisCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (trivia == null)
            {
                throw new ArgumentNullException(nameof(trivia));
            }

            // Just return ourselves if we're not being asked to add anything.
            var triviaCollection = trivia as ICollection<SyntaxTrivia>;
            if (triviaCollection != null && triviaCollection.Count == 0)
            {
                return this;
            }

            var builder = GetBuilder();
            try
            {
                for (int i = 0; i < index; i++)
                {
                    builder.Add(this[i]);
                }

                builder.AddRange(trivia);

                for (int i = index; i < thisCount; i++)
                {
                    builder.Add(this[i]);
                }

                return builder.Count == thisCount ? this : builder.ToList();
            }
            finally
            {
                ClearAndFreeBuilder(builder);
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
            return new SyntaxTriviaList(default(SyntaxToken), GreenNode.CreateList(list, static n => n.RequiredUnderlyingNode), 0, 0);
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
                return new SyntaxTriviaList(default(SyntaxToken), GreenNode.CreateList(list, static n => n.RequiredUnderlyingNode), 0, 0);
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

            return new EnumeratorImpl(in this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (Node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
            }

            return new EnumeratorImpl(in this);
        }

        /// <summary>
        /// get the green node at the specific slot
        /// </summary>
        private GreenNode? GetGreenNodeAt(int i)
        {
            Debug.Assert(Node is object);
            return GetGreenNodeAt(Node, i);
        }

        private static GreenNode? GetGreenNodeAt(GreenNode node, int i)
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

        public override bool Equals(object? obj)
        {
            return (obj is SyntaxTriviaList list) && Equals(list);
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
