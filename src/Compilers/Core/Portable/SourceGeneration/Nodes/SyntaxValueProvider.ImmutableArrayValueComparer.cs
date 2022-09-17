// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial struct SyntaxValueProvider
{
    private class ImmutableArrayValueComparer<T> : IEqualityComparer<ImmutableArray<T>>
    {
        public static readonly IEqualityComparer<ImmutableArray<T>> Instance = new ImmutableArrayValueComparer<T>();

        public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
        {
            if (x == y)
                return true;

            return x.SequenceEqual(y, 0, static (a, b, _) => EqualityComparer<T>.Default.Equals(a, b));
        }

        public int GetHashCode(ImmutableArray<T> obj)
        {
            var hashCode = 0;
            foreach (var value in obj)
                hashCode = Hash.Combine(hashCode, EqualityComparer<T>.Default.GetHashCode(value!));

            return hashCode;
        }
    }
}
