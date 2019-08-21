// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
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

                return IComparableHelper.CompareTo(x, y, GetComparisonComponents);
            }

            private static IEnumerable<IComparable> GetComparisonComponents(SyntaxToken t)
            {
                // partial always goes last.
                yield return t.Kind() == SyntaxKind.PartialKeyword;

                yield return GetOrdering(t);
            }

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
}
