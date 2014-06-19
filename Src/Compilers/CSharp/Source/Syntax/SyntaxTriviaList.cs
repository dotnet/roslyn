using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
#if REMOVE
    /// <summary>
    /// A List of SyntaxTrivia.
    /// </summary>
    public partial struct SyntaxTriviaList : IEquatable<SyntaxTriviaList>, IReadOnlyCollection<SyntaxTrivia>
    {
        private readonly SyntaxToken token;
        private readonly Syntax.InternalSyntax.CSharpSyntaxNode node;
        private readonly int position;
        private readonly int index;

        internal SyntaxTriviaList(SyntaxTrivia trivia)
        {
            this.token = trivia.Token;
            this.node = trivia.UnderlyingNode;
            this.position = trivia.Position;
            this.index = trivia.Index;
        }

        internal SyntaxTriviaList(SyntaxToken token, Syntax.InternalSyntax.CSharpSyntaxNode node)
        {
            this.token = token;
            this.node = node;
            this.position = token.Position;
            this.index = 0;
        }

        internal SyntaxTriviaList(SyntaxToken token, Syntax.InternalSyntax.CSharpSyntaxNode node, int position, int index)
        {
            this.token = token;
            this.node = node;
            this.position = position;
            this.index = index;
        }

        internal SyntaxToken Token
        {
            get { return this.token; }
        }

        internal Syntax.InternalSyntax.CSharpSyntaxNode Node
        {
            get { return this.node; }
        }

        /// <summary>
        /// Count of items in the list.
        /// </summary>
        public int Count
        {
            get { return node == null ? 0 : (node.IsList ? node.SlotCount : 1); }
        }

        public SyntaxTrivia ElementAt(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Returns the SyntaxTrivia at this index.
        /// 
        /// don't use this in a loop. use enumerator instead
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal SyntaxTrivia this[int index]
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

                    return new SyntaxTrivia(this.token, node.GetSlot(index), node.GetSlotOffset(index) + this.position, this.index + index);
                }
                else if (index == 0)
                {
                    return new SyntaxTrivia(this.token, node, this.position, this.index);
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Returns the index of the given SyntaxTrivia.
        /// </summary>
        /// <param name="trivia"></param>
        /// <returns></returns>
        public int IndexOf(SyntaxTrivia trivia)
        {
            var index = 0;
            foreach (var child in this)
            {
                if (object.Equals(child, trivia))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Returns the string representation of the trivia in this list, not including 
        /// the first trivia's leading sub-trivia or the last trivia's trailing sub-trivia
        /// if they are structured.
        /// </summary>
        /// <returns>
        /// The string representation of the trivia in this list, not including 
        /// the first trivia's leading sub-trivia or the last trivia's trailing sub-trivia
        /// if they are structured.
        /// </returns>
        public override string ToString()
        {
            return this.node != null ? this.node.ToString() : string.Empty;
        }

        /// <summary>
        /// Returns the full string representation of the trivia in this list including 
        /// the first trivia's leading sub-trivia and the last trivia's trailing sub-trivia
        /// even if they are structured.
        /// </summary>
        /// <returns>
        /// The full string representation of the trivia in this list including 
        /// the first trivia's leading sub-trivia and the last trivia's trailing sub-trivia
        /// even if they are structured.
        /// </returns>
        public string ToFullString()
        {
            return this.node != null ? this.node.ToFullString() : string.Empty;
        }
        /// <summary>
        /// Returns the first SyntaxTrivia in the list.
        /// </summary>
        /// <returns></returns>
        public SyntaxTrivia First()
        {
            return this[0];
        }

        /// <summary>
        /// Returns the first SyntaxTrivia in the list or the default value of SyntaxTrivia if the list has no items.
        /// </summary>
        /// <returns></returns>
        public SyntaxTrivia FirstOrDefault()
        {
            if (this.Count > 0)
            {
                return this[0];
            }
            else
            {
                return default(SyntaxTrivia);
            }
        }

        /// <summary>
        /// Returns the last SyntaxTrivia in the list. May throw ArgumentOutOfRange exception.
        /// </summary>
        /// <returns></returns>
        public SyntaxTrivia Last()
        {
            return this[this.Count - 1];
        }

        /// <summary>
        /// Returns the last SyntaxTrivia in the list or the default value of SyntaxTrivia if the list has no items.
        /// </summary>
        /// <returns></returns>
        public SyntaxTrivia LastOrDefault()
        {
            if (this.Count > 0)
            {
                return this[this.Count - 1];
            }
            else
            {
                return default(SyntaxTrivia);
            }
        }

        /// <summary>
        /// Does any SyntaxTrivia in this list has diagnostics.
        /// </summary>
        public bool ContainsDiagnostics
        {
            get { return this.node != null && this.node.ContainsDiagnostics; }
        }

        /// <summary>
        /// Does this list have any items.
        /// </summary>
        /// <returns></returns>
        public bool Any()
        {
            return this.Count > 0;
        }

        /// <summary>
        /// Does this list contain any item of the given kind.
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public bool Any(SyntaxKind kind)
        {
            foreach (var element in this)
            {
                if (element.CSharpKind() == kind)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// return reversed enumerable
        /// </summary>
        public Reversed Reverse()
        {
            return new Reversed(this);
        }

        /// <summary>
        /// Get an enumerator for this list.
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        // for debugging
        private SyntaxTrivia[] Nodes
        {
            get { return this.ToArray(); }
        }

        IEnumerator<SyntaxTrivia> IEnumerable<SyntaxTrivia>.GetEnumerator()
        {
            if (node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
            }

            return new EnumeratorImpl(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (node == null)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
            }

            return new EnumeratorImpl(ref this);
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
                current = new SyntaxTrivia(this.token, GetGreenNodeAt(offset + i), position, this.index + i);

                array[arrayOffset + i] = current;
            }
        }

        /// <summary>
        /// get the green node at the specific slot
        /// </summary>
        private Syntax.InternalSyntax.CSharpSyntaxNode GetGreenNodeAt(int i)
        {
            return GetGreenNodeAt(this.node, i);
        }

        /// <summary>
        /// get the green node at the specific slot
        /// </summary>
        private static Syntax.InternalSyntax.CSharpSyntaxNode GetGreenNodeAt(Syntax.InternalSyntax.CSharpSyntaxNode node, int i)
        {
            Debug.Assert(node.IsList || (i == 0 && !node.IsList));
            return node.IsList ? node.GetSlot(i) : node;
        }

        /// <summary>
        /// Compares equality between this list and other list.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SyntaxTriviaList other)
        {
            return this.node == other.node && this.index == other.index && this.token.Equals(other.token);
        }

        /// <summary>
        /// Are two SyntaxTriviaLists, left and right equal.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(SyntaxTriviaList left, SyntaxTriviaList right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Are two SyntaxTriviaLists, left and right not equal.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(SyntaxTriviaList left, SyntaxTriviaList right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Implicitly converts SyntaxTrivia to SyntaxTriviaList.
        /// </summary>
        /// <param name="trivia"></param>
        /// <returns></returns>
        public static implicit operator SyntaxTriviaList(SyntaxTrivia trivia)
        {
            return new SyntaxTriviaList(trivia.Token, trivia.UnderlyingNode, trivia.Position, trivia.Index);
        }

        /// <summary>
        /// Implicitly converts SyntaxTriviaList to SyntaxTriviaList
        /// </summary>
        /// <param name="triviaList"></param>
        /// <returns></returns>
        public static implicit operator Common.SyntaxTriviaList(SyntaxTriviaList triviaList)
        {
            return new Common.SyntaxTriviaList(triviaList.token, triviaList.node, triviaList.position, triviaList.index);
        }

        /// <summary>
        /// Explicitly converts SyntaxTriviaList to SyntaxTriviaList
        /// </summary>
        /// <param name="commonTriviaList"></param>
        /// <returns></returns>
        public static explicit operator SyntaxTriviaList(Common.SyntaxTriviaList commonTriviaList)
        {
            return new SyntaxTriviaList((SyntaxToken)commonTriviaList.Token, (Syntax.InternalSyntax.CSharpSyntaxNode)commonTriviaList.Node, commonTriviaList.Position, commonTriviaList.Index);
        }

        /// <summary>
        /// Is this list equal to the passed in object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (obj is SyntaxTriviaList) && Equals((SyntaxTriviaList)obj);
        }

        public override int GetHashCode()
        {
            return this.token.GetHashCode() + (this.node != null ? this.node.GetHashCode() : 0);
        }

        //EDMAURER using this method will likely produce different results than using Enumerable.Concat()
        //Using Enumerable.Concat() will produce a sequence containing items whose parent, span, index, and node are
        //relative to the list from which they were pulled. A concatenation of two lists from different parents may 
        //or may not make sense depending on the usage. A concatenation using this method will normalize all of the
        //spans, etc. of the elements of the resulting sequence to be relative to the parameters used in the call
        //to the SyntaxTriviaList constructor. Having these two Concat mechanisms that produce different results
        //is too confusing. We could hijack Enumerable.Concat(this SyntaxTriviaList), but that seems like a dark path.
        //public SyntaxTriviaList Concat(SyntaxTriviaList tail)
        //{
        //    return new SyntaxTriviaList(default(SyntaxToken), Syntax.InternalSyntax.SyntaxList.Concat(this.Node, tail.Node), position: 0, index: 0);
        //}

        /// <summary>
        /// Default instance of SyntaxTriviaList.
        /// </summary>
        public static readonly SyntaxTriviaList Empty = default(SyntaxTriviaList);
    }
#endif
}