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
    ///  Represents a read-only list of <see cref="SyntaxToken"/>.
    /// </summary>
    public partial struct SyntaxTokenList : IEquatable<SyntaxTokenList>, IReadOnlyCollection<SyntaxToken>
    {
        private readonly CSharpSyntaxNode parent;
        private readonly Syntax.InternalSyntax.CSharpSyntaxNode node;
        private readonly int position;
        private readonly int index;

        internal SyntaxTokenList(CSharpSyntaxNode parent, Syntax.InternalSyntax.CSharpSyntaxNode node, int position, int index)
        {
            Debug.Assert(node != null || (position == 0 && index == 0 && parent == null));
            Debug.Assert(position >= 0);

            this.parent = parent;
            this.node = node;
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

        internal Syntax.InternalSyntax.CSharpSyntaxNode Node
        {
            get
            {
                return this.node;
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
        /// 
        /// This operation is not guaranteed to execute in constant time. When iterating over
        /// the contents of a <see cref="SyntaxTokenList"/> prefer foreach.
        /// </summary>
        /// <param name="index">The zero-based index of the token to get or set.</param>
        /// <returns>The token at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException" >"><paramref name="index"/>is out of bounds.</exception>
        public SyntaxToken ElementAt(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Gets the token at the specified index.
        /// 
        /// don't use this in a loop. use enumerator instead
        /// </summary>
        /// <param name="index">The zero-based index of the token to get or set.</param>
        /// <returns>The token at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException" >"><paramref name="index"/>is out of bounds.</exception>
        internal SyntaxToken this[int index]
        {
            get
            {
                if (node == null)
                {
                    throw new IndexOutOfRangeException();
                }
                else if (node.IsList)
                {
                    if (index < 0 || index > node.SlotCount)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return new SyntaxToken(this.parent, (Syntax.InternalSyntax.SyntaxToken)node.GetSlot(index), this.position + node.GetSlotOffset(index), this.index + index);
                }
                else if (index == 0)
                {
                    return new SyntaxToken(this.parent, (Syntax.InternalSyntax.SyntaxToken)this.node, this.position, this.index);
                }
                else
                {
                    throw new IndexOutOfRangeException();
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
        /// Add one or more tokens to the end of the list.
        /// </summary>
        /// <returns>A new list with the tokens added.</returns>
        public SyntaxTokenList Add(params SyntaxToken[] items)
        {
            return this.Insert(this.Count, items);
        }

        /// <summary>
        /// Insert one or more tokens in the list at the specified index.
        /// </summary>
        /// <returns>A new list with the tokens inserted.</returns>
        public SyntaxTokenList Insert(int index, params SyntaxToken[] items)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (this.Count == 0)
            {
                return SyntaxFactory.TokenList(items);
            }
            else
            {
                var builder = new SyntaxTokenListBuilder(this.Count + items.Length);
                if (index > 0)
                {
                    builder.Add(this, 0, index);
                }

                builder.Add(items);

                if (index < this.Count)
                {
                    builder.Add(this, index, this.Count - index);
                }

                return builder.ToList();
            }
        }

        /// <summary>
        /// Returns the first token in the list.
        /// </summary>
        /// <returns>The first token in the list.</returns>
        public SyntaxToken First()
        {
            return this[0];
        }

        /// <summary>
        /// Returns the first token in the list or a default token if the list is empty.
        /// </summary>
        /// <returns>default(<see cref="SyntaxToken"/>) if source is empty; otherwise, the first token in the
        /// list.</returns>
        public SyntaxToken FirstOrDefault()
        {
            if (this.Count > 0)
            {
                return this[0];
            }
            else
            {
                return default(SyntaxToken);
            }
        }

        internal SyntaxToken FirstOrDefault(SyntaxKind kind)
        {
            if (this.Count > 0)
            {
                foreach (var element in this)
                {
                    if (element.CSharpKind() == kind)
                    {
                        return element;
                    }
                }
            }
            return default(SyntaxToken);
        }

        /// <summary>
        /// Returns the last token in the list.
        /// </summary>
        /// <returns> The last token in the list.</returns>
        public SyntaxToken Last()
        {
            return this[this.Count - 1];
        }

        /// <summary>
        /// Returns the last token in the list of a default token if the list is empty.
        /// </summary>
        /// <returns>default(<see cref="SyntaxToken"/>) if list is empty; otherwise, the last token in the
        /// list.</returns>
        public SyntaxToken LastOrDefault()
        {
            if (this.Count > 0)
            {
                return this[this.Count - 1];
            }
            else
            {
                return default(SyntaxToken);
            }
        }

        /// <summary>
        /// Tests whether the list is non-empty.
        /// </summary>
        /// <returns>True if the list contains any tokens.</returns>
        public bool Any()
        {
            return this.Count > 0;
        }

        /// <summary>
        /// Tests whether a list contains tokens of a particular kind.
        /// </summary>
        /// <param name="kind">The <see cref="SyntaxKind"/> to test for.</param>
        /// <returns>Returns true if the list contains a token which matches <paramref name="kind"/></returns>
        public bool Any(SyntaxKind kind)
        {
            if (this.Count > 0)
            {
                foreach (var element in this)
                {
                    if (element.CSharpKind() == kind)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal void CopyTo(int offset, Syntax.InternalSyntax.CSharpSyntaxNode[] array, int arrayOffset, int count)
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
        private Syntax.InternalSyntax.SyntaxToken GetGreenNodeAt(int i)
        {
            return GetGreenNodeAt(this.node, i);
        }

        /// <summary>
        /// get the green node at the given slot
        /// </summary>
        private static Syntax.InternalSyntax.SyntaxToken GetGreenNodeAt(Syntax.InternalSyntax.CSharpSyntaxNode node, int i)
        {
            Debug.Assert(node.IsList || (i == 0 && !node.IsList));
            return (Syntax.InternalSyntax.SyntaxToken)(node.IsList ? node.GetSlot(i) : node);
        }

        // for debugging
        private SyntaxToken[] Nodes
        {
            get { return this.ToArray(); }
        }

        /// <summary>
        /// return reversed enumerable
        /// </summary>
        public Reversed Reverse()
        {
            return new Reversed(this);
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
        // <returns>True if the two <see cref="SytaxTokenList"/>s are not equal.</returns>
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
            return this.node != null ? this.node.GetHashCode() : 0;
        }

        /// <summary>
        /// Converts a <see cref="SyntaxToken"/> to a <see cref="SyntaxTokenList"/>
        /// </summary>
        public static implicit operator SyntaxTokenList(SyntaxToken token)
        {
            return new SyntaxTokenList(token);
        }
    }
#endif
}