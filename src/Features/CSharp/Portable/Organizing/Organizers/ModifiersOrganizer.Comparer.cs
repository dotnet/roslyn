// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers;

internal partial class ModifiersOrganizer
{
    private class Comparer : IComparer<SyntaxToken>
    {
        // TODO(cyrusn): Allow users to specify the ordering they want
        private enum Ordering
        {
            Accessibility,
            StaticInstance,
            Remainder
        }

        public int Compare(SyntaxToken x, SyntaxToken y)
        {
            if (x.Kind() == y.Kind())
            {
                return 0;
            }

            return ComparerWithState.CompareTo(x, y, s_comparers);
        }

        private static readonly ImmutableArray<Func<SyntaxToken, IComparable>> s_comparers =
            [t => t.Kind() == SyntaxKind.PartialKeyword, t => GetOrdering(t)];

        private static Ordering GetOrdering(SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.StaticKeyword:
                    return Ordering.StaticInstance;
                case SyntaxKind.PrivateKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.PublicKeyword:
                    return Ordering.Accessibility;
                default:
                    return Ordering.Remainder;
            }
        }
    }
}
