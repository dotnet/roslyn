// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFacts
    {
        private sealed class SyntaxKindEqualityComparer : IEqualityComparer<SyntaxKind>
        {
            public bool Equals(SyntaxKind x, SyntaxKind y)
            {
                return x == y;
            }

            public int GetHashCode(SyntaxKind obj)
            {
                return (int)obj;
            }
        }

        /// <summary>
        /// A custom equality comparer for <see cref="SyntaxKind"/>
        /// </summary>
        /// <remarks>
        /// PERF: The framework specializes EqualityComparer for enums, but only if the underlying type is System.Int32
        /// Since SyntaxKind's underlying type is System.UInt16, ObjectEqualityComparer will be chosen instead.
        /// </remarks>
        public static IEqualityComparer<SyntaxKind> EqualityComparer { get; } = new SyntaxKindEqualityComparer();
    }
}
