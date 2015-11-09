// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

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

                // partial always goes last.
                if (x.Kind() == SyntaxKind.PartialKeyword)
                {
                    return 1;
                }

                if (y.Kind() == SyntaxKind.PartialKeyword)
                {
                    return -1;
                }

                var xOrdering = GetOrdering(x);
                var yOrdering = GetOrdering(y);

                return xOrdering - yOrdering;
            }

            private Ordering GetOrdering(SyntaxToken token)
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
