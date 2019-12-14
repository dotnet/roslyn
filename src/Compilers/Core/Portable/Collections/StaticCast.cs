// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal static class StaticCast<T>
    {
        internal static ImmutableArray<T> From<TDerived>(ImmutableArray<TDerived> from) where TDerived : class, T
        {
            return ImmutableArray<T>.CastUp(from);
        }
    }
}
