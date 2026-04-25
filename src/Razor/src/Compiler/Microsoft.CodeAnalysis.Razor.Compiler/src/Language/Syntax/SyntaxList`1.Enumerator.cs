// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly partial struct SyntaxList<TNode>
    where TNode : SyntaxNode
{
    public struct Enumerator
    {
        private readonly SyntaxList<TNode> _list;
        private int _index;

        internal Enumerator(in SyntaxList<TNode> list)
        {
            _list = list;
            _index = -1;
        }

        public bool MoveNext()
        {
            var newIndex = _index + 1;
            if (newIndex < _list.Count)
            {
                _index = newIndex;
                return true;
            }

            return false;
        }

        public readonly TNode Current
            => (TNode)_list.ItemInternal(_index)!;

        public void Reset()
        {
            _index = -1;
        }

        public override readonly bool Equals(object? obj)
            => throw new NotSupportedException();

        public override readonly int GetHashCode()
            => throw new NotSupportedException();
    }

    private sealed class EnumeratorImpl : IEnumerator<TNode>
    {
        private Enumerator _enumerator;

        internal EnumeratorImpl(in SyntaxList<TNode> list)
        {
            _enumerator = new Enumerator(in list);
        }

        public bool MoveNext()
            => _enumerator.MoveNext();

        public TNode Current
            => _enumerator.Current;

        void IDisposable.Dispose()
        {
        }

        object IEnumerator.Current
            => _enumerator.Current;

        void IEnumerator.Reset()
            => _enumerator.Reset();
    }
}
