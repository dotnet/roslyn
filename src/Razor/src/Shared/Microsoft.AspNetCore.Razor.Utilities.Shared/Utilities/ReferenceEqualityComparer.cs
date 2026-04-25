// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static readonly ReferenceEqualityComparer<T> Instance = new();

    private ReferenceEqualityComparer()
    {
    }

    bool IEqualityComparer<T>.Equals(T? x, T? y)
        => ReferenceEquals(x, y);

    int IEqualityComparer<T>.GetHashCode(T obj)
        => RuntimeHelpers.GetHashCode(obj);
}
