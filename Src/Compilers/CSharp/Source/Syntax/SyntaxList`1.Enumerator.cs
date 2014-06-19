using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial struct SyntaxList<TNode>
    {
        public struct Enumerator
        {
            private SyntaxList<TNode> list;
            private int index;

            internal Enumerator(SyntaxList<TNode> list)
            {
                this.list = list;
                this.index = -1;
            }

            public bool MoveNext()
            {
                int newIndex = this.index + 1;
                if (newIndex < this.list.Count)
                {
                    this.index = newIndex;
                    return true;
                }

                return false;
            }

            public TNode Current
            {
                get
                {
                    return this.list[this.index];
                }
            }

            public void Reset()
            {
                this.index = -1;
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException();
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }
        }

        private class EnumeratorImpl : IEnumerator<TNode>
        {
            Enumerator e;

            internal EnumeratorImpl(SyntaxList<TNode> list)
            {
                this.e = new Enumerator(list);
            }

            public bool MoveNext()
            {
                return e.MoveNext();
            }

            public TNode Current
            {
                get
                {
                    return e.Current;
                }
            }

            void IDisposable.Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return e.Current;
                }
            }

            void IEnumerator.Reset()
            {
                e.Reset();
            }
        }
    }
}