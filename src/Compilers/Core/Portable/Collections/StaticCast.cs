// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class StaticCast<T>
    {
        internal static ImmutableArray<T> From<TDerived>(ImmutableArray<TDerived> from) where TDerived : class, T
        {
            return ImmutableArray<T>.CastUp(from);
        }

        internal static OneOrMany<T> From<TDerived>(OneOrMany<TDerived> from) where TDerived : class, T
        {
            return OneOrMany<T>.CastUp(from);
        }
    }
}
