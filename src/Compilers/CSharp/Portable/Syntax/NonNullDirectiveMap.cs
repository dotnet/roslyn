// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal sealed class NonNullDirectiveMap
    {
        private static readonly NonNullDirectiveMap Empty = new NonNullDirectiveMap(ImmutableArray<(int, bool)>.Empty);

        private readonly ImmutableArray<(int, bool)> _directives;

        internal static NonNullDirectiveMap Create(SyntaxTree tree)
        {
            var directives = GetDirectives(tree);
            return directives.IsEmpty ? Empty : new NonNullDirectiveMap(directives);
        }

        private NonNullDirectiveMap(ImmutableArray<(int, bool)> directives)
        {
#if DEBUG
            for (int i = 1; i < directives.Length; i++)
            {
                Debug.Assert(directives[i - 1].Item1 < directives[i].Item1);
            }
#endif
            _directives = directives;
        }

        /// <summary>
        /// Returns true if the `#nonnull` directive preceding the position is
        /// `restore`, false for if `disable`, and null if no preceding directive.
        /// </summary>
        internal bool? GetDirectiveState(int position)
        {
            int index = _directives.BinarySearch((position, false), PositionComparer.Instance);
            if (index < 0)
            {
                index = ~index - 1;
            }
            if (index < 0)
            {
                return null;
            }
            Debug.Assert(_directives[index].Item1 <= position);
            Debug.Assert(index == _directives.Length - 1 || position < _directives[index + 1].Item1);
            return _directives[index].Item2;
        }

        private static ImmutableArray<(int, bool)> GetDirectives(SyntaxTree tree)
        {
            var builder = ArrayBuilder<(int, bool)>.GetInstance();
            foreach (var d in tree.GetRoot().GetDirectives())
            {
                if (d.Kind() != SyntaxKind.NonNullDirectiveTrivia)
                {
                    continue;
                }
                var nn = (NonNullDirectiveTriviaSyntax)d;
                if (nn.DisableOrRestoreKeyword.IsMissing || !nn.IsActive)
                {
                    continue;
                }
                builder.Add((nn.Location.SourceSpan.End, nn.DisableOrRestoreKeyword.Kind() == SyntaxKind.RestoreKeyword));
            }
            return builder.ToImmutableAndFree();
        }

        private sealed class PositionComparer : IComparer<(int, bool)>
        {
            internal static readonly PositionComparer Instance = new PositionComparer();

            public int Compare((int, bool) x, (int, bool) y)
            {
                return x.Item1.CompareTo(y.Item1);
            }
        }
    }
}
