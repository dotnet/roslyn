// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Roslyn.Utilities;

internal sealed class ImmutableArrayComparer<T> : IEqualityComparer<ImmutableArray<T>>
{
    public static readonly ImmutableArrayComparer<T> Instance = new();

    private ImmutableArrayComparer() { }

    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
        => x.SequenceEqual(y);

    public int GetHashCode(ImmutableArray<T> obj)
        => Hash.CombineValues(obj);
}
