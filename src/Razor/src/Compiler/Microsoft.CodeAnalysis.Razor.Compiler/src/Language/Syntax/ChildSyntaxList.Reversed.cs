// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly partial struct ChildSyntaxList
{
    public readonly partial struct Reversed : IEnumerable<SyntaxNodeOrToken>, IEquatable<Reversed>
    {
        private readonly SyntaxNode _node;
        private readonly int _count;

        internal Reversed(SyntaxNode node, int count)
        {
            _node = node;
            _count = count;
        }

        public Enumerator GetEnumerator()
            => new(_node, _count);

        IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
            => _node == null
                ? SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>()
                : new EnumeratorImpl(_node, _count);

        IEnumerator IEnumerable.GetEnumerator()
            => _node == null
                ? SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>()
                : (IEnumerator)new EnumeratorImpl(_node, _count);

        public override int GetHashCode()
        {
            if (_node == null)
            {
                return 0;
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(_node.GetHashCode());
            hash.Add(_count);

            return hash.CombinedHash;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Reversed reversed && Equals(reversed);

        public bool Equals(Reversed other)
        {
            return _node == other._node
                && _count == other._count;
        }

        public struct Enumerator
        {
            private readonly SyntaxNode _node;
            private readonly int _count;
            private int _childIndex;

            internal Enumerator(SyntaxNode node, int count)
            {
                _node = node;
                _count = count;
                _childIndex = count;
            }

            public bool MoveNext()
            {
                return --_childIndex >= 0;
            }

            public readonly SyntaxNodeOrToken Current
                => ItemInternal(_node, _childIndex);

            public void Reset()
            {
                _childIndex = _count;
            }
        }
    }
}
