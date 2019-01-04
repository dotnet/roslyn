// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal sealed class NullableDirectiveMap
    {
        private static readonly NullableDirectiveMap Empty = new NullableDirectiveMap(ImmutableArray<(int Position, bool? State)>.Empty);

        private readonly ImmutableArray<(int Position, bool? State)> _directives;

        internal static NullableDirectiveMap Create(SyntaxTree tree)
        {
            var directives = GetDirectives(tree);
            return directives.IsEmpty ? Empty : new NullableDirectiveMap(directives);
        }

        private NullableDirectiveMap(ImmutableArray<(int Position, bool? State)> directives)
        {
#if DEBUG
            for (int i = 1; i < directives.Length; i++)
            {
                Debug.Assert(directives[i - 1].Position < directives[i].Position);
            }
#endif
            _directives = directives;
        }

        /// <summary>
        /// Returns true if the `#nullable` directive preceding the position is
        /// `enable` or `safeonly`, false if `disable`, and null if no preceding directive,
        /// or directive preceding the position is `restore`.
        /// </summary>
        internal bool? GetDirectiveState(int position)
        {
            int index = _directives.BinarySearch((position, false), PositionComparer.Instance);
            if (index < 0)
            {
                // If no exact match, BinarySearch returns the complement
                // of the index of the next higher value.
                index = ~index - 1;
            }
            if (index < 0)
            {
                return null;
            }
            Debug.Assert(_directives[index].Position <= position);
            Debug.Assert(index == _directives.Length - 1 || position < _directives[index + 1].Position);
            return _directives[index].State;
        }

        private static ImmutableArray<(int Position, bool? State)> GetDirectives(SyntaxTree tree)
        {
            var builder = ArrayBuilder<(int Position, bool? State)>.GetInstance();
            foreach (var d in tree.GetRoot().GetDirectives())
            {
                if (d.Kind() != SyntaxKind.NullableDirectiveTrivia)
                {
                    continue;
                }
                var nn = (NullableDirectiveTriviaSyntax)d;
                if (nn.SettingToken.IsMissing || !nn.IsActive)
                {
                    continue;
                }

                bool? state;
                switch (nn.SettingToken.Kind())
                {
                    case SyntaxKind.EnableKeyword:
                    case SyntaxKind.SafeOnlyKeyword:
                        state = true;
                        break;
                    case SyntaxKind.RestoreKeyword:
                        state = null;
                        break;
                    case SyntaxKind.DisableKeyword:
                        state = false;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(nn.SettingToken.Kind());
                }

                builder.Add((nn.Location.SourceSpan.End, state));
            }
            return builder.ToImmutableAndFree();
        }

        private sealed class PositionComparer : IComparer<(int Position, bool? State)>
        {
            internal static readonly PositionComparer Instance = new PositionComparer();

            public int Compare((int Position, bool? State) x, (int Position, bool? State) y)
            {
                return x.Position.CompareTo(y.Position);
            }
        }
    }
}
